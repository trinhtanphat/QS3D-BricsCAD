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
            DetectsWrongCategoryAndStaleState();
            DetectsCrossKeyOwnershipConflict();
        }

        private static void HealthyFoundation()
        {
            var project = new ProjectState("P", "P");
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA;BB", "2");
            project.Elements.Add(foundation);
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA", "BB" };
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, live);
            Require(!issues.Any(x => x.Severity == HealthSeverity.Error), "healthy foundation mesh produced an error");
        }

        private static void DetectsWrongCategoryAndStaleState()
        {
            var project = new ProjectState("P", "P");
            var slab = MeshElement("S1", ElementCategory.Slab, "AA", "1");
            slab.Dirty = ElementDirtyFlags.Geometry;
            project.Elements.Add(slab);
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_CATEGORY_MISMATCH"), "category mismatch not detected");
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_GENERATED_STALE"), "dirty foundation mesh not detected");
        }

        private static void DetectsCrossKeyOwnershipConflict()
        {
            var project = new ProjectState("P", "P");
            var foundation = MeshElement("F1", ElementCategory.Foundation, "AA", "1");
            foundation.Properties["GeneratedSlabMeshHandles"] = "AA";
            project.Elements.Add(foundation);
            var issues = new GeneratedFoundationMeshHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT"), "cross-key ownership conflict not detected");
        }

        private static ProjectElement MeshElement(string id, ElementCategory category, string handles, string count)
        {
            var element = new ProjectElement(id, category, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedFoundationMeshHandles"] = handles;
            element.Properties["GeneratedFoundationMeshCount"] = count;
            element.Properties["GeneratedFoundationMeshXDiameterMm"] = "16";
            element.Properties["GeneratedFoundationMeshYDiameterMm"] = "16";
            element.Properties["GeneratedFoundationMeshCoverM"] = "0.05";
            element.Properties["GeneratedFoundationMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshYActualSpacingM"] = "0.2";
            element.Properties["GeneratedFoundationMeshFaces"] = "Bottom";
            element.Properties["GeneratedFoundationMeshMode"] = "FoundationMeshXY";
            return element;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FoundationMeshHealthSmoke: " + message);
        }
    }
}
