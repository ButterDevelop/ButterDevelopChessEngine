using ButterDevelopChessEngine.Controllers;
using ButterDevelopChessEngine.Models;
using System.Numerics;
using System.Text;

namespace GUI
{
    public partial class MainForm : Form
    {
        private static readonly Color DEFAULT_TRANSPARENT_COLOR   = Color.Transparent,
                                      DEFAULT_CHOSEN_SQUARE_COLOR = Color.Orange,
                                      DEFAULT_POSSIBLE_MOVE_COLOR = Color.FromArgb(128, 128, 128, 192),
                                      DEFAULT_LAST_MOVE_COLOR     = Color.FromArgb(100, 200, 200, 30);

        private const int AI_DEPTH = 5;

        private bool _playerTurn, _isPlayerPlayingWhite;
        private Board _board;
        private PictureBox? _chosenPiece;
        private Stack<Move> _unmadeMoves;

        private readonly object lockObject;

        public MainForm()
        {
            InitializeComponent();

            _playerTurn = true;
            _board = new Board();
            _unmadeMoves = new Stack<Move>();

            _isPlayerPlayingWhite = true;

            lockObject = new object();

            foreach (var pictureBox in IterateAllPieces())
            {
                pictureBox.MouseDown += PictureBox_MouseDown;
            }

            buttonRestart_Click(this, new EventArgs());
        }

        private void RefreshGameField()
        {
            foreach (var pictureBox in IterateAllPieces())
            {
                int squareIndex = int.Parse(pictureBox.Tag.ToString() ?? "-1");
                if (squareIndex < 0) continue;

                bool white = true;
                ulong square = BitCalculations.GetNumberByBit(squareIndex);
                int piece = _board.WhatPieceIsThatSquare(square, Board.WHITE);
                if (piece == Board.UNKNOWN)
                {
                    white = false;
                    piece = _board.WhatPieceIsThatSquare(square, Board.BLACK);
                }

                lock (lockObject)
                {
                    switch (piece)
                    {
                        case Board.PAWNS:
                            pictureBox.Image = white ? new Bitmap(Properties.Resources.white_pawn)
                                                                     : new Bitmap(Properties.Resources.black_pawn);
                            break;
                        case Board.ROOKS:
                            pictureBox.Image = white ? new Bitmap(Properties.Resources.white_rook)
                                                                     : new Bitmap(Properties.Resources.black_rook);
                            break;
                        case Board.KNIGHTS:
                            pictureBox.Image = white ? new Bitmap(Properties.Resources.white_knight)
                                                                     : new Bitmap(Properties.Resources.black_knight);
                            break;
                        case Board.BISHOPS:
                            pictureBox.Image = white ? new Bitmap(Properties.Resources.white_bishop)
                                                                     : new Bitmap(Properties.Resources.black_bishop);
                            break;
                        case Board.QUEENS:
                            pictureBox.Image = white ? new Bitmap(Properties.Resources.white_queen)
                                                                     : new Bitmap(Properties.Resources.black_queen);
                            break;
                        case Board.KING:
                            pictureBox.Image = white ? new Bitmap(Properties.Resources.white_king)
                                                                     : new Bitmap(Properties.Resources.black_king);
                            break;
                        default:
                            pictureBox.Image?.Dispose();
                            pictureBox.Image = null;
                            break;
                    }
                }
            }

            lock (lockObject)
            {
                string text = BitboardToText(_board.Bitboard(Board.WHITE, Board.PAWNS));
                UpdateTextBox(textBoxDebugWhitePawns,   text);
                text = BitboardToText(_board.Bitboard(Board.WHITE, Board.ROOKS));
                UpdateTextBox(textBoxDebugWhiteRooks,   text);
                text = BitboardToText(_board.Bitboard(Board.WHITE, Board.KNIGHTS));
                UpdateTextBox(textBoxDebugWhiteKnights, text);
                text = BitboardToText(_board.Bitboard(Board.WHITE, Board.BISHOPS));
                UpdateTextBox(textBoxDebugWhiteBishops, text);
                text = BitboardToText(_board.Bitboard(Board.WHITE, Board.QUEENS));
                UpdateTextBox(textBoxDebugWhiteQueens,  text);
                text = BitboardToText(_board.Bitboard(Board.WHITE, Board.KING));
                UpdateTextBox(textBoxDebugWhiteKing,    text);

                text = BitboardToText(_board.Bitboard(Board.BLACK, Board.PAWNS));
                UpdateTextBox(textBoxDebugBlackPawns,   text);
                text = BitboardToText(_board.Bitboard(Board.BLACK, Board.ROOKS));
                UpdateTextBox(textBoxDebugBlackRooks,   text);
                text = BitboardToText(_board.Bitboard(Board.BLACK, Board.KNIGHTS));
                UpdateTextBox(textBoxDebugBlackKnights, text);
                text = BitboardToText(_board.Bitboard(Board.BLACK, Board.BISHOPS));
                UpdateTextBox(textBoxDebugBlackBishops, text);
                text = BitboardToText(_board.Bitboard(Board.BLACK, Board.QUEENS));
                UpdateTextBox(textBoxDebugBlackQueens,  text);
                text = BitboardToText(_board.Bitboard(Board.BLACK, Board.KING));
                UpdateTextBox(textBoxDebugBlackKing,    text);

                text = BitboardToText(_board.WholeBitboards[Board.WHITE]);
                UpdateTextBox(textBoxDebugWhiteWhole, text);
                text = BitboardToText(_board.WholeBitboards[Board.BLACK]);
                UpdateTextBox(textBoxDebugBlackWhole, text);
            }
        }
        private string BitboardToText(ulong bitboard)
        {
            var builder = new StringBuilder();

            for (int i = 7; i >= 0; i--)
            {
                for (int j = 7; j >= 0; j--)
                {
                    builder.Append(BitCalculations.IsBitTurnedOn(bitboard, (8 * i) + j) ? '1' : '0');
                }
                builder.Append('\n');
            }

            return builder.ToString();
        }

