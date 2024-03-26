using ButterDevelopChessEngine.Controllers;

namespace ButterDevelopChessEngine.Models
{
    public class Board
    {
        private const ulong DEFAULT_BITBOARD_WHITE_PAWNS   = 0xff00,
                            DEFAULT_BITBOARD_BLACK_PAWNS   = 0xff000000000000,
                            DEFAULT_BITBOARD_WHITE_ROOKS   = 0x81,
                            DEFAULT_BITBOARD_BLACK_ROOKS   = 0x8100000000000000,
                            DEFAULT_BITBOARD_WHITE_KNIGHTS = 0x42,
                            DEFAULT_BITBOARD_BLACK_KNIGHTS = 0x4200000000000000,
                            DEFAULT_BITBOARD_WHITE_BISHOPS = 0x24,
                            DEFAULT_BITBOARD_BLACK_BISHOPS = 0x2400000000000000,
                            DEFAULT_BITBOARD_WHITE_QUEENS  = 0x10,
                            DEFAULT_BITBOARD_BLACK_QUEENS  = 0x1000000000000000,
                            DEFAULT_BITBOARD_WHITE_KING    = 0x8,
                            DEFAULT_BITBOARD_BLACK_KING    = 0x800000000000000,
                            DEFAULT_WHITE_WHOLE_BITBOARD   = 0xffff,
                            DEFAULT_BLACK_WHOLE_BITBOARD   = 0xffff000000000000,
                            DEFAULT_WHOLE_BITBOARD         = DEFAULT_WHITE_WHOLE_BITBOARD | DEFAULT_BLACK_WHOLE_BITBOARD,
                            DEFAULT_CASTLE_RIGHTS          = 0x8900000000000089;
        internal const int  TEAMS_COUNT = 2, PIECES_GROUPS_COUNT = 6;
        public   const int  WHITE = 0, 
                            BLACK = 1, 
                            WHOLE = 2;
        public   const int  UNKNOWN = -1,
                            PAWNS   = 0,
                            ROOKS   = 1,
                            KNIGHTS = 2,
                            BISHOPS = 3,
                            QUEENS  = 4,
                            KING    = 5;

        // https://gekomad.github.io/Cinnamon/BitboardCalculator/
        // https://www.rapidtables.com/calc/math/binary-calculator.html

        private Stack<Move> _moves, _castleMoves;
        private ulong       _castleRights;
        private ulong[]     _wholeBitboards;
        private ulong[][]   _bitboards;

        public Board()
        {
            _bitboards = new ulong[TEAMS_COUNT][];
            for (int i = 0; i < TEAMS_COUNT; i++) _bitboards[i] = new ulong[PIECES_GROUPS_COUNT];

            _moves       = new Stack<Move>();
            _castleMoves = new Stack<Move>();

            _castleRights = DEFAULT_CASTLE_RIGHTS;

            _bitboards[WHITE][PAWNS]   = DEFAULT_BITBOARD_WHITE_PAWNS;
            _bitboards[BLACK][PAWNS]   = DEFAULT_BITBOARD_BLACK_PAWNS;
            _bitboards[WHITE][ROOKS]   = DEFAULT_BITBOARD_WHITE_ROOKS;
            _bitboards[BLACK][ROOKS]   = DEFAULT_BITBOARD_BLACK_ROOKS;
            _bitboards[WHITE][KNIGHTS] = DEFAULT_BITBOARD_WHITE_KNIGHTS;
            _bitboards[BLACK][KNIGHTS] = DEFAULT_BITBOARD_BLACK_KNIGHTS;
            _bitboards[WHITE][BISHOPS] = DEFAULT_BITBOARD_WHITE_BISHOPS;
            _bitboards[BLACK][BISHOPS] = DEFAULT_BITBOARD_BLACK_BISHOPS;
            _bitboards[WHITE][QUEENS]  = DEFAULT_BITBOARD_WHITE_QUEENS;
            _bitboards[BLACK][QUEENS]  = DEFAULT_BITBOARD_BLACK_QUEENS;
            _bitboards[WHITE][KING]    = DEFAULT_BITBOARD_WHITE_KING;
            _bitboards[BLACK][KING]    = DEFAULT_BITBOARD_BLACK_KING;

            _wholeBitboards = new ulong[TEAMS_COUNT + 1];
            _wholeBitboards[WHITE] = DEFAULT_WHITE_WHOLE_BITBOARD;
            _wholeBitboards[BLACK] = DEFAULT_BLACK_WHOLE_BITBOARD;
            _wholeBitboards[WHOLE] = DEFAULT_WHOLE_BITBOARD;
        }

