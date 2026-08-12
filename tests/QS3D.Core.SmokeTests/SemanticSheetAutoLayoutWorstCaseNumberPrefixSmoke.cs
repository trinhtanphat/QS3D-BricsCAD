using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetAutoLayoutWorstCaseNumberPrefixSmoke
    {
        internal static void Run()
        {
            MaximumPrefixReservesFiveDigitOrdinal();
            OverBoundPrefixFailsBeforeEnumeration();
        }

        private static void MaximumPrefixReservesFiveDigitOrdinal()
        {
            var maxPrefix = new string('N', 59);
            var sheets = SemanticSheetAutoLayoutPlanner.Build(
                new[] { new SemanticSheetAutoLayoutItem("V1", 100d, 80d) },
                BuildViews(1),
                new SemanticSheetAutoLayoutOptions("S", maxPrefix, "Sheet", 297d, 210d));

            Equal(1, sheets.Count);
            Equal(maxPrefix + "01", sheets[0].Number);
            Equal(64, (maxPrefix + "10000").Length);
        }

        private static void OverBoundPrefixFailsBeforeEnumeration()
        {
            Throws<ArgumentException>(() => SemanticSheetAutoLayoutPlanner.Build(
                new ThrowingEnumerable<SemanticSheetAutoLayoutItem>("items were enumerated before sheet-number prefix validation"),
                new ThrowingEnumerable<SemanticViewPlan>("views were enumerated before sheet-number prefix validation"),
                new SemanticSheetAutoLayoutOptions("S", new string('N', 60), "Sheet", 297d, 210d)));
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews(int count)
        {
            var project = new ProjectState("P-AUTO-SHEET-WORST-PREFIX", "Auto Sheet Worst Prefix");
            var definitions = new List<SemanticViewDefinition>();
            for (var i = 1; i <= count; i++)
                definitions.Add(new SemanticViewDefinition("V" + i, "View " + i));
            return SemanticViewPlanner.BuildCatalog(project, definitions);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private sealed class ThrowingEnumerable<T> : IEnumerable<T>
        {
            private readonly string _message;

            internal ThrowingEnumerable(string message) => _message = message;

            public IEnumerator<T> GetEnumerator() => throw new ApplicationException(_message);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
