using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectZeroCountOverYieldBeforeCurrent();
            RejectTransientMoveNextCountDrift();
            RejectTransientCurrentCountDrift();
            AcceptStableCountedSource();
            AcceptPureStreamingSource();
        }

        private static void RejectZeroCountOverYieldBeforeCurrent()
        {
            var source = new HostileCountSource(0, DriftPoint.None, yieldItem: true);
            ExpectInvalidOperation(() => new WallJunctionPlanner().Plan(source), "Count=0 over-yield");
            if (source.CurrentReads != 0) throw new Exception("Wall junction known-count over-yield must fail before unexpected Current.");
        }

        private static void RejectTransientMoveNextCountDrift()
        {
            var source = new HostileCountSource(1, DriftPoint.MoveNext, yieldItem: true);
            ExpectInvalidOperation(() => new WallJunctionPlanner().Plan(source), "MoveNext Count drift");
            if (source.CurrentReads != 0) throw new Exception("Wall junction MoveNext Count drift must fail before Current.");
        }

        private static void RejectTransientCurrentCountDrift()
        {
            var source = new HostileCountSource(1, DriftPoint.Current, yieldItem: true);
            ExpectInvalidOperation(() => new WallJunctionPlanner().Plan(source), "Current Count drift");
            if (source.CurrentReads != 1) throw new Exception("Wall junction Current Count drift probe should read Current exactly once.");
        }

        private static void AcceptStableCountedSource()
        {
            var source = new HostileCountSource(1, DriftPoint.None, yieldItem: true);
            var result = new WallJunctionPlanner().Plan(source);
            if (result.Count != 2) throw new Exception("Stable single-segment wall junction input should publish two end junctions.");
        }

        private static void AcceptPureStreamingSource()
        {
            IEnumerable<WallAxisSegment> Source()
            {
                yield return Segment("STREAM");
            }
            var result = new WallJunctionPlanner().Plan(Source());
            if (result.Count != 2) throw new Exception("Pure-streaming wall junction input must remain supported.");
        }

        private static WallAxisSegment Segment(string id) => new WallAxisSegment(id, new Point2(0d, 0d), new Point2(1d, 0d));

        private static void ExpectInvalidOperation(Action action, string label)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new Exception("Expected wall junction known-count rejection: " + label + ".");
        }

        private enum DriftPoint
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileCountSource : IEnumerable<WallAxisSegment>, IReadOnlyCollection<WallAxisSegment>, ICollection
        {
            private readonly int _admittedCount;
            private readonly DriftPoint _driftPoint;
            private readonly bool _yieldItem;
            private int _reportedCount;

            public HostileCountSource(int admittedCount, DriftPoint driftPoint, bool yieldItem)
            {
                _admittedCount = admittedCount;
                _reportedCount = admittedCount;
                _driftPoint = driftPoint;
                _yieldItem = yieldItem;
            }

            public int Count => _reportedCount;
            int ICollection.Count => _reportedCount;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public int CurrentReads { get; private set; }

            public IEnumerator<WallAxisSegment> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<WallAxisSegment>
            {
                private readonly HostileCountSource _owner;
                private int _state;

                public Enumerator(HostileCountSource owner) { _owner = owner; }

                public WallAxisSegment Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current) _owner._reportedCount = _owner._admittedCount + 1;
                        return Segment("HOSTILE");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_state++ != 0 || !_owner._yieldItem)
                    {
                        _owner._reportedCount = _owner._admittedCount;
                        return false;
                    }
                    if (_owner._driftPoint == DriftPoint.MoveNext) _owner._reportedCount = _owner._admittedCount + 1;
                    else _owner._reportedCount = _owner._admittedCount;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
