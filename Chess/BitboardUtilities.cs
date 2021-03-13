using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    public class BitboardUtilities
    {
        public static ulong ShiftUp(ulong bitboard)
        {
            return bitboard << 8;
        }

        public static ulong ShiftDown(ulong bitboard)
        {
            return bitboard >> 8;
        }

        public static ulong ShiftLeft(ulong bitboard)
        {
            return (bitboard << 1) & ~MoveHelper.FileBitboards[7];
        }

        public static ulong ShiftRight(ulong bitboard)
        {
            return (bitboard >> 1) & ~MoveHelper.FileBitboards[0];
        }
    }
}
