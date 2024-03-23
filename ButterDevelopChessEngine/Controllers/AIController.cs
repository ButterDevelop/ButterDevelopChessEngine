using ButterDevelopChessEngine.Models;
using System.Numerics;

namespace ButterDevelopChessEngine.Controllers
{
    public class AIController
    {
        public static ulong[] ONE_SINGLE_BIT_MASK;

        static AIController()
        {
            ONE_SINGLE_BIT_MASK = new ulong[64];
            for (int i = 0; i < ONE_SINGLE_BIT_MASK.Length; i++) ONE_SINGLE_BIT_MASK[i] = (ulong)Math.Pow(2, i);
        }

        private static int AlphaBeta(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
        {
            if (depth == 0 || VerificationController.IsGameOver(board))
            {
                return EvaluationController.EvaluateBoard(board);
            }

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var move in GenerateAllPossibleMoves(board, true))
                {
                    board.MakeMove(move);
                    int eval = AlphaBeta(board, depth - 1, alpha, beta, false);
                    board.UnmakeMove();

                    maxEval = Math.Max(maxEval, eval);
                    alpha   = Math.Max(alpha,   eval);
                    if (beta <= alpha) break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in GenerateAllPossibleMoves(board, false))
                {
                    board.MakeMove(move);
                    int eval = AlphaBeta(board, depth - 1, alpha, beta, true);
                    board.UnmakeMove();

                    minEval = Math.Min(minEval, eval);
                    beta    = Math.Min(beta,    eval);
                    if (beta <= alpha) break;
                }
                return minEval;
            }
        }

        public static Move? BestMove(Board board, bool white, int depth)
        {
            Move? bestMove = null;
            int bestValue = white ? int.MinValue : int.MaxValue;

            var possibleMoves = GenerateAllPossibleMoves(board, white);
            foreach (var move in possibleMoves)
            {
                board.MakeMove(move);
                int value = AlphaBeta(board, depth - 1, int.MinValue, int.MaxValue, !white);
                board.UnmakeMove();

                if (white && value > bestValue || !white && value < bestValue)
                {
                    bestValue = value;
                    bestMove  = move;
                }
            }

            return bestMove;
        }

        private static ulong GenerateMoveBySinglePiece(Board board, ulong square, bool white, int piece, bool onlyAttack = false)
        {
            int color         = white ? Board.WHITE : Board.BLACK;
            int reversedColor = white ? Board.BLACK : Board.WHITE;
            ulong legalMaskNotPawn = ~board.WholeBitboards[color];
            switch (piece)
            {
                case Board.PAWNS:   return (onlyAttack ? 0 : 
                                               (
                                                    (white ? MoveMaskController.WhitePawnMove(square, board.WholeBitboards[Board.WHOLE]) : 
                                                             MoveMaskController.BlackPawnMove(square, board.WholeBitboards[Board.WHOLE])) 
                                                    &
                                                    (~board.WholeBitboards[Board.WHOLE])
                                               )
                                           ) |
                                           (
                                                (white ? MoveMaskController.WhitePawnAttack(square) : MoveMaskController.BlackPawnAttack(square))
                                                &
                                                (~board.WholeBitboards[color]) & board.WholeBitboards[reversedColor]
                                           );
                case Board.KNIGHTS: return legalMaskNotPawn & MoveMaskController.KnightsAttack(square);
                case Board.BISHOPS: return legalMaskNotPawn & MoveMaskController.BishopsAttack(square, board.WholeBitboards[color], board.WholeBitboards[reversedColor]);
                case Board.QUEENS:  return legalMaskNotPawn & MoveMaskController.QueensAttack( square, board.WholeBitboards[color], board.WholeBitboards[reversedColor]);
                case Board.ROOKS:   return legalMaskNotPawn & MoveMaskController.RooksAttack(  square, board.WholeBitboards[color], board.WholeBitboards[reversedColor]);
                case Board.KING:    return legalMaskNotPawn & MoveMaskController.KingMove(     square);
                default:            return 0;
            }
        }

        private static IEnumerable<ulong> IterateSingleBits(ulong bitboard)
        {
            while (bitboard != 0)
            {
                ulong lsb = bitboard & ~(bitboard - 1); // Получаем самый младший установленный бит
                int index = BitOperations.Log2(lsb); // Получаем индекс этого бита (0-63)
                yield return ONE_SINGLE_BIT_MASK[index];
                bitboard &= bitboard - 1; // Убираем обработанный бит
            }
        }

        private static bool TryMove(Board board, Move move, bool white)
        {
            bool result = false;

            board.MakeMove(move);
            if (!VerificationController.IsCheckFor(board, white)) result = true;
            board.UnmakeMove();

            return result;
        }

