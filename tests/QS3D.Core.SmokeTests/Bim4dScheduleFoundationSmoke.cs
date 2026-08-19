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
            NonUtcActivityFailsClosed();
        }

        private static void CanonicalOrderingAndAllocation()
        {
            var trace = Trace(10d, "rule-a", "1");
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);
            var dependency = new ScheduleDependency("A", "B", ScheduleDependencyType.FinishToStart);
            var linkA = new ScheduleQuantityLink("A", trace, 4d);
            var linkB = new ScheduleQuantityLink("B", trace, 6d);

            var first = new ScheduleSnapshot(
                new[] { activityB, activityA },
                new[] { dependency },
                new[] { linkB, linkA });
            var second = new ScheduleSnapshot(
                new[] { activityA, activityB },
                new[] { dependency },
                new[] { linkA, linkB });

            Equal("A", first.Activities[0].Id);
            Equal("B", first.Activities[1].Id);
            Near(10d, first.GetAllocatedValue("wall-1", "AB12", "NetVolumeM3"));
            Equal(first.ToCanonicalString(), second.ToCanonicalString());
            Equal(linkA.MeasurementFingerprint, linkB.MeasurementFingerprint);
            Equal(trace.NetValue, linkA.MeasuredValue);
            Equal(trace.Unit, linkA.Unit);
        }

        private static void DependencyCycleFailsClosed()
        {
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);
            Throws<ArgumentException>(() => new ScheduleSnapshot(
                new[] { activityA, activityB },
                new[]
                {
                    new ScheduleDependency("A", "B"),
                    new ScheduleDependency("B", "A")
                }));
        }

        private static void QuantityOverallocationFailsClosed()
        {
            var trace = Trace(10d, "rule-a", "1");
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);
            Throws<ArgumentException>(() => new ScheduleSnapshot(
                new[] { activityA, activityB },
                null,
                new[]
                {
                    new ScheduleQuantityLink("A", trace, 6d),
                    new ScheduleQuantityLink("B", trace, 5d)
                }));
        }

        private static void ConflictingMeasurementProvenanceFailsClosed()
        {
            var before = Trace(10d, "rule-a", "1");
            var after = Trace(10d, "rule-a", "2");
            var activityA = Activity("A", 1, 3);
            var activityB = Activity("B", 3, 5);

            True(!string.Equals(
                new ScheduleQuantityLink("A", before, 4d).MeasurementFingerprint,
                new ScheduleQuantityLink("B", after, 4d).MeasurementFingerprint,
                StringComparison.Ordinal));

            Throws<ArgumentException>(() => new ScheduleSnapshot(
                new[] { activityA, activityB },
                null,
                new[]
                {
                    new ScheduleQuantityLink("A", before, 4d),
                    new ScheduleQuantityLink("B", after, 4d)
                }));
        }

        private static void UnknownActivityFailsClosed()
        {
            var trace = Trace(10d, "rule-a", "1");
            Throws<ArgumentException>(() => new ScheduleSnapshot(
                new[] { Activity("A", 1, 3) },
                null,
                new[] { new ScheduleQuantityLink("MISSING", trace, 1d) }));
        }

        private static void NonUtcActivityFailsClosed()
        {
            Throws<ArgumentException>(() => new ScheduleActivity(
                "A",
                "Activity A",
                new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.FromHours(7)),
                new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.FromHours(7))));
        }

        private static ScheduleActivity Activity(string id, int startDay, int finishDay)
        {
            return new ScheduleActivity(
                id,
                "Activity " + id,
                new DateTimeOffset(2026, 8, startDay, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, finishDay, 0, 0, 0, TimeSpan.Zero),
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