        public Board(List<Move> moves) : this()
        {
            foreach (var move in moves) MakeMove(move);
        }

        public int WhatPieceIsThatSquare(ulong square, int color)
        {
            if (VerificationController.IsThisSquareOccupied(square, _bitboards[color][PAWNS]))   return PAWNS;
            if (VerificationController.IsThisSquareOccupied(square, _bitboards[color][ROOKS]))   return ROOKS;
            if (VerificationController.IsThisSquareOccupied(square, _bitboards[color][KNIGHTS])) return KNIGHTS;
            if (VerificationController.IsThisSquareOccupied(square, _bitboards[color][BISHOPS])) return BISHOPS;
            if (VerificationController.IsThisSquareOccupied(square, _bitboards[color][QUEENS]))  return QUEENS;
            if (VerificationController.IsThisSquareOccupied(square, _bitboards[color][KING]))    return KING;
            return UNKNOWN;
        }

        public void MakeMove(Move move)
        {           
            int color         = move.IsWhite ? WHITE : BLACK;
            int reversedColor = move.IsWhite ? BLACK : WHITE;

            BitCalculations.TurnOffBit(ref _bitboards[color][move.WhatPieceMoved], move.From);
            BitCalculations.TurnOnBit( ref _bitboards[color][move.WhatPieceMoved], move.To);

            BitCalculations.TurnOffBit(ref _wholeBitboards[color], move.From);
            BitCalculations.TurnOnBit( ref _wholeBitboards[color], move.To);

            if (move.IsPieceTaken && move.SpecialMove != SpecialMove.EnPassant)
            {
                BitCalculations.TurnOffBit(ref _bitboards[reversedColor][move.TakenPiece], move.To);
                BitCalculations.TurnOffBit(ref _wholeBitboards[reversedColor],             move.To);
            }

            if (move.SpecialMove == SpecialMove.PromotePawn)
            {
                BitCalculations.TurnOffBit(ref _bitboards[color][move.WhatPieceMoved], move.To);
                BitCalculations.TurnOnBit( ref _bitboards[color][move.PromoteTo],      move.To);
            }
            else
            if (move.SpecialMove == SpecialMove.EnPassant)
            {
                ulong whereToTakeEnPassant = move.IsWhite ? (move.To >> 8) : (move.To << 8);
                BitCalculations.TurnOffBit(ref _bitboards[reversedColor][move.TakenPiece], whereToTakeEnPassant);
                BitCalculations.TurnOffBit(ref _wholeBitboards[reversedColor],             whereToTakeEnPassant);
            }
            else
            if (move.SpecialMove == SpecialMove.LongCastle || move.SpecialMove == SpecialMove.ShortCastle)
            {
                ulong rookSquareFrom = move.To << 2;
                ulong rookSquareTo   = move.To >> 1;
                if (move.SpecialMove == SpecialMove.ShortCastle)
                {
                    rookSquareFrom = move.To >> 1;
                    rookSquareTo   = move.To << 1;
                }

                BitCalculations.TurnOffBit(ref _bitboards[color][ROOKS], rookSquareFrom);
                BitCalculations.TurnOnBit( ref _bitboards[color][ROOKS], rookSquareTo);

                BitCalculations.TurnOffBit(ref _wholeBitboards[color], rookSquareFrom);
                BitCalculations.TurnOnBit( ref _wholeBitboards[color], rookSquareTo);
            }

            if (move.WhatPieceMoved == KING || move.WhatPieceMoved == ROOKS)
            {
                //BitCalculations.TurnOffBit(ref _castleRights, move.From);
                _castleMoves.Push(move);
            }

            _wholeBitboards[WHOLE] = _wholeBitboards[color] | _wholeBitboards[reversedColor];

            _moves.Push(move);
        }

