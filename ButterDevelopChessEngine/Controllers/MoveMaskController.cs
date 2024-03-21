using ButterDevelopChessEngine.Models;
using System.Diagnostics;

namespace ButterDevelopChessEngine.Controllers
{
    internal class MoveMaskController
    {
        private const ulong WHITE_PAWN_DOUBLE_PUSH_MASK         = 0xff00,
                            BLACK_PAWN_DOUBLE_PUSH_MASK         = 0xff000000000000,
                            WHITE_PAWN_DOUBLE_PUSHED_MASK       = WHITE_PAWN_DOUBLE_PUSH_MASK << 16,
                            BLACK_PAWN_DOUBLE_PUSHED_MASK       = BLACK_PAWN_DOUBLE_PUSH_MASK >> 16,
                            NO_LEFT_VERTICAL_MASK               = 0x7f7f7f7f7f7f7f7f,
                            NO_RIGHT_VERTICAL_MASK              = 0xfefefefefefefefe,
                            NO_UPPER_HORIZONTAL_MASK            = 0xffffffffffffff,
                            NO_LOWER_HORIZONTAL_MASK            = 0xffffffffffffff00,
                            NO_LEFT_UPPER_LINE_MASK             = NO_LEFT_VERTICAL_MASK  & NO_UPPER_HORIZONTAL_MASK,
                            NO_RIGHT_LOWER_LINE_MASK            = NO_RIGHT_VERTICAL_MASK & NO_LOWER_HORIZONTAL_MASK,
                            NO_RIGHT_UPPER_LINE_MASK            = NO_RIGHT_VERTICAL_MASK & NO_UPPER_HORIZONTAL_MASK,
                            NO_LEFT_LOWER_LINE_MASK             = NO_LEFT_VERTICAL_MASK  & NO_LOWER_HORIZONTAL_MASK,
                            KNIGHTS_NO_TWO_LEFT_VERTICALS_MASK  = 0x3f3f3f3f3f3f3f3f,
                            KNIGHTS_NO_TWO_RIGHT_VERTICALS_MASK = 0xfcfcfcfcfcfcfcfc,
                            FIRST_VERTICAL_LINE_MASK            = 0x101010101010101,
                            FIRST_HORIZONTAL_LINE_MASK          = 0xff,
                            LAST_VERTICAL_LINE_MASK             = 0x8080808080808080,
                            LAST_HORIZONTAL_LINE_MASK           = 0xff00000000000000,
                            BISHOP_BORDER_LEFT_UP_MASK          = LAST_VERTICAL_LINE_MASK  | LAST_HORIZONTAL_LINE_MASK,
                            BISHOP_BORDER_RIGHT_UP_MASK         = FIRST_VERTICAL_LINE_MASK | LAST_HORIZONTAL_LINE_MASK,
                            BISHOP_BORDER_LEFT_DOWN_MASK        = LAST_VERTICAL_LINE_MASK  | FIRST_HORIZONTAL_LINE_MASK,
                            BISHOP_BORDER_RIGHT_DOWN_MASK       = FIRST_VERTICAL_LINE_MASK | FIRST_HORIZONTAL_LINE_MASK;

        internal static bool IsPawnReadyToPromote(ulong moveTo, bool white)
        {
            return white ? ((moveTo & LAST_HORIZONTAL_LINE_MASK) != 0) : ((moveTo & FIRST_HORIZONTAL_LINE_MASK) != 0);
        }
        internal static bool IsPawnDoublePushed(Move move)
        {
            return  move.WhatPieceMoved == Board.PAWNS && 
                   (move.From & (move.IsWhite ? WHITE_PAWN_DOUBLE_PUSH_MASK   : BLACK_PAWN_DOUBLE_PUSH_MASK))   != 0 && 
                   (move.To   & (move.IsWhite ? WHITE_PAWN_DOUBLE_PUSHED_MASK : BLACK_PAWN_DOUBLE_PUSHED_MASK)) != 0;
        }

        internal static ulong WhitePawnMove(ulong square, ulong bitboard)
        {
            return (square << 8) | (
                                    (((square << 8) & bitboard) == 0 && ((square << 16) & bitboard) == 0) 
                                    ? ((square & WHITE_PAWN_DOUBLE_PUSH_MASK) << 16) : 0
                                   );
        }
        internal static ulong WhitePawnAttack(ulong square)
        {
            return ((square & NO_RIGHT_UPPER_LINE_MASK) << 7) | ((square & NO_LEFT_UPPER_LINE_MASK) << 9);
        }

