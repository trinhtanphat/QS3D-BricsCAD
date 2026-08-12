using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishNullHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullElementFailsVisible();
            ValidUnlinkedFinishStillWarns();
        }

        private static void NullElementFailsVisible()
        {
            var project = new ProjectState("health-room-finish-null", "Room finish null health");
            project.Elements.Add(null!);

            try
            {
                new RoomFinishHealthService().Inspect(project);
            }
            catch (InvalidOperationException)
            {
                var composite = new ComprehensiveModelHealthService().Inspect(project);
                if (composite.Any(issue =>
                    string.Equals(issue.Code, "HEALTH_PROVIDER_FAILED", StringComparison.Ordinal) &&
                    issue.Severity == HealthSeverity.Error &&
                    issue.Message.StartsWith("RoomFinishHealthService ", StringComparison.Ordinal)))
                    return;
                throw new InvalidOperationException("Composite health must surface the Room Finish provider failure instead of hiding malformed project state.");
            }

            throw new InvalidOperationException("Room Finish health must reject null semantic elements instead of silently filtering them out.");
        }

        private static void ValidUnlinkedFinishStillWarns()
        {
            var project = new ProjectState("health-room-finish-valid", "Room finish valid diagnostics");
            var finish = new ProjectElement("E-FINISH", ElementCategory.WallFinish);
            project.Elements.Add(finish);

            var issues = new RoomFinishHealthService().Inspect(project);
            if (!issues.Any(issue =>
                string.Equals(issue.Code, "UNLINKED_ROOM_FINISH", StringComparison.Ordinal) &&
                issue.Severity == HealthSeverity.Warning &&
                string.Equals(issue.ElementId, finish.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException("Existing unlinked Room Finish diagnostics regressed.");
        }
    }
}
