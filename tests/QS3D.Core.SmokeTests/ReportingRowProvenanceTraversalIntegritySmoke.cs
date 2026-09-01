using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingRowProvenanceTraversalIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LateMalformedEntryPublishesNothing();
            LateDuplicatePublishesNothing();
            EnumeratorFailurePublishesNothing();
            KnownCountOverrunRejectsBeforeExtraCurrent();
            KnownCountUnderYieldPublishesNothing();
            CountDriftAfterCurrentFailsBeforeNextMoveNext();
            MoveNextInducedCountDriftFailsBeforeCurrent();
            StableCountedSourcePublishesAtomically();
            StreamingSourceRemainsSupported();
            StreamingHardCapRejectsBeforeExtraCurrent();
        }

        private static void LateMalformedEntryPublishesNothing()
        {
            var target = Seed();
            ThrowsContaining(() => Append(target, new[] { "BB", "   " }), "empty stored SourceHandles entry at index 1");
            AssertSeedOnly(target, "late malformed");
        }

        private static void LateDuplicatePublishesNothing()
        {
            var target = Seed();
            ThrowsContaining(() => Append(target, new[] { "BB", "bb" }), "duplicate stored SourceHandles identity");
            AssertSeedOnly(target, "late duplicate");
        }

        private static void EnumeratorFailurePublishesNothing()
        {
            var target = Seed();
            var source = new ThrowingStreamingSource("BB");
            ThrowsContaining(() => Append(target, source), "hostile reporting provenance MoveNext failure");
            Equal(1, source.CurrentReads, "throwing Current reads");
            AssertSeedOnly(target, "enumerator failure");
        }

        private static void KnownCountOverrunRejectsBeforeExtraCurrent()
        {
            var target = Seed();
            var source = new CountedSource(new[] { "BB", "CC" }, admittedCount: 1);
            ThrowsContaining(() => Append(target, source), "more entries than its known Count of 1");
            Equal(2, source.MoveNextCalls, "overrun MoveNext calls");
            Equal(1, source.CurrentReads, "overrun Current reads");
            AssertSeedOnly(target, "known Count overrun");
        }

        private static void KnownCountUnderYieldPublishesNothing()
        {
            var target = Seed();
            var source = new CountedSource(new[] { "BB" }, admittedCount: 2);
            ThrowsContaining(() => Append(target, source), "known Count reported 2 entries but traversal produced 1");
            AssertSeedOnly(target, "known Count under-yield");
        }

        private static void CountDriftAfterCurrentFailsBeforeNextMoveNext()
        {
            var target = Seed();
            var source = new CountedSource(new[] { "BB", "CC" }, admittedCount: 2, driftAfterFirstCurrentTo: 3);
            ThrowsContaining(() => Append(target, source), "known Count changed during traversal from 2 to 3");
            Equal(1, source.MoveNextCalls, "pre-MoveNext drift MoveNext calls");
            Equal(1, source.CurrentReads, "pre-MoveNext drift Current reads");
            AssertSeedOnly(target, "pre-MoveNext Count drift");
        }

        private static void MoveNextInducedCountDriftFailsBeforeCurrent()
        {
            var target = Seed();
            var source = new CountedSource(new[] { "BB", "CC" }, admittedCount: 2, driftOnMoveNextCall: 2, driftTo: 3);
            ThrowsContaining(() => Append(target, source), "known Count changed during traversal from 2 to 3");
            Equal(2, source.MoveNextCalls, "MoveNext-induced drift MoveNext calls");
            Equal(1, source.CurrentReads, "MoveNext-induced drift Current reads");
            AssertSeedOnly(target, "post-MoveNext Count drift");
        }

        private static void StableCountedSourcePublishesAtomically()
        {
            var target = Seed();
            var source = new CountedSource(new[] { "BB", "CC" }, admittedCount: 2);
            Append(target, source);
            Equal(3, target.Count, "stable counted target count");
            Equal("AA", target[0], "stable seed");
            Equal("BB", target[1], "stable first staged handle");
            Equal("CC", target[2], "stable second staged handle");
        }

        private static void StreamingSourceRemainsSupported()
        {
            var target = Seed();
            Append(target, Stream("BB", "CC"));
            Equal(3, target.Count, "streaming target count");
            Equal("BB", target[1], "streaming first handle");
            Equal("CC", target[2], "streaming second handle");
        }

        private static void StreamingHardCapRejectsBeforeExtraCurrent()
        {
            var target = Seed();
            var source = new RepeatingStreamingSource("BB", 10001);
            ThrowsContaining(() => Append(target, source), "cannot exceed 10000 input entries");
            Equal(10001, source.MoveNextCalls, "streaming cap MoveNext calls");
            Equal(10000, source.CurrentReads, "streaming cap Current reads");
            AssertSeedOnly(target, "streaming hard cap");
        }

        private static List<string> Seed() => new List<string> { "AA" };

        private static IEnumerable<string> Stream(params string[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static void Append(IList<string> target, IEnumerable<string> source)
        {
            var type = typeof(DoorOpeningScheduleBuilder).Assembly.GetType("QS3D.Core.Reporting.ReportingRowProvenance", throwOnError: true)!;
            var method = type.GetMethod("AppendSourceHandles", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                method.Invoke(null, new object[] { target, source });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void AssertSeedOnly(IList<string> target, string label)
        {
            Equal(1, target.Count, label + " target count");
            Equal("AA", target[0], label + " seed");
        }

        private static void ThrowsContaining(Action action, string expectedText)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Expected reporting provenance failure containing '" + expectedText + "', got '" + ex.Message + "'.", ex);
            }
            throw new InvalidOperationException("Expected reporting provenance failure containing '" + expectedText + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ReportingRowProvenanceTraversalIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedSource : IReadOnlyCollection<string>
        {
            private readonly string[] _items;
            private readonly int _admittedCount;
            private readonly int? _driftAfterFirstCurrentTo;
            private readonly int? _driftOnMoveNextCall;
            private readonly int? _driftTo;
            private bool _drifted;

            public CountedSource(
                string[] items,
                int admittedCount,
                int? driftAfterFirstCurrentTo = null,
                int? driftOnMoveNextCall = null,
                int? driftTo = null)
            {
                _items = items;
                _admittedCount = admittedCount;
                _driftAfterFirstCurrentTo = driftAfterFirstCurrentTo;
                _driftOnMoveNextCall = driftOnMoveNextCall;
                _driftTo = driftTo;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _drifted ? (_driftTo ?? _driftAfterFirstCurrentTo ?? _admittedCount) : _admittedCount;

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly CountedSource _owner;
                private int _index = -1;

                public Enumerator(CountedSource owner) { _owner = owner; }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        var value = _owner._items[_index];
                        if (_owner.CurrentReads == 1 && _owner._driftAfterFirstCurrentTo.HasValue)
                            _owner._drifted = true;
                        return value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_owner._driftOnMoveNextCall == _owner.MoveNextCalls)
                        _owner._drifted = true;
                    return _index < _owner._items.Length;
                }

                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }
        }

        private sealed class ThrowingStreamingSource : IEnumerable<string>
        {
            private readonly string _first;
            public ThrowingStreamingSource(string first) { _first = first; }
            public int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly ThrowingStreamingSource _owner;
                private int _moveNextCalls;
                public Enumerator(ThrowingStreamingSource owner) { _owner = owner; }
                public string Current { get { _owner.CurrentReads++; return _owner._first; } }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _moveNextCalls++;
                    if (_moveNextCalls == 1) return true;
                    throw new InvalidOperationException("hostile reporting provenance MoveNext failure");
                }
                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }
        }

        private sealed class RepeatingStreamingSource : IEnumerable<string>
        {
            private readonly string _value;
            private readonly int _count;
            public RepeatingStreamingSource(string value, int count) { _value = value; _count = count; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly RepeatingStreamingSource _owner;
                private int _index = -1;
                public Enumerator(RepeatingStreamingSource owner) { _owner = owner; }
                public string Current { get { _owner.CurrentReads++; return _owner._value + _index.ToString("X"); } }
                object IEnumerator.Current => Current;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._count; }
                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }
        }
    }
}
