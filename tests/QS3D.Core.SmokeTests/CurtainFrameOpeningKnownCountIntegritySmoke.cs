using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectFrameOverrunBeforeCurrent();
            RejectOpeningOverrunBeforeCurrent();
            RejectFrameUnderYield();
            RejectOpeningUnderYield();
            RejectFrameTransientGrowthAfterMoveNextBeforeCurrent();
            RejectOpeningTransientNegativeAfterMoveNextBeforeCurrent();
            RejectOpeningTransientShrinkBeforeNextMoveNext();
            RejectFramePostTraversalCountDrift();
            RejectOpeningPostTraversalNegativeCount();
            RejectOpeningPostTraversalCountConflict();
            AcceptStableMultiInterfaceCounts();
            AcceptPureStreamingInputs();
        }

        private static void RejectFrameOverrunBeforeCurrent()
        {
            var frames = new CurrentCountingReadOnlySource<CurtainWallRect>(2, 1, i => Frame(i), true);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(frames, Array.Empty<CurtainOpeningRect>()), "frame collection count changed", "Frame Count=1 must reject frame 2 before reading Current.");
            if (frames.MoveNextCalls != 2 || frames.CurrentReads != 1)
                throw new InvalidOperationException("Curtain frame known-Count overrun must stop at N+1 MoveNext without N+1 Current.");
        }

        private static void RejectOpeningOverrunBeforeCurrent()
        {
            var openings = new CurrentCountingReadOnlySource<CurtainOpeningRect>(2, 1, i => Opening(i), true);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(new[] { Frame(0) }, openings), "opening collection count changed", "Opening Count=1 must reject opening 2 before reading Current.");
            if (openings.MoveNextCalls != 2 || openings.CurrentReads != 1)
                throw new InvalidOperationException("Curtain opening known-Count overrun must stop at N+1 MoveNext without N+1 Current.");
        }

        private static void RejectFrameUnderYield()
        {
            var frames = new MultiCountSource<CurtainWallRect>(new[] { Frame(0) }, 2, 2, 2);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(frames, Array.Empty<CurtainOpeningRect>()), "frame collection count changed", "Curtain frames must reject Count=2 with one yielded frame.");
        }

        private static void RejectOpeningUnderYield()
        {
            var openings = new MultiCountSource<CurtainOpeningRect>(new[] { Opening(0) }, 2, 2, 2);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(new[] { Frame(0) }, openings), "opening collection count changed", "Curtain openings must reject Count=2 with one yielded opening.");
        }

        private static void RejectFrameTransientGrowthAfterMoveNextBeforeCurrent()
        {
            var frames = new TransientCountReadOnlySource<CurtainWallRect>(
                new[] { Frame(0) },
                read => read >= 3 ? 2 : 1);
            ExpectInvalid(
                () => CurtainFrameOpeningPlanner.Interrupt(frames, Array.Empty<CurtainOpeningRect>()),
                "frame collection count changed",
                "Curtain frames must reject transient Count growth after MoveNext before Current.");
            if (frames.MoveNextCalls != 1 || frames.CurrentReads != 0)
                throw new InvalidOperationException("Transient frame Count growth must fail after the successful MoveNext and before Current.");
        }

        private static void RejectOpeningTransientNegativeAfterMoveNextBeforeCurrent()
        {
            var openings = new TransientCountReadOnlySource<CurtainOpeningRect>(
                new[] { Opening(0) },
                read => read >= 3 ? -1 : 1);
            ExpectInvalid(
                () => CurtainFrameOpeningPlanner.Interrupt(new[] { Frame(0) }, openings),
                "invalid negative count",
                "Curtain openings must reject transient negative Count after MoveNext before Current.");
            if (openings.MoveNextCalls != 1 || openings.CurrentReads != 0)
                throw new InvalidOperationException("Transient negative opening Count must fail after the successful MoveNext and before Current.");
        }

        private static void RejectOpeningTransientShrinkBeforeNextMoveNext()
        {
            var openings = new TransientCountReadOnlySource<CurtainOpeningRect>(
                new[] { Opening(0), Opening(1) },
                read => read >= 4 ? 1 : 2);
            ExpectInvalid(
                () => CurtainFrameOpeningPlanner.Interrupt(new[] { Frame(0) }, openings),
                "opening collection count changed",
                "Curtain openings must reject transient Count shrink before advancing to the next item.");
            if (openings.MoveNextCalls != 1 || openings.CurrentReads != 1)
                throw new InvalidOperationException("Transient opening Count shrink must fail before the second caller-controlled MoveNext.");
        }

        private static void RejectFramePostTraversalCountDrift()
        {
            var frames = new MultiCountSource<CurtainWallRect>(new[] { Frame(0) }, 1, 1, 1, 2, 2, 2);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(frames, Array.Empty<CurtainOpeningRect>()), "frame collection count changed", "Curtain frames must reject deterministic Count drift after traversal.");
        }

        private static void RejectOpeningPostTraversalNegativeCount()
        {
            var openings = new MultiCountSource<CurtainOpeningRect>(new[] { Opening(0) }, 1, 1, 1, -1, -1, -1);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(new[] { Frame(0) }, openings), "invalid negative count", "Curtain openings must reject rebound negative Count evidence.");
        }

        private static void RejectOpeningPostTraversalCountConflict()
        {
            var openings = new MultiCountSource<CurtainOpeningRect>(new[] { Opening(0) }, 1, 1, 1, 1, 2, 1);
            ExpectInvalid(() => CurtainFrameOpeningPlanner.Interrupt(new[] { Frame(0) }, openings), "conflicting known counts", "Curtain openings must reject rebound conflicting Count evidence.");
        }

        private static void AcceptStableMultiInterfaceCounts()
        {
            var frames = new MultiCountSource<CurtainWallRect>(new[] { Frame(0) }, 1, 1, 1);
            var openings = new MultiCountSource<CurtainOpeningRect>(Array.Empty<CurtainOpeningRect>(), 0, 0, 0);
            var result = CurtainFrameOpeningPlanner.Interrupt(frames, openings);
            if (result.Count != 1)
                throw new InvalidOperationException("Stable multi-interface curtain Count evidence must preserve ordinary geometry.");
        }

        private static void AcceptPureStreamingInputs()
        {
            var result = CurtainFrameOpeningPlanner.Interrupt(StreamFrames(), StreamOpenings());
            if (result.Count != 1)
                throw new InvalidOperationException("Pure streaming curtain inputs must remain supported.");
        }

        private static IEnumerable<CurtainWallRect> StreamFrames()
        {
            yield return new CurtainWallRect(0d, 0d, 4d, 4d);
        }

        private static IEnumerable<CurtainOpeningRect> StreamOpenings()
        {
            yield return new CurtainOpeningRect(10d, 10d, 1d, 1d);
        }

        private static CurtainWallRect Frame(int index) => new CurtainWallRect(index * 2d, 0d, 1d, 1d);
        private static CurtainOpeningRect Opening(int index) => new CurtainOpeningRect(10d + index * 2d, 10d, 1d, 1d);

        private static void ExpectInvalid(Action action, string fragment, string failure)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failure + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failure);
        }

        private sealed class MultiCountSource<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int? _finalGenericCount;
            private readonly int? _finalReadOnlyCount;
            private readonly int? _finalNonGenericCount;

            internal MultiCountSource(T[] items, int genericCount, int readOnlyCount, int nonGenericCount, int? finalGenericCount = null, int? finalReadOnlyCount = null, int? finalNonGenericCount = null)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _finalGenericCount = finalGenericCount;
                _finalReadOnlyCount = finalReadOnlyCount;
                _finalNonGenericCount = finalNonGenericCount;
            }

            internal bool TraversalCompleted { get; private set; }
            int ICollection<T>.Count => TraversalCompleted && _finalGenericCount.HasValue ? _finalGenericCount.Value : _genericCount;
            int IReadOnlyCollection<T>.Count => TraversalCompleted && _finalReadOnlyCount.HasValue ? _finalReadOnlyCount.Value : _readOnlyCount;
            int ICollection.Count => TraversalCompleted && _finalNonGenericCount.HasValue ? _finalNonGenericCount.Value : _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly MultiCountSource<T> _owner;
                private int _index = -1;
                internal Enumerator(MultiCountSource<T> owner) { _owner = owner; }
                public T Current => _owner._items[_index];
                object IEnumerator.Current => Current!;
                public bool MoveNext()
                {
                    _index++;
                    if (_index < _owner._items.Length) return true;
                    _owner.TraversalCompleted = true;
                    return false;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TransientCountReadOnlySource<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly Func<int, int> _countByRead;

            internal TransientCountReadOnlySource(T[] items, Func<int, int> countByRead)
            {
                _items = items;
                _countByRead = countByRead;
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public int Count
            {
                get
                {
                    CountReads++;
                    return _countByRead(CountReads);
                }
            }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCountReadOnlySource<T> _owner;
                private int _index = -1;
                internal Enumerator(TransientCountReadOnlySource<T> owner) { _owner = owner; }
                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
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

        private sealed class CurrentCountingReadOnlySource<T> : IReadOnlyCollection<T>
        {
            private readonly int _actualCount;
            private readonly Func<int, T> _factory;
            private readonly bool _throwOnUnexpectedCurrent;
            internal CurrentCountingReadOnlySource(int actualCount, int reportedCount, Func<int, T> factory, bool throwOnUnexpectedCurrent)
            {
                _actualCount = actualCount;
                Count = reportedCount;
                _factory = factory;
                _throwOnUnexpectedCurrent = throwOnUnexpectedCurrent;
            }
            public int Count { get; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentCountingReadOnlySource<T> _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingReadOnlySource<T> owner) { _owner = owner; }
                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._throwOnUnexpectedCurrent && _owner.CurrentReads > _owner.Count)
                            throw new InvalidOperationException("Unexpected curtain Current read beyond admitted Count.");
                        return _owner._factory(_index);
                    }
                }
                object IEnumerator.Current => Current!;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._actualCount;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
