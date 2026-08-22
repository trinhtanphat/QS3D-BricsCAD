using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqBillItemTotalCostUnderflowSmoke
    {
        private const decimal SmallestNonZeroDecimal = 0.0000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPositiveTotalCostUnderflow();
            PreservesExactZeroOperands();
            PreservesOrdinaryTotalCost();
            PreservesOverflowFailure();
        }

        private static void RejectsPositiveTotalCostUnderflow()
        {
            var item = Item("TBQ-UNDERFLOW", SmallestNonZeroDecimal, SmallestNonZeroDecimal);
            var error = Capture<OverflowException>(() => ReadTotalCost(item));

            Contains(
                "TBQ bill item total cost overflowed decimal arithmetic",
                error.Message,
                "Positive TBQ total-cost underflow must fail through the existing overflow contract.");
        }

        private static void PreservesExactZeroOperands()
        {
            Equal(
                0m,
                Item("TBQ-ZERO-QTY", 0m, SmallestNonZeroDecimal).TotalCost,
                "Exact zero quantity must remain a valid zero total.");
            Equal(
                0m,
                Item("TBQ-ZERO-RATE", SmallestNonZeroDecimal, 0m).TotalCost,
                "Exact zero unit rate must remain a valid zero total.");
        }

        private static void PreservesOrdinaryTotalCost()
        {
            Equal(
                100m,
                Item("TBQ-ORDINARY", 12.5m, 8m).TotalCost,
                "Ordinary TBQ total-cost arithmetic changed.");
        }

        private static void PreservesOverflowFailure()
        {
            var item = Item("TBQ-OVERFLOW", decimal.MaxValue, 2m);
            var error = Capture<OverflowException>(() => ReadTotalCost(item));

            Contains(
                "TBQ bill item total cost overflowed decimal arithmetic",
                error.Message,
                "TBQ total-cost overflow contract changed.");
        }

        private static TbqBillItem Item(string id, decimal quantity, decimal unitRate)
        {
            return new TbqBillItem(
                id,
                "Deterministic TBQ item",
                "m",
                "TEST",
                quantity,
                unitRate);
        }

        private static void ReadTotalCost(TbqBillItem item)
        {
            _ = item.TotalCost;
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
