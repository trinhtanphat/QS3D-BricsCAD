using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetIndexBoundedInputSmoke
    {
        private const int Limit = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsKnownCollectionBeforeEnumeration();
            RejectsKnownReadOnlyCollectionBeforeEnumeration();
            RejectsDishonestCountAtFirstDisallowedSheet();
            AcceptsExactBoundAndKeepsDeterministicOrdering();
            PreservesNullIndexDiagnostic();
            PreservesDuplicateIdentityDiagnostics();
        }

        private static void RejectsKnownCollectionBeforeEnumeration()
        {
            var source = new OversizedKnownCollection();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "known ICollection");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke enumerated a known oversized ICollection.");
        }

        private static void RejectsKnownReadOnlyCollectionBeforeEnumeration()
        {
            var source = new OversizedKnownReadOnlyCollection();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "known IReadOnlyCollection");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke enumerated a known oversized IReadOnlyCollection.");
        }

        private static void RejectsDishonestCountAtFirstDisallowedSheet()
        {
            var source = new DishonestCollection(Limit + 5);
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "dishonest ICollection");
            Equal(Limit + 1, source.Seen, "dishonest collection enumeration count");
        }

        private static void AcceptsExactBoundAndKeepsDeterministicOrdering()
        {
            var exact = new List<SemanticSheetPlan>(Limit);
            for (var index = Limit - 1; index >= 0; index--)
                exact.Add(Plan("S-" + index.ToString("D5"), "N-" + index.ToString("D5"), "Sheet " + index));

            var result = SemanticSheetIndexBuilder.Build(exact);
            Equal(Limit, result.Rows.Count, "exact-bound row count");
            Equal("N-00000", result.Rows[0].Number, "deterministic first number");
            Equal("S-00000", result.Rows[0].SheetId, "deterministic first id");
            Equal("N-09999", result.Rows[Limit - 1].Number, "deterministic last number");
        }

        private static void PreservesNullIndexDiagnostic()
        {
            try
            {
                SemanticSheetIndexBuilder.Build(WithNullAtIndexOne());
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("null sheet at index 1", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke null diagnostic changed: " + ex.Message, ex);
            }

            throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke null source item did not fail closed.");
        }

        private static void PreservesDuplicateIdentityDiagnostics()
        {
            ThrowsDuplicate(
                new[]
                {
                    Plan("S-A", "N-001", "First"),
                    Plan("s-a", "N-002", "Second")
                },
                "duplicate sheet id",
                "duplicate id");

            ThrowsDuplicate(
                new[]
                {
                    Plan("S-A", "N-001", "First"),
                    Plan("S-B", "n-001", "Second")
                },
                "duplicate sheet number",
                "duplicate number");
        }

        private static IEnumerable<SemanticSheetPlan> WithNullAtIndexOne()
        {
            yield return Plan("S-1", "N-001", "First");
            yield return null!;
            yield return Plan("S-3", "N-003", "Third");
        }

        private static SemanticSheetPlan Plan(string id, string number, string name)
        {
            return SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    id,
                    number,
                    name,
                    841d,
                    594d,
                    Array.Empty<SemanticSheetPlacementDefinition>(),
                    "A1"),
                Array.Empty<SemanticViewPlan>());
        }

        private static void ThrowsLimit(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("10000", StringComparison.Ordinal) >= 0 &&
                    ex.Message.IndexOf("at most", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke " + label + " returned the wrong limit diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke " + label + " did not fail closed.");
        }

        private static void ThrowsDuplicate(
            IEnumerable<SemanticSheetPlan> sheets,
            string expectedDiagnostic,
            string label)
        {
            try
            {
                SemanticSheetIndexBuilder.Build(sheets);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedDiagnostic, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke " + label + " returned the wrong diagnostic: " + ex.Message, ex);
            }

            throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke " + label + " did not fail closed.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class DishonestCollection : ICollection<SemanticSheetPlan>
        {
            private readonly int _actualCount;

            public DishonestCollection(int actualCount)
            {
                _actualCount = actualCount;
            }

            public int Count => 1;
            public bool IsReadOnly => true;
            public int Seen { get; private set; }

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                for (var index = 0; index < _actualCount; index++)
                {
                    Seen++;
                    yield return Plan("D-" + index.ToString("D5"), "DN-" + index.ToString("D5"), "Dishonest " + index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) { }
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }

        private sealed class OversizedKnownCollection : ICollection<SemanticSheetPlan>
        {
            public int Count => Limit + 1;
            public bool IsReadOnly => true;
            public bool Enumerated { get; private set; }

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                return ((IEnumerable<SemanticSheetPlan>)Array.Empty<SemanticSheetPlan>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) { }
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }

        private sealed class OversizedKnownReadOnlyCollection : IReadOnlyCollection<SemanticSheetPlan>
        {
            public int Count => Limit + 1;
            public bool Enumerated { get; private set; }

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                return ((IEnumerable<SemanticSheetPlan>)Array.Empty<SemanticSheetPlan>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
