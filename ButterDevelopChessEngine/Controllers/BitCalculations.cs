namespace ButterDevelopChessEngine.Controllers
{
    public class BitCalculations
    {
        internal static void TurnOnBit(ref ulong number, ulong bit)
        {
            number |= bit;
        }
        internal static void TurnOffBit(ref ulong number, ulong bit)
        {
            number &= ~bit;
        }

        public static ulong GetNumberByBit(int bitNumber)
        {
            return (ulong)1 << bitNumber;
        }

        internal static void TurnOnBit(ref ulong number, int bitNumber)
        {
            TurnOnBit(ref number, GetNumberByBit(bitNumber));
        }
        internal static void TurnOffBit(ref ulong number, int bitNumber)
        {
            TurnOffBit(ref number, GetNumberByBit(bitNumber));
        }
    }
}
