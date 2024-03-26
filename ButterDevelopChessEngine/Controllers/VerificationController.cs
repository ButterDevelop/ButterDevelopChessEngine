using ButterDevelopChessEngine.Models;

namespace ButterDevelopChessEngine.Controllers
{
    public class VerificationController
    {
        private const ulong DEFAULT_WHITE_SHORT_CASTLE_MASK_CHECK = 0x6,
                            DEFAULT_BLACK_SHORT_CASTLE_MASK_CHECK = 0x6000000000000000,
                            DEFAULT_WHITE_LONG_CASTLE_MASK_CHECK  = 0x70,
                            DEFAULT_BLACK_LONG_CASTLE_MASK_CHECK  = 0x7000000000000000;

        public static bool IsGameOver(Board board)
        {
            return IsCheckmateFor(board, white: false) || IsCheckmateFor(board, white: true) || 
                   IsStalemateFor(board, white: false) || IsStalemateFor(board, white: true);
        }

        public static bool IsCheckFor(Board board, bool white)
        {
            int color = white ? Board.WHITE : Board.BLACK;

            return IsThisSquareOccupied(board.Bitboard(color, Board.KING), AIController.GenerateMovesBitboard(board, !white, onlyAttack: true));
        }

        public static bool IsCheckmateFor(Board board, bool white)
        {
            if (!IsCheckFor(board, white)) return false;

            bool checkmate = true;
            foreach (var move in AIController.GenerateAllPossibleMoves(board, white))
            {
                board.MakeMove(move);
                checkmate = IsCheckFor(board, white);
                board.UnmakeMove();

                if (!checkmate) break;
            }

            return checkmate;
        }

        public static bool IsStalemateFor(Board board, bool white)
        {
            return AIController.GenerateAllPossibleMoves(board, white).Count()  == 0;
        }
         
        internal static bool IsThisSquareOccupied(ulong square, ulong bitboard)
        {
            return (square & bitboard) != 0;
        }

        internal static bool IsCastleAvailableByEmptySquares(ulong attackMovesBitboard, ulong figuresBitboard, bool white, bool shortCastle)
        {
            ulong checkMask = white ? 
                              (shortCastle ? DEFAULT_WHITE_SHORT_CASTLE_MASK_CHECK : DEFAULT_WHITE_LONG_CASTLE_MASK_CHECK) : 
                              (shortCastle ? DEFAULT_BLACK_SHORT_CASTLE_MASK_CHECK : DEFAULT_BLACK_LONG_CASTLE_MASK_CHECK);

            return !IsThisSquareOccupied(checkMask, figuresBitboard | attackMovesBitboard);
        }
    }
}
