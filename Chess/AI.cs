using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Chess
{
    /*
    public class EvalScore
    {
        public static readonly EvalScore DRAW = new EvalScore(0);
        public int Score { get; private set; }
        public bool IsForcedMate { get; private set; } = false;
        public int ForcedMateIn { get; private set; } = -1;

        public EvalScore (int score, int forcedMateIn = -1)
        {
            Score = score;
            if(forcedMateIn >= 0)
            {
                IsForcedMate = true;
                ForcedMateIn = forcedMateIn;
            }
        }

        public EvalScore Negate()
        {
            return new EvalScore(-Score, ForcedMateIn);
        }

        public EvalScore Increment()
        {
            return IsForcedMate ? new EvalScore(Score, ForcedMateIn + 1) : this;
        }

        public int Compare(EvalScore e2)
        {
            if(Score == e2.Score)
            {
                if (e2.IsForcedMate)
                {
                    if (IsForcedMate)
                    {
                        return ForcedMateIn == e2.ForcedMateIn ? 0 : (ForcedMateIn > e2.ForcedMateIn ? 1 : -1);
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return Score > e2.Score ? 1 : -1;
            }
        }

        public override string ToString()
        {
            if (IsForcedMate)
            {
                var sign = Score > 0 ? "" : "-";
                return $"{sign}M{(ForcedMateIn+1) / 2}";
            }
            else
            {
                return Math.Round(Score / 100m, 2).ToString(CultureInfo.InvariantCulture);
            }
        }

        public string ToUciString()
        {
            if (IsForcedMate)
            {
                var sign = Score > 0 ? "" : "-";
                return $"mate {sign}{(ForcedMateIn + 1) / 2}";
            }
            else
            {
                return $"cp {Score}";
            }
        }
    }
    */

    public class AI
    {
        public const int INFINITY = 1000000;
        public const int CHECKMATE = 100000;
        public const int CHECKMATE_TRESHOLD = CHECKMATE - 1000;
        public const int DRAW = 0;

        public const int R = 2;

        private Board board;
        private MoveGenerator generator;

        public int Nodes = 0;
        private IDictionary<uint, int> transpositions = new Dictionary<uint, int>();

        Move[] lastpv;
        int searchDepth = 0;
        private Move[][] KillerMoves;

        public AI(Board board)
        {
            this.board = board;
            generator = new MoveGenerator(board);
            KillerMoves = new Move[1024][];
            for (int i = 0; i < 1024; i++)
            {
                KillerMoves[i] = new Move[3];
            }
        }

        private int GetKillerMoveValue(Move move)
        {
            for(int i = 0; i < 3; i++)
            {
                var killer = KillerMoves[board.Ply][i];
                if (killer == null)
                    return 0;
                if (killer.Start == move.Start && killer.Target == move.Target)
                    return (3-i) * 1000000;
            }
            return 0;
        }

        private int MoveImportance(Move move, int depth)
        {
            var pvMove = lastpv == null ? null : searchDepth - depth >= lastpv.Length ? null : depth < 0 ? null : lastpv[searchDepth - depth];
            var pvValue = pvMove == null ? 0 : (pvMove.Start == move.Start && pvMove.Target == move.Target) ? 10000000 : 0;

            var killerValue = GetKillerMoveValue(move);

            var captureValue = -Evaluation.PieceValues_ColorIndependent[move.MovedPiece] * 100 + 1000 * Evaluation.PieceValues_ColorIndependent[move.CapturedPiece];
            var promotionValue = Evaluation.PieceValues_ColorIndependent[move.Promotion];

            var pawnCapturePenalty = (generator.opponentPawnAttackMap & 1ul << move.Target) != 0 ? -10 : 0;

            return killerValue + pvValue + captureValue + promotionValue + pawnCapturePenalty;
        }

        public (int, Move[]) FindBestMove(int depth, Move[] lastpv, int startAlpha, int startBeta)
        {
            Nodes = 0;
            transpositions.Clear();
            var pv = new Move[depth];

            this.lastpv = lastpv;
            this.searchDepth = depth;

            var score = DeepEval(depth, startAlpha, startBeta, false, pv);
            return (score, pv);
        }

        private int DeepEval(int depth, int alpha, int beta, bool nullMoveAllowed, Move[] pv)
        {
            Nodes++;

            //if (transpositions.ContainsKey(board.positionKey))
            //{
            //    return (transpositions[board.positionKey], null);
            //}

            generator.Setup();
            var moves = generator.GetMoves(false);

            if (!moves.Any())
            {
                //When in Check: Checkmate:
                if (generator.InCheck)
                {
                    //Compute checkmate score:
                    var currentDepth = board.states.Count; //TODO add ply variable to board
                    return -CHECKMATE + currentDepth;
                }
                else //Stalemate
                    return DRAW;
            }
            else if(board.states.Any(s => s.positionKey == board.positionKey))
            {
                //Repetition
                return DRAW;
            }

            //TODO Add 50 moves rule

            if (depth == 0)
                return QuiesceneSearch(alpha, beta);

            if(depth == 1)
            {
                //Futility pruning
                var currentEval = Evaluation.Evaluate(board);
                if(currentEval + Evaluation.BishopValue < alpha)
                {
                    //Prune this node, i.e. go directly to QuiesceneSearch
                    return QuiesceneSearch(alpha, beta);
                }
            }

            if(nullMoveAllowed && !generator.InCheck && depth >= 1 + R)
            {
                //Null move pruning
                var npv = new Move[depth]; //irellevant here
                board.SubmitNullMove();
                int eval = -DeepEval(depth - 1 - R, -beta, -beta+1, false, npv);
                board.UndoNullMove();
                if (eval >= beta)
                {
                    return eval;
                }
            }

            int movenumber = 1;
            foreach (var move in moves.OrderByDescending(m => MoveImportance(m, depth)))
            {
                if (depth == searchDepth) //Upper level
                    Console.WriteLine($"info currmove {move.ToAlgebraicNotation()} currmovenumber {movenumber++}");

                var npv = new Move[depth-1];
                board.SubmitMove(move);
                var eval = -DeepEval(depth - 1, -beta, -alpha, true, npv);
                if(!transpositions.ContainsKey(board.positionKey))
                    transpositions.Add(board.positionKey, eval);
                board.UndoMove();

                if (eval >= beta)
                {
                    if (move.CapturedPiece == Piece.NONE)
                    {
                        KillerMoves[board.Ply][2] = KillerMoves[board.Ply][1];
                        KillerMoves[board.Ply][1] = KillerMoves[board.Ply][0];
                        KillerMoves[board.Ply][0] = move;
                    }
                    return beta;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                    pv[0] = move;
                    Array.Copy(npv, 0, pv, 1, npv.Length);
                }
            }

            return alpha;
        }

        private int QuiesceneSearch(int alpha, int beta)
        {
            Nodes++;

            int currentEval = Evaluation.Evaluate(board);
            if(currentEval >= beta)
            {
                return beta;
            }
            if(currentEval > alpha)
            {
                alpha = currentEval;
            }

            generator.Setup();
            var moves = generator.GetMoves(true);
            foreach (var move in moves.OrderByDescending(m => MoveImportance(m, -1)))
            {
                board.SubmitMove(move);
                var eval = -QuiesceneSearch(-beta, -alpha);
                board.UndoMove();

                if (eval >= beta)
                    return beta;

                if(eval > alpha)
                {
                    alpha = eval;
                }
            }

            return alpha;
        }
    }
}
