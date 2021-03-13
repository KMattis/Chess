using System;
using System.Collections.Generic;
using System.Linq;

namespace Chess.Client
{
    class Program
    {
        public static Board board = new Board();
        public static AI ai = new AI(board);

        public static void SetPosition(string[] line)
        {
            if(line[1] == "startpos")
            {
                board = new Board();
                ai = new AI(board);
            }
            else
            {
                throw new Exception("UNSUPPORTED");
            }

            for(int i = 3; i < line.Length; i++)
            {
                var moveAlg = line[i];
                Move theMove = null;
                var gen = new MoveGenerator(board);
                gen.Setup();
                foreach (var move in gen.GetMoves(false))
                {
                    if (move.ToAlgebraicNotation().Equals(moveAlg))
                    {
                        theMove = move;
                        break;
                    }
                }
                if (theMove == null)
                    Console.WriteLine("Move "+  moveAlg + " was not found");
                board.SubmitMove(theMove);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("id name ChessTest1");
            Console.WriteLine("id author Klaus Mattis");
            Console.WriteLine("uciok");

            while (true)
            {
                var line = Console.ReadLine().Split(" ");

                switch (line[0])
                {
                    case "isready":
                        Console.WriteLine("readyok");
                        break;
                    case "position":
                        SetPosition(line);
                        break;
                    case "ucinewgame":
                        SetPosition(new string[] { "position", "startpos" });
                        break;
                    case "go":
                        Move move = SearchBestMove();
                        if (move != null)
                        {
                            board.SubmitMove(move);
                            MoveHelper.PrintBitboard(board.WhitePawnBitboard | board.BlackPawnBitboard);
                            Console.WriteLine($"bestmove {move.ToAlgebraicNotation()}"); //Tell UCI the best move
                        }
                        break;
                    case "drawboard":
                        Console.WriteLine(board);
                        Console.WriteLine();
                        Console.WriteLine(board.Us == Piece.WHITE ? "W" : "B");
                        break;
                    case "quit":
                        return;
                    default:
                        Console.WriteLine("Unknown command: " + string.Join(" ", line));
                        break;
                }

            }
        }
        public static Move SearchBestMove()
        {
            var startTime = DateTime.Now;
            int depth = 4;
            Move[] lastpv = null;
            int? lastScore = null;
            IDictionary<Move, int> topLevelMoveOrder = null;
            while (true) //iterate search depth
            {
                var currentSearchStartTime = DateTime.Now;
                var lastAspirationWindowRestart = DateTime.Now;

                int delta = 17; //TODO: goood value?
                int alpha = lastScore.HasValue ? lastScore.Value - delta : -AI.INFINITY;
                int beta = lastScore.HasValue ? lastScore.Value + delta : AI.INFINITY;
                SearchInfo info;

                int failLowCount = 0;
                int failHighCount = 0;

                while (true) //Search, if necessary widen aspiration window size
                {
                    info = ai.FindBestMove(depth, lastpv, alpha, beta, topLevelMoveOrder, (failLowCount + failHighCount == 0));

                    if (info.Score <= alpha) //Fail low
                    {
                        beta = (alpha + beta) / 2;
                        alpha = info.Score - delta;
                        failLowCount++;
                    }
                    else if (info.Score >= beta) //Fail high
                    {
                        beta = info.Score + delta;
                        failHighCount++;
                    }
                    else
                        break;

                    lastAspirationWindowRestart = DateTime.Now;
                    delta += delta / 4 + 5;
                }

                var pvstring = "";
                foreach (var move in info.PV)
                {
                    if (move == null)
                        break;
                    pvstring += move.ToAlgebraicNotation() + " ";
                }

                var searchTime = DateTime.Now - currentSearchStartTime;
                var aspirationTime = lastAspirationWindowRestart - currentSearchStartTime;

                int percentage = (int)(100 * aspirationTime.TotalMilliseconds / searchTime.TotalMilliseconds);

                var nodesPerSecond = (int)(info.Nodes / searchTime.TotalSeconds);

                //TODO: Add mating score info
                Console.WriteLine($"info score cp {info.Score} pv {pvstring} depth {info.Depth} nodes {info.Nodes} nps {nodesPerSecond}");
                Console.WriteLine($"info string AWPer {percentage}% FailL {failLowCount} FailH {failHighCount} NM {info.NullMoves} NMS {info.NullMovesSuccess} QNodes {info.QNodes} BCutoffs {info.BetaCutoffs} FP {info.FutilityPrunes} TTHits {info.Transpositions} SR {info.ScoutRemovals} Scouts {info.Scouts} MCPrunes {info.MCPrunes}");
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(5))
                {
                    return info.PV[0];
                }
                lastpv = info.PV;
                lastScore = info.Score;
                topLevelMoveOrder = info.TopLevelMoveOrder;
                depth++;
            }
        }

        public static int CountMoves(Board board, MoveGenerator generator, uint us, int depth)
        {
            if (depth == 0)
                return 1;

            var count = 0;
            var them = Piece.OtherColor(us);

            generator.Setup();
            foreach (var move in generator.GetMoves(false))
            {
                board.SubmitMove(move);
                count += CountMoves(board, generator, them, depth-1);
                board.UndoMove();
            }

            return count;
        }
    }
}
