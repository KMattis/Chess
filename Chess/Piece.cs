using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
    /// <summary>
    /// 5 Bits: 
    /// 
    /// 3 bits the piece type
    /// 2 bits the piece colour
    /// </summary>
    public static class Piece
    {
        public const uint NONE = 0b00000;
        public const uint PAWN = 0b00100;
        public const uint BISHOP = 0b01000;
        public const uint ROOK = 0b10000;
        public const uint QUEEN = 0b11000;
        public const uint KNIGHT = 0b01100;
        public const uint KING = 0b10100;

        public const uint WHITE = 0b01;
        public const uint BLACK = 0b10;

        public const uint PIECE_MASK = 0b11100;
        public const uint COLOR_MASK = 0b00011;

        public static readonly uint[] PIECE_TYPES = { WHITE | KING, WHITE | QUEEN, WHITE | ROOK, WHITE | BISHOP, WHITE | KNIGHT,  WHITE | PAWN,
                                                      BLACK | KING, BLACK | QUEEN, BLACK | ROOK, BLACK | BISHOP, BLACK | KNIGHT,  BLACK | PAWN };

        public static char ToFenString(uint piece)
        {
            char res;
            switch (piece & PIECE_MASK)
            {
                case NONE: res = '.'; break;
                case PAWN: res = 'p'; break;
                case BISHOP: res = 'b'; break;
                case KNIGHT: res = 'n'; break;
                case ROOK: res = 'r'; break;
                case QUEEN: res = 'q'; break;
                case KING: res = 'k'; break;
                default: throw new Exception("CANNOT HAPPEN");
            }

            if ((piece & COLOR_MASK) == WHITE)
                return char.ToUpper(res);

            return res;
        }

        public static uint OtherColor(uint color)
        {
            return COLOR_MASK ^ color;
        }

        public static uint FromFenString(char fen)
        {
            uint piece;

            switch (char.ToLower(fen))
            {
                case 'p': piece = PAWN; break;
                case 'b': piece = BISHOP; break;
                case 'n': piece = KNIGHT; break;
                case 'r': piece = ROOK; break;
                case 'q': piece = QUEEN; break;
                case 'k': piece = KING; break;
                default:
                    throw new InvalidOperationException($"{fen} is not a valid fen piece type");
            }

            if (char.IsUpper(fen))
                return piece | WHITE;
            else
                return piece | BLACK;
        }
    }
}
