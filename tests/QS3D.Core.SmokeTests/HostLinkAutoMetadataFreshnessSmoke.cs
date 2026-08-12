using System;
using System.Threading;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkAutoMetadataFreshnessSmoke
    {
        internal static void Run()
        {
            CleanupOnlyAdvancesElementFreshness();
            EmptyAbsentHostRemainsNoOp();
        }

        private static void CleanupOnlyAdvancesElementFreshness()
        {
            var project = new ProjectState("P-HOST-FRESH", "Host freshness");
            var opening = new ProjectElement("OPENING-1", ElementCategory.WallOpening);
            opening.Properties["AutoHostMatched"] = "true";
            opening.Properties["AutoHostGapM"] = "0.10";
            opening.Properties["AutoHostElevDeltaM"] = "0.20";
            opening.Properties["AutoHostCandidateCount"] = "2";
            project.Elements.Add(opening);
            opening.MarkClean(ElementDirtyFlags.All);

            var beforeUpdatedUtc = opening.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var beforeAuditCount = project.AuditEvents.Count;
            WaitForLaterUtc(beforeUpdatedUtc);

            new HostLinkService().UnlinkOpening(project, opening.Id);

            if (opening.Properties.ContainsKey("AutoHostMatched") ||
                opening.Properties.ContainsKey("AutoHostGapM") ||
                opening.Properties.ContainsKey("AutoHostElevDeltaM") ||
                opening.Properties.ContainsKey("AutoHostCandidateCount"))
                throw new InvalidOperationException("Cleanup-only unlink must remove stale Auto Host provenance metadata.");
            if (opening.Properties.ContainsKey("HostWallId"))
                throw new InvalidOperationException("Cleanup-only unlink must not invent a HostWallId relation.");
            if (opening.DependsOn.Count != 0)
                throw new InvalidOperationException("Cleanup-only unlink must not invent dependencies.");
            if (opening.Dirty != ElementDirtyFlags.None)
                throw new InvalidOperationException("Cleanup-only Auto Host provenance repair must not add dirty semantics.");
            if (opening.UpdatedUtc <= beforeUpdatedUtc)
                throw new InvalidOperationException("Removing stale Auto Host provenance must advance element persistence freshness.");
            if (project.ChangeVersion != beforeVersion + 1L)
                throw new InvalidOperationException("Cleanup-only Auto Host provenance repair must advance the project revision exactly once through audit ownership.");
            if (project.AuditEvents.Count != beforeAuditCount + 1)
                throw new InvalidOperationException("Cleanup-only Auto Host provenance repair must append one audit event.");
            var audit = project.AuditEvents[project.AuditEvents.Count - 1];
            if (!string.Equals(audit.Action, "host.auto-provenance.clear", StringComparison.Ordinal) ||
                !string.Equals(audit.ElementId, opening.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Cleanup-only Auto Host provenance repair must record the expected audit identity.");
        }

        private static void EmptyAbsentHostRemainsNoOp()
        {
            var project = new ProjectState("P-HOST-NOOP", "Host no-op");
            var opening = new ProjectElement("OPENING-2", ElementCategory.Door);
            project.Elements.Add(opening);
            opening.MarkClean(ElementDirtyFlags.All);

            var beforeUpdatedUtc = opening.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var beforeAuditCount = project.AuditEvents.Count;

            new HostLinkService().UnlinkOpening(project, opening.Id);

            if (opening.UpdatedUtc != beforeUpdatedUtc)
                throw new InvalidOperationException("Absent-host unlink without stale metadata must preserve element freshness.");
            if (opening.Dirty != ElementDirtyFlags.None)
                throw new InvalidOperationException("Absent-host unlink without stale metadata must preserve clean state.");
            if (project.ChangeVersion != beforeVersion || project.AuditEvents.Count != beforeAuditCount)
                throw new InvalidOperationException("Absent-host unlink without stale metadata must remain a project/audit no-op.");
        }

        private static void WaitForLaterUtc(DateTime baseline)
        {
            for (var attempt = 0; attempt < 100 && DateTime.UtcNow <= baseline; attempt++)
                Thread.Sleep(1);
            if (DateTime.UtcNow <= baseline)
                throw new InvalidOperationException("Test clock did not advance beyond the element freshness baseline.");
        }
    }
}
