using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Chess.MoveHelper;

namespace Chess
{
    public class MoveGenerator
    {
        private Board board;

        //Mark where the pinned pieces are
        private ulong pinBitmask;
        private ulong rayCheckBitmask;

        public ulong opponentKnightAttackMap;
        public ulong opponentPawnAttackMap;
        public ulong opponentRayAttackMap;
        public ulong opponentKingAttackMap;
        public ulong opponentAttackMap;

        private int ourKingSquare;

        public bool InCheck { get; private set; }
        public bool InDoubleCheck { get; private set; }

        public bool PinsExist { get; private set; }

        public MoveGenerator(Board board)
        {
            this.board = board;
        }

        private void Reset()
        {
            opponentKnightAttackMap = 0;
            opponentPawnAttackMap = 0;
            opponentRayAttackMap = 0;
            opponentKingAttackMap = 0;
            opponentAttackMap = 0;

            pinBitmask = 0;
            rayCheckBitmask = 0;

            InCheck = false;
            InDoubleCheck = false;
            PinsExist = false;
            ourKingSquare = board.pieceList[Piece.KING | board.Us][0];
        }

        public void Setup()
        {
            Reset();
            GenerateBitmasks();
        }

        //Returns a list of all possible moves, not accounting for checks
        public IList<Move> GetMoves(bool onlyCaptures)
        {
            var moves = new List<Move>(64);

            //Add King moves
            AddKingMoves(moves, onlyCaptures);

            //if in double check: only king moves (excluding casteling) are legal
            if (InDoubleCheck)
                return moves;

            if (!InCheck)
                AddCastelingMoves(moves, ourKingSquare);

            var rooks = board.pieceList[Piece.ROOK | board.Us];
            for(int i = 0; i < rooks.Count; i++)
            {
                AddRayMoves(moves, rooks[i], 0, 4, onlyCaptures);
            }

            var bishops = board.pieceList[Piece.BISHOP | board.Us];
            for (int i = 0; i < bishops.Count; i++)
            {
                AddRayMoves(moves, bishops[i], 4, 8, onlyCaptures);
            }

            var queens = board.pieceList[Piece.QUEEN | board.Us];
            for (int i = 0; i < queens.Count; i++)
            {
                AddRayMoves(moves, queens[i], 0, 8, onlyCaptures);
            }

            var knights = board.pieceList[Piece.KNIGHT | board.Us];
            for (int i = 0; i < knights.Count; i++)
            {
                AddKnightMoves(moves, knights[i], onlyCaptures);
            }

            var pawns = board.pieceList[Piece.PAWN | board.Us];
            for (int i = 0; i < pawns.Count; i++)
            {
                AddPawnMoves(moves, pawns[i], onlyCaptures);
            }

            return moves;
        }

        private void AddRayMoves(IList<Move> moves, int start, int directionIndexStart, int directionIndexEnd, bool onlyCaptures)
        {
            bool isPinned = IsPinned(start);
            for(int direction = directionIndexStart; direction < directionIndexEnd; direction++)
            {
                if (isPinned && !IsMovingAlongRay(Directions[direction], start, ourKingSquare))
                    continue;

                for (int n = 1; n <= NumSquaresToEdge[start][direction]; n++)
                {
                    var target = start + n * Directions[direction];
                    if ((board.board[target] & Piece.COLOR_MASK) == board.Us)
                        break;
                    bool isCapture = (board.board[target] & Piece.COLOR_MASK) == board.Them;

                    if (!onlyCaptures || isCapture)
                    {
                        if (!InCheck || IsRayCheckSquare(target))
                        {
                            moves.Add(new Move(start, target, board));
                        }
                    }
                    if (isCapture)
                        break;
                }
            }
        }

        bool IsMovingAlongRay(int rayDir, int startSquare, int targetSquare)
        {
            int moveDir = DirectionLookup[targetSquare - startSquare + 63];
            return (rayDir == moveDir || -rayDir == moveDir);
        }

