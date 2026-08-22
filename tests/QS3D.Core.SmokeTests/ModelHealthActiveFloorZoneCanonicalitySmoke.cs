using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthActiveFloorZoneCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedActiveFloorNormalizesBeforeHealth();
            CaseVariantActiveZoneFailsVisible();
            CanonicalActiveIdsDoNotEmitCanonicalityErrors();
            MissingActiveIdsKeepInvalidDiagnostics();
            DuplicateActiveTargetsKeepAmbiguityDiagnostics();
        }

        private static void PaddedActiveFloorNormalizesBeforeHealth()
        {
            var project = ProjectWithFloorAndZone("FLOOR-PAD");
            project.ActiveFloorId = " Floor-A ";
            if (!string.Equals(project.ActiveFloorId, "Floor-A", StringComparison.Ordinal))
                throw new InvalidOperationException("Padded ActiveFloorId must normalize before health inspection.");
            var issues = new ModelHealthService().Inspect(project);
            if (issues.Any(x =>
                string.Equals(x.Code, "ACTIVE_FLOOR_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "INVALID_ACTIVE_FLOOR", StringComparison.Ordinal) ||
                string.Equals(x.Code, "AMBIGUOUS_ACTIVE_FLOOR", StringComparison.Ordinal)))
                throw new InvalidOperationException("Normalized ActiveFloorId must remain a healthy canonical reference.");
        }

        private static void CaseVariantActiveZoneFailsVisible()
        {
            var project = ProjectWithFloorAndZone("ZONE-CASE");
            project.ActiveZoneId = "zone-a";
            RequireIssue(project, "ACTIVE_ZONE_NON_CANONICAL", HealthSeverity.Error);
        }

        private static void CanonicalActiveIdsDoNotEmitCanonicalityErrors()
        {
            var project = ProjectWithFloorAndZone("CANONICAL");
            var issues = new ModelHealthService().Inspect(project);
            if (issues.Any(x =>
                string.Equals(x.Code, "ACTIVE_FLOOR_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "ACTIVE_ZONE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Exact canonical active Floor/Zone ids must not produce canonicality errors.");
        }

        private static void MissingActiveIdsKeepInvalidDiagnostics()
        {
            var project = ProjectWithFloorAndZone("MISSING");
            project.ActiveFloorId = "missing-floor";
            project.ActiveZoneId = "missing-zone";
            var issues = new ModelHealthService().Inspect(project);
            RequireIssue(issues, "INVALID_ACTIVE_FLOOR", HealthSeverity.Warning);
            RequireIssue(issues, "INVALID_ACTIVE_ZONE", HealthSeverity.Warning);
            if (issues.Any(x =>
                string.Equals(x.Code, "ACTIVE_FLOOR_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "ACTIVE_ZONE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Missing active Floor/Zone ids must remain invalid references, not canonicality aliases.");
        }

        private static void DuplicateActiveTargetsKeepAmbiguityDiagnostics()
        {
            var project = new ProjectState("P-ACTIVE-DUP", "Active duplicate identity smoke");
            project.Floors.Add(new FloorDefinition("Floor-A", "Level A", 0d));
            project.Floors.Add(new FloorDefinition("floor-a", "Level A duplicate", 3d));
            project.Zones.Add(new ZoneDefinition("Zone-A", "Zone A"));
            project.Zones.Add(new ZoneDefinition("zone-a", "Zone A duplicate"));
            project.ActiveFloorId = "Floor-A";
            project.ActiveZoneId = "Zone-A";

            var issues = new ModelHealthService().Inspect(project);
            RequireIssue(issues, "AMBIGUOUS_ACTIVE_FLOOR", HealthSeverity.Error);
            RequireIssue(issues, "AMBIGUOUS_ACTIVE_ZONE", HealthSeverity.Error);
            if (issues.Any(x =>
                string.Equals(x.Code, "ACTIVE_FLOOR_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "ACTIVE_ZONE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Duplicate active targets must stay ambiguous without selecting an arbitrary canonical identity.");
        }

        private static ProjectState ProjectWithFloorAndZone(string suffix)
        {
            var project = new ProjectState("P-ACTIVE-" + suffix, "Active Floor Zone canonicality smoke");
            ProjectFloorService.Create(project, "Floor-A", "Level A", 0d);
            ProjectZoneService.Create(project, "Zone-A", "Zone A");
            return project;
        }

        private static void RequireIssue(ProjectState project, string code, HealthSeverity severity)
        {
            RequireIssue(new ModelHealthService().Inspect(project), code, severity);
        }

        private static void RequireIssue(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string code, HealthSeverity severity)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && x.Severity == severity))
                return;
            throw new InvalidOperationException("Expected active Floor/Zone health issue was not reported: " + code + ".");
        }
    }
}
