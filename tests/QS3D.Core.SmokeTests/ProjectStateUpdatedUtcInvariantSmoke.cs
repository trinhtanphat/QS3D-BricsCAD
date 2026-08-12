using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateUpdatedUtcInvariantSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-UPDATED-UTC", "UpdatedUtc invariant smoke");
            if (project.UpdatedUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("ProjectState must initialize UpdatedUtc as UTC.");

            var assignedUtc = new DateTime(2026, 8, 12, 1, 2, 3, DateTimeKind.Utc);
            var initialVersion = project.ChangeVersion;
            project.UpdatedUtc = assignedUtc;
            if (project.UpdatedUtc != assignedUtc || project.UpdatedUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("ProjectState must preserve an explicitly assigned UTC timestamp exactly.");
            if (project.ChangeVersion != initialVersion)
                throw new InvalidOperationException("Direct UTC timestamp assignment must not change ProjectState.ChangeVersion.");

            RejectsWithoutMutation(project, DateTime.SpecifyKind(assignedUtc, DateTimeKind.Local), "Local");
            RejectsWithoutMutation(project, DateTime.SpecifyKind(assignedUtc, DateTimeKind.Unspecified), "Unspecified");

            var beforeTouchVersion = project.ChangeVersion;
            project.Touch();
            if (project.ChangeVersion != checked(beforeTouchVersion + 1L))
                throw new InvalidOperationException("ProjectState.Touch must advance ChangeVersion exactly once.");
            if (project.UpdatedUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("ProjectState.Touch must preserve the UpdatedUtc UTC invariant.");
        }

        private static void RejectsWithoutMutation(ProjectState project, DateTime invalid, string label)
        {
            var beforeTimestamp = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            try
            {
                project.UpdatedUtc = invalid;
            }
            catch (ArgumentException)
            {
                if (project.UpdatedUtc != beforeTimestamp)
                    throw new InvalidOperationException(label + " UpdatedUtc rejection changed the previous project timestamp.");
                if (project.ChangeVersion != beforeVersion)
                    throw new InvalidOperationException(label + " UpdatedUtc rejection changed ProjectState.ChangeVersion.");
                return;
            }
            throw new InvalidOperationException("ProjectState accepted a " + label + " UpdatedUtc value.");
        }
    }
}
