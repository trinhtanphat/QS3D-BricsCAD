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
            RejectsDishonestCountAtFirstDisallowedSheetAndDisposes();
            AcceptsExactBoundAndKeepsDeterministicOrdering();
            PreservesSinglePassEnumeration();
            PreservesNullIndexDiagnostic();
            PreservesDuplicateIdentityDiagnostics();
        }

        private static void RejectsKnownCollectionBeforeEnumeration()
        {
            var source = new OversizedKnownCollection();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "known ICollection");
            Require(!source.GetEnumeratorCalled, "known oversized ICollection should reject before GetEnumerator");
        }

        private static void RejectsKnownReadOnlyCollectionBeforeEnumeration()
        {
            var source = new OversizedKnownReadOnlyCollection();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "known IReadOnlyCollection");
            Require(!source.GetEnumeratorCalled, "known oversized IReadOnlyCollection should reject before GetEnumerator");
        }

        private static void RejectsDishonestCountAtFirstDisallowedSheetAndDisposes()
        {
            var source = new DishonestCollection(Limit + 5);
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "dishonest ICollection");
            Equal(Limit + 1, source.Seen, "dishonest collection enumeration count");
            Require(!source.ConsumedPastFirstDisallowed, "dishonest source consumed item 10002 after the limit was known to be exceeded");
            Require(source.DisposeCount == 1, "dishonest source enumerator was not disposed exactly once");
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

        private static void PreservesSinglePassEnumeration()
        {
            var source = new SinglePassSource(new[]
            {
                Plan("S-3", "N-003", "Third"),
                Plan("S-1", "N-001", "First"),
                Plan("S-2", "N-002", "Second")
            });

            var result = SemanticSheetIndexBuilder.Build(source);
            Equal(1, source.GetEnumeratorCalls, "single-pass GetEnumerator count");
            Equal(1, source.DisposeCount, "single-pass Dispose count");
            Equal(3, source.MoveNextTrueCount, "single-pass consumed item count");
            Equal("N-001", result.Rows[0].Number, "single-pass deterministic first row");
            Equal("N-003", result.Rows[2].Number, "single-pass deterministic last row");
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

        private static void ThrowsDuplicate(IEnumerable<SemanticSheetPlan> sheets, string expectedDiagnostic, string label)
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("SemanticSheetIndexBoundedInputSmoke " + message + ".");
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
            public int DisposeCount { get; private set; }
            public bool ConsumedPastFirstDisallowed => Seen > Limit + 1;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                return new TrackingEnumerator(this, _actualCount);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) { }
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();

            private sealed class TrackingEnumerator : IEnumerator<SemanticSheetPlan>
            {
                private readonly DishonestCollection _owner;
                private readonly int _actualCount;
                private int _index = -1;

                public TrackingEnumerator(DishonestCollection owner, int actualCount)
                {
                    _owner = owner;
                    _actualCount = actualCount;
                }

                public SemanticSheetPlan Current => Plan("D-" + _index.ToString("D5"), "DN-" + _index.ToString("D5"), "Dishonest " + _index);
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_index + 1 >= _actualCount)
                        return false;
                    _index++;
                    _owner.Seen++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() => _owner.DisposeCount++;
            }
        }

        private sealed class OversizedKnownCollection : ICollection<SemanticSheetPlan>
        {
            public int Count => Limit + 1;
            public bool IsReadOnly => true;
            public bool GetEnumeratorCalled { get; private set; }

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                GetEnumeratorCalled = true;
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
            public bool GetEnumeratorCalled { get; private set; }

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                GetEnumeratorCalled = true;
                return ((IEnumerable<SemanticSheetPlan>)Array.Empty<SemanticSheetPlan>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SinglePassSource : IEnumerable<SemanticSheetPlan>
        {
            private readonly IReadOnlyList<SemanticSheetPlan> _items;

            public SinglePassSource(IReadOnlyList<SemanticSheetPlan> items)
            {
                _items = items;
            }

            public int GetEnumeratorCalls { get; private set; }
            public int MoveNextTrueCount { get; private set; }
            public int DisposeCount { get; private set; }

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (GetEnumeratorCalls > 1)
                    throw new InvalidOperationException("single-pass source was enumerated more than once");
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<SemanticSheetPlan>
            {
                private readonly SinglePassSource _owner;
                private int _index = -1;

                public Enumerator(SinglePassSource owner)
                {
                    _owner = owner;
                }

                public SemanticSheetPlan Current => _owner._items[_index];
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_index + 1 >= _owner._items.Count)
                        return false;
                    _index++;
                    _owner.MoveNextTrueCount++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() => _owner.DisposeCount++;
            }
        }
    }
}
