using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishAggregationPrecisionSmoke
    {
        internal static void Run()
        {
            PreservesRepresentableSmallContributions();
            PreservesRepresentableSmallContributionsWhenSmallValuesSortFirst();
            OrdinaryAggregationAndProvenanceRemainStable();
            FinalUnrepresentableTotalStillFailsClosed();
            InvalidQuantityStillFailsClosed();
        }

        private static void PreservesRepresentableSmallContributions()
        {
            var project = NewProject("room-finish-compensated-large-first");
            AddFloorFinish(project, "A", 10000000000000000d, "H-A");
            AddFloorFinish(project, "B", 1d, "H-B");
            AddFloorFinish(project, "C", 1d, "H-C");

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            Equal(10000000000000002d, row.AreaM2, "Area must preserve two small contributions when their combined result is representable.");
            Equal(10000000000000002d, row.PrimaryQuantity, "Primary quantity must preserve two small contributions when representable.");
            Equal(3, row.Count, "Compensated aggregation must preserve checked element count.");
            SequenceEqual(new[] { "A", "B", "C" }, row.ElementIds, "Element provenance order must remain deterministic.");
            SequenceEqual(new[] { "H-A", "H-B", "H-C" }, row.SourceHandles, "Source-handle provenance must remain complete.");
        }

        private static void PreservesRepresentableSmallContributionsWhenSmallValuesSortFirst()
        {
            var project = NewProject("room-finish-compensated-small-first");
            AddFloorFinish(project, "A", 1d, "H-A");
            AddFloorFinish(project, "B", 1d, "H-B");
            AddFloorFinish(project, "C", 10000000000000000d, "H-C");

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            Equal(10000000000000002d, row.AreaM2, "Compensated aggregate must remain correct when small contributions sort before the large value.");
            Equal(10000000000000002d, row.PrimaryQuantity, "Primary aggregate must remain order-robust for the same representable sum.");
        }

        private static void OrdinaryAggregationAndProvenanceRemainStable()
        {
            var project = NewProject("room-finish-compensated-ordinary");
            AddFloorFinish(project, "A", 4.25d, "H-A");
            AddFloorFinish(project, "B", 5.75d, "H-B");

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            Equal(10d, row.AreaM2, "Ordinary area aggregation must remain exact.");
            Equal(10d, row.PrimaryQuantity, "Ordinary primary aggregation must remain exact.");
            Equal(2, row.Count, "Ordinary aggregation must preserve count.");
            Equal("FloorFinish", row.Category, "Category projection must remain unchanged.");
            Equal("Floor finish family", row.FamilyName, "Family projection must remain unchanged.");
            Equal("m²", row.UnitHint, "Unit projection must remain unchanged.");
        }

        private static void FinalUnrepresentableTotalStillFailsClosed()
        {
            var project = NewProject("room-finish-final-unrepresentable");
            AddFloorFinish(project, "A", 9007199254740992d, "H-A");
            AddFloorFinish(project, "B", 1d, "H-B");

            Capture<OverflowException>(() => RoomFinishScheduleBuilder.Build(project));
        }

        private static void InvalidQuantityStillFailsClosed()
        {
            var project = NewProject("room-finish-invalid");
            AddFloorFinish(project, "A", double.PositiveInfinity, "H-A");

            Capture<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project));
        }

        private static ProjectState NewProject(string id)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Families.Add(new ProjectFamily("FF", "Floor finish family", ElementCategory.FloorFinish));
            return project;
        }

        private static void AddFloorFinish(ProjectState project, string id, double areaM2, string sourceHandle)
        {
            var element = new ProjectElement(id, ElementCategory.FloorFinish, "FF", "F1", "Z1");
            element.Quantities["BottomAreaM2"] = areaM2;
            element.SourceHandles.Add(sourceHandle);
            project.Elements.Add(element);
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (expected.Equals(actual)) return;
            throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected == actual) return;
            throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal)) return;
            throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string message)
        {
            if (expected.SequenceEqual(actual, StringComparer.Ordinal)) return;
            throw new InvalidOperationException(message + " Expected=" + string.Join(",", expected) + ", actual=" + string.Join(",", actual) + ".");
        }
    }

    internal static class RoomFinishAggregationPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RoomFinishAggregationPrecisionSmoke.Run();
        }
    }
}