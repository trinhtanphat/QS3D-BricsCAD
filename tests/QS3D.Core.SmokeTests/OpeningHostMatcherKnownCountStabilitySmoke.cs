using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningHostMatcherKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AdvertisedOverrunRejectsBeforeSecondCurrent();
            AssertMoveNextTransientRejected(TransientMode.Growth);
            AssertMoveNextTransientRejected(TransientMode.Shrink);
            AssertMoveNextTransientRejected(TransientMode.Negative);
            AssertMoveNextTransientRejected(TransientMode.Conflict);
            AssertCurrentTransientRejected(TransientMode.Growth);
            AssertCurrentTransientRejected(TransientMode.Shrink);
            AssertCurrentTransientRejected(TransientMode.Negative);
            AssertCurrentTransientRejected(TransientMode.Conflict);
            StableCountedInputStillMatches();
            StreamingInputStillMatches();
        }

        private static void AdvertisedOverrunRejectsBeforeSecondCurrent()
        {
            var source = HostileSegments.Overrun(Segment("HOST-A", 0d), Segment("HOST-B", 3d));
            Throws<InvalidOperationException>(() => Match(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void AssertMoveNextTransientRejected(TransientMode mode)
        {
            var source = HostileSegments.MoveNextTransient(mode, Segment("HOST-MOVE-" + mode, 0d));
            Throws<InvalidOperationException>(() => Match(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void AssertCurrentTransientRejected(TransientMode mode)
        {
            var source = HostileSegments.CurrentTransient(mode, Segment("HOST-CURRENT-" + mode, 0d));
            Throws<InvalidOperationException>(() => Match(source));
            Equal(1, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
            Equal(1, source.PostCurrentCountRebounds);
        }

        private static void StableCountedInputStillMatches()
        {
            var source = HostileSegments.Stable(Segment("HOST-STABLE", 0d));
            var result = Match(source);
            Equal(OpeningHostMatchStatus.Matched, result.Status);
            Equal("HOST-STABLE", result.HostElementId);
            Equal(1, result.CandidateHostCount);
            Equal(1, source.CurrentReads);
        }

        private static void StreamingInputStillMatches()
        {
            var result = Match(Streaming(Segment("HOST-STREAM", 0d)));
            Equal(OpeningHostMatchStatus.Matched, result.Status);
            Equal("HOST-STREAM", result.HostElementId);
            Equal(1, result.CandidateHostCount);
        }

        private static IEnumerable<OpeningHostSegment> Streaming(OpeningHostSegment segment)
        {
            yield return segment;
        }

        private static OpeningHostMatchResult Match(IEnumerable<OpeningHostSegment> source) =>
            new OpeningHostMatcher().Match(new Point2(0.5d, 0.05d), source, 0.25d, 0.02d);

        private static OpeningHostSegment Segment(string id, double y) =>
            new OpeningHostSegment(id, new Point2(0d, y), new Point2(1d, y), 0.2d);

        private enum TransientMode
        {
            Stable,
            Growth,
            Shrink,
            Negative,
            Conflict
        }

        private enum TriggerPoint
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileSegments :
            ICollection<OpeningHostSegment>,
            IReadOnlyCollection<OpeningHostSegment>,
            ICollection
        {
            private readonly OpeningHostSegment[] _items;
            private readonly int _advertisedCount;
            private readonly TransientMode _mode;
            private readonly TriggerPoint _triggerPoint;
            private bool _transientActive;
            private bool _awaitingPostCurrentRebound;

            private HostileSegments(
                OpeningHostSegment[] items,
                int advertisedCount,
                TransientMode mode,
                TriggerPoint triggerPoint)
            {
                _items = items;
                _advertisedCount = advertisedCount;
                _mode = mode;
                _triggerPoint = triggerPoint;
            }

            internal static HostileSegments Stable(params OpeningHostSegment[] items) =>
                new HostileSegments(items, items.Length, TransientMode.Stable, TriggerPoint.None);

            internal static HostileSegments Overrun(params OpeningHostSegment[] items) =>
                new HostileSegments(items, 1, TransientMode.Stable, TriggerPoint.None);

            internal static HostileSegments MoveNextTransient(TransientMode mode, params OpeningHostSegment[] items) =>
                new HostileSegments(items, items.Length, mode, TriggerPoint.MoveNext);

            internal static HostileSegments CurrentTransient(TransientMode mode, params OpeningHostSegment[] items) =>
                new HostileSegments(items, items.Length, mode, TriggerPoint.Current);

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int PostCurrentCountRebounds { get; private set; }

            int ICollection<OpeningHostSegment>.Count => ReadCount(CountSurface.Generic);
            int IReadOnlyCollection<OpeningHostSegment>.Count => ReadCount(CountSurface.ReadOnly);
            int ICollection.Count => ReadCount(CountSurface.NonGeneric);
            bool ICollection<OpeningHostSegment>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<OpeningHostSegment> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<OpeningHostSegment>.Add(OpeningHostSegment item) => throw new NotSupportedException();
            void ICollection<OpeningHostSegment>.Clear() => throw new NotSupportedException();
            bool ICollection<OpeningHostSegment>.Contains(OpeningHostSegment item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<OpeningHostSegment>.CopyTo(OpeningHostSegment[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<OpeningHostSegment>.Remove(OpeningHostSegment item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private int ReadCount(CountSurface surface)
            {
                if (_awaitingPostCurrentRebound)
                {
                    PostCurrentCountRebounds++;
                    _awaitingPostCurrentRebound = false;
                }
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

            private enum CountSurface { Generic, ReadOnly, NonGeneric }

            private sealed class Enumerator : IEnumerator<OpeningHostSegment>
            {
                private readonly HostileSegments _owner;
                private int _index = -1;

                internal Enumerator(HostileSegments owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _owner._transientActive = false;
                    if (_index + 1 >= _owner._items.Length) return false;
                    _index++;
                    if (_index == 0 && _owner._triggerPoint == TriggerPoint.MoveNext && _owner._mode != TransientMode.Stable)
                        _owner._transientActive = true;
                    return true;
                }

                public OpeningHostSegment Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._transientActive = _owner._triggerPoint == TriggerPoint.Current && _owner._mode != TransientMode.Stable;
                        _owner._awaitingPostCurrentRebound = _owner._transientActive;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
