using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
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

        public string ToAlgebraicNotation()
        {
            var promotion = Promotion == Piece.NONE ? "" : Piece.ToFenString(Promotion).ToString().ToLower();

            return $"{SquareToAlgebraicNotation(Start)}{SquareToAlgebraicNotation(Target)}{promotion}";
        }

        public static string SquareToAlgebraicNotation(int square)
        {
            var files = "abcdefgh";
            var ranks = "12345678";
            int rank = square / 8;
            int file = square % 8;
            return $"{files[file]}{ranks[rank]}";
        }
    }
}
