using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{

    //TODO: maybe make move a integer (6 bit start, 6 bit target, 5 bits moved piece, 5 bits captured piece, 5 bits promoted, 1 bit casteling, 1 bit enPassent etc.)
    public class Move
    {
        public int Start { get; private set; }
        public int Target { get; private set; }
        public uint MovedPiece { get; private set; }
        public uint CapturedPiece { get; private set; }

        public uint Promotion { get; private set; } = Piece.NONE;

        public bool IsCasteling { get; private set; } = false;

        public bool IsEnPassent { get; private set; } = false;

        public bool IsDoublePawnMove { get; private set; } = false;

        public Move(int start, int target, Board b)
        {
            Start = start;
            Target = target;
            MovedPiece = b.board[start];
            CapturedPiece = b.board[target];
        }

        public Move PromoteTo(uint promotion)
        {
            Promotion = promotion;
            return this;
        }

        public Move EnPassent()
        {
            IsEnPassent = true;
            return this;
        }

        public Move DoublePawnMove()
        {
            IsDoublePawnMove = true;
            return this;
        }

        public Move Casteling()
        {
            IsCasteling = true;
            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj == null) return false;
            if (obj.GetType() != GetType()) return false;
            var move = (Move)obj;
            return move.Start == Start && move.Target == Target;
        }

        public override int GetHashCode()
        {
            return Start * 64 + Target;
        }

        public string ToAlgebraicNotation()
        {
            var promotion = Promotion == Piece.NONE ? "" : Piece.ToFenString(Promotion).ToString().ToLower();

            return $"{SquareToAlgebraicNotation(Start)}{SquareToAlgebraicNotation(Target)}{promotion}";
        }

        public override string ToString()
        {
            return ToAlgebraicNotation();
        }

        public static string SquareToAlgebraicNotation(int square)
        {
            const string files = "abcdefgh";
            const string ranks = "12345678";
            int rank = square / 8;
            int file = square % 8;
            return $"{files[file]}{ranks[rank]}";
        }
    }
}
