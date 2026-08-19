using System;
using QS3D.Core.Measurement;
using QS3D.Core.Scheduling;

namespace QS3D.Core.SmokeTests
{
    internal static class Bim4dScheduleFoundationSmoke
    {
        public static void Run()
        {
            CanonicalOrderingAndAllocation();
            DependencyCycleFailsClosed();
            QuantityOverallocationFailsClosed();
            ConflictingMeasurementProvenanceFailsClosed();
            UnknownActivityFailsClosed();
            WorkstationTimeSemanticsFailClosed();
            DuplicateAllocationIdFailsClosed();
        }

        private static void CanonicalOrderingAndAllocation()
        {
            var trace = Trace(10d, "rule-a", "1");
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);
            var dependency = new ScheduleDependency("A", "B", ScheduleDependencyType.FinishToStart);
            var linkA = Link("alloc-a", "A", "measure-v1", trace, 4d);
            var linkB = Link("alloc-b", "B", "measure-v1", trace, 6d);

            var first = Snapshot(
                new[] { activityB, activityA },
                new[] { dependency },
                new[] { linkB, linkA });
            var second = Snapshot(
                new[] { activityA, activityB },
                new[] { dependency },
                new[] { linkA, linkB });

            Equal("schedule-1", first.ScheduleId);
            Equal("schedule-v1", first.ScheduleVersionId);
            Equal("allocation-v1", first.AllocationVersionId);
            Equal("Asia/Ho_Chi_Minh", first.ProjectTimeZoneId);
            Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Unspecified), first.DataDate);
            Equal("A", first.Activities[0].Id);
            Equal("B", first.Activities[1].Id);
            Equal("cal-std", first.Activities[0].CalendarId);
            Equal("cal-v1", first.Activities[0].CalendarVersion);
            Near(10d, first.GetAllocatedValue("wall-1", "AB12", "NetVolumeM3"));
            Equal(first.ToCanonicalString(), second.ToCanonicalString());
            Equal(linkA.MeasurementFingerprint, linkB.MeasurementFingerprint);
            Equal(trace.NetValue, linkA.MeasuredValue);
            Equal(trace.Unit, linkA.Unit);
            Equal(ActivityAllocationBasis.AbsoluteQuantity, linkA.Basis);
        }

        private static void DependencyCycleFailsClosed()
        {
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);
            Throws<ArgumentException>(() => Snapshot(
                new[] { activityA, activityB },
                new[]
                {
                    new ScheduleDependency("A", "B"),
                    new ScheduleDependency("B", "A")
                },
                null));
        }

        private static void QuantityOverallocationFailsClosed()
        {
            var trace = Trace(10d, "rule-a", "1");
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);
            Throws<ArgumentException>(() => Snapshot(
                new[] { activityA, activityB },
                null,
                new[]
                {
                    Link("alloc-a", "A", "measure-v1", trace, 6d),
                    Link("alloc-b", "B", "measure-v1", trace, 5d)
                }));
        }

        private static void ConflictingMeasurementProvenanceFailsClosed()
        {
            var before = Trace(10d, "rule-a", "1");
            var after = Trace(10d, "rule-a", "2");
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);

            True(!string.Equals(
                Link("alloc-a", "A", "measure-v1", before, 4d).MeasurementFingerprint,
                Link("alloc-b", "B", "measure-v2", after, 4d).MeasurementFingerprint,
                StringComparison.Ordinal));

            Throws<ArgumentException>(() => Snapshot(
                new[] { activityA, activityB },
                null,
                new[]
                {
                    Link("alloc-a", "A", "measure-v1", before, 4d),
                    Link("alloc-b", "B", "measure-v2", after, 4d)
                }));
        }

        private static void UnknownActivityFailsClosed()
        {
            var trace = Trace(10d, "rule-a", "1");
            Throws<ArgumentException>(() => Snapshot(
                new[] { Activity("A", 1, 3) },
                null,
                new[] { Link("alloc-missing", "MISSING", "measure-v1", trace, 1d) }));
        }

        private static void WorkstationTimeSemanticsFailClosed()
        {
            Throws<ArgumentException>(() => new ScheduleActivity(
                "A",
                "Activity A",
                new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Local),
                new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Local),
                "cal-std",
                "cal-v1"));

            Throws<ArgumentException>(() => new ScheduleSnapshot(
                "schedule-1",
                "schedule-v1",
                "allocation-v1",
                "Asia/Ho_Chi_Minh",
                new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Unspecified),
                new[] { Activity("A", 1, 3) }));
        }

        private static void DuplicateAllocationIdFailsClosed()
        {
            var traceA = Trace(10d, "rule-a", "1");
            var traceB = new MeasurementTrace(
                "wall-2",
                "AB13",
                "NetVolumeM3",
                Array.Empty<MeasurementTraceFact>(),
                8d,
                Array.Empty<MeasurementTraceAdjustment>(),
                8d,
                "m3",
                "none",
                ruleId: "rule-a",
                ruleVersion: "1");

            Throws<ArgumentException>(() => Snapshot(
                new[] { Activity("A", 1, 3), Activity("B", 3, 5) },
                null,
                new[]
                {
                    Link("alloc-duplicate", "A", "measure-v1", traceA, 1d),
                    Link("alloc-duplicate", "B", "measure-v1", traceB, 1d)
                }));
        }

        private static ScheduleSnapshot Snapshot(
            ScheduleActivity[] activities,
            ScheduleDependency[]? dependencies,
            ScheduleQuantityLink[]? links)
        {
            return new ScheduleSnapshot(
                "schedule-1",
                "schedule-v1",
                "allocation-v1",
                "Asia/Ho_Chi_Minh",
                new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Unspecified),
                activities,
                dependencies,
                links);
        }

        private static ScheduleQuantityLink Link(
            string allocationId,
            string activityId,
            string measurementSnapshotId,
            MeasurementTrace trace,
            double allocatedValue)
        {
            return new ScheduleQuantityLink(
                allocationId,
                activityId,
                measurementSnapshotId,
                trace,
                allocatedValue);
        }

        private static ScheduleActivity Activity(string id, int startDay, int finishDay)
        {
            return new ScheduleActivity(
                id,
                "Activity " + id,
                new DateTime(2026, 8, startDay, 8, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 8, finishDay, 17, 0, 0, DateTimeKind.Unspecified),
                "cal-std",
                "cal-v1",
                "WBS-" + id);
        }

        private static MeasurementTrace Trace(double value, string ruleId, string ruleVersion)
        {
            return new MeasurementTrace(
                "wall-1",
                "AB12",
                "NetVolumeM3",
                Array.Empty<MeasurementTraceFact>(),
                value,
                Array.Empty<MeasurementTraceAdjustment>(),
                value,
                "m3",
                "none",
                ruleId: ruleId,
                ruleVersion: ruleVersion);
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
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
            throw new Exception("Expected exception: " + typeof(T).FullName + ".");
        }
    }
}
