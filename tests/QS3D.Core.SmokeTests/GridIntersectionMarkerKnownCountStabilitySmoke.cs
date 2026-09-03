using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionMarkerKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AdvertisedOverrunRejectsBeforeSecondCurrent();
            AssertTransientRejected(TransientMode.Growth);
            AssertTransientRejected(TransientMode.Shrink);
            AssertTransientRejected(TransientMode.Negative);
            AssertTransientRejected(TransientMode.Conflict);
            StableCountedInputStillPlans();
        }

        private static void AdvertisedOverrunRejectsBeforeSecondCurrent()
        {
            var source = HostileIntersections.Overrun(Intersection("A", "B", 0d), Intersection("C", "D", 1d));
            Throws<InvalidOperationException>(() => GridIntersectionMarkerPlanner.Plan(source));
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void AssertTransientRejected(TransientMode mode)
        {
            var source = HostileIntersections.Transient(mode, Intersection("A", "B", 0d));
            Throws<InvalidOperationException>(() => GridIntersectionMarkerPlanner.Plan(source));
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void StableCountedInputStillPlans()
        {
            var source = HostileIntersections.Stable(Intersection("A", "B", 0d));
            var result = GridIntersectionMarkerPlanner.Plan(source);
            Equal(1, result.Count);
            Equal("A", result[0].FirstElementId);
            Equal("B", result[0].SecondElementId);
            Equal(1, source.CurrentReads);
        }

        private static GridIntersection Intersection(string first, string second, double x) =>
            new GridIntersection(first, second, new Point2(x, x));

        private enum TransientMode { Stable, Growth, Shrink, Negative, Conflict }

        private sealed class HostileIntersections : ICollection<GridIntersection>, IReadOnlyCollection<GridIntersection>, ICollection
        {
            private readonly GridIntersection[] _items;
            private readonly int _advertisedCount;
            private readonly TransientMode _mode;
            private bool _transientActive;

            private HostileIntersections(GridIntersection[] items, int advertisedCount, TransientMode mode)
            {
                _items = items;
                _advertisedCount = advertisedCount;
                _mode = mode;
            }

            internal static HostileIntersections Stable(params GridIntersection[] items) => new HostileIntersections(items, items.Length, TransientMode.Stable);
            internal static HostileIntersections Overrun(params GridIntersection[] items) => new HostileIntersections(items, 1, TransientMode.Stable);
            internal static HostileIntersections Transient(TransientMode mode, params GridIntersection[] items) => new HostileIntersections(items, items.Length, mode);

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            int ICollection<GridIntersection>.Count => ReadCount(CountSurface.Generic);
            int IReadOnlyCollection<GridIntersection>.Count => ReadCount(CountSurface.ReadOnly);
            int ICollection.Count => ReadCount(CountSurface.NonGeneric);
            bool ICollection<GridIntersection>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<GridIntersection> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<GridIntersection>.Add(GridIntersection item) => throw new NotSupportedException();
            void ICollection<GridIntersection>.Clear() => throw new NotSupportedException();
            bool ICollection<GridIntersection>.Contains(GridIntersection item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<GridIntersection>.CopyTo(GridIntersection[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<GridIntersection>.Remove(GridIntersection item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private int ReadCount(CountSurface surface)
            {
                if (!_transientActive || _mode == TransientMode.Stable) return _advertisedCount;
                switch (_mode)
                {
                    case TransientMode.Growth: return _advertisedCount + 1;
                    case TransientMode.Shrink: return Math.Max(0, _advertisedCount - 1);
                    case TransientMode.Negative: return -1;
                    case TransientMode.Conflict: return surface == CountSurface.ReadOnly ? _advertisedCount + 1 : _advertisedCount;
                    default: return _advertisedCount;
                }
            }

            private enum CountSurface { Generic, ReadOnly, NonGeneric }

            private sealed class Enumerator : IEnumerator<GridIntersection>
            {
                private readonly HostileIntersections _owner;
                private int _index = -1;
                internal Enumerator(HostileIntersections owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_index + 1 >= _owner._items.Length) return false;
                    _index++;
                    if (_index == 0 && _owner._mode != TransientMode.Stable) _owner._transientActive = true;
                    return true;
                }

                public GridIntersection Current
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
