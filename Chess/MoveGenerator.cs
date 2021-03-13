using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        private ulong freeSquares;
        private ulong enemyPieces;
        private ulong friendlyPieces;

        private int ourKingSquare;

        public bool InCheck;
        public bool InDoubleCheck;
        public bool PinsExist;

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

            AddPawnMoves(moves, onlyCaptures);

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
        private bool IsMovingAlongRay(int rayDir, int startSquare, int targetSquare)
        {
            int moveDir = DirectionLookup[targetSquare - startSquare + 63];
            return (rayDir == moveDir || -rayDir == moveDir);
        }

        private void AddPawnMoves(IList<Move> moves, bool onlyCaptures)
        {
            var pawnDirection = board.Us == Piece.WHITE ? Directions[N] : Directions[S];
            var promotionRank = board.Us == Piece.WHITE ? 7 : 0;

            //one square moves
            var ourPawnsBitboard = board.Us == Piece.WHITE ? board.WhitePawnBitboard : board.BlackPawnBitboard;

            var pinnedPawns = pinBitmask & ourPawnsBitboard;

            if (!onlyCaptures)
            {
                var pseudoLegalSinglePawnMoveTargets = board.Us == Piece.WHITE ? BitboardUtilities.ShiftUp(ourPawnsBitboard) : BitboardUtilities.ShiftDown(ourPawnsBitboard);
                pseudoLegalSinglePawnMoveTargets &= freeSquares;
                var legalSinglePawnMoveTarget = pseudoLegalSinglePawnMoveTargets & rayCheckBitmask;

                while (legalSinglePawnMoveTarget != 0)
                {
                    var targetSquare = BitOperations.TrailingZeroCount(legalSinglePawnMoveTarget);
                    var startSquare = targetSquare - pawnDirection;
                    legalSinglePawnMoveTarget ^= 1ul << targetSquare;
                    if ((pinnedPawns & (1ul << startSquare)) != 0 && !IsMovingAlongRay(pawnDirection, startSquare, ourKingSquare))
                    {
                        continue;
                    }
                    if (targetSquare / 8 == promotionRank)
                    {
                        moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.QUEEN));
                        moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.ROOK));
                        moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.BISHOP));
                        moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.KNIGHT));
                    }
                    else
                    {
                        moves.Add(new Move(startSquare, targetSquare, board));
                    }
                }

                var doublePawnMoveTarget = board.Us == Piece.WHITE ? BitboardUtilities.ShiftUp(pseudoLegalSinglePawnMoveTargets) : BitboardUtilities.ShiftDown(pseudoLegalSinglePawnMoveTargets);
                doublePawnMoveTarget &= RankBitboards[board.Us == Piece.WHITE ? 3 : 4] & freeSquares & rayCheckBitmask;

                while (doublePawnMoveTarget != 0)
                {
                    var targetSquare = BitOperations.TrailingZeroCount(doublePawnMoveTarget);
                    var startSquare = targetSquare - 2 * pawnDirection;

                    doublePawnMoveTarget ^= 1ul << targetSquare;

                    if ((pinnedPawns & (1ul << startSquare)) != 0)
                    {
                        if (!IsMovingAlongRay(pawnDirection, startSquare, ourKingSquare))
                        {
                            continue;
                        }
                    }
                    moves.Add(new Move(startSquare, targetSquare, board).DoublePawnMove());
                }
            }

            //Captures
            var upWest = Directions[board.Us == Piece.WHITE ? NW : SW];
            var upEast = Directions[board.Us == Piece.WHITE ? NE : SE];
            var enemyPiecesAndEnPassent = board.EnPassentSquare >= 0 ? enemyPieces | (1ul << board.EnPassentSquare) : enemyPieces;
            var captureUpWestTargets = board.Us == Piece.WHITE ? BitboardUtilities.ShiftUpLeft(ourPawnsBitboard) : BitboardUtilities.ShiftDownLeft(ourPawnsBitboard);
            captureUpWestTargets &= enemyPiecesAndEnPassent & rayCheckBitmask;
            var captureUpEastTargets = board.Us == Piece.WHITE ? BitboardUtilities.ShiftUpRight(ourPawnsBitboard) : BitboardUtilities.ShiftDownRight(ourPawnsBitboard);
            captureUpEastTargets &= enemyPiecesAndEnPassent & rayCheckBitmask;

            while(captureUpWestTargets != 0)
            {
                var targetSquare = BitOperations.TrailingZeroCount(captureUpWestTargets);
                var startSquare = targetSquare - upWest;
                captureUpWestTargets ^= 1ul << targetSquare;
                if ((pinnedPawns & (1ul << startSquare)) != 0 && !IsMovingAlongRay(upWest, startSquare, ourKingSquare))
                {
                    continue;
                }
                if (targetSquare / 8 == promotionRank)
                {
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.QUEEN));
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.ROOK));
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.BISHOP));
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.KNIGHT));
                }
                else
                {
                    var move = new Move(startSquare, targetSquare, board);
                    if (targetSquare == board.EnPassentSquare)
                        move.EnPassent();
                    moves.Add(move);
                }
            }

            while (captureUpEastTargets != 0)
            {
                var targetSquare = BitOperations.TrailingZeroCount(captureUpEastTargets);
                var startSquare = targetSquare - upEast;
                captureUpEastTargets ^= 1ul << targetSquare;
                if ((pinnedPawns & (1ul << startSquare)) != 0 && !IsMovingAlongRay(upEast, startSquare, ourKingSquare))
                {
                    continue;
                }
                if (targetSquare / 8 == promotionRank)
                {
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.QUEEN));
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.ROOK));
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.BISHOP));
                    moves.Add(new Move(startSquare, targetSquare, board).PromoteTo(Piece.KNIGHT));
                }
                else
                {
                    var move = new Move(startSquare, targetSquare, board);
                    if (targetSquare == board.EnPassentSquare)
                        move.EnPassent();
                    moves.Add(move);
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
            //targets are king moves which are not under a attack and not a friendly piece
            var targets = KingAttackBitboards[ourKingSquare] & ~friendlyPieces & ~opponentAttackMap;
            while(targets != 0)
            {
                var target = BitOperations.TrailingZeroCount(targets);
                targets ^= 1ul << target;
                moves.Add(new Move(ourKingSquare, target, board));
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
                    opponentRayAttackMap |= 1ul << target;
                    if (target != ourKingSquare)
                    {
                        if (board.board[target] != Piece.NONE)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void GenerateBitmasks()
        {
            enemyPieces = board.Us == Piece.WHITE ? board.BlackPawnBitboard | board.BlackPiecesBitboard : board.WhitePawnBitboard | board.WhitePiecesBitboard;
            friendlyPieces = board.Us == Piece.WHITE ? board.WhitePawnBitboard | board.WhitePiecesBitboard : board.BlackPawnBitboard | board.BlackPiecesBitboard;
            freeSquares = ~(enemyPieces | friendlyPieces);

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

            if (!InCheck)
                rayCheckBitmask = ~0ul; //Every square "blocks the checks"

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
