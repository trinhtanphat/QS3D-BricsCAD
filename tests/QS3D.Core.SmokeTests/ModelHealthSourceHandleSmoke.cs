using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthSourceHandleSmoke
    {
        internal static void Run()
        {
            DuplicateWithinElementIsVisible();
            UniqueHandleStaysClean();
            CrossElementCollisionIsPreserved();
            NumericAliasesShareIdentity();
            MalformedTextCompatibilityIsPreserved();
        }

        private static void DuplicateWithinElementIsVisible()
        {
            var project = NewProject();
            var element = NewRoom("R1");
            element.SourceHandles.Add("ABCD");
            element.SourceHandles.Add("abcd");
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(
                project,
                new HashSet<string>(new[] { "ABCD" }, StringComparer.OrdinalIgnoreCase));

            Equal(
                1,
                issues.Count(x => x.Code == "DUPLICATE_SOURCE_HANDLE" && x.ElementId == "R1"),
                "Case-insensitive duplicate SourceHandles inside one element must be reported exactly once.");
            False(
                issues.Any(x => x.Code == "DUPLICATE_HANDLE" && x.ElementId == "R1"),
                "An intra-element duplicate must not be mislabeled as cross-element ownership.");
            False(
                issues.Any(x => x.Code == "ORPHAN_HANDLE" && x.ElementId == "R1"),
                "A duplicated handle that is live must not become an orphan.");
        }

        private static void UniqueHandleStaysClean()
        {
            var project = NewProject();
            var element = NewRoom("R1");
            element.SourceHandles.Add("ABCD");
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(
                project,
                new HashSet<string>(new[] { "abcd" }, StringComparer.OrdinalIgnoreCase));

            False(
                issues.Any(x => x.Code == "DUPLICATE_SOURCE_HANDLE"),
                "A unique SourceHandle must not produce a duplicate-source diagnostic.");
            False(
                issues.Any(x => x.Code == "ORPHAN_HANDLE" && x.ElementId == "R1"),
                "Live handle matching must remain case-insensitive after duplicate diagnostics are added.");
        }

        private static void CrossElementCollisionIsPreserved()
        {
            var project = NewProject();
            var first = NewRoom("R1");
            first.SourceHandles.Add("ABCD");
            var second = NewRoom("R2");
            second.SourceHandles.Add("abcd");
            project.Elements.Add(first);
            project.Elements.Add(second);

            var issues = new ModelHealthService().Inspect(
                project,
                new HashSet<string>(new[] { "ABCD" }, StringComparer.OrdinalIgnoreCase));

            Equal(
                1,
                issues.Count(x => x.Code == "DUPLICATE_HANDLE"),
                "Cross-element source-handle ownership collision must retain the existing diagnostic.");
            False(
                issues.Any(x => x.Code == "DUPLICATE_SOURCE_HANDLE"),
                "One handle per element must not be classified as an intra-element duplicate.");
        }

        private static void NumericAliasesShareIdentity()
        {
            var intraProject = NewProject();
            var duplicated = NewRoom("R1");
            duplicated.SourceHandles.Add("A");
            duplicated.SourceHandles.Add("00a");
            intraProject.Elements.Add(duplicated);

            var intraIssues = new ModelHealthService().Inspect(
                intraProject,
                new HashSet<string>(new[] { "0xA" }, StringComparer.OrdinalIgnoreCase));
            Equal(
                1,
                intraIssues.Count(x => x.Code == "DUPLICATE_SOURCE_HANDLE" && x.ElementId == "R1"),
                "Numeric aliases of one CAD handle must be one intra-element SourceHandle identity.");
            False(
                intraIssues.Any(x => x.Code == "ORPHAN_HANDLE" && x.ElementId == "R1"),
                "A live numeric alias must satisfy SourceHandle liveness.");

            var crossProject = NewProject();
            var first = NewRoom("R1");
            first.SourceHandles.Add("A");
            var second = NewRoom("R2");
            second.SourceHandles.Add("0xA");
            crossProject.Elements.Add(first);
            crossProject.Elements.Add(second);

            var crossIssues = new ModelHealthService().Inspect(
                crossProject,
                new HashSet<string>(new[] { "00a" }, StringComparer.OrdinalIgnoreCase));
            Equal(
                1,
                crossIssues.Count(x => x.Code == "DUPLICATE_HANDLE"),
                "Numeric aliases owned by different elements must retain cross-element ambiguity diagnostics.");
            False(
                crossIssues.Any(x => x.Code == "ORPHAN_HANDLE"),
                "Numeric alias live matching must prevent false orphan diagnostics across owners.");
        }

        private static void MalformedTextCompatibilityIsPreserved()
        {
            var project = NewProject();
            var element = NewRoom("R1");
            element.SourceHandles.Add("NOT-HEX");
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(
                project,
                new HashSet<string>(new[] { " not-hex " }, StringComparer.OrdinalIgnoreCase));

            False(
                issues.Any(x => x.Code == "ORPHAN_HANDLE" && x.ElementId == "R1"),
                "Malformed textual SourceHandle compatibility must remain trimmed and case-insensitive.");
            False(
                issues.Any(x => x.Code == "DUPLICATE_SOURCE_HANDLE" && x.ElementId == "R1"),
                "One malformed textual SourceHandle must remain a unique identity.");
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("p", "Health source handles");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Families.Add(new ProjectFamily("room", "Room", ElementCategory.Room));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            return project;
        }

        private static ProjectElement NewRoom(string id)
        {
            return new ProjectElement(id, ElementCategory.Room, "room", "f", "z");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void False(bool value, string message)
        {
            if (value) throw new InvalidOperationException(message);
        }
    }
}
