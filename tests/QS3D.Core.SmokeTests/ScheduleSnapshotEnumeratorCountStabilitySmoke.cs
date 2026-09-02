using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Scheduling;

namespace QS3D.Core.SmokeTests
{
    internal static class ScheduleSnapshotEnumeratorCountStabilitySmoke
    {
        internal static void Run()
        {
            AcquisitionCountDriftFailsBeforeTraversal();
            StableCountedActivitiesRemainAccepted();
            StreamingActivitiesRemainAccepted();
        }

        private static void AcquisitionCountDriftFailsBeforeTraversal()
        {
            var source = new AcquisitionDriftCollection(Activity("A"));
            try
            {
                _ = Snapshot(source);
                throw new Exception("Schedule snapshot accepted acquisition-induced Count drift.");
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("count changed during enumeration", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }

            if (source.MoveNextCalls != 0)
                throw new Exception("Schedule snapshot traversed after GetEnumerator changed the admitted Count: MoveNextCalls=" + source.MoveNextCalls + ".");
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

        private sealed class AcquisitionDriftCollection : ICollection<ScheduleActivity>, IReadOnlyCollection<ScheduleActivity>, ICollection
        {
            private readonly ScheduleActivity _item;
            private bool _enumeratorAcquired;

            internal AcquisitionDriftCollection(ScheduleActivity item)
            {
                _item = item;
            }

            internal int MoveNextCalls { get; private set; }

            int ICollection<ScheduleActivity>.Count => _enumeratorAcquired ? 2 : 1;
            int IReadOnlyCollection<ScheduleActivity>.Count => _enumeratorAcquired ? 2 : 1;
            int ICollection.Count => _enumeratorAcquired ? 2 : 1;
            bool ICollection<ScheduleActivity>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ScheduleActivity> GetEnumerator()
            {
                _enumeratorAcquired = true;
                return new CountingEnumerator(this, _item);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<ScheduleActivity>.Add(ScheduleActivity item) => throw new NotSupportedException();
            void ICollection<ScheduleActivity>.Clear() => throw new NotSupportedException();
            bool ICollection<ScheduleActivity>.Contains(ScheduleActivity item) => ReferenceEquals(item, _item);
            void ICollection<ScheduleActivity>.CopyTo(ScheduleActivity[] array, int arrayIndex) => array[arrayIndex] = _item;
            bool ICollection<ScheduleActivity>.Remove(ScheduleActivity item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);

            private sealed class CountingEnumerator : IEnumerator<ScheduleActivity>
            {
                private readonly AcquisitionDriftCollection _owner;
                private readonly ScheduleActivity _item;
                private int _state;

                internal CountingEnumerator(AcquisitionDriftCollection owner, ScheduleActivity item)
                {
                    _owner = owner;
                    _item = item;
                }

                public ScheduleActivity Current => _state == 1 ? _item : throw new InvalidOperationException();
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
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class ScheduleSnapshotEnumeratorCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ScheduleSnapshotEnumeratorCountStabilitySmoke.Run();
        }
    }
}
