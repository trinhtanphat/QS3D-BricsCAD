using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqWorkspaceBaseTotalPrecisionSmoke
    {
        private const decimal Large = 10000000000000000000000000000m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesRepresentableRecoveredAggregate();
            PreservesCanonicalOrderIndependence();
            PreservesOrdinaryAggregateAndPreview();
            RejectsFinalUnrepresentableAggregate();
        }

        private static void PreservesRepresentableRecoveredAggregate()
        {
            var state = State(
                Item("A-LARGE", Large),
                Item("B-HALF", 0.5m),
                Item("C-HALF", 0.5m));

            Equal(
                Large + 1m,
                state.BaseTotal,
                "TBQ base total must preserve two half-unit contributions whose complete aggregate is representable.");
        }

        private static void PreservesCanonicalOrderIndependence()
        {
            var state = State(
                Item("C-HALF", 0.5m),
                Item("A-LARGE", Large),
                Item("B-HALF", 0.5m));

            Equal(
                Large + 1m,
                state.BaseTotal,
                "TBQ base total must depend on the canonical bill-item snapshot, not caller order.");
        }

        private static void PreservesOrdinaryAggregateAndPreview()
        {
            var state = State(
                Item("A", 12.5m),
                Item("B", 7.5m));

            Equal(20m, state.BaseTotal, "Ordinary TBQ base total changed.");
            Equal(20m, state.PreviewAdjustment().AdjustedTotal, "Zero-ratio TBQ preview must preserve the exact base total.");
        }

        private static void RejectsFinalUnrepresentableAggregate()
        {
            var state = State(
                Item("A-LARGE", Large),
                Item("B-HALF", 0.5m));

            Capture<OverflowException>(
                () => ReadBaseTotal(state),
                "TBQ base total must fail closed when the complete exact aggregate is not representable as decimal.");
        }

        private static TbqBillItem Item(string id, decimal total)
        {
            return new TbqBillItem(
                id,
                "TBQ precision regression item",
                "ea",
                "TEST",
                1m,
                total);
        }

        private static TbqProjectWorkspaceState State(params TbqBillItem[] items)
        {
            return new TbqProjectWorkspaceState(
                "VND",
                0m,
                items,
                Array.Empty<BuildUpRateSnapshot>(),
                Array.Empty<RateReferenceEdge>(),
                "PROJECT",
                Array.Empty<BqLibraryEntry>());
        }

        private static void ReadBaseTotal(TbqProjectWorkspaceState state)
        {
            _ = state.BaseTotal;
        }

        private static TException Capture<TException>(Action action, string message)
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

            throw new InvalidOperationException(message + " Expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
