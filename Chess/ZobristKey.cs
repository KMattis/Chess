using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    public static class ZobristKey
    {
        private static ulong[][] pieceSquareData = new ulong[32][];

        private static Random randomGenerator = new Random();
        static ZobristKey()
        {
            foreach(var pieceType in Piece.PIECE_TYPES)
            {
                pieceSquareData[pieceType] = new ulong[64];
                for(int i = 0; i < 64; i++)
                {
                    pieceSquareData[pieceType][i] = NextULong();
                }
            }
        }

        private static ulong NextULong()
        {
            var data = new byte[8];
            randomGenerator.NextBytes(data);
            return BitConverter.ToUInt64(data);
        }

        public static ulong GetKey(Board board)
        {
            ulong hash = 0;
            for(int i = 0; i < 64; i++)
            {
                if (board.board[i] != Piece.NONE)
                    hash ^= pieceSquareData[board.board[i]][i];
            }

            hash ^= board.Us;
            hash ^= (uint)board.EnPassentSquare;

            return hash;
        }
    }
}
