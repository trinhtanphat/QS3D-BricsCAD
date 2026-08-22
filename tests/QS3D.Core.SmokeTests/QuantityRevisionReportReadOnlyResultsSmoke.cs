using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionReportReadOnlyResultsSmoke
    {
        internal static void Run()
        {
            var before = Snapshot("before", 2d);
            var after = Snapshot("after", 3d);
            var report = new QuantityRevisionReport();

            var rows = report.Build(before, after);
            Equal(1, rows.Count, "revision row count");
            Equal("E1", rows[0].ElementId, "revision row element id");
            Equal("VolumeM3", rows[0].QuantityName, "revision row quantity");
            Equal(2d, rows[0].Before, "revision row before");
            Equal(3d, rows[0].After, "revision row after");
            AssertStructurallyReadOnly(rows, new QuantityRevisionRow(), "Build result");

            var summaries = report.Summarize(rows);
            Equal(1, summaries.Count, "summary count");
            Equal("VolumeM3", summaries[0].QuantityName, "summary quantity");
            Equal(2d, summaries[0].Before, "summary before");
            Equal(3d, summaries[0].After, "summary after");
            AssertStructurallyReadOnly(summaries, new QuantityRevisionSummary(), "Summarize result");
        }

        private static RevisionSnapshot Snapshot(string id, double volume)
        {
            var snapshot = new RevisionSnapshot { Id = id, CreatedUtc = DateTime.UtcNow };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E1",
                Category = ElementCategory.Beam.ToString()
            };
            element.Quantities["VolumeM3"] = volume;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void AssertStructurallyReadOnly<T>(IReadOnlyList<T> values, T appendValue, string label)
        {
            if (values is List<T>)
                throw new InvalidOperationException(label + " leaked a mutable List<T> runtime instance.");
            if (!(values is IList<T> list))
                throw new InvalidOperationException(label + " did not expose the expected read-only IList<T> contract.");
            Throws<NotSupportedException>(() => list.Add(appendValue), label + " structural mutation");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