        internal static ulong BlackPawnMove(ulong square, ulong bitboard)
        {
            return (square >> 8) | (
                                    (((square >> 8) & bitboard) == 0 && ((square >> 16) & bitboard) == 0)
                                    ? ((square & BLACK_PAWN_DOUBLE_PUSH_MASK) >> 16) : 0
                                   );
        }
        internal static ulong BlackPawnAttack(ulong square)
        {
            return ((square & NO_LEFT_LOWER_LINE_MASK) >> 7) | ((square & NO_RIGHT_LOWER_LINE_MASK) >> 9);
        }

        internal static ulong KingMove(ulong square)
        {
            return ((square & NO_LEFT_VERTICAL_MASK)    << 1) | ((square & NO_RIGHT_VERTICAL_MASK)   >> 1) |
                   ((square & NO_UPPER_HORIZONTAL_MASK) << 8) | ((square & NO_LOWER_HORIZONTAL_MASK) >> 8) |
                   ((square & NO_LEFT_UPPER_LINE_MASK)  << 9) | ((square & NO_RIGHT_LOWER_LINE_MASK) >> 9) |
                   ((square & NO_RIGHT_UPPER_LINE_MASK) << 7) | ((square & NO_LEFT_LOWER_LINE_MASK)  >> 7);
        }

        internal static ulong KnightsAttack(ulong square)
        {
            return KNIGHTS_NO_TWO_LEFT_VERTICALS_MASK  & (square << 6  | square >> 10) |
                   NO_LEFT_VERTICAL_MASK               & (square << 15 | square >> 17) |
                   NO_RIGHT_VERTICAL_MASK              & (square << 17 | square >> 15) |
                   KNIGHTS_NO_TWO_RIGHT_VERTICALS_MASK & (square << 10 | square >> 6);
        }

        internal static ulong BishopsAttack(ulong square, ulong playerBitboard, ulong opponentBitboard)
        {
            ulong resultMoves = 0;

            for (ulong move = square << 9; (square & BISHOP_BORDER_LEFT_UP_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move <<= 9) // Moving to left + up
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & BISHOP_BORDER_LEFT_UP_MASK) != 0) break;
            }
            for (ulong move = square >> 9; (square & BISHOP_BORDER_RIGHT_DOWN_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move >>= 9) // Moving to right + down
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & BISHOP_BORDER_RIGHT_DOWN_MASK) != 0) break;
            }
            for (ulong move = square << 7; (square & BISHOP_BORDER_RIGHT_UP_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move <<= 7) // Moving to right + up
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & BISHOP_BORDER_RIGHT_UP_MASK) != 0) break;
            }
            for (ulong move = square >> 7; (square & BISHOP_BORDER_LEFT_DOWN_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move >>= 7) // Moving to left + down
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & BISHOP_BORDER_LEFT_DOWN_MASK) != 0) break;
            }

            return resultMoves;
        }

        internal static ulong RooksAttack(ulong square, ulong playerBitboard, ulong opponentBitboard)
        {
            ulong resultMoves = 0;

            for (ulong move = square << 1; (square & LAST_VERTICAL_LINE_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move <<= 1) // Moving to left
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & LAST_VERTICAL_LINE_MASK) != 0) break;
            }
            for (ulong move = square >> 1; (square & FIRST_VERTICAL_LINE_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move >>= 1) // Moving to right
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & FIRST_VERTICAL_LINE_MASK) != 0) break;
            }
            for (ulong move = square << 8; (square & LAST_HORIZONTAL_LINE_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move <<= 8) // Moving to up
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & LAST_HORIZONTAL_LINE_MASK) != 0) break;
            }
            for (ulong move = square >> 8; (square & FIRST_HORIZONTAL_LINE_MASK) == 0 && 
                                            move > 0 && 
                                           (move & playerBitboard) == 0; move >>= 8) // Moving to down
            {
                resultMoves |= move;
                if ((move & opponentBitboard) != 0 || (move & FIRST_HORIZONTAL_LINE_MASK) != 0) break;
            }

            return resultMoves;
        }

        internal static ulong QueensAttack(ulong bitboard, ulong playerBitboard, ulong opponentBitboard)
        {
            return BishopsAttack(bitboard, playerBitboard, opponentBitboard) | RooksAttack(bitboard, playerBitboard, opponentBitboard);
        }
    }
}
