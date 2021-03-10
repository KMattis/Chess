﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{

    public class BoardState
    {
        public uint positionKey;

        public int enPassentSquare;

        public uint castelingRights;

        public Move lastMove;
    }

    public class Board
    {
        public const string STARTING_POSITION = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w QKqk - 0 1";

        public uint[] board = new uint[64];

        public uint CastelingRights; // WHITE Q K, then BLACK Q K

        public const uint WHITE_QUEENSIDE_CASTLE = 0b1000;
        public const uint WHITE_KINGSIDE_CASTLE  = 0b0100;
        public const uint BLACK_QUEENSIDE_CASTLE = 0b0010;
        public const uint BLACK_KINGSIDE_CASTLE  = 0b0001;

        public Stack<BoardState> states = new Stack<BoardState>();

        public IList<int>[] pieceList = new List<int>[32];

        private MoveGenerator generator;

        public uint positionKey;

        public uint Us;
        public uint Them;

        public int EnPassentSquare = -1;

        public int Ply = 0;

        public Board(string fen = STARTING_POSITION)
        {
            foreach(var pieceType in Piece.PIECE_TYPES)
            {
                pieceList[pieceType] = new List<int>();
            }

            generator = new MoveGenerator(this);

            var fenData = fen.Split(' ');
            var rankDataArray = fenData[0].Split('/');

            for (int rank = 0; rank < 8; rank++) //rank
            {
                var rankData = rankDataArray[7 - rank]; //Ranks are stored backwards in fen

                int file = 0;
                foreach (char datapoint in rankData)
                {
                    if (char.IsNumber(datapoint))
                    {
                        file += int.Parse(datapoint.ToString());
                    }
                    else
                    {
                        var piece = Piece.FromFenString(datapoint);
                        var pos = rank * 8 + file;
                        board[pos] = piece;
                        if(piece != Piece.NONE)
                            pieceList[piece].Add(pos);
                        file++;
                    }
                }
            }

            Us = fenData[1] == "w" ? Piece.WHITE : Piece.BLACK;
            Them = Piece.OtherColor(Us);

            var castelingRightsString = fenData[2];
            if (castelingRightsString.Contains("Q"))
                CastelingRights |= WHITE_QUEENSIDE_CASTLE;
            if (castelingRightsString.Contains("K"))
                CastelingRights |= WHITE_KINGSIDE_CASTLE;
            if (castelingRightsString.Contains("q"))
                CastelingRights |= BLACK_QUEENSIDE_CASTLE;
            if (castelingRightsString.Contains("k"))
                CastelingRights |= BLACK_KINGSIDE_CASTLE;
        }

        public void SubmitNullMove()
        {
            states.Push(new BoardState
            {
                lastMove = null,
                positionKey = positionKey,
                enPassentSquare = EnPassentSquare,
                castelingRights = CastelingRights,
            }) ;

            //No EnPassent in NullMovePruning
            EnPassentSquare = -1;
            Ply++;

            Us = Them;
            Them = Piece.OtherColor(Us);
            positionKey = ComputePositionKey();
        }

        public void UndoNullMove()
        {
            var state = states.Pop();
            positionKey = state.positionKey;
            Ply--;
            Us = Them;
            Them = Piece.OtherColor(Us);
            EnPassentSquare = state.enPassentSquare;
            CastelingRights = state.castelingRights;
        }

        //We assume the submitted move is legal
        public void SubmitMove(Move move)
        {
            states.Push(new BoardState
            {
                lastMove = move,
                positionKey = positionKey,
                enPassentSquare = EnPassentSquare,
                castelingRights = CastelingRights
            });
            Ply++;
            board[move.Start] = Piece.NONE;
            pieceList[move.MovedPiece].Remove(move.Start);
            if(move.Promotion != Piece.NONE)
            {
                var promotedPiece = (Piece.COLOR_MASK & move.MovedPiece) | move.Promotion;
                board[move.Target] = promotedPiece;
                pieceList[promotedPiece].Add(move.Target);
            }
            else
            {
                board[move.Target] = move.MovedPiece;
                pieceList[move.MovedPiece].Add(move.Target);
            }

            if(move.CapturedPiece != Piece.NONE)
            {
                pieceList[move.CapturedPiece].Remove(move.Target);
            }

            //EnPassent related stuff
            if (move.IsDoublePawnMove)
            {
                //set the enPassent square to the square behind the target
                EnPassentSquare = Us == Piece.WHITE ? move.Target - 8 : move.Target + 8;
            }
            else
            {
                //reset the enPassent square
                EnPassentSquare = -1;
            }
            if (move.IsEnPassent)
            {
                //Remove the pawn on the advanced square
                var captureSquare = Us == Piece.WHITE ? move.Target - 8 : move.Target + 8; //rank of start + file of target
                pieceList[Piece.PAWN | Them].Remove(captureSquare);
                board[captureSquare] = Piece.NONE;
            }

            //Casteling related stuff
            if(move.MovedPiece == (Piece.WHITE | Piece.KING))
            {
                //Revoke whites casteling rights after a king move
                CastelingRights &= BLACK_KINGSIDE_CASTLE | BLACK_QUEENSIDE_CASTLE;
            }
            else if (move.MovedPiece == (Piece.BLACK | Piece.KING))
            {
                //Revoke blacks casteling rights after a king move
                CastelingRights &= WHITE_KINGSIDE_CASTLE | WHITE_QUEENSIDE_CASTLE;
            }
            else
            {
                //Revoke whites casteling rights if the correpsonding rook moved or was captured
                if (move.Start == 0 || move.Target == 0) //A1
                    CastelingRights &= ~WHITE_QUEENSIDE_CASTLE;
                if (move.Start == 7 || move.Target == 7) //H1
                    CastelingRights &= ~WHITE_KINGSIDE_CASTLE;
                //Revoke blacks casteling rights if the correpsonding rook moved or was captured
                if (move.Start == 7 * 8 || move.Target == 7 * 8) //A8
                    CastelingRights &= ~BLACK_QUEENSIDE_CASTLE;
                if (move.Start == 7 * 8 + 7 || move.Target == 7 * 8 + 7) //H8
                    CastelingRights &= ~BLACK_KINGSIDE_CASTLE;
            }
            if (move.IsCasteling)
            {
                //We need not revoke casteling rights after castle, this has already happend
                //We need to move the rook

                switch (move.Target)
                {
                    case 2: //C1
                        board[0] = Piece.NONE;
                        board[3] = Piece.WHITE | Piece.ROOK;
                        pieceList[Piece.WHITE | Piece.ROOK].Remove(0);
                        pieceList[Piece.WHITE | Piece.ROOK].Add(3);
                        break;
                    case 6: //G1
                        board[7] = Piece.NONE;
                        board[5] = Piece.WHITE | Piece.ROOK;
                        pieceList[Piece.WHITE | Piece.ROOK].Remove(7);
                        pieceList[Piece.WHITE | Piece.ROOK].Add(5);
                        break;
                    case 7 * 8 + 2: //C8
                        board[7*8] = Piece.NONE;
                        board[7*8+3] = Piece.BLACK | Piece.ROOK;
                        pieceList[Piece.BLACK | Piece.ROOK].Remove(7*8);
                        pieceList[Piece.BLACK | Piece.ROOK].Add(7*8+3);
                        break;
                    case 7 * 8 + 6: //G8
                        board[7*8+7] = Piece.NONE;
                        board[7*8+5] = Piece.BLACK | Piece.ROOK;
                        pieceList[Piece.BLACK | Piece.ROOK].Remove(7*8+7);
                        pieceList[Piece.BLACK | Piece.ROOK].Add(7*8+5);
                        break;
                    default:
                        throw new Exception("Illegal casteling move: " + move.ToAlgebraicNotation());
                }
            }

            Us = Them;
            Them = Piece.OtherColor(Us);
            positionKey = ComputePositionKey();
        }

        public void UndoMove()
        {
            var state = states.Pop();
            var move = state.lastMove;
            positionKey = state.positionKey;
            Ply--;
            Us = Them;
            Them = Piece.OtherColor(Us);
            EnPassentSquare = state.enPassentSquare;
            CastelingRights = state.castelingRights;
            
            board[move.Start] = move.MovedPiece;
            board[move.Target] = move.CapturedPiece;

            if(move.Promotion != Piece.NONE)
            {
                var promotedPiece = (Piece.COLOR_MASK & move.MovedPiece) | move.Promotion;
                pieceList[promotedPiece].Remove(move.Target);
            }
            else
            {
                pieceList[move.MovedPiece].Remove(move.Target);
            }
            pieceList[move.MovedPiece].Add(move.Start);

            if (move.CapturedPiece != Piece.NONE)
            {
                pieceList[move.CapturedPiece].Add(move.Target);
            }

            //EnPassent related stuff
            if (move.IsEnPassent)
            {
                var captureSquare = Us == Piece.WHITE ? move.Target - 8 : move.Target + 8; //rank of start + file of target
                board[captureSquare] = Piece.PAWN | Them;
                pieceList[Piece.PAWN | Them].Add(captureSquare);
            }

            //Casteling related stuff
            if (move.IsCasteling)
            {
                //We need not revoke casteling rights after castle, this has already happend
                //We need to move the rook

                switch (move.Target)
                {
                    case 2: //C1
                        board[0] = Piece.WHITE | Piece.ROOK;
                        board[3] = Piece.NONE;
                        pieceList[Piece.WHITE | Piece.ROOK].Remove(3);
                        pieceList[Piece.WHITE | Piece.ROOK].Add(0);
                        break;
                    case 6: //G1
                        board[7] = Piece.WHITE | Piece.ROOK;
                        board[5] = Piece.NONE;
                        pieceList[Piece.WHITE | Piece.ROOK].Remove(5);
                        pieceList[Piece.WHITE | Piece.ROOK].Add(7);
                        break;
                    case 7 * 8 + 2: //C8
                        board[7 * 8] = Piece.BLACK | Piece.ROOK;
                        board[7 * 8 + 3] = Piece.NONE;
                        pieceList[Piece.BLACK | Piece.ROOK].Remove(7 * 8 + 3);
                        pieceList[Piece.BLACK | Piece.ROOK].Add(7 * 8);
                        break;
                    case 7 * 8 + 6: //G8
                        board[7 * 8 + 7] = Piece.BLACK | Piece.ROOK;
                        board[7 * 8 + 5] = Piece.NONE;
                        pieceList[Piece.BLACK | Piece.ROOK].Remove(7 * 8 + 5);
                        pieceList[Piece.BLACK | Piece.ROOK].Add(7 * 8 + 7);
                        break;
                    default:
                        throw new Exception("Illegal casteling move: " + move.ToAlgebraicNotation());
                }
            }

            foreach(var pieceType in Piece.PIECE_TYPES)
            {
                if(pieceList[pieceType].Count > 10)
                {
                    throw new Exception("Too much of type " + pieceType);
                }
            }
        }

        private uint ComputePositionKey()
        {
            unchecked
            {
                const int p = 16777619;
                uint hash = 2166136261;

                for (int i = 0; i < 64; i++)
                    hash = (hash ^ board[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;

                hash += Us;
                return hash;
            }
        }

        public override string ToString()
        {
            var result = "";
            for(int rank = 7; rank >= 0; rank--)
            {
                for(int file = 0; file < 8; file++)
                {
                    result += Piece.ToFenString(board[rank * 8 + file]);
                }
                result += "\n";
            }
            return result;
        }
    }
}
