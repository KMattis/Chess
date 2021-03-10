using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
    public static class MoveHelper
    {

        public const int N = 0;
        public const int E = 1;
        public const int S = 2;
        public const int W = 3;
        public const int NE = 4;
        public const int SE = 5;
        public const int SW = 6;
        public const int NW = 7;

        /// <summary>
        /// N, E, S, W, NE, SE, SW, NW
        /// </summary>
        public static int[] Directions = { 8, 1, -8, -1, 9, -7, -9, 7 };

        public static int[][] NumSquaresToEdge = new int[64][];

        public static int[][] KnightMoves = new int[64][];
        public static ulong[] KnightBitboards = new ulong[64];

        public static ulong[][] PawnAttackBitboards = new ulong[3][];

        public static int[][] KingMoves = new int[64][];
        public static ulong[] KingAttackBitboards = new ulong[64];

        public static int[] DirectionLookup = new int[127];


        public static ulong[] FileBitboards = new ulong[8];
        public static ulong[] IsolatedPawnBitboards = new ulong[8];
        public static ulong[] PassedPawnBitboards = new ulong[8];

        static MoveHelper()
        {
            for (int square = 0; square < 64; square++)
            {
                int x = square % 8;
                int y = square / 8;
                int N = 7 - y;
                int S = y;
                int E = 7 - x;
                int W = x;

                NumSquaresToEdge[square] = new int[] { N, E, S, W, Math.Min(N, E), Math.Min(S, E), Math.Min(S, W), Math.Min(N, W) };
            }

            //init knightMoves and king moves
            for (int square = 0; square < 64; square++)
            {
                int x = square % 8;
                int y = square / 8;

                var possibleKnightMoves = new List<int>();

                if (x > 0 && y > 1) possibleKnightMoves.Add(x - 1 + 8 * (y - 2));
                if (x > 1 && y > 0) possibleKnightMoves.Add(x - 2 + 8 * (y - 1));
                if (x > 0 && y < 6) possibleKnightMoves.Add(x - 1 + 8 * (y + 2));
                if (x > 1 && y < 7) possibleKnightMoves.Add(x - 2 + 8 * (y + 1));
                if (x < 7 && y > 1) possibleKnightMoves.Add(x + 1 + 8 * (y - 2));
                if (x < 6 && y > 0) possibleKnightMoves.Add(x + 2 + 8 * (y - 1));
                if (x < 7 && y < 6) possibleKnightMoves.Add(x + 1 + 8 * (y + 2));
                if (x < 6 && y < 7) possibleKnightMoves.Add(x + 2 + 8 * (y + 1));

                foreach (var target in possibleKnightMoves)
                    KnightBitboards[square] |= 1ul << target;

                KnightMoves[square] = possibleKnightMoves.ToArray();

                var possibleKingMoves = new List<int>();
                for(int dirIndex = 0; dirIndex < 8; dirIndex++)
                {
                    if(NumSquaresToEdge[square][dirIndex] > 0)
                    {
                        possibleKingMoves.Add(square + Directions[dirIndex]);
                    }
                }

                foreach (var target in possibleKingMoves)
                    KingAttackBitboards[square] |= 1ul << target;

                KingMoves[square] = possibleKingMoves.ToArray();
            }

            //Pawn attacks
            PawnAttackBitboards[Piece.WHITE] = new ulong[64];
            PawnAttackBitboards[Piece.BLACK] = new ulong[64];
            for(int square = 8; square < 56; square++) //Pawns can only be on 2nd to 7th rank
            {
                //Eastward attack
                if(NumSquaresToEdge[square][E] > 0)
                {
                    PawnAttackBitboards[Piece.WHITE][square] |= 1ul << (square + Directions[NE]);
                    PawnAttackBitboards[Piece.BLACK][square] |= 1ul << (square + Directions[SE]);
                }

                //Westward attack
                if (NumSquaresToEdge[square][W] > 0)
                {
                    PawnAttackBitboards[Piece.WHITE][square] |= 1ul << (square + Directions[NW]);
                    PawnAttackBitboards[Piece.BLACK][square] |= 1ul << (square + Directions[SW]);
                }
            }

            //Direction Lookup
            for (int i = 0; i < 127; i++)
            {
                int offset = i - 63;
                int absOffset = Math.Abs(offset);
                int absDir = 1;
                if (absOffset % 9 == 0)
                {
                    absDir = 9;
                }
                else if (absOffset % 8 == 0)
                {
                    absDir = 8;
                }
                else if (absOffset % 7 == 0)
                {
                    absDir = 7;
                }

                DirectionLookup[i] = absDir * Math.Sign(offset);
            }

            for(var file = 0; file < 8; file++)
            {
                for(var rank = 0; rank < 8; rank++)
                {
                    FileBitboards[file] |= 1ul << (rank*8 + file);
                }
            }

            for (var file = 0; file < 8; file++)
            {
                if (file >= 1)
                {
                    IsolatedPawnBitboards[file] |= FileBitboards[file - 1];
                    PassedPawnBitboards[file] |= FileBitboards[file - 1];
                }
                PassedPawnBitboards[file] |= FileBitboards[file];
                if (file <= 6)
                {
                    IsolatedPawnBitboards[file] |= FileBitboards[file + 1];
                    PassedPawnBitboards[file] |= FileBitboards[file + 1];
                }
            }
        }

        public static void PrintBitboard(ulong bitboard)
        {
            for(int rank = 7; rank >= 0; rank--)
            {
                for(int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    if((bitboard & 1ul << square) != 0)
                    {
                        Console.Write('#');
                    }
                    else
                    {
                        Console.Write('.');
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
