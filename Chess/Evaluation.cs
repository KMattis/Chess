using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Chess
{
    public static class Evaluation
    {
        public const int PawnValue = 100;
        public const int KnightValue = 325;
        public const int BishopValue = 325;
        public const int RookValue = 550;
        public const int QueenValue = 1000;

        public static int[] PawnPositionTable =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
            10, 10,-10,-30,-30,-10, 10, 10,
             0, 10, 10, 20, 20, 10, 10,  0,
             0,  0, 20, 30, 30, 20,  0,  0,
             5,  5,  5, 10, 10,  5,  5,  5,
            10, 10, 10, 10, 10, 10, 10, 10,
            30, 30, 30, 30, 30, 30, 30, 30,
             0,  0,  0,  0,  0,  0,  0,  0,
        };

        public static int[] RookPositionTable =
        {
             0,  0,  0, 20, 20,  0,  0,  0,
             0,  0,  0, 20, 20,  0,  0,  0,
             0,  0,  0, 20, 20,  0,  0,  0,
             0,  0,  0, 20, 20,  0,  0,  0,
             0,  0,  0, 20, 20,  0,  0,  0,
             0,  0,  0, 20, 20,  0,  0,  0,
            25, 25, 25, 25, 25, 25, 25, 25,
             0,  0,  0, 20, 20,  0,  0,  0,
        };

        public static int[] KnightPositionTable =
        {
            -20, -20, -10, -10, -10, -10, -20, -20,
            -10,  -5,   0,   0,   0,   0,  -5, -10,
            -10,   0,  20,  10,  10,  20,   0, -10,
            -10,   0,  10,  10,  10,  10,   0, -10,
            -10,   0,  10,  10,  10,  10,   0, -10,
            -10,   0,  20,  10,  10,  20,   0, -10,
            -10,  -5,   0,   0,   0,   0,  -5, -10,
            -20, -10, -10, -10, -10, -10, -10, -20,
        }; 
        
        public static int[] BishopPositionTable =
        {
            -20, -20, -20,   0,   0, -20, -20, -20,
            -10,   5,   0,   5,   5,   0,   5, -10,
             -5,   0,   5,  10,  10,   5,   0,  -5,
              0,   5,  10,  20,  20,  10,   5,   0,
              0,   5,  10,  20,  20,  10,   5,   0,
             -5,   0,   5,  10,  10,   5,   0,  -5,
            -10,  -5,   0,   5,   5,   0,  -5, -10,
            -20, -10,  -5,   0,   0,  -5, -10, -20,
        };

        public static int[] KingPositionTable = //King Safety
        {
             30,  20, -10, -30, -30,   0,  20,  30,
             10,  10, -80, -80, -80, -80,  10,  10,
            -80, -80, -80, -80, -80, -80, -80, -80,
            -80, -80, -80, -80, -80, -80, -80, -80,
            -80, -80, -80, -80, -80, -80, -80, -80,
            -80, -80, -80, -80, -80, -80, -80, -80,
            -80, -80, -80, -80, -80, -80, -80, -80,
            -80, -80, -80, -80, -80, -80, -80, -80,
        };

        public static int[] Mirror =
        {
            56, 57, 58, 59, 60, 61, 62, 63,
            48, 49, 50, 51, 52, 53, 54, 55,
            40, 41, 42, 43, 44, 45, 46, 47,
            32, 33, 34, 35, 36, 37, 38, 39,
            24, 25, 26, 27, 28, 29, 30, 31,
            16, 17, 18, 19, 20, 21, 22, 23,
             8,  9, 10, 11, 12, 13, 14, 15,
             0,  1,  2,  3,  4,  5,  6,  7,
        };

        public static int[][] PositionTables = new int[32][];
        public static int[] PieceValues = new int[32];
        public static int[] PieceValues_ColorIndependent = new int[32];

        public static int BishopPairScore = 35;

        //[isOpenSelf][isOpenOpponent] (0 = isOpen, 1 = isClosed)
        public static int[][] RookFileScores = { new int[]{ 25, 15 }, new int[]{ 5, -10 } };
        public static int DoubledRookBonus = 30;

        public static int IsolatedPawnBonus = -20;
        public static int[] PassedPawnBonus = new int[] { 0, 22, 33, 44, 55, 77, 88, 0 }; //Per rank

        public static int BishopClosednessPenalty = -1; //-1 per pawn

        public static int PawnShieldBonus = 40;

        public static IDictionary<ulong, int> MaterialDatabase = new Dictionary<ulong, int>(); //TODO: Clear database regularily?

        static Evaluation()
        {
            //Setup piece Values
            PieceValues[Piece.WHITE | Piece.QUEEN ] =   QueenValue;
            PieceValues[Piece.WHITE | Piece.ROOK  ] =    RookValue;
            PieceValues[Piece.WHITE | Piece.KNIGHT] =  KnightValue;
            PieceValues[Piece.WHITE | Piece.BISHOP] =  BishopValue;
            PieceValues[Piece.WHITE | Piece.PAWN  ] =    PawnValue;
            PieceValues[Piece.BLACK | Piece.QUEEN ] = - QueenValue;
            PieceValues[Piece.BLACK | Piece.ROOK  ] = -  RookValue;
            PieceValues[Piece.BLACK | Piece.KNIGHT] = -KnightValue;
            PieceValues[Piece.BLACK | Piece.BISHOP] = -BishopValue;
            PieceValues[Piece.BLACK | Piece.PAWN  ] = -  PawnValue;

            foreach (var pieceType in Piece.PIECE_TYPES)
            {
                PieceValues_ColorIndependent[pieceType] = Math.Abs(PieceValues[pieceType]);
                PieceValues_ColorIndependent[pieceType & Piece.PIECE_MASK] = PieceValues_ColorIndependent[pieceType];
            }
            //Setup position tables
            foreach (var pieceType in Piece.PIECE_TYPES)
            {
                PositionTables[pieceType] = new int[64];
                var color = Piece.COLOR_MASK & pieceType;
                for(int i = 0; i < 64; i++)
                {
                    var square = color == Piece.WHITE ? i : Mirror[i];
                    switch (pieceType & Piece.PIECE_MASK)
                    {
                        case Piece.PAWN:
                            PositionTables[pieceType][i] = PawnPositionTable[square];
                            break;
                        case Piece.ROOK:
                            PositionTables[pieceType][i] = RookPositionTable[square];
                            break;
                        case Piece.KNIGHT:
                            PositionTables[pieceType][i] = KnightPositionTable[square];
                            break;
                        case Piece.BISHOP:
                            PositionTables[pieceType][i] = BishopPositionTable[square];
                            break;
                        case Piece.KING:
                            PositionTables[pieceType][i] = KingPositionTable[square];
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static int ProbeMaterial(Board board)
        {
            if (MaterialDatabase.ContainsKey(board.materialKey))
                return MaterialDatabase[board.materialKey];

            //Basic material counting
            var score = CountMaterial(board);

            var whitePawnsCount = board.pieceList[Piece.WHITE | Piece.PAWN].Count;
            var blackPawnsCount = board.pieceList[Piece.BLACK | Piece.PAWN].Count;

            //Bishop pair bonus
            if (board.pieceList[Piece.WHITE | Piece.BISHOP].Count >= 2)
                score += BishopPairScore;
            if (board.pieceList[Piece.BLACK | Piece.BISHOP].Count >= 2)
                score -= BishopPairScore;

            //Bishop Closedness Penalty
            score += board.pieceList[Piece.WHITE | Piece.BISHOP].Count * (whitePawnsCount * 2 + blackPawnsCount) * BishopClosednessPenalty;
            score -= board.pieceList[Piece.BLACK | Piece.BISHOP].Count * (blackPawnsCount * 2 + whitePawnsCount) * BishopClosednessPenalty;

            MaterialDatabase.Add(board.materialKey, score);
            return score;
        }

        public static int Evaluate(Board board)
        {
            var preference = board.Us == Piece.WHITE ? 1 : -1;

            var whitePawns = board.pieceList[Piece.WHITE | Piece.PAWN];
            var blackPawns = board.pieceList[Piece.BLACK | Piece.PAWN];

            var score = ProbeMaterial(board);

            var positionValues = new int[] { 0, 0 };

            for(int pieceTypeIndex = 0; pieceTypeIndex < Piece.PIECE_TYPES.Length; pieceTypeIndex++)
            {
                var pieceType = Piece.PIECE_TYPES[pieceTypeIndex];
                var color = Piece.COLOR_MASK & pieceType;
                var colorBit = color == Piece.WHITE ? 0 : 1;

                for(int pieceIndex = 0; pieceIndex < board.pieceList[pieceType].Count; pieceIndex++)
                {
                    positionValues[colorBit] += PositionTables[pieceType][board.pieceList[pieceType][pieceIndex]];
                }
            }

            score += positionValues[0] - positionValues[1];

            //Pawn Scores
            int whitePawnScore = 0;
            int blackPawnScore = 0;
            for (int i = 0; i < whitePawns.Count; i++)
            {
                var pawnSquare = whitePawns[i];
                var pawnFile = pawnSquare % 8;

                if ((MoveHelper.IsolatedPawnBitboards[pawnFile] & board.WhitePawnBitboard) == 0)
                    //Isolated pawn
                    whitePawnScore += IsolatedPawnBonus;
                if ((MoveHelper.PassedPawnBitboards[pawnFile] & board.BlackPawnBitboard) == 0)
                    //Passed pawn
                    whitePawnScore += PassedPawnBonus[pawnSquare / 8];
            }
            for (int i = 0; i < blackPawns.Count; i++)
            {
                var pawnSquare = blackPawns[i];
                var pawnFile = pawnSquare % 8;

                if ((MoveHelper.IsolatedPawnBitboards[pawnFile] & board.BlackPawnBitboard) == 0)
                    //Isolated pawn
                    blackPawnScore += IsolatedPawnBonus;
                if ((MoveHelper.PassedPawnBitboards[pawnFile] & board.WhitePawnBitboard) == 0)
                    //Passed pawn
                    blackPawnScore += PassedPawnBonus[7 - pawnSquare / 8];
            }
            score += whitePawnScore - blackPawnScore;

            //Rook-Pawn Scores
            int whiteRookPawnScore = 0;
            int blackRookPawnScore = 0;
            int f0 = -1;
            for(int i = 0; i < board.pieceList[Piece.WHITE | Piece.ROOK].Count; i++)
            {
                var rookSquare = board.pieceList[Piece.WHITE | Piece.ROOK][i];
                var rookFile = rookSquare % 8;
                if (rookFile == f0) {
                    whiteRookPawnScore += DoubledRookBonus;
                    //Does not work if there are more than 2 rooks
                }
                f0 = rookFile;
                
                bool isOpenWhite = (MoveHelper.FileBitboards[rookFile] & board.WhitePawnBitboard) == 0;
                bool isOpenBlack = (MoveHelper.FileBitboards[rookFile] & board.BlackPawnBitboard) == 0;

                whiteRookPawnScore += RookFileScores[isOpenWhite ? 0 : 1][isOpenBlack ? 0 : 1];
            }
            f0 = -1;
            for (int i = 0; i < board.pieceList[Piece.BLACK | Piece.ROOK].Count; i++)
            {
                var rookSquare = board.pieceList[Piece.BLACK | Piece.ROOK][i];
                var rookFile = rookSquare % 8;
                if (rookFile == f0)
                {
                    blackRookPawnScore += DoubledRookBonus;
                    //Does not work if there are more than 2 rooks
                }
                f0 = rookFile;

                bool isOpenWhite = (MoveHelper.FileBitboards[rookFile] & board.WhitePawnBitboard) == 0;
                bool isOpenBlack = (MoveHelper.FileBitboards[rookFile] & board.BlackPawnBitboard) == 0;

                blackRookPawnScore += RookFileScores[isOpenBlack ? 0 : 1][isOpenWhite ? 0 : 1];
            }

            score += whiteRookPawnScore - blackRookPawnScore;

            var whiteKingSquare = board.pieceList[Piece.WHITE | Piece.KING][0];
            var blackKingSquare = board.pieceList[Piece.BLACK | Piece.KING][0];

            var whiteKingOnFlank = whiteKingSquare % 8 <= 2 || whiteKingSquare % 8 >= 6;
            var blackKingOnFlank = blackKingSquare % 8 <= 2 || blackKingSquare % 8 >= 6;

            var whiteKingPawnShield = whiteKingOnFlank ? BitOperations.PopCount(board.WhitePawnBitboard & MoveHelper.KingAttackBitboards[whiteKingSquare]) : 0;
            var blackKingPawnShield = blackKingOnFlank ? BitOperations.PopCount(board.BlackPawnBitboard & MoveHelper.KingAttackBitboards[blackKingSquare]) : 0;

            score += (whiteKingPawnShield - blackKingPawnShield) * PawnShieldBonus;

            return preference * score;
        }

        public static int CountMaterial(Board board)
        {
            var count = 0;
            for(int pieceTypeIndex = 0; pieceTypeIndex < Piece.PIECE_TYPES.Length; pieceTypeIndex++)
            {
                var pieceType = Piece.PIECE_TYPES[pieceTypeIndex];
                count += PieceValues[pieceType] * board.pieceList[pieceType].Count;
            }
            return count;
        }
    }
}
