using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Scheduling;

namespace QS3D.Core.SmokeTests
{
    internal static class ScheduleSnapshotCountDriftSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsKnownCountOverrunBeforeUnexpectedItemProcessing();
            RejectsCountMetadataDriftAfterTraversal();
            RejectsUnderTraversal();
            AcceptsHonestMultiInterfaceCollection();
            AcceptsPureStreamingInput();
            RejectsDependencyCountDriftBeforeSemanticValidation();
        }

        private static void RejectsKnownCountOverrunBeforeUnexpectedItemProcessing()
        {
            var first = Activity("A1");
            ScheduleActivity? unexpected = null;
            var source = new AdversarialCollection<ScheduleActivity>(
                new[] { first, unexpected! },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1);

            var ex = Throws<ArgumentException>(() => Snapshot(source));
            Contains(ex.Message, "count changed during enumeration");
        }

        private static void RejectsCountMetadataDriftAfterTraversal()
        {
            var source = new AdversarialCollection<ScheduleActivity>(
                new[] { Activity("A1"), Activity("A2") },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2,
                afterEnumeration: collection => collection.SetCounts(1, 1, 1));

            var ex = Throws<ArgumentException>(() => Snapshot(source));
            Contains(ex.Message, "count changed during enumeration");
        }

        private static void RejectsUnderTraversal()
        {
            var source = new AdversarialCollection<ScheduleActivity>(
                new[] { Activity("A1") },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2);

            var ex = Throws<ArgumentException>(() => Snapshot(source));
            Contains(ex.Message, "count changed during enumeration");
        }

        private static void AcceptsHonestMultiInterfaceCollection()
        {
            var source = new AdversarialCollection<ScheduleActivity>(
                new[] { Activity("A2"), Activity("A1") },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2);

            var snapshot = Snapshot(source);
            Equal(2, snapshot.Activities.Count);
            Equal("A1", snapshot.Activities[0].Id);
            Equal("A2", snapshot.Activities[1].Id);
        }

        private static void AcceptsPureStreamingInput()
        {
            var snapshot = Snapshot(Stream(Activity("A1"), Activity("A2")));
            Equal(2, snapshot.Activities.Count);
        }

        private static void RejectsDependencyCountDriftBeforeSemanticValidation()
        {
            var activities = new[] { Activity("A1"), Activity("A2") };
            var dependencies = new AdversarialCollection<ScheduleDependency>(
                new[]
                {
                    new ScheduleDependency("A1", "A2"),
                    new ScheduleDependency("UNKNOWN", "A2")
                },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1);

            var ex = Throws<ArgumentException>(() => new ScheduleSnapshot(
                "SCH", "V1", "ALLOC1", "Asia/Bangkok", Date(), activities, dependencies));
            Contains(ex.Message, "count changed during enumeration");
        }

        private static ScheduleSnapshot Snapshot(IEnumerable<ScheduleActivity> activities)
        {
            return new ScheduleSnapshot("SCH", "V1", "ALLOC1", "Asia/Bangkok", Date(), activities);
        }

        private static ScheduleActivity Activity(string id)
        {
            var start = new DateTime(2026, 8, 28, 8, 0, 0, DateTimeKind.Unspecified);
            return new ScheduleActivity(id, "Activity " + id, start, start.AddHours(1), "CAL", "1");
        }

        private static DateTime Date() => new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Unspecified);

        private static IEnumerable<T> Stream<T>(params T[] values)
        {
            for (var i = 0; i < values.Length; i++) yield return values[i];
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ScheduleSnapshotCountDriftSmoke: expected " + typeof(TException).Name + ", got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("ScheduleSnapshotCountDriftSmoke: expected " + typeof(TException).Name + " but no exception was thrown.");
        }

        private static void Contains(string actual, string expected)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("ScheduleSnapshotCountDriftSmoke: expected message containing '" + expected + "', actual '" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("ScheduleSnapshotCountDriftSmoke: expected '" + expected + "', actual '" + actual + "'.");
        }

        private sealed class AdversarialCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly Action<AdversarialCollection<T>>? _afterEnumeration;
            private int _genericCount;
            private int _readOnlyCount;
            private int _nonGenericCount;

            public AdversarialCollection(T[] items, int genericCount, int readOnlyCount, int nonGenericCount, Action<AdversarialCollection<T>>? afterEnumeration = null)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _afterEnumeration = afterEnumeration;
            }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public void SetCounts(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            public IEnumerator<T> GetEnumerator()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++) yield return _items[i];
                }
                finally
                {
                    _afterEnumeration?.Invoke(this);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => Array.Copy(_items, 0, array, index, _items.Length);
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }
    }
}
