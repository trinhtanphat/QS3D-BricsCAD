using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookCountNoOverreadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunStopsBeforeUnexpectedCurrent();
            ZeroCountOverrunNeverReadsCurrent();
            UnderYieldFailsExactCardinality();
            MoveNextInducedCountDriftFailsBeforeCurrent();
            CurrentInducedCountDriftFailsBeforeRetention();
            PostTraversalCountDriftFailsClosed();
            StableCountedSnapshotReadsEachAdmittedCurrentExactlyOnce();
        }

        private static void KnownCountOverrunStopsBeforeUnexpectedCurrent()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11, 22 }, 1, 1, 1, 1, 1, 1, 1, 1);
            ExpectInvalidData(source);
            Require(source.MoveNextCalls == 2, "Count=1/yield=2 must detect the second item with MoveNext.");
            Require(source.CurrentReads == 1, "Count=1/yield=2 must reject before observing the second Current.");
        }

        private static void ZeroCountOverrunNeverReadsCurrent()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11 }, 0, 0, 0, 0);
            ExpectInvalidData(source);
            Require(source.MoveNextCalls == 1, "Count=0/yield=1 must detect the first item with MoveNext.");
            Require(source.CurrentReads == 0, "Count=0/yield=1 must reject before any Current read.");
        }

        private static void UnderYieldFailsExactCardinality()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11 }, 2, 2, 2, 2, 2, 2, 2);
            ExpectInvalidData(source);
            Require(source.MoveNextCalls == 2, "Count=2/yield=1 must traverse to normal termination.");
            Require(source.CurrentReads == 1, "Under-yield must read only the one admitted item.");
        }

        private static void MoveNextInducedCountDriftFailsBeforeCurrent()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11 }, 1, 1, 2);
            ExpectInvalidData(source);
            Require(source.MoveNextCalls == 1, "MoveNext-induced Count drift must fail at the first traversal boundary.");
            Require(source.CurrentReads == 0, "MoveNext-induced Count drift must fail before Current is observed.");
        }

        private static void CurrentInducedCountDriftFailsBeforeRetention()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11 }, true, 1, 1, 1, 2);
            ExpectInvalidData(source);
            Require(source.MoveNextCalls == 1, "Current-induced Count drift must fail on the first admitted item.");
            Require(source.CurrentReads == 1, "Current-induced Count drift must read the admitted Current exactly once.");
            Require(source.CountReads == 4, "Current-induced Count drift must be observed immediately after Current.");
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11 }, 1, 1, 1, 1, 1, 1, 2);
            ExpectInvalidData(source);
            Require(source.MoveNextCalls == 2, "Count drift must be checked after normal traversal termination.");
            Require(source.CurrentReads == 1, "Post-traversal Count drift must not add extra Current reads.");
        }

        private static void StableCountedSnapshotReadsEachAdmittedCurrentExactlyOnce()
        {
            var source = new InstrumentedReadOnlyList<int>(new[] { 11, 22 }, 2);
            var result = InvokeSnapshot(source);
            Require(result.SequenceEqual(new[] { 11, 22 }), "Stable counted input must preserve values and order.");
            Require(source.MoveNextCalls == 3, "Stable two-item input must include terminal MoveNext.");
            Require(source.CurrentReads == 2, "Stable input must read Current exactly once per admitted item.");
            Require(source.CountReads == 10, "Stable two-item input must bind Count at admission, around traversal, after Current, and before publication.");
        }

        private static void ExpectInvalidData(InstrumentedReadOnlyList<int> source)
        {
            try
            {
                InvokeSnapshot(source);
                throw new InvalidOperationException("Expected review workbook counted snapshot rejection.");
            }
            catch (TargetInvocationException error) when (error.InnerException is InvalidDataException)
            {
            }
        }

        private static IReadOnlyList<int> InvokeSnapshot(InstrumentedReadOnlyList<int> source)
        {
            var method = typeof(Qs3dReviewWorkbookExporter).GetMethod(
                "SnapshotCounted",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Review workbook SnapshotCounted helper is missing.");
            var closed = method.MakeGenericMethod(typeof(int));
            var admittedCount = source.Count;
            return (IReadOnlyList<int>)(closed.Invoke(null, new object[] { source, admittedCount, "smoke" })
                ?? throw new InvalidOperationException("Review workbook SnapshotCounted returned null."));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class InstrumentedReadOnlyList<T> : IReadOnlyList<T>
        {
            private readonly T[] _items;
            private readonly int[] _counts;
            private readonly bool _advanceCountOnCurrent;
            private int _countIndex;

            internal InstrumentedReadOnlyList(T[] items, params int[] counts)
                : this(items, false, counts)
            {
            }

            internal InstrumentedReadOnlyList(T[] items, bool advanceCountOnCurrent, params int[] counts)
            {
                _items = items;
                _advanceCountOnCurrent = advanceCountOnCurrent;
                _counts = counts.Length == 0 ? new[] { items.Length } : counts;
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    var index = Math.Min(_countIndex, _counts.Length - 1);
                    _countIndex++;
                    return _counts[index];
                }
            }

            public T this[int index] => _items[index];

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly InstrumentedReadOnlyList<T> _owner;
                private int _index = -1;

                internal Enumerator(InstrumentedReadOnlyList<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._advanceCountOnCurrent)
                            _owner._countIndex++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
