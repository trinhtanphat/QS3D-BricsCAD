using System;
using System.Linq;
using System.Reflection;
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
            PaddedStoredActiveFloorFailsVisible();
            CaseVariantActiveZoneFailsVisible();
            CanonicalActiveIdsDoNotEmitCanonicalityErrors();
            MissingActiveIdsKeepInvalidDiagnostics();
            DuplicateActiveTargetsKeepAmbiguityDiagnostics();
        }

        private static void PaddedStoredActiveFloorFailsVisible()
        {
            var project = ProjectWithFloorAndZone("FLOOR-PAD");

            // Public active-context assignment now rejects surrounding whitespace. Health still
            // needs to diagnose a legacy/corrupt persisted value, so inject that state directly.
            SetRawActiveFloorId(project, " Floor-A ");
            RequireIssue(project, "ACTIVE_FLOOR_NON_CANONICAL", HealthSeverity.Error);
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

        private static void SetRawActiveFloorId(ProjectState project, string value)
        {
            var field = typeof(ProjectState).GetField("_activeFloorId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("ProjectState._activeFloorId field was not found.");
            field.SetValue(project, value);
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
