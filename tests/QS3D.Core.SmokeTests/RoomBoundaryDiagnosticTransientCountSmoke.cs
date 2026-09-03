using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryDiagnosticTransientCountSmoke
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
            StableCountedInputRemainsAccepted();
            StreamingInputRemainsAccepted();
        }

        private static void AdvertisedOverrunRejectsBeforeSecondCurrent()
        {
            var source = HostileSegments.Overrun(Segment("OVER-1"), Segment("OVER-2"));
            Throws<InvalidOperationException>(() => Analyze(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void AssertMoveNextTransientRejected(TransientMode mode)
        {
            var source = HostileSegments.MoveNextTransient(mode, Segment("MOVE-" + mode));
            Throws<InvalidOperationException>(() => Analyze(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void AssertCurrentTransientRejected(TransientMode mode)
        {
            var source = HostileSegments.CurrentTransient(mode, Segment("CURRENT-" + mode));
            Throws<InvalidOperationException>(() => Analyze(source));
            Equal(1, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
            Equal(1, source.PostCurrentCountRebounds);
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = HostileSegments.Stable(Segment("STABLE"));
            var analysis = Analyze(source);
            Equal(RoomBoundaryDiagnosticReason.InsufficientSegments, analysis.Report.Reason);
            Equal(1, analysis.Report.InputSegmentCount);
            Equal(1, source.CurrentReads);
        }

        private static void StreamingInputRemainsAccepted()
        {
            var analysis = Analyze(Streaming(Segment("STREAM")));
            Equal(RoomBoundaryDiagnosticReason.InsufficientSegments, analysis.Report.Reason);
            Equal(1, analysis.Report.InputSegmentCount);
        }

        private static IEnumerable<BoundarySegment> Streaming(BoundarySegment segment)
        {
            yield return segment;
        }

        private static RoomBoundaryDiagnosticAnalysis Analyze(IEnumerable<BoundarySegment> source) =>
            new RoomBoundaryDiagnosticService().Analyze(source);

        private static BoundarySegment Segment(string id) =>
            new BoundarySegment(new Point2(0d, 0d), new Point2(1d, 0d), id);

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
            ICollection<BoundarySegment>,
            IReadOnlyCollection<BoundarySegment>,
            ICollection
        {
            private readonly BoundarySegment[] _items;
            private readonly int _advertisedCount;
            private readonly TransientMode _mode;
            private readonly TriggerPoint _triggerPoint;
            private bool _transientActive;
            private bool _awaitingPostCurrentRebound;

            private HostileSegments(
                BoundarySegment[] items,
                int advertisedCount,
                TransientMode mode,
                TriggerPoint triggerPoint)
            {
                _items = items;
                _advertisedCount = advertisedCount;
                _mode = mode;
                _triggerPoint = triggerPoint;
            }

            internal static HostileSegments Stable(params BoundarySegment[] items) =>
                new HostileSegments(items, items.Length, TransientMode.Stable, TriggerPoint.None);

            internal static HostileSegments Overrun(params BoundarySegment[] items) =>
                new HostileSegments(items, 1, TransientMode.Stable, TriggerPoint.None);

            internal static HostileSegments MoveNextTransient(TransientMode mode, params BoundarySegment[] items) =>
                new HostileSegments(items, items.Length, mode, TriggerPoint.MoveNext);

            internal static HostileSegments CurrentTransient(TransientMode mode, params BoundarySegment[] items) =>
                new HostileSegments(items, items.Length, mode, TriggerPoint.Current);

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int PostCurrentCountRebounds { get; private set; }

            int ICollection<BoundarySegment>.Count => ReadCount(CountSurface.Generic);
            int IReadOnlyCollection<BoundarySegment>.Count => ReadCount(CountSurface.ReadOnly);
            int ICollection.Count => ReadCount(CountSurface.NonGeneric);
            bool ICollection<BoundarySegment>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<BoundarySegment> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<BoundarySegment>.Add(BoundarySegment item) => throw new NotSupportedException();
            void ICollection<BoundarySegment>.Clear() => throw new NotSupportedException();
            bool ICollection<BoundarySegment>.Contains(BoundarySegment item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<BoundarySegment>.CopyTo(BoundarySegment[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<BoundarySegment>.Remove(BoundarySegment item) => throw new NotSupportedException();
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

            private enum CountSurface
            {
                Generic,
                ReadOnly,
                NonGeneric
            }

            private sealed class Enumerator : IEnumerator<BoundarySegment>
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

                public BoundarySegment Current
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
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
