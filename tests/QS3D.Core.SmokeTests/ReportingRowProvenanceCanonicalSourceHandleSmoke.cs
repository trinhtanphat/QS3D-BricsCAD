using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingRowProvenanceCanonicalSourceHandleSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PaddedStoredHandleFailsClosed();
            BlankStoredHandleFailsClosed();
            DuplicateStoredHandleFailsClosed();
            CanonicalStoredHandlesRemainVisible();
        }

        private static void PaddedStoredHandleFailsClosed()
        {
            var project = ProjectWithDoor("D1", " AB ");
            Throws(
                () => DoorOpeningScheduleBuilder.Build(project),
                "Report provenance contains a non-canonical stored SourceHandles entry at index 0. Repair source ownership before reporting.");
        }

        private static void BlankStoredHandleFailsClosed()
        {
            var project = ProjectWithDoor("D1", "   ");
            Throws(
                () => DoorOpeningScheduleBuilder.Build(project),
                "Report provenance contains an empty stored SourceHandles entry at index 0. Repair source ownership before reporting.");
        }

        private static void DuplicateStoredHandleFailsClosed()
        {
            var project = ProjectWithDoor("D1", "AB");
            AddDoor(project, "D2", "ab");
            Throws(
                () => DoorOpeningScheduleBuilder.Build(project),
                "Report provenance contains duplicate stored SourceHandles identity: ab. Repair source ownership before reporting.");
        }

        private static void CanonicalStoredHandlesRemainVisible()
        {
            var project = ProjectWithDoor("D1", "AB");
            AddDoor(project, "D2", "CD");

            var rows = DoorOpeningScheduleBuilder.Build(project);

            Equal(1, rows.Count, "row count");
            Equal(2, rows[0].Count, "door count");
            Equal(2, rows[0].SourceHandles.Count, "source handle count");
            Equal("AB", rows[0].SourceHandles[0], "first source handle");
            Equal("CD", rows[0].SourceHandles[1], "second source handle");
        }

        private static ProjectState ProjectWithDoor(string id, string sourceHandle)
        {
            var project = new ProjectState("REPORT-PROV", "Reporting provenance canonicality");
            AddDoor(project, id, sourceHandle);
            return project;
        }

        private static void AddDoor(ProjectState project, string id, string sourceHandle)
        {
            var element = new ProjectElement(id, ElementCategory.Door, string.Empty, string.Empty, string.Empty);
            element.SourceHandles.Add(sourceHandle);
            project.Elements.Add(element);
        }

        private static void Throws(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected reporting provenance error.", ex);
            }
            throw new InvalidOperationException("Expected reporting provenance rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ReportingRowProvenanceCanonicalSourceHandleSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
