using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Scheduling;

namespace QS3D.Core.SmokeTests
{
    internal static class ScheduleSnapshotTraversalCountStabilitySmoke
    {
        internal static void Run()
        {
            MoveNextCountDriftFailsBeforeCurrent();
            CurrentCountDriftFailsBeforeAcceptance();
            StableCountedActivitiesRemainAccepted();
            StreamingActivitiesRemainAccepted();
        }

        private static void MoveNextCountDriftFailsBeforeCurrent()
        {
            var source = new TraversalDriftCollection(Activity("M"), DriftPoint.MoveNext);
            ExpectCountDrift(source);
            if (source.CurrentReads != 0)
                throw new Exception("Schedule snapshot read Current after MoveNext changed admitted Count: CurrentReads=" + source.CurrentReads + ".");
        }

        private static void CurrentCountDriftFailsBeforeAcceptance()
        {
            var source = new TraversalDriftCollection(Activity("C"), DriftPoint.Current);
            ExpectCountDrift(source);
            if (source.CurrentReads != 1)
                throw new Exception("Schedule Current-drift control expected exactly one Current read, actual=" + source.CurrentReads + ".");
            if (source.MoveNextCalls != 1)
                throw new Exception("Schedule Current-drift control expected one MoveNext, actual=" + source.MoveNextCalls + ".");
        }

        private static void ExpectCountDrift(TraversalDriftCollection source)
        {
            try
            {
                _ = Snapshot(source);
                throw new Exception("Schedule snapshot accepted traversal-induced Count drift.");
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("count changed during enumeration", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }
        }

        private static void StableCountedActivitiesRemainAccepted()
        {
            var snapshot = Snapshot(new List<ScheduleActivity> { Activity("B"), Activity("A") });
            if (snapshot.Activities.Count != 2 || snapshot.Activities[0].Id != "A" || snapshot.Activities[1].Id != "B")
                throw new Exception("Stable counted schedule activities lost deterministic acceptance/order.");
        }

        private static void StreamingActivitiesRemainAccepted()
        {
            var snapshot = Snapshot(Stream(Activity("S2"), Activity("S1")));
            if (snapshot.Activities.Count != 2 || snapshot.Activities[0].Id != "S1" || snapshot.Activities[1].Id != "S2")
                throw new Exception("Pure streaming schedule activities lost deterministic acceptance/order.");
        }

        private static ScheduleSnapshot Snapshot(IEnumerable<ScheduleActivity> activities)
        {
            return new ScheduleSnapshot(
                "SCH",
                "VER",
                "ALLOC",
                "Asia/Bangkok",
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Unspecified),
                activities);
        }

        private static ScheduleActivity Activity(string id)
        {
            return new ScheduleActivity(
                id,
                "Activity " + id,
                new DateTime(2026, 9, 2, 8, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 9, 2, 17, 0, 0, DateTimeKind.Unspecified),
                "CAL",
                "V1");
        }

        private static IEnumerable<ScheduleActivity> Stream(params ScheduleActivity[] activities)
        {
            for (var i = 0; i < activities.Length; i++)
                yield return activities[i];
        }

        private enum DriftPoint
        {
            MoveNext,
            Current
        }

        private sealed class TraversalDriftCollection : ICollection<ScheduleActivity>, IReadOnlyCollection<ScheduleActivity>, ICollection
        {
            private readonly ScheduleActivity _item;
            private readonly DriftPoint _driftPoint;
            private bool _drifted;

            internal TraversalDriftCollection(ScheduleActivity item, DriftPoint driftPoint)
            {
                _item = item;
                _driftPoint = driftPoint;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            private int CurrentCount => _drifted ? 2 : 1;
            int ICollection<ScheduleActivity>.Count => CurrentCount;
            int IReadOnlyCollection<ScheduleActivity>.Count => CurrentCount;
            int ICollection.Count => CurrentCount;
            bool ICollection<ScheduleActivity>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ScheduleActivity> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<ScheduleActivity>.Add(ScheduleActivity item) => throw new NotSupportedException();
            void ICollection<ScheduleActivity>.Clear() => throw new NotSupportedException();
            bool ICollection<ScheduleActivity>.Contains(ScheduleActivity item) => ReferenceEquals(item, _item);
            void ICollection<ScheduleActivity>.CopyTo(ScheduleActivity[] array, int arrayIndex) => array[arrayIndex] = _item;
            bool ICollection<ScheduleActivity>.Remove(ScheduleActivity item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);

            private sealed class Enumerator : IEnumerator<ScheduleActivity>
            {
                private readonly TraversalDriftCollection _owner;
                private int _state;

                internal Enumerator(TraversalDriftCollection owner) => _owner = owner;

                public ScheduleActivity Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_state != 1) throw new InvalidOperationException();
                        if (_owner._driftPoint == DriftPoint.Current) _owner._drifted = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_state != 0)
                    {
                        _state = 2;
                        return false;
                    }
                    _state = 1;
                    if (_owner._driftPoint == DriftPoint.MoveNext) _owner._drifted = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class ScheduleSnapshotTraversalCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ScheduleSnapshotTraversalCountStabilitySmoke.Run();
    }
}
