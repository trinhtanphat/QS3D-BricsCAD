using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetAutoLayoutTitleBlockValidationSmoke
    {
        internal static void Run()
        {
            MaximumLengthRemainsAccepted();
            OverLengthFailsBeforeEnumeration();
            WhitespaceOnlyRemainsOptional();
        }

        private static void MaximumLengthRemainsAccepted()
        {
            var sheets = SemanticSheetAutoLayoutPlanner.Build(
                Array.Empty<SemanticSheetAutoLayoutItem>(),
                Array.Empty<SemanticViewPlan>(),
                new SemanticSheetAutoLayoutOptions(
                    "S", "A-", "Sheet", 297d, 210d,
                    titleBlockName: new string('T', 160)));
            Equal(0, sheets.Count);
        }

        private static void OverLengthFailsBeforeEnumeration()
        {
            Throws<ArgumentException>(() => SemanticSheetAutoLayoutPlanner.Build(
                new ThrowingEnumerable<SemanticSheetAutoLayoutItem>("items were enumerated before title-block validation"),
                new ThrowingEnumerable<SemanticViewPlan>("views were enumerated before title-block validation"),
                new SemanticSheetAutoLayoutOptions(
                    "S", "A-", "Sheet", 297d, 210d,
                    titleBlockName: new string('T', 161))));
        }

        private static void WhitespaceOnlyRemainsOptional()
        {
            var sheets = SemanticSheetAutoLayoutPlanner.Build(
                new[] { new SemanticSheetAutoLayoutItem("V1", 100d, 80d) },
                BuildViews(),
                new SemanticSheetAutoLayoutOptions(
                    "S", "A-", "Sheet", 297d, 210d,
                    titleBlockName: "   "));
            Equal(1, sheets.Count);
            Equal<string?>(null, sheets[0].TitleBlockName);
        }

        private static IReadOnlyList<SemanticViewPlan> BuildViews()
        {
            var project = new ProjectState("P-AUTO-SHEET-TITLEBLOCK", "Auto Sheet Title Block");
            return SemanticViewPlanner.BuildCatalog(
                project,
                new[] { new SemanticViewDefinition("V1", "View 1") });
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