        private void AddPawnMoves(IList<Move> moves, int start, bool onlyCaptures)
        {
            bool isWhiteToMove = board.Us == Piece.WHITE;
            int[] attackDirections = isWhiteToMove ? new int[]{ NE, NW } : new int[]{ SE, SW };
            int rank = start / 8;
            bool onStartingRank = isWhiteToMove ? rank == 1 : rank == 6;
            bool promotion = isWhiteToMove ? rank == 6 : rank == 1;
            //Pawns cannot be on the last rank, so we need not check whether they stay on the board
            bool isPinned = IsPinned(start);


            if (!onlyCaptures)
            {
                int direction = board.Us == Piece.WHITE ? Directions[N] : Directions[S]; //White moves N, black moves S
                if (!isPinned || IsMovingAlongRay(direction, start, ourKingSquare))
                {
                    for (int n = 1; n <= (onStartingRank ? 2 : 1); n++)
                    {
                        var target = start + n * direction;
                        if (board.board[target] != Piece.NONE)
                            break;
                        if (InCheck && !IsRayCheckSquare(target))
                            continue;

                        if (promotion)
                        {
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.QUEEN));
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.ROOK));
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.BISHOP));
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.KNIGHT));
                        }
                        else
                        {
                            var move = new Move(start, target, board);
                            if (n == 2) //double pawn move, cannot happen in promotion!
                            {
                                move.DoublePawnMove();
                            }
                            moves.Add(move);
                        }
                    }
                }
            }

            //Pawn attack moves
            for(int i = 0; i < attackDirections.Length; i++)
            {
                var attackDirection = attackDirections[i];
                if (isPinned && !IsMovingAlongRay(Directions[attackDirection], start, ourKingSquare))
                    continue;

                if (NumSquaresToEdge[start][attackDirection] > 0)
                {
                    var target = start + Directions[attackDirection];
                    if (target == board.EnPassentSquare)
                    {
                        //TODO Extra care for enPassent legality (eg remove check obstructing pawn through enPassent)
                        //EnPassent
                        moves.Add(new Move(start, target, board).EnPassent());
                    }
                    else if ((Piece.COLOR_MASK & board.board[target]) == board.Them)
                    {
                        if (InCheck && !IsRayCheckSquare(target))
                            continue;

                        if (promotion)
                        {
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.QUEEN));
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.ROOK));
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.BISHOP));
                            moves.Add(new Move(start, target, board).PromoteTo(Piece.KNIGHT));
                        }
                        else
                        {
                            moves.Add(new Move(start, target, board));
                        }
                    }
                }
            }
        }

        private void AddKnightMoves(IList<Move> moves, int start, bool onlyCaptures)
        {
            if (IsPinned(start))
                return; //Pinned knights cannot move

            for(int i = 0; i < KnightMoves[start].Length; i++)
            {
                var target = KnightMoves[start][i];
                if (!InCheck || IsRayCheckSquare(target))
                {
                    //If InCheck, we might only move to a ray check square
                    var color = board.board[target] & Piece.COLOR_MASK;
                    if (color != board.Us)
                        if (!onlyCaptures || color == board.Them)
                            moves.Add(new Move(start, target, board));
                }
            }
        }

        private void AddKingMoves(IList<Move> moves, bool onlyCaptures)
        {
            for(int i = 0; i < KingMoves[ourKingSquare].Length; i++)
            {
                var target = KingMoves[ourKingSquare][i];
                var color = board.board[target] & Piece.COLOR_MASK;
                //Only add the move if the target is not a friendly piece, the target is not attacked and if it is a capture in onlyCaptures mode
                if(!(color == board.Us) && (opponentAttackMap & (1ul << target)) == 0 && (!onlyCaptures || color == board.Them))
                {
                    moves.Add(new Move(ourKingSquare, target, board));
                }
            }
        }

        private void AddCastelingMoves(IList<Move> moves, int start)
        {
            const int B1 = 1;
            const int C1 = 2;
            const int D1 = 3;
            const int E1 = 4;
            const int F1 = 5;
            const int G1 = 6;
            const int B8 = B1 + 7 * 8;
            const int C8 = C1 + 7 * 8;
            const int D8 = D1 + 7 * 8;
            const int E8 = E1 + 7 * 8;
            const int F8 = F1 + 7 * 8;
            const int G8 = G1 + 7 * 8;
            if (board.Us == Piece.WHITE)
            {
                if((board.CastelingRights & Board.WHITE_KINGSIDE_CASTLE) != 0)
                {
                    //White Kingside casteling allowed, hence the King is on e1.
                    //We need to check if f1 and g1 are free and not attacked
                    if(board.board[F1] == Piece.NONE && board.board[G1] == Piece.NONE && (opponentAttackMap & 1ul<<F1) == 0 && (opponentAttackMap & 1ul<<G1) == 0)
                    {
                        moves.Add(new Move(E1, G1, board).Casteling());
                    }
                }
                if((board.CastelingRights & Board.WHITE_QUEENSIDE_CASTLE) != 0)
                {
                    if (board.board[D1] == Piece.NONE && board.board[C1] == Piece.NONE && board.board[B1] == Piece.NONE && (opponentAttackMap & 1ul << D1) == 0 && (opponentAttackMap & 1ul << C1) == 0)
                    {
                        moves.Add(new Move(E1, C1, board).Casteling());
                    }
                }
            }
            else
            {
                if ((board.CastelingRights & Board.BLACK_KINGSIDE_CASTLE) != 0)
                {
                    if (board.board[F8] == Piece.NONE && board.board[G8] == Piece.NONE && (opponentAttackMap & 1ul << F8) == 0 && (opponentAttackMap & 1ul << G8) == 0)
                    {
                        moves.Add(new Move(E8, G8, board).Casteling());
                    }
                }
                if ((board.CastelingRights & Board.BLACK_QUEENSIDE_CASTLE) != 0)
                {
                    if (board.board[D8] == Piece.NONE && board.board[C8] == Piece.NONE && board.board[B8] == Piece.NONE && (opponentAttackMap & 1ul << D8) == 0 && (opponentAttackMap & 1ul << C8) == 0)
                    {
                        moves.Add(new Move(E8, C8, board).Casteling());
                    }
                }
            }
        }

        private void GenerateRayAttackMap()
        {
            var rooks = board.pieceList[board.Them | Piece.ROOK];
            for(int i = 0; i < rooks.Count; i++)
            {
                GenRayAttackMapForPiece(rooks[i], 0, 4);
            }

            var queens = board.pieceList[board.Them | Piece.QUEEN];
            for(int i = 0; i < queens.Count; i++)
            {
                GenRayAttackMapForPiece(queens[i], 0, 8);
            }

            var bishops = board.pieceList[board.Them | Piece.BISHOP];
            for(int i = 0; i < bishops.Count; i++)
            {
                GenRayAttackMapForPiece(bishops[i], 4, 8);
            }
        }

        void GenRayAttackMapForPiece(int start, int startDirIndex, int endDirIndex)
        {
            //Ray attacks can see through kings to help with determining king moves

            for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
            {
                for (int n = 1; n <= NumSquaresToEdge[start][directionIndex]; n++)
                {
                    int target = start + Directions[directionIndex] * n;
                    uint targetPiece = board.board[target];
                    opponentRayAttackMap |= 1ul << target;
                    if (target != ourKingSquare)
                    {
                        if (targetPiece != Piece.NONE)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void GenerateBitmasks()
        {
            GenerateRayAttackMap();

            int startDirIndex = 0, endDirIndex = 8;
            if(board.pieceList[Piece.QUEEN | board.Them].Count == 0)
            {
                startDirIndex = board.pieceList[Piece.ROOK | board.Them].Count > 0 ? 0 : 4;
                endDirIndex = board.pieceList[Piece.BISHOP | board.Them].Count > 0 ? 8 : 4;
            }

            for(int dirIndex = startDirIndex; dirIndex < endDirIndex; dirIndex++)
            {
                bool isDiagonal = dirIndex > 3;

                bool isFriendlyPieceAlongRay = false;

                ulong rayBitmask = 0;

                for (int n = 1; n <= NumSquaresToEdge[ourKingSquare][dirIndex]; n++)
                {
                    var target = ourKingSquare + Directions[dirIndex] * n;
                    rayBitmask |= 1ul << target;

                    var piece = board.board[target];

                    if(piece != Piece.NONE)
                    {
                        if((piece & Piece.COLOR_MASK) == board.Us)
                        {
                            //Friendly piece
                            if (!isFriendlyPieceAlongRay)
                                isFriendlyPieceAlongRay = true;
                            else
                                //Second friendly piece, no pins
                                break;
                        }
                        else
                        {
                            //enemy piece, lookup its attack direction
                            var type = Piece.PIECE_MASK & piece;
                            if((isDiagonal && (type == Piece.QUEEN || type == Piece.BISHOP)) || (!isDiagonal && (type == Piece.QUEEN || type == Piece.ROOK)))
                            {
                                //Can attack (excluding pawns for checks)
                                if (isFriendlyPieceAlongRay)
                                {
                                    //pin
                                    pinBitmask |= rayBitmask;
                                    PinsExist = true;
                                }
                                else
                                {
                                    //Check
                                    rayCheckBitmask |= rayBitmask;
                                    InDoubleCheck = InCheck;
                                    InCheck = true;
                                }
                            }
                            break;
                        }
                    }
                }

                if (InDoubleCheck)
                    break; //No need to search for pins, as only the king can move in double Check
            }

            //knight moves
            var knights = board.pieceList[Piece.KNIGHT | board.Them];
            for (int i = 0; i < knights.Count; i++)
            {
                var knightSquare = knights[i];
                opponentKnightAttackMap |= KnightBitboards[knightSquare];
                if (((1ul << ourKingSquare) & opponentKnightAttackMap) != 0)
                {
                    //Knight check
                    rayCheckBitmask |= 1ul << knightSquare;
                    InDoubleCheck = InCheck;
                    InCheck = true;
                }
            }

            var pawns = board.pieceList[Piece.PAWN | board.Them];
            for(int i = 0; i < pawns.Count; i++)
            {
                var pawnSquare = pawns[i];
                opponentPawnAttackMap |= PawnAttackBitboards[board.Them][pawnSquare];
                if (((1ul << ourKingSquare) & opponentPawnAttackMap) != 0)
                {
                    //Pawn check
                    rayCheckBitmask |= 1ul << pawnSquare;
                    InDoubleCheck = InCheck;
                    InCheck = true;
                }
            }

            opponentKingAttackMap = KingAttackBitboards[board.pieceList[board.Them | Piece.KING][0]];
            opponentAttackMap = opponentKingAttackMap | opponentKnightAttackMap | opponentRayAttackMap | opponentPawnAttackMap;
        }

        private bool IsPinned(int square)
        {
            return PinsExist && (pinBitmask & 1ul << square) != 0;
        }

        private bool IsRayCheckSquare(int square)
        {
            return (rayCheckBitmask & 1ul << square) != 0;
        }
    }
}
