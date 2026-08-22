using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedSlabMeshHealthSmoke
    {
        public static void Run()
        {
            AcceptsLegacyMissingFootprintMode();
            AcceptsRectangleFootprintMode();
            AcceptsPolygonFootprintMode();
            RejectsInvalidFootprintMode();
            DetectsLaterOwnershipConflict();
            IgnoresNullSemanticEntry();
        }

        private static void AcceptsLegacyMissingFootprintMode()
        {
            var project = new ProjectState("P-legacy", "Legacy slab mesh");
            var slab = MeshElement("S-LEGACY", "AA", "1");
            slab.Properties.Remove("GeneratedSlabMeshFootprintMode");
            project.Elements.Add(slab);

            var issues = Inspect(project, "AA");
            Require(!issues.Any(x => x.Code == "SLAB_MESH_FOOTPRINT_MODE_INVALID"),
                "legacy rectangle metadata without footprint mode must remain valid");
        }

        private static void AcceptsRectangleFootprintMode()
        {
            var project = new ProjectState("P-rect", "Rectangle slab mesh");
            var slab = MeshElement("S-RECT", "AA", "1");
            slab.Properties["GeneratedSlabMeshFootprintMode"] = "RectangleLocalXY";
            project.Elements.Add(slab);

            var issues = Inspect(project, "AA");
            Require(!issues.Any(x => x.Code == "SLAB_MESH_FOOTPRINT_MODE_INVALID"),
                "RectangleLocalXY must be accepted");
        }

        private static void AcceptsPolygonFootprintMode()
        {
            var project = new ProjectState("P-poly", "Polygon slab mesh");
            var slab = MeshElement("S-POLY", "AA", "1");
            slab.Properties["GeneratedSlabMeshFootprintMode"] = "PolygonGlobalXY";
            project.Elements.Add(slab);

            var issues = Inspect(project, "AA");
            Require(!issues.Any(x => x.Code == "SLAB_MESH_FOOTPRINT_MODE_INVALID"),
                "PolygonGlobalXY must be accepted");
        }

        private static void RejectsInvalidFootprintMode()
        {
            var project = new ProjectState("P-bad", "Invalid slab footprint");
            var slab = MeshElement("S-BAD", "AA", "1");
            slab.Properties["GeneratedSlabMeshFootprintMode"] = "PolygonLocalMagic";
            project.Elements.Add(slab);

            var issues = Inspect(project, "AA");
            Require(issues.Any(x => x.Code == "SLAB_MESH_FOOTPRINT_MODE_INVALID" && x.Severity == HealthSeverity.Error),
                "invalid slab footprint mode must fail closed");
        }

        private static void DetectsLaterOwnershipConflict()
        {
            var project = new ProjectState("P-owner", "Ownership ambiguity");
            var slab = MeshElement("S1", "AA", "1");
            project.Elements.Add(slab);

            var later = new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            later.Properties["GeneratedFutureMeshHandles"] = "AA";
            project.Elements.Add(later);

            var issues = Inspect(project, "AA");
            Require(issues.Any(x => x.Code == "SLAB_MESH_GENERATED_OWNERSHIP_CONFLICT" && x.ElementId == slab.Id),
                "slab health must detect a conflicting owner regardless of project order or future generated slot name");
        }

        private static void IgnoresNullSemanticEntry()
        {
            var project = new ProjectState("P-null", "Null-safe diagnostics");
            project.Elements.Add(null!);
            var slab = MeshElement("S1", "AA", "1");
            project.Elements.Add(slab);

            var issues = Inspect(project, "AA");
            Require(!issues.Any(x => x.Code == "SLAB_MESH_FOOTPRINT_MODE_INVALID"),
                "null semantic entries must not break standalone slab mesh health");
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, params string[] liveHandles)
        {
            return new GeneratedSlabMeshHealthService().Inspect(
                project,
                new HashSet<string>(liveHandles, StringComparer.OrdinalIgnoreCase));
        }

        private static ProjectElement MeshElement(string id, string handles, string count)
        {
            var element = new ProjectElement(id, ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSlabMeshHandles"] = handles;
            element.Properties["GeneratedSlabMeshCount"] = count;
            element.Properties["GeneratedSlabMeshXDiameterMm"] = "12";
            element.Properties["GeneratedSlabMeshYDiameterMm"] = "10";
            element.Properties["GeneratedSlabMeshXActualSpacingM"] = "0.2";
            element.Properties["GeneratedSlabMeshYActualSpacingM"] = "0.15";
            element.Properties["GeneratedSlabMeshCoverM"] = "0.03";
            element.Properties["GeneratedSlabMeshFaces"] = "Bottom";
            element.Properties["GeneratedSlabMeshMode"] = "SlabMeshXY";
            element.Properties["GeneratedSlabMeshFootprintMode"] = "RectangleLocalXY";
            return element;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("GeneratedSlabMeshHealthSmoke: " + message);
        }
    }
}
