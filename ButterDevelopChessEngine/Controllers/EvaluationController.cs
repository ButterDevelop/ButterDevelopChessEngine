using ButterDevelopChessEngine.Models;

namespace ButterDevelopChessEngine.Controllers
{
    internal class EvaluationController
    {
        private const int PAWN_WEIGHT   = 1,
                          KNIGHT_WEIGHT = 3,
                          BISHOP_WEIGHT = 3,
                          ROOK_WEIGHT   = 5,
                          QUEEN_WEIGHT  = 9;

        private static int CountBits(ulong bitboard)
        {
            int result = 0;
            while (bitboard != 0)
            {
                ++result;
                bitboard &= bitboard - 1; // Убираем обработанный бит
            }

            return result;
        }

        internal static int EvaluateBoard(Board board)
        {
            if (VerificationController.IsStalemate(board)) return 0;
            if (VerificationController.IsCheckmateFor(board, white: true))  return int.MinValue;
            if (VerificationController.IsCheckmateFor(board, white: false)) return int.MaxValue;

            int eval = 0;

            eval += PAWN_WEIGHT   * CountBits(board.Bitboard(Board.WHITE, Board.PAWNS));
            eval += KNIGHT_WEIGHT * CountBits(board.Bitboard(Board.WHITE, Board.KNIGHTS));
            eval += BISHOP_WEIGHT * CountBits(board.Bitboard(Board.WHITE, Board.BISHOPS));
            eval += ROOK_WEIGHT   * CountBits(board.Bitboard(Board.WHITE, Board.ROOKS));
            eval += QUEEN_WEIGHT  * CountBits(board.Bitboard(Board.WHITE, Board.QUEENS));

            eval -= PAWN_WEIGHT   * CountBits(board.Bitboard(Board.BLACK, Board.PAWNS));
            eval -= KNIGHT_WEIGHT * CountBits(board.Bitboard(Board.BLACK, Board.KNIGHTS));
            eval -= BISHOP_WEIGHT * CountBits(board.Bitboard(Board.BLACK, Board.BISHOPS));
            eval -= ROOK_WEIGHT   * CountBits(board.Bitboard(Board.BLACK, Board.ROOKS));
            eval -= QUEEN_WEIGHT  * CountBits(board.Bitboard(Board.BLACK, Board.QUEENS));

            return eval;
        }
    }
}
