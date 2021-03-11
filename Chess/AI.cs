using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Chess
{
    public class SearchInfo
    {
        public int Depth { get; set; }
        public int Score { get; set; }
        public Move[] PV { get; set; }
        public int Nodes { get; set; }
        public int NullMoves { get; set; }
        public int NullMovesSuccess { get; set; }
        public int BetaCutoffs { get; set; }
        public int QNodes { get; set; }
        public int FutilityPrunes { get; set; }
    
        public int Transpositions { get; set; }

        public IDictionary<Move, int> TopLevelMoveOrder { get; set; } = new Dictionary<Move, int>();
    }

    public enum TranspositionEntryType
    {
        Exact,
        UpperBound,
        LowerBound
    }

    //TODO Safe pv in TT
    public class TranspositionEntry
    {
        public int Score;
        public TranspositionEntryType Type;
    }

    public class AI
    {
        public const int INFINITY = 1000000;
        public const int CHECKMATE = 100000;
        public const int CHECKMATE_TRESHOLD = CHECKMATE - 1000;
        public const int DRAW = 0;

        public const int LookupFailed = int.MinValue;

        public const int R = 2;

        private Board board;
        private MoveGenerator generator;

        private IDictionary<ulong, TranspositionEntry> transpositions = new Dictionary<ulong, TranspositionEntry>();
        private SearchInfo searchInfo;

        Move[] lastpv;
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

        private int MoveImportance(Move move, int depth, IDictionary<Move, int> ordering)
        {
            if (ordering != null && ordering.ContainsKey(move))
                return ordering[move];

            var pvMove = lastpv == null ? null : searchInfo.Depth - depth >= lastpv.Length ? null : depth < 0 ? null : lastpv[searchInfo.Depth - depth];
            var pvValue = pvMove == null ? 0 : (pvMove.Start == move.Start && pvMove.Target == move.Target) ? 10000000 : 0;

            var killerValue = GetKillerMoveValue(move);

            var captureValue = -Evaluation.PieceValues_ColorIndependent[move.MovedPiece] * 100 + 1000 * Evaluation.PieceValues_ColorIndependent[move.CapturedPiece];
            var promotionValue = Evaluation.PieceValues_ColorIndependent[move.Promotion];

            var pawnCapturePenalty = (generator.opponentPawnAttackMap & 1ul << move.Target) != 0 ? -10 : 0;

            return killerValue + pvValue + captureValue + promotionValue + pawnCapturePenalty;
        }

        public SearchInfo FindBestMove(int depth, Move[] lastpv, int startAlpha, int startBeta, IDictionary<Move, int> lastTopLevelMoveOrder)
        {
            transpositions.Clear();
            var pv = new Move[depth];

            this.lastpv = lastpv;
            this.searchInfo = new SearchInfo
            {
                Depth = depth
            };

            var score = DeepEval(depth, startAlpha, startBeta, false, pv, lastTopLevelMoveOrder);
            searchInfo.Score = score;
            searchInfo.PV = pv;
            return searchInfo;
        }

        private int LookupEvaluation(int alpha, int beta)
        {
            //TODO account for depth

            var transposition = transpositions[board.positionKey];
            if (transposition.Type == TranspositionEntryType.Exact)
            {
                return transposition.Score;
            }
            if (transposition.Type == TranspositionEntryType.UpperBound && transposition.Score <= alpha)
            {
                return transposition.Score;
            }
            if (transposition.Type == TranspositionEntryType.LowerBound && transposition.Score >= beta)
            {
                return transposition.Score;
            }

            return LookupFailed;
        }

        private void StoreEvaluation(int score, TranspositionEntryType type)
        {
            if (!transpositions.ContainsKey(board.positionKey))
                transpositions.Add(board.positionKey, new TranspositionEntry { Score = score, Type = type });
            else if(type == TranspositionEntryType.Exact) //We have a better evaluation
            {
                transpositions[board.positionKey] = new TranspositionEntry { Score = score, Type = type };
            }
        }

        private int DeepEval(int depth, int alpha, int beta, bool nullMoveAllowed, Move[] pv, IDictionary<Move, int> orderedTopLevelMoves = null)
        {
            searchInfo.Nodes++;

            if (transpositions.ContainsKey(board.positionKey))
            {
                searchInfo.Transpositions++;
                var score = LookupEvaluation(alpha, beta);

                if (score != LookupFailed)
                    return score;
            }

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
            {
                return QuiesceneSearch(alpha, beta);
            }

            if(depth == 1)
            {
                //Futility pruning
                var currentEval = Evaluation.Evaluate(board);
                if(currentEval + Evaluation.BishopValue < alpha)
                {
                    searchInfo.FutilityPrunes++;
                    //Prune this node, i.e. go directly to QuiesceneSearch
                    return QuiesceneSearch(alpha, beta);
                }
            }

            if(nullMoveAllowed && !generator.InCheck && depth >= 1 + R)
            {
                searchInfo.NullMoves++;
                //Null move pruning
                var npv = new Move[depth]; //irellevant here
                board.SubmitNullMove();
                int eval = -DeepEval(depth - 1 - R, -beta, -beta+1, false, npv);
                board.UndoNullMove();
                if (eval >= beta)
                {
                    searchInfo.NullMovesSuccess++;
                    return eval;
                }
            }

            TranspositionEntryType ttType = TranspositionEntryType.UpperBound;
            int movenumber = 1;
            
            foreach (var move in moves.OrderByDescending(m => MoveImportance(m, depth, orderedTopLevelMoves)))
            {
                if (depth == searchInfo.Depth) //Upper level
                {
                    Console.WriteLine($"info currmove {move.ToAlgebraicNotation()} currmovenumber {movenumber++} depth {depth}");
                }

                var npv = new Move[depth-1];
                board.SubmitMove(move);
                var eval = -DeepEval(depth - 1, -beta, -alpha, true, npv);
                board.UndoMove();

                if(depth == searchInfo.Depth)
                {
                    searchInfo.TopLevelMoveOrder.Add(move, eval);
                }

                if (eval >= beta)
                {
                    searchInfo.BetaCutoffs++;
                    //transposition is an upper bound
                    StoreEvaluation(eval, TranspositionEntryType.LowerBound);
                    if (move.CapturedPiece == Piece.NONE)
                    {
                        KillerMoves[board.Ply][2] = KillerMoves[board.Ply][1];
                        KillerMoves[board.Ply][1] = KillerMoves[board.Ply][0];
                        KillerMoves[board.Ply][0] = move;
                    }
                    return beta;
                }
                else if (eval > alpha)
                {
                    alpha = eval;
                    pv[0] = move;
                    Array.Copy(npv, 0, pv, 1, npv.Length);
                    ttType = TranspositionEntryType.Exact;
                }
            }

            StoreEvaluation(alpha, ttType);

            return alpha;
        }

        private int QuiesceneSearch(int alpha, int beta)
        {
            searchInfo.Nodes++;
            searchInfo.QNodes++;

            int currentEval = Evaluation.Evaluate(board);
            if(currentEval >= beta)
            {
                searchInfo.BetaCutoffs++;
                return beta;
            }
            if(currentEval > alpha)
            {
                alpha = currentEval;
            }

            generator.Setup();
            var moves = generator.GetMoves(true);
            foreach (var move in moves.OrderByDescending(m => MoveImportance(m, -1, null)))
            {
                board.SubmitMove(move);
                var eval = -QuiesceneSearch(-beta, -alpha);
                board.UndoMove();

                if (eval >= beta)
                {
                    searchInfo.BetaCutoffs++;
                    return beta;
                }

                if(eval > alpha)
                {
                    alpha = eval;
                }
            }

            return alpha;
        }
    }
}
