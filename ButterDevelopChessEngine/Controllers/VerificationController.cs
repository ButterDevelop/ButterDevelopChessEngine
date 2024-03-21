using ButterDevelopChessEngine.Models;

namespace ButterDevelopChessEngine.Controllers
{
    public class VerificationController
    {
        public static bool IsGameOver(Board board)
        {
            return IsCheckmateFor(board, white: false) || IsCheckmateFor(board, white: true) || IsStalemate(board);
        }

        public static bool IsCheckFor(Board board, bool white)
        {
            int color = white ? Board.WHITE : Board.BLACK;

            return (board.Bitboard(color, Board.KING) & AIController.GenerateMovesBitboard(board, !white, onlyAttack: true)) != 0;
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

        public static bool IsStalemate(Board board)
        {
            return AIController.GenerateAllPossibleMoves(board, white: true).Count()  == 0 ||
                   AIController.GenerateAllPossibleMoves(board, white: false).Count() == 0;
        }

        public static bool IsThisSquareOccupied(ulong square, ulong bitboard)
        {
            return (square & bitboard) != 0;
        }
    }
}
