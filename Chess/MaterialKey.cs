using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    /// <summary>
    /// Used to compute material keys
    /// </summary>
    public class MaterialKey
    {
        public static int[] OFFSETS = new int[32];
        public static ulong[] MASKS = new ulong[32];

        static MaterialKey()
        {
            int offset = 0;
            foreach(var pieceType in Piece.PIECE_TYPES)
            {
                //4 Bits for each piece, so we can distinguish up to 15 pieces (max: 10 rooks, 10 bishops, 10 knights, 9 Queens, 8 pawns)
                //This sumes to 2*(5*4) = 2*20 = 40 Bit, need a ulong!
                OFFSETS[pieceType] = offset;
                MASKS[pieceType] = 0xFul << offset;

                offset += 4;
            }
        }

        public static void AddPiece(uint pieceType, ref ulong key)
        {
            //Increase amount by one
            var amount = ((key & MASKS[pieceType]) >> OFFSETS[pieceType]) + 1;
            key &= ~MASKS[pieceType];
            key |= amount << OFFSETS[pieceType];
        }

        public static void RemovePiece(uint pieceType, ref ulong key)
        {
            //Decrease amount by one
            var amount = ((key & MASKS[pieceType]) >> OFFSETS[pieceType]) - 1;
            key &= ~MASKS[pieceType];
            key |= amount << OFFSETS[pieceType];
        }
    }
}