        private void UpdateTextBox(TextBox textBox, string text)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action<TextBox, string>(UpdateTextBox), textBox, text);
            }
            else
            {
                textBox.Text = text;
            }
        }
        private void UpdateLabelText(Label label, string text)
        {
            if (labelStatus.InvokeRequired)
            {
                labelStatus.Invoke(new Action<Label, string>(UpdateLabelText), labelStatus, text);
            }
            else
            {
                labelStatus.Text = text;
            }
        }
        private void RefreshStatusLabel()
        {
            string text = "Game is running";
            if (VerificationController.IsStalemateFor(_board, _isPlayerPlayingWhite ? !_playerTurn : _playerTurn)) text = "Stalemate";
            if (VerificationController.IsCheckFor(_board, white: true))      text = "Check for white";
            if (VerificationController.IsCheckFor(_board, white: false))     text = "Check for black";
            if (VerificationController.IsCheckmateFor(_board, white: true))  text = "Black won";
            if (VerificationController.IsCheckmateFor(_board, white: false)) text = "White won";

            UpdateLabelText(labelStatus, text);
        }

        private bool MakePlayerBoardMove(int squareIndexFrom, int squareIndexTo)
        {
            RefreshStatusLabel();

            if (squareIndexFrom < 0 || squareIndexFrom >= AIController.ONE_SINGLE_BIT_MASK.Length ||
                squareIndexTo < 0 || squareIndexTo >= AIController.ONE_SINGLE_BIT_MASK.Length) return false;

            ulong moveFrom = BitCalculations.GetNumberByBit(squareIndexFrom);
            ulong moveTo   = BitCalculations.GetNumberByBit(squareIndexTo);

            var thisMove = AIController.GenerateAllPossibleMoves(_board, white: true).FirstOrDefault(m => m.From == moveFrom && m.To == moveTo);
            if (thisMove == null) return false;

            _board.MakeMove(thisMove);

            RefreshStatusLabel();

            return true;
        }

        private void MakeAIMove()
        {
            RefreshStatusLabel();
            if (VerificationController.IsGameOver(_board)) return;

            foreach (var pictureBox in IterateAllPieces()) pictureBox.BackColor = DEFAULT_TRANSPARENT_COLOR;

            var move = AIController.BestMove(_board, white: false, AI_DEPTH);
            if (move == null) return;

            int indexFrom = BitOperations.Log2(move.From);
            int indexTo   = BitOperations.Log2(move.To);

            _board.MakeMove(move);

            PictureBox? squareFrom = null, squareTo = null;
            foreach (var square in IterateAllPieces())
            {
                int squareIndex = int.Parse(square.Tag.ToString() ?? "-1");
                if (squareIndex < 0) continue;

                if (squareIndex == indexFrom) squareFrom = square;
                if (squareIndex == indexTo)   squareTo = square;
            }

            if (squareFrom == null || squareTo == null) return;

            RefreshGameField();

            squareTo.BackColor   = DEFAULT_LAST_MOVE_COLOR;
            squareFrom.BackColor = DEFAULT_LAST_MOVE_COLOR;

            RefreshStatusLabel();

            _playerTurn = true;
        }

        private IEnumerable<PictureBox> IterateAllPieces()
        {
            foreach (var element in tlpChess.Controls)
            {
                if (element is Panel panel)
                {
                    foreach (var box in panel.Controls)
                    {
                        if (box is PictureBox pictureBox)
                        {
                            yield return pictureBox;
                        }
                    }
                }
            }
        }

        private void ChooseThisSquare(PictureBox clickedSquare)
        {
            if (clickedSquare.Image == null) return;

            clickedSquare.BackColor = DEFAULT_CHOSEN_SQUARE_COLOR;
            _chosenPiece = clickedSquare;

            int squareIndexFrom = int.Parse(_chosenPiece.Tag.ToString() ?? "-1");
            ulong squareFromNumber = BitCalculations.GetNumberByBit(squareIndexFrom);

            foreach (var validMove in AIController.GenerateAllPossibleMoves(_board, white: true).Where(m => m.From == squareFromNumber))
            {
                foreach (var square in IterateAllPieces())
                {
                    int squareIndex = int.Parse(square.Tag.ToString() ?? "-1");
                    if (squareIndex < 0) continue;

                    if (squareIndex == BitOperations.Log2(validMove.To)) square.BackColor = DEFAULT_POSSIBLE_MOVE_COLOR;
                }
            }
        }

        private void PictureBox_MouseDown(object? sender, EventArgs e)
        {
            if (!_playerTurn) return;
            if (VerificationController.IsGameOver(_board)) return;
            if (_unmadeMoves.Count != 0) return;

            if (sender != null)
            {
                foreach (var pictureBox in IterateAllPieces()) pictureBox.BackColor = DEFAULT_TRANSPARENT_COLOR;

                var clickedSquare = (PictureBox)sender;
                if (_chosenPiece != null)
                {
                    int squareIndexFrom = int.Parse(_chosenPiece.Tag.ToString()  ?? "-1");
                    int squareIndexTo   = int.Parse(clickedSquare.Tag.ToString() ?? "-1");

                    if (squareIndexFrom < 0 || squareIndexTo < 0) return;

                    if (MakePlayerBoardMove(squareIndexFrom, squareIndexTo))
                    {
                        _playerTurn = false;

                        RefreshGameField();

                        new Thread(() =>
                        {
                            MakeAIMove();
                        }).Start();

                        _chosenPiece = null;
                    }
                    else
                    {
                        _chosenPiece = null;
                        ChooseThisSquare(clickedSquare);
                    }
                }
                else
                {
                    ChooseThisSquare(clickedSquare);
                }
            }
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            _playerTurn = true;
            _board = new Board();
            _chosenPiece = null;

            foreach (var pictureBox in IterateAllPieces())
            {
                pictureBox.BackColor = DEFAULT_TRANSPARENT_COLOR;
            }

            RefreshGameField();
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            if (_board.LastMove == null) return;

            var move = _board.UnmakeMove();
            _unmadeMoves.Push(move);

            RefreshGameField();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (_unmadeMoves.Count == 0) return;

            var move = _unmadeMoves.Pop();
            _board.MakeMove(move);

            RefreshGameField();
        }
    }
}