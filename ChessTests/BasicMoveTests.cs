using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chess.Tests
{
    public class BasicMoveTests
    {

        public static IDictionary<string, int> positionToLegalMoves = new Dictionary<string, int>
        {
            { "7k/8/8/4K3/8/8/8/8 w -", 8 },
            { "7k/8/8/K7/8/8/8/8 w -", 5 },
            { "7k/8/8/8/8/8/8/K7 w -", 3 },
            { "7k/8/8/4R3/8/8/8/7K w -", 17 },
            { "7k/8/8/R7/8/8/8/7K w -", 17 },
            { "7k/8/8/7K/8/8/8/R7 w -", 19 },
            { "4k3/8/8/4B3/8/8/8/4K3 w -", 18 },
            { "7k/8/8/B7/8/8/8/7K w -", 10 },
            { "7K/8/7k/8/8/8/8/7B w -", 8 },
        };

        public static IDictionary<string, int[]> positions = new Dictionary<string, int[]>
        {
            { "7K/P7/8/8/8/8/8/7k w - 0 1", new int[]{ 7, 19, 219, 1073, 15841 } },
            { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", new int[]{20, 400, 8902, 197281, 4865609 } },
            { "rnQq1k1r/pp2bppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R b KQ - 1 9", new int[]{31, 1459, 44226, 2106366 } },      
            { "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", new int[]{44, 1486, 62379, 2103487, 89941194 } },
        };

        public StreamWriter Debug;

        [SetUp]
        public void SetUp()
        {
            Debug = new StreamWriter(new FileStream("debug.log", FileMode.Create));
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Flush();
            Debug.Close();
        }

        [Test]
        public void TestBasicMoves()
        {   
            foreach((string pos, int moves) in positionToLegalMoves)
            {
                Assert.AreEqual(moves, CountLegalMoves(pos, 1));
            }
        }

        [Test]
        public void TestPerftPositions()
        {
            foreach((string pos, int[] movesPerDepth) in positions)
            {
                for(int depth = 0; depth < movesPerDepth.Length; depth++)
                {
                    Assert.AreEqual(movesPerDepth[depth], CountLegalMoves(pos, depth + 1));
                }
            }
        }

        public int CountLegalMoves(string startingPosition, int depth)
        {
            var board = new Board(startingPosition);
            Debug.WriteLine("Beginning Counting Moves to depth " + depth + " in position " + startingPosition);
            Debug.Flush();
            return CountLegalMoves(board, depth);
        }
        public int CountLegalMoves(Board board, int depth)
        {
            if (depth == 0)
                return 1;

            var count = 0;
            var generator = new MoveGenerator(board);
            generator.Setup();
            foreach(var move in generator.GetMoves(false))
            {
                board.SubmitMove(move);
                count += CountLegalMoves(board, depth - 1);
                board.UndoMove();
            }

            if (board.states.Count == 1)
                Debug.WriteLine($"depth: {board.states.Count}, count: {count}, after {board.states.Peek().lastMove.ToAlgebraicNotation()}");

            return count;
        }
    }
}