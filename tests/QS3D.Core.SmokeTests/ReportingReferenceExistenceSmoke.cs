using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingReferenceExistenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MissingFamilyFailsClosed();
            MissingFloorFailsClosed();
            MissingZoneFailsClosed();
            BlankReferencesRemainValid();
            ExistingReferencesRemainValid();
        }

        private static void MissingFamilyFailsClosed()
        {
            var project = new ProjectState("report-missing-family", "Reporting missing Family");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "F-MISSING", string.Empty, string.Empty));
            AssertSharedBuildersReject(project, "missing family id 'F-MISSING'");
        }

        private static void MissingFloorFailsClosed()
        {
            var project = new ProjectState("report-missing-floor", "Reporting missing Floor");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, string.Empty, "L-MISSING", string.Empty));
            AssertSharedBuildersReject(project, "missing floor id 'L-MISSING'");
        }

        private static void MissingZoneFailsClosed()
        {
            var project = new ProjectState("report-missing-zone", "Reporting missing Zone");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, string.Empty, string.Empty, "Z-MISSING"));
            AssertSharedBuildersReject(project, "missing zone id 'Z-MISSING'");
        }

        private static void BlankReferencesRemainValid()
        {
            var project = new ProjectState("report-blank-refs", "Reporting blank references");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab));

            var quantity = ProjectQuantityReportBuilder.Group(project);
            if (quantity.Count != 1 || quantity[0].ElementIds.Count != 1 || quantity[0].ElementIds[0] != "E1")
                throw new InvalidOperationException("Blank reporting references no longer preserve valid unassigned elements.");

            _ = MaterialUsageScheduleBuilder.Build(project);
        }

        private static void ExistingReferencesRemainValid()
        {
            var project = new ProjectState("report-existing-refs", "Reporting existing references");
            project.Floors.Add(new FloorDefinition("Floor-A", "Floor A", 0d));
            project.Zones.Add(new ZoneDefinition("Zone-A", "Zone A"));
            project.Families.Add(new ProjectFamily("Family-A", "Family A", ElementCategory.Slab));
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family-a", "floor-a", "zone-a"));

            var quantity = ProjectQuantityReportBuilder.Group(project);
            if (quantity.Count != 1 || quantity[0].FamilyName != "Family A" || quantity[0].Floor != "Floor A" || quantity[0].Zone != "Zone A")
                throw new InvalidOperationException("Existing case-insensitive reporting references changed valid lookup semantics.");

            _ = MaterialUsageScheduleBuilder.Build(project);
        }

        private static void AssertSharedBuildersReject(ProjectState project, string expectedMessage)
        {
            ExpectInvalid(() => MaterialUsageScheduleBuilder.Build(project), expectedMessage);
            ExpectInvalid(() => ProjectQuantityReportBuilder.Group(project), expectedMessage);
            ExpectInvalid(() => ProjectQuantityReportBuilder.Detail(project), expectedMessage);
        }

        private static void ExpectInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Reporting rejected the malformed reference for an unexpected reason: " + ex.Message, ex);
            }

            throw new InvalidOperationException("Expected reporting to reject a dangling semantic reference: " + expectedMessage + ".");
        }
    }
}
