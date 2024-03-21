using ButterDevelopChessEngine.Controllers;
using ButterDevelopChessEngine.Models;
using System;
using System.Numerics;

namespace GUI
{
    public partial class MainForm : Form
    {
        private static readonly Color DEFAULT_TRANSPARENT_COLOR = Color.Transparent,
                                      DEFAULT_CHOSEN_SQUARE_COLOR = Color.Orange,
                                      DEFAULT_POSSIBLE_MOVE_COLOR = Color.FromArgb(128, 128, 128, 192),
                                      DEFAULT_LAST_MOVE_COLOR = Color.FromArgb(100, 200, 200, 30);

        private const int AI_DEPTH = 4;

        private bool _playerTurn;
        private Board _board;
        private PictureBox? _chosenPiece;

        private Dictionary<PictureBox, Image> _defaultGUIPosition;

        public MainForm()
        {
            InitializeComponent();

            _defaultGUIPosition = new Dictionary<PictureBox, Image>();

            _playerTurn = true;
            _board = new Board();

            foreach (var pictureBox in IterateAllPieces())
            {
                pictureBox.MouseDown += PictureBox_MouseDown;
                _defaultGUIPosition.Add(pictureBox, pictureBox.Image);
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
                int  piece = _board.WhatPieceIsThatSquare(AIController.ONE_SINGLE_BIT_MASK[squareIndex], Board.WHITE);
                if (piece == Board.UNKNOWN)
                {
                    white = false;
                    piece = _board.WhatPieceIsThatSquare(AIController.ONE_SINGLE_BIT_MASK[squareIndex], Board.BLACK);
                }

                switch (piece)
                {
                    case Board.PAWNS:   pictureBox.Image = white ? Properties.Resources.white_pawn   : Properties.Resources.black_pawn  ; break;
                    case Board.ROOKS:   pictureBox.Image = white ? Properties.Resources.white_rook   : Properties.Resources.black_rook  ; break;
                    case Board.KNIGHTS: pictureBox.Image = white ? Properties.Resources.white_knight : Properties.Resources.black_knight; break;
                    case Board.BISHOPS: pictureBox.Image = white ? Properties.Resources.white_bishop : Properties.Resources.black_bishop; break;
                    case Board.QUEENS:  pictureBox.Image = white ? Properties.Resources.white_queen  : Properties.Resources.black_queen ; break;
                    case Board.KING:    pictureBox.Image = white ? Properties.Resources.white_king   : Properties.Resources.black_king  ; break;
                    default:            pictureBox.Image = null; break;
                }
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
            if (VerificationController.IsStalemate(_board)) text = "Stalemate";
            if (VerificationController.IsCheckFor(_board, white: true)) text = "Check for white";
            if (VerificationController.IsCheckFor(_board, white: false)) text = "Check for black";
            if (VerificationController.IsCheckmateFor(_board, white: true)) text = "Black won";
            if (VerificationController.IsCheckmateFor(_board, white: false)) text = "White won";

            UpdateLabelText(labelStatus, text);
        }

        private bool MakePlayerBoardMove(int squareIndexFrom, int squareIndexTo)
        {
            if (squareIndexFrom < 0 || squareIndexFrom >= AIController.ONE_SINGLE_BIT_MASK.Length ||
                squareIndexTo   < 0 || squareIndexTo   >= AIController.ONE_SINGLE_BIT_MASK.Length) return false;

            ulong moveFrom = AIController.ONE_SINGLE_BIT_MASK[squareIndexFrom];
            ulong moveTo   = AIController.ONE_SINGLE_BIT_MASK[squareIndexTo];

            var thisMove = AIController.GenerateAllPossibleMoves(_board, white: true).FirstOrDefault(m => m.From == moveFrom && m.To == moveTo);
            if (thisMove == null) return false;

            _board.MakeMove(thisMove);

            return true;
        }

        private void MakeAIMove()
        {
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
                if (squareIndex == indexTo)   squareTo   = square;
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

            foreach (var validMove in AIController.GenerateAllPossibleMoves(_board, white: true)
                                                  .Where(m => m.From == AIController.ONE_SINGLE_BIT_MASK[squareIndexFrom]))
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

                        RefreshStatusLabel();

                        new Thread(() =>
                        {
                            MakeAIMove();
                        }).Start();

                        _chosenPiece = null;
                    }
                    else
                    {
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
            _playerTurn  = true;
            _board       = new Board();
            _chosenPiece = null;

            foreach (var (pictureBox, image) in _defaultGUIPosition)
            {
                pictureBox.Image     = image;
                pictureBox.BackColor = DEFAULT_TRANSPARENT_COLOR;
            }
        }
    }
}