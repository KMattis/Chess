﻿using System;
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
            int depth = 5;
            Move[] lastpv = null;
            int? lastScore = null;
            while (true)
            {
                var currentSearchStartTime = DateTime.Now;
                int alpha = lastScore.HasValue ? lastScore.Value - 20 : -AI.INFINITY;
                int beta = lastScore.HasValue ? lastScore.Value + 20 : AI.INFINITY;

                (int score, Move[] pv) = ai.FindBestMove(depth, lastpv, alpha, beta);
                var pvstring = "";
                foreach (var move in pv)
                {
                    if (move == null)
                        break;
                    pvstring += move.ToAlgebraicNotation() + " ";
                }
                var nodesPerSecond = (int)(ai.Nodes / (DateTime.Now - currentSearchStartTime).TotalSeconds);

                if (score <= alpha || score >= beta)
                {
                    
                    Console.WriteLine($"info score cp {score} pv {pvstring} depth {depth} nodes {ai.Nodes} nps {nodesPerSecond}");
                    currentSearchStartTime = DateTime.Now;
                    (score, pv) = ai.FindBestMove(depth, lastpv, -AI.INFINITY, AI.INFINITY);
                    pvstring = "";
                    foreach (var move in pv)
                    {
                        if (move == null)
                            break;
                        pvstring += move.ToAlgebraicNotation() + " ";
                    }
                    nodesPerSecond = (int)(ai.Nodes / (DateTime.Now - currentSearchStartTime).TotalSeconds);
                }
                
                //TODO: Add mating score info
                Console.WriteLine($"info score cp {score} pv {pvstring} depth {depth} nodes {ai.Nodes} nps {nodesPerSecond}");
                if (DateTime.Now - startTime > TimeSpan.FromSeconds(10))
                {
                    return pv[0];
                }
                lastpv = pv;
                lastScore = score;
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