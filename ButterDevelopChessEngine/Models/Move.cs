namespace ButterDevelopChessEngine.Models
{
    public enum SpecialMove
    {
        None        = 0,
        ShortCastle = 1,
        LongCastle  = 2,
        PromotePawn = 3,
        EnPassant   = 4
    }
    public class Move
    {
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
