using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionControlIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ControlCharacterProjectIdsFailClosed();
            ControlCharacterElementIdsFailClosed();
            ControlCharacterQuantityKeysFailClosed();
            ControlCharacterSummaryKeysFailClosed();
            CanonicalInputsRemainValid();
        }

        private static void ControlCharacterProjectIdsFailClosed()
        {
            Capture<InvalidOperationException>(() => Build("P\n1", "E1", "Concrete", 1d, 2d));
        }

        private static void ControlCharacterElementIdsFailClosed()
        {
            Capture<InvalidOperationException>(() => Build("P1", "E\t1", "Concrete", 1d, 2d));
        }

        private static void ControlCharacterQuantityKeysFailClosed()
        {
            Capture<InvalidOperationException>(() => Build("P1", "E1", "Concrete\rVolume", 1d, 2d));
        }

        private static void ControlCharacterSummaryKeysFailClosed()
        {
            Capture<InvalidOperationException>(() =>
                new QuantityRevisionReport().Summarize(new[]
                {
                    new QuantityRevisionRow
                    {
                        ElementId = "E1",
                        Category = "StructuralColumn",
                        QuantityName = "Concrete\nVolume",
                        Change = "Changed",
                        Before = 1d,
                        After = 2d
                    }
                }));
        }

        private static void CanonicalInputsRemainValid()
        {
            var rows = Build("P1", "E1", "Concrete", 1d, 2d);
            Assert(rows.Count == 1, "Canonical quantity revision input must still produce one changed row.");
            Assert(rows[0].ElementId == "E1", "Canonical element id changed unexpectedly.");
            Assert(rows[0].QuantityName == "Concrete", "Canonical quantity key changed unexpectedly.");
            Assert(rows[0].Change == "Changed", "Canonical quantity revision classification changed unexpectedly.");
            Assert(rows[0].Before.Equals(1d) && rows[0].After.Equals(2d), "Canonical quantity values changed unexpectedly.");
        }

        private static System.Collections.Generic.IReadOnlyList<QuantityRevisionRow> Build(
            string projectId,
            string elementId,
            string quantityKey,
            double beforeValue,
            double afterValue)
        {
            var before = Snapshot("before", projectId, elementId, quantityKey, beforeValue);
            var after = Snapshot("after", projectId, elementId, quantityKey, afterValue);
            return new QuantityRevisionReport().Build(before, after);
        }

        private static RevisionSnapshot Snapshot(
            string revisionId,
            string projectId,
            string elementId,
            string quantityKey,
            double value)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = revisionId,
                CreatedUtc = DateTime.UtcNow,
                ProjectId = projectId
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = elementId,
                Category = "StructuralColumn"
            };
            element.Quantities[quantityKey] = value;
            snapshot.Elements.Add(element);
            return snapshot;
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
