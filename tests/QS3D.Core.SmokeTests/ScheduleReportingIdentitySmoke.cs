using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ScheduleReportingIdentitySmoke
    {
        internal static void Run()
        {
            ExactDuplicateIdsFailClosed();
            CaseVariantDuplicateIdsFailClosed();
            UniqueIdsRemainAccepted();
        }

        private static void ExactDuplicateIdsFailClosed()
        {
            AssertAllScheduleBuildersReject(DuplicateProject("E1"));
        }

        private static void CaseVariantDuplicateIdsFailClosed()
        {
            AssertAllScheduleBuildersReject(DuplicateProject("e1"));
        }

        private static void UniqueIdsRemainAccepted()
        {
            var project = new ProjectState("schedule-identity-valid", "Schedule identity valid");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family", "floor", "zone"));
            project.Elements.Add(new ProjectElement("E2", ElementCategory.Slab, "family", "floor", "zone"));

            if (MaterialUsageScheduleBuilder.Build(project).Count != 0 ||
                CurtainWallScheduleBuilder.Build(project).Count != 0 ||
                DoorOpeningScheduleBuilder.Build(project).Count != 0 ||
                RoomFinishScheduleBuilder.Build(project).Count != 0)
                throw new Exception("Schedule identity guard must not change valid non-schedule project output.");
        }

        private static ProjectState DuplicateProject(string secondId)
        {
            var project = new ProjectState("schedule-identity-duplicate", "Schedule identity duplicate");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family", "floor", "zone"));
            project.Elements.Add(new ProjectElement(secondId, ElementCategory.Slab, "family", "floor", "zone"));
            return project;
        }

        private static void AssertAllScheduleBuildersReject(ProjectState project)
        {
            ExpectThrows<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project));
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
