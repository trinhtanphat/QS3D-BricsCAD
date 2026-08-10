using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FoundationMeshHealthSmoke
    {
        public static void Run()
        {
            HealthyFoundation();
            AcceptsLegacyMissingFootprintMode();
            AcceptsPolygonFootprintMode();
            DetectsInvalidFootprintMode();
            DetectsWrongCategoryAndStaleSnapshot();
            DetectsCrossKeyOwnershipConflict();
            DetectsLaterOwnerConflictAndFutureGeneratedSlot();
            ClearsFoundationStaleIndependently();
            DedicatedModeHealthReadsFoundationSlot();
        }

        private static void HealthyFoundation()
        {
            var project = new ProjectState("P", "P");
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA;BB", "2");
            project.Elements.Add(foundation);
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA", "BB" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, live);
            Require(!issues.Any(x => x.Severity == HealthSeverity.Error), "healthy foundation mesh produced an error");
            Require(!issues.Any(x => x.Code == "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID"), "RectangleLocalXY should be a healthy Foundation footprint mode");
            Require(!issues.Any(x => x.Code == "FOUNDATION_MESH_GENERATED_STALE"), "fresh foundation mesh should not be stale");
        }

        private static void AcceptsLegacyMissingFootprintMode()
        {
            var project = new ProjectState("P-legacy", "Legacy rectangle");
            var foundation = MeshElement("F-LEGACY", ElementCategory.Foundation, "AA", "1");
            foundation.Properties.Remove("GeneratedFoundationMeshFootprintMode");
            project.Elements.Add(foundation);
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(!issues.Any(x => x.Code == "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID"), "legacy rectangle metadata without footprint mode must remain release-compatible");
        }

        private static void AcceptsPolygonFootprintMode()
        {
            var project = new ProjectState("P-poly", "Polygon Foundation");
            var foundation = MeshElement("F-POLY", ElementCategory.Foundation, "AA", "1");
            foundation.Properties["GeneratedFoundationMeshFootprintMode"] = "PolygonGlobalXY";
            project.Elements.Add(foundation);
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(!issues.Any(x => x.Code == "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID"), "PolygonGlobalXY should be a healthy Foundation footprint mode");
        }

        private static void DetectsInvalidFootprintMode()
        {
            var project = new ProjectState("P-bad-footprint", "Bad footprint");
            var foundation = MeshElement("F-BAD", ElementCategory.Foundation, "AA", "1");
            foundation.Properties["GeneratedFoundationMeshFootprintMode"] = "PolygonLocalMagic";
            project.Elements.Add(foundation);
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID"), "invalid Foundation footprint mode was not detected");
        }

        private static void DetectsWrongCategoryAndStaleSnapshot()
        {
            var project = new ProjectState("P", "P");
            var slab = MeshElement("S1", ElementCategory.Slab, "AA", "1");
            slab.MarkDirty(ElementDirtyFlags.Properties);
            project.Elements.Add(slab);
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, live);
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_CATEGORY_MISMATCH"), "category mismatch not detected");
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_GENERATED_STALE"), "foundation stale snapshot not detected");
            Require(new GeneratedGeometryStaleHealthService().Inspect(project).Any(x => x.Code == "FOUNDATION_MESH_GENERATED_STALE"), "aggregate stale health missed foundation mesh");
        }

        private static void DetectsCrossKeyOwnershipConflict()
        {
            var project = new ProjectState("P", "P");
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA", "1");
            foundation.Properties["GeneratedSlabMeshHandles"] = "AA";
            project.Elements.Add(foundation);
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT"), "foundation health missed cross-key ownership conflict");
            Require(new GeneratedRebarOwnershipHealthService().Inspect(project).Any(x => x.Code == "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT"), "global rebar ownership health missed foundation conflict");
        }

        private static void DetectsLaterOwnerConflictAndFutureGeneratedSlot()
        {
            var project = new ProjectState("P-order", "Ownership order");
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA", "1");
            project.Elements.Add(foundation);
            var later = new ProjectElement("FUTURE", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            later.Properties["GeneratedFutureMeshHandles"] = "AA";
            project.Elements.Add(later);

            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT" && x.ElementId == foundation.Id),
                "foundation health must detect a later conflicting owner regardless of project order or future generated slot name");
        }

        private static void ClearsFoundationStaleIndependently()
        {
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA", "1");
            foundation.MarkDirty(ElementDirtyFlags.Geometry);
            Require(foundation.IsGeneratedFoundationMeshStale(), "foundation mesh should become stale after semantic geometry mutation");
            foundation.ClearGeneratedFoundationMeshStale();
            Require(!foundation.IsGeneratedFoundationMeshStale(), "foundation stale state should clear after successful rebuild");
        }

        private static void DedicatedModeHealthReadsFoundationSlot()
        {
            var project = new ProjectState("P", "P");
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA", "1");
            project.Elements.Add(foundation);
            var issues = new GeneratedRebarModeHealthService().Inspect(project);
            Require(!issues.Any(x => x.Severity == HealthSeverity.Error || x.Code == "GENERATED_REBAR_MODE_METADATA_INVALID"), "healthy FoundationMeshXY dedicated mode should pass mode health");

            foundation.Category = ElementCategory.Slab;
            issues = new GeneratedRebarModeHealthService().Inspect(project);
            Require(issues.Any(x => x.Code == "GENERATED_REBAR_MODE_CATEGORY_MISMATCH"), "dedicated FoundationMeshXY mode/category mismatch not detected");
        }

        private static ProjectElement MeshElement(string id, ElementCategory category, string handles, string count)
        {
            var element = new ProjectElement(id, category, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedFoundationMeshHandles"] = handles;
            element.Properties["GeneratedFoundationMeshCount"] = count;
            element.Properties["GeneratedFoundationMeshXDiameterMm"] = "16";
            element.Properties["GeneratedFoundationMeshYDiameterMm"] = "12";
            element.Properties["GeneratedFoundationMeshCoverM"] = "0.05";
            element.Properties["GeneratedFoundationMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshYActualSpacingM"] = "0.15";
            element.Properties["GeneratedFoundationMeshFaces"] = "Bottom";
            element.Properties["GeneratedFoundationMeshMode"] = "FoundationMeshXY";
            element.Properties["GeneratedFoundationMeshFootprintMode"] = "RectangleLocalXY";
            return element;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FoundationMeshHealthSmoke: " + message);
        }
    }
}
