namespace ButterDevelopChessEngine.Models
{
    public enum SpecialMove
    {
        None        = 0,
        LongCastle  = 1,
        ShortCastle = 2,
        PromotePawn = 3,
        EnPassant   = 4
    }
    public class Move
    {
        public static Move Clone(Move moveToClone)
        {
            return new Move
            {
                IsWhite        = moveToClone.IsWhite,
                WhatPieceMoved = moveToClone.WhatPieceMoved,
                From           = moveToClone.From,
                To             = moveToClone.To,
                IsPieceTaken   = moveToClone.IsPieceTaken,
                TakenPiece     = moveToClone.TakenPiece,
                SpecialMove    = moveToClone.SpecialMove,
                PromoteTo      = moveToClone.PromoteTo
            };
        }

        public bool  IsWhite { get; set; }
        public int   WhatPieceMoved { get; set; }
        public ulong From { get; set; }
        public ulong To   { get; set; }
        public bool  IsPieceTaken { get; set; }
        public int   TakenPiece { get; set; }

        public SpecialMove SpecialMove { get; set; }
        public int         PromoteTo { get; set; }
    }
}
