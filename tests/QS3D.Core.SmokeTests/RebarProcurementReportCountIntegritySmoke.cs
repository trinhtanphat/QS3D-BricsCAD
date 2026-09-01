using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarProcurementReportCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunRejectsBeforeSecondCurrent();
            KnownCountUnderYieldFailsClosed();
            TransientGrowthRejectsBeforeCurrent();
            TransientShrinkRejectsBeforeCurrent();
            TransientNegativeRejectsBeforeCurrent();
            TransientConflictRejectsBeforeCurrent();
            CurrentInducedCountDriftRejectsBeforeNullAcceptance();
            StableCountedAndStreamingRemainAccepted();
        }

        private static void KnownCountOverrunRejectsBeforeSecondCurrent()
        {
            var source = HostileCountCollection.Overrun(Result("G-OVER-1"), Result("G-OVER-2"));
            Throws<InvalidOperationException>(() => RebarProcurementReportBuilder.Build(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void KnownCountUnderYieldFailsClosed()
        {
            var source = HostileCountCollection.UnderYield(Result("G-UNDER-1"));
            Throws<InvalidOperationException>(() => RebarProcurementReportBuilder.Build(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void TransientGrowthRejectsBeforeCurrent() => AssertTransientRejected(TransientMode.Growth);
        private static void TransientShrinkRejectsBeforeCurrent() => AssertTransientRejected(TransientMode.Shrink);
        private static void TransientNegativeRejectsBeforeCurrent() => AssertTransientRejected(TransientMode.Negative);
        private static void TransientConflictRejectsBeforeCurrent() => AssertTransientRejected(TransientMode.Conflict);

        private static void AssertTransientRejected(TransientMode mode)
        {
            var source = HostileCountCollection.Transient(mode, Result("G-TRANSIENT-" + mode));
            Throws<InvalidOperationException>(() => RebarProcurementReportBuilder.Build(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void CurrentInducedCountDriftRejectsBeforeNullAcceptance()
        {
            var source = new CurrentMutatingCountCollection();
            try
            {
                RebarProcurementReportBuilder.Build(source);
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("known Count changed during traversal", StringComparison.Ordinal))
                    throw new Exception("Current-induced procurement Count drift returned the wrong integrity diagnostic: " + ex.Message, ex);
                Equal(1, source.MoveNextCalls);
                Equal(1, source.CurrentReads);
                return;
            }
            catch (ArgumentException ex)
            {
                throw new Exception("Current-induced procurement Count drift must be rejected before null-result acceptance.", ex);
            }

            throw new Exception("Current-induced procurement Count drift expected InvalidOperationException.");
        }

        private static void StableCountedAndStreamingRemainAccepted()
        {
            var counted = HostileCountCollection.Stable(Result("G-STABLE"));
            var countedRows = RebarProcurementReportBuilder.Build(counted);
            Equal(1, countedRows.Count);
            Equal(1, counted.CurrentReads);

            var streamedRows = RebarProcurementReportBuilder.Build(Stream(Result("G-STREAM")));
            Equal(1, streamedRows.Count);
            Equal("G-STREAM", streamedRows[0].GroupId);
        }

        private static IEnumerable<RebarCuttingOptimizationResult> Stream(RebarCuttingOptimizationResult result)
        {
            yield return result;
        }

        private static RebarCuttingOptimizationResult Result(string groupId)
        {
            var demand = new RebarStockDemand(
                groupId,
                "CB400-V",
                16d,
                12d,
                new[] { new RebarCutRequirement(groupId + "-CUT", 6d, 1) },
                new RebarCutAllowancePolicy(0.01d, 0d));
            return RebarCuttingOptimizer.Plan(demand);
        }

        private enum TransientMode
        {
            Stable,
            Growth,
            Shrink,
            Negative,
            Conflict
        }

        private sealed class CurrentMutatingCountCollection : IReadOnlyCollection<RebarCuttingOptimizationResult>
        {
            private bool _currentObserved;

            public int Count => _currentObserved ? 2 : 1;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<RebarCuttingOptimizationResult> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<RebarCuttingOptimizationResult>
            {
                private readonly CurrentMutatingCountCollection _owner;
                private int _index = -1;

                internal Enumerator(CurrentMutatingCountCollection owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index == 0;
                }

                public RebarCuttingOptimizationResult Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentObserved = true;
                        return null!;
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class HostileCountCollection :
            ICollection<RebarCuttingOptimizationResult>,
            IReadOnlyCollection<RebarCuttingOptimizationResult>,
            ICollection
        {
            private readonly RebarCuttingOptimizationResult[] _items;
            private readonly int _advertisedCount;
            private readonly TransientMode _mode;
            private bool _transientActive;

            private HostileCountCollection(
                RebarCuttingOptimizationResult[] items,
                int advertisedCount,
                TransientMode mode)
            {
                _items = items;
                _advertisedCount = advertisedCount;
                _mode = mode;
            }

            internal static HostileCountCollection Stable(params RebarCuttingOptimizationResult[] items) =>
                new HostileCountCollection(items, items.Length, TransientMode.Stable);

            internal static HostileCountCollection Overrun(params RebarCuttingOptimizationResult[] items) =>
                new HostileCountCollection(items, 1, TransientMode.Stable);

            internal static HostileCountCollection UnderYield(params RebarCuttingOptimizationResult[] items) =>
                new HostileCountCollection(items, items.Length + 1, TransientMode.Stable);

            internal static HostileCountCollection Transient(TransientMode mode, params RebarCuttingOptimizationResult[] items) =>
                new HostileCountCollection(items, items.Length, mode);

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            int ICollection<RebarCuttingOptimizationResult>.Count => ReadCount(CountSurface.Generic);
            int IReadOnlyCollection<RebarCuttingOptimizationResult>.Count => ReadCount(CountSurface.ReadOnly);
            int ICollection.Count => ReadCount(CountSurface.NonGeneric);
            bool ICollection<RebarCuttingOptimizationResult>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<RebarCuttingOptimizationResult> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<RebarCuttingOptimizationResult>.Add(RebarCuttingOptimizationResult item) => throw new NotSupportedException();
            void ICollection<RebarCuttingOptimizationResult>.Clear() => throw new NotSupportedException();
            bool ICollection<RebarCuttingOptimizationResult>.Contains(RebarCuttingOptimizationResult item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<RebarCuttingOptimizationResult>.CopyTo(RebarCuttingOptimizationResult[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<RebarCuttingOptimizationResult>.Remove(RebarCuttingOptimizationResult item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private int ReadCount(CountSurface surface)
            {
                if (!_transientActive || _mode == TransientMode.Stable) return _advertisedCount;
                switch (_mode)
                {
                    case TransientMode.Growth: return _advertisedCount + 1;
                    case TransientMode.Shrink: return Math.Max(0, _advertisedCount - 1);
                    case TransientMode.Negative: return -1;
                    case TransientMode.Conflict:
                        return surface == CountSurface.ReadOnly ? _advertisedCount + 1 : _advertisedCount;
                    default: return _advertisedCount;
                }
            }

            private enum CountSurface
            {
                Generic,
                ReadOnly,
                NonGeneric
            }

            private sealed class Enumerator : IEnumerator<RebarCuttingOptimizationResult>
            {
                private readonly HostileCountCollection _owner;
                private int _index = -1;

                internal Enumerator(HostileCountCollection owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_index + 1 >= _owner._items.Length) return false;
                    _index++;
                    if (_index == 0 && _owner._mode != TransientMode.Stable)
                        _owner._transientActive = true;
                    return true;
                }

                public RebarCuttingOptimizationResult Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._transientActive = false;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
