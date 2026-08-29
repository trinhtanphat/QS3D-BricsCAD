using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningAggregationPrecisionSmoke
    {
        internal static void Run()
        {
            PreservesRepresentableSmallContributions();
            PreservesRepresentableSmallContributionsWhenSmallValuesSortFirst();
            OrdinaryGroupingHostAndProvenanceRemainStable();
            FinalUnrepresentableTotalStillFailsClosed();
            InvalidAreaStillFailsClosed();
        }

        private static void PreservesRepresentableSmallContributions()
        {
            var project = NewProject("door-opening-compensated-large-first");
            AddDoor(project, "A", 10000000000000000d, "wall-a", "H-A");
            AddDoor(project, "B", 1d, "wall-a", "H-B");
            AddDoor(project, "C", 1d, "wall-b", "H-C");

            var row = DoorOpeningScheduleBuilder.Build(project).Single();
            Equal(10000000000000002d, row.OpeningAreaM2, "Opening area must preserve two small contributions when their combined result is representable.");
            Equal(3, row.Count, "Compensated aggregation must preserve checked element count.");
            Equal(2, row.HostCount, "Distinct host count must remain stable.");
            SequenceEqual(new[] { "A", "B", "C" }, row.ElementIds, "Element provenance order must remain deterministic.");
            SequenceEqual(new[] { "H-A", "H-B", "H-C" }, row.SourceHandles, "Source-handle provenance must remain complete.");
        }

        private static void PreservesRepresentableSmallContributionsWhenSmallValuesSortFirst()
        {
            var project = NewProject("door-opening-compensated-small-first");
            AddDoor(project, "A", 1d, "wall-a", "H-A");
            AddDoor(project, "B", 1d, "wall-b", "H-B");
            AddDoor(project, "C", 10000000000000000d, "wall-a", "H-C");

            var row = DoorOpeningScheduleBuilder.Build(project).Single();
            Equal(10000000000000002d, row.OpeningAreaM2, "Compensated opening area must remain correct when small contributions sort before the large value.");
            Equal(3, row.Count, "Small-first compensation must not change count.");
        }

        private static void OrdinaryGroupingHostAndProvenanceRemainStable()
        {
            var project = NewProject("door-opening-compensated-ordinary");
            AddDoor(project, "A", 4.25d, "wall-a", "H-A");
            AddDoor(project, "B", 5.75d, "wall-b", "H-B");

            var row = DoorOpeningScheduleBuilder.Build(project).Single();
            Equal(10d, row.OpeningAreaM2, "Ordinary opening-area aggregation must remain exact.");
            Equal(2, row.Count, "Ordinary aggregation must preserve count.");
            Equal(2, row.HostCount, "Ordinary aggregation must preserve distinct hosts.");
            Equal("Door", row.Category, "Category projection must remain unchanged.");
            Equal("Door family", row.FamilyName, "Family projection must remain unchanged.");
            Equal("Concrete", row.Material, "Material projection must remain unchanged.");
            SequenceEqual(new[] { "wall-a", "wall-b" }, row.HostIds, "Host provenance must remain deterministic.");
        }

        private static void FinalUnrepresentableTotalStillFailsClosed()
        {
            var project = NewProject("door-opening-final-unrepresentable");
            AddDoor(project, "A", 9007199254740992d, "wall-a", "H-A");
            AddDoor(project, "B", 1d, "wall-a", "H-B");

            Capture<OverflowException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void InvalidAreaStillFailsClosed()
        {
            var project = NewProject("door-opening-invalid");
            AddDoor(project, "A", double.PositiveInfinity, "wall-a", "H-A");

            Capture<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static ProjectState NewProject(string id)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var family = new ProjectFamily("D1", "Door family", ElementCategory.Door);
            family.Properties["WidthM"] = "1";
            family.Properties["HeightM"] = "1";
            family.Properties["SillHeightM"] = "0";
            family.Properties["ThicknessM"] = "0.1";
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("wall-a", ElementCategory.ArchitecturalWall));
            project.Elements.Add(new ProjectElement("wall-b", ElementCategory.StructuralWall));
            return project;
        }

        private static void AddDoor(ProjectState project, string id, double areaM2, string hostId, string sourceHandle)
        {
            var element = new ProjectElement(id, ElementCategory.Door, "D1", "F1", "Z1");
            element.Properties["HostWallId"] = hostId;
            element.Quantities["OpeningAreaM2"] = areaM2;
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

    internal static class DoorOpeningAggregationPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DoorOpeningAggregationPrecisionSmoke.Run();
        }
    }
}