        internal void UnmakeMove()
        {
            var move = _moves.Pop();

            int color         = move.IsWhite ? WHITE : BLACK;
            int reversedColor = move.IsWhite ? BLACK : WHITE;

            BitCalculations.TurnOffBit(ref _bitboards[color][move.WhatPieceMoved], move.To);
            BitCalculations.TurnOnBit( ref _bitboards[color][move.WhatPieceMoved], move.From);

            BitCalculations.TurnOffBit(ref _wholeBitboards[color], move.To);
            BitCalculations.TurnOnBit( ref _wholeBitboards[color], move.From);

            if (move.IsPieceTaken && move.SpecialMove != SpecialMove.EnPassant)
            {
                BitCalculations.TurnOnBit(ref _bitboards[reversedColor][move.TakenPiece], move.To);
                BitCalculations.TurnOnBit(ref _wholeBitboards[reversedColor],             move.To);
            }

            if (move.SpecialMove == SpecialMove.PromotePawn)
            {
                BitCalculations.TurnOffBit(ref _bitboards[color][move.PromoteTo], move.To);
            }
            else
            if (move.SpecialMove == SpecialMove.EnPassant)
            {
                ulong whereToTakeEnPassant = move.IsWhite ? (move.To >> 8) : (move.To << 8);
                BitCalculations.TurnOnBit(ref _bitboards[reversedColor][move.TakenPiece], whereToTakeEnPassant);
                BitCalculations.TurnOnBit(ref _wholeBitboards[reversedColor],             whereToTakeEnPassant);
            }
            else
            if (move.SpecialMove == SpecialMove.LongCastle || move.SpecialMove == SpecialMove.ShortCastle)
            {
                ulong rookSquareFrom = move.To << 2;
                ulong rookSquareTo   = move.To >> 1;
                if (move.SpecialMove == SpecialMove.ShortCastle)
                {
                    rookSquareFrom = move.To >> 1;
                    rookSquareTo   = move.To << 1;
                }

                BitCalculations.TurnOffBit(ref _bitboards[color][ROOKS], rookSquareTo);
                BitCalculations.TurnOnBit( ref _bitboards[color][ROOKS], rookSquareFrom);

                BitCalculations.TurnOffBit(ref _wholeBitboards[color], rookSquareTo);
                BitCalculations.TurnOnBit( ref _wholeBitboards[color], rookSquareFrom);
            }

            if (move.WhatPieceMoved == KING || move.WhatPieceMoved == ROOKS)
            {
                //BitCalculations.TurnOnBit(ref _castleRights, move.From);
                _castleMoves.Pop();
            }

            _wholeBitboards[WHOLE] = _wholeBitboards[color] | _wholeBitboards[reversedColor];
        }

        internal ulong Bitboard(int color, int piece)
        {
            return _bitboards[color][piece];
        }

        public Move? LastMove
        {
            get { return _moves.Count == 0 ? null : _moves.Peek(); }
        }

        public ulong CastleRights
        {
            get
            {
                ulong localCastleRights = DEFAULT_CASTLE_RIGHTS;
                foreach (var move in _castleMoves)
                {
                    BitCalculations.TurnOffBit(ref localCastleRights, move.From);
                }
                //return _castleRights;
                return localCastleRights;
            }
        }

        internal ulong[] WholeBitboards
        {
            get { return _wholeBitboards; }
        }
    }
}
