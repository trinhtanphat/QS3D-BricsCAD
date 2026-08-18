using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSubtractPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RightOperandPrecisionLossFailsClosed();
            LeftOperandPrecisionLossFailsClosed();
            SignedPrecisionLossFailsClosed();
            RevisionComparePrecisionLossFailsClosed();
            OrdinaryAndZeroOperandDeltasRemainStable();
            CompensatedSummaryPrecisionRemainsStable();
        }

        private static void RightOperandPrecisionLossFailsClosed()
        {
            var error = Capture<OverflowException>(() => ReadDelta(Row("right-loss", 1d, 1e16d)));
            Assert(error.Message.IndexOf("right operand", StringComparison.Ordinal) >= 0,
                "Revision delta must identify precision loss of the right operand.");
        }

        private static void LeftOperandPrecisionLossFailsClosed()
        {
            var error = Capture<OverflowException>(() => ReadDelta(Row("left-loss", 1e16d, 1d)));
            Assert(error.Message.IndexOf("left operand", StringComparison.Ordinal) >= 0,
                "Revision delta must identify precision loss of the left operand.");
        }

        private static void SignedPrecisionLossFailsClosed()
        {
            Capture<OverflowException>(() => ReadDelta(Row("negative-right-loss", -1d, -1e16d)));
            Capture<OverflowException>(() => ReadDelta(Row("negative-left-loss", -1e16d, -1d)));
        }

        private static void RevisionComparePrecisionLossFailsClosed()
        {
            var service = new RevisionService();
            Capture<OverflowException>(() => service.Compare(
                Snapshot("before-right-loss", 1e16d),
                Snapshot("after-right-loss", 1d)));
            Capture<OverflowException>(() => service.Compare(
                Snapshot("before-left-loss", 1d),
                Snapshot("after-left-loss", 1e16d)));
        }

        private static void OrdinaryAndZeroOperandDeltasRemainStable()
        {
            var ordinary = Row("ordinary", 10d, 12d);
            Assert(ordinary.Delta.Equals(2d), "Ordinary revision subtraction changed unexpectedly.");
            Assert(ordinary.PercentChange.HasValue && ordinary.PercentChange.Value.Equals(20d),
                "Ordinary revision percentage changed unexpectedly.");
            Assert(Row("zero-before", 0d, 12d).Delta.Equals(12d),
                "Zero before operand must remain valid.");
            Assert(Row("zero-after", 12d, 0d).Delta.Equals(-12d),
                "Zero after operand must remain valid.");
            Assert(Row("cancel", 12d, 12d).Delta.Equals(0d),
                "Exact cancellation must remain valid.");
            Assert(Row("signed", -4d, -1d).Delta.Equals(3d),
                "Ordinary signed revision subtraction changed unexpectedly.");
        }

        private static void CompensatedSummaryPrecisionRemainsStable()
        {
            const double expected = 10000000000000002d;
            var summaries = new QuantityRevisionReport().Summarize(new[]
            {
                Row("large", 1e16d, 1e16d),
                Row("small-a", 1d, 1d),
                Row("small-b", 1d, 1d)
            });

            Assert(summaries.Count == 1, "Compensated revision summary must remain in one quantity group.");
            Assert(summaries[0].Before.Equals(expected),
                "Subtraction hardening must not break compensated Before aggregation.");
            Assert(summaries[0].After.Equals(expected),
                "Subtraction hardening must not break compensated After aggregation.");
            Assert(summaries[0].Delta.Equals(0d),
                "Equal compensated totals must retain a zero delta.");
        }

        private static QuantityRevisionRow Row(string elementId, double before, double after) =>
            new QuantityRevisionRow
            {
                ElementId = elementId,
                Category = "StructuralColumn",
                QuantityName = "Concrete",
                Change = "Changed",
                Before = before,
                After = after
            };

        private static RevisionSnapshot Snapshot(string id, double quantity)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = id,
                CreatedUtc = DateTime.UtcNow,
                ProjectId = "revision-subtract-precision-project"
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = "element-1",
                Category = "StructuralColumn"
            };
            element.Quantities["Concrete"] = quantity;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void ReadDelta(QuantityRevisionRow row)
        {
            var value = row.Delta;
            GC.KeepAlive(value);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