        private static List<Move> GenerateEnPassantMoves(Board board, bool white)
        {
            int color         = white ? Board.WHITE : Board.BLACK;
            var possibleMoves = new List<Move>();

            if (board.LastMove != null && board.LastMove.IsWhite != white && MoveMaskController.IsPawnDoublePushed(board.LastMove))
            {
                ulong moveTo = white ? (board.LastMove.To << 8) : (board.LastMove.To >> 8);

                ulong manyMovesTo = !white ? MoveMaskController.WhitePawnAttack(moveTo) : MoveMaskController.BlackPawnAttack(moveTo);
                manyMovesTo &= board.Bitboard(color, Board.PAWNS);

                foreach (ulong moveFrom in IterateSingleBits(manyMovesTo))
                {
                    var move = new Move
                    {
                        IsWhite        = white,
                        WhatPieceMoved = Board.PAWNS,
                        From           = moveFrom,
                        To             = moveTo,
                        IsPieceTaken   = true,
                        TakenPiece     = Board.PAWNS,
                        SpecialMove    = SpecialMove.EnPassant
                    };

                    if (TryMove(board, move, white)) possibleMoves.Add(move);
                }
            }

            return possibleMoves;
        }

        private static List<Move> GeneratePossibleMovesForPiece(Board board, bool white, int piece)
        {
            int color         = white ? Board.WHITE : Board.BLACK;
            int reversedColor = white ? Board.BLACK : Board.WHITE;
            var possibleMoves = new List<Move>();
        
            ulong bitboard = board.Bitboard(color, piece);
            foreach (ulong moveFrom in IterateSingleBits(bitboard))
            {
                ulong manyMovesTo = GenerateMoveBySinglePiece(board, moveFrom, white, piece);
        
                foreach (ulong moveTo in IterateSingleBits(manyMovesTo))
                {
                    bool isPieceTaken = VerificationController.IsThisSquareOccupied(moveTo, board.WholeBitboards[reversedColor]);
                    int  takenPiece   = isPieceTaken ? board.WhatPieceIsThatSquare(moveTo, reversedColor) : Board.UNKNOWN;
                    
                    var move = new Move
                    {
                        IsWhite        = white,
                        WhatPieceMoved = piece,
                        From           = moveFrom,
                        To             = moveTo,
                        IsPieceTaken   = isPieceTaken,
                        TakenPiece     = takenPiece,
                        SpecialMove    = SpecialMove.None
                    };

                    if (piece == Board.PAWNS && MoveMaskController.IsPawnReadyToPromote(move.To, white))
                    {
                        move.SpecialMove = SpecialMove.PromotePawn;
                        move.PromoteTo   = Board.QUEENS;
                        if (TryMove(board, move, white))
                        {
                            for (int promotePiece = Board.QUEENS; promotePiece > Board.PAWNS; promotePiece--)
                            {
                                var newMove = Move.Clone(move);
                                newMove.PromoteTo = promotePiece;
                                possibleMoves.Add(newMove);
                            }
                        }
                    }
                    else
                    {
                        if (TryMove(board, move, white)) possibleMoves.Add(move);
                    }
                }
            }

            return possibleMoves.OrderByDescending(m => m.IsPieceTaken).ToList();
        }

        public static IEnumerable<Move> GenerateAllPossibleMoves(Board board, bool white)
        {
            var moves =    GeneratePossibleMovesForPiece(board, white, Board.PAWNS);
            moves.AddRange(GenerateEnPassantMoves(board, white));
            moves.AddRange(GeneratePossibleMovesForPiece(board, white, Board.KNIGHTS));
            moves.AddRange(GeneratePossibleMovesForPiece(board, white, Board.BISHOPS));
            moves.AddRange(GeneratePossibleMovesForPiece(board, white, Board.QUEENS));
            moves.AddRange(GeneratePossibleMovesForPiece(board, white, Board.ROOKS));
            moves.AddRange(GeneratePossibleMovesForPiece(board, white, Board.KING));

            return moves;
        }

        internal static ulong GenerateMovesBitboard(Board board, bool white, bool onlyAttack)
        {
            int color = white ? Board.WHITE : Board.BLACK;

            ulong result = 0;
            for (int piece = 0; piece < Board.PIECES_GROUPS_COUNT; piece++)
            {
                ulong bitboard = board.Bitboard(color, piece);
                foreach (ulong moveFrom in IterateSingleBits(bitboard))
                {
                    ulong manyMovesTo = GenerateMoveBySinglePiece(board, moveFrom, white, piece, onlyAttack);

                    result |= manyMovesTo;
                }
            }

            return result;
        }
    }
}
