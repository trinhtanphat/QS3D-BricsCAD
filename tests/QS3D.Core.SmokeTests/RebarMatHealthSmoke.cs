using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarMatHealthSmoke
    {
        public static void Run()
        {
            HealthyMat();
            DetectsCategoryAndStale();
            DetectsCrossSetOwnershipConflict();
        }

        private static void HealthyMat()
        {
            var project = new ProjectState("P", "P");
            var slab = MatElement("S1", ElementCategory.Slab, "AA;BB", "2", "Bottom");
            project.Elements.Add(slab);
            var issues = new GeneratedRebarMatHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA", "BB" });
            Require(!issues.Any(x => x.Severity == HealthSeverity.Error), "healthy slab mat produced an error");
        }

        private static void DetectsCategoryAndStale()
        {
            var project = new ProjectState("P", "P");
            var beam = MatElement("B1", ElementCategory.Beam, "AA", "1", "Both");
            beam.Dirty = ElementDirtyFlags.Geometry;
            project.Elements.Add(beam);
            var issues = new GeneratedRebarMatHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "REBAR_MAT_CATEGORY_MISMATCH"), "category mismatch not detected");
            Require(issues.Any(x => x.Code == "REBAR_MAT_GENERATED_STALE"), "dirty/stale mat not detected");
        }

        private static void DetectsCrossSetOwnershipConflict()
        {
            var project = new ProjectState("P", "P");
            var slab = MatElement("S1", ElementCategory.Slab, "AA", "1", "Bottom");
            slab.Properties["GeneratedRebarHandles"] = "AA";
            slab.Properties["GeneratedRebarCount"] = "1";
            slab.Properties["GeneratedRebarDiameterMm"] = "16";
            slab.Properties["GeneratedRebarMode"] = "ColumnVerticalBars";
            project.Elements.Add(slab);
            var issues = new GeneratedRebarMatHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            Require(issues.Any(x => x.Code == "REBAR_MAT_GENERATED_OWNERSHIP_CONFLICT"), "cross-set ownership conflict not detected");
        }

        private static ProjectElement MatElement(string id, ElementCategory category, string handles, string count, string faces)
        {
            var element = new ProjectElement(id, category, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedRebarMatHandles"] = handles;
            element.Properties["GeneratedRebarMatCount"] = count;
            element.Properties["GeneratedRebarMatFaces"] = faces;
            element.Properties["GeneratedRebarMatMode"] = "Rectangular.OrthogonalMat";
            return element;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("RebarMatHealthSmoke: " + message);
        }
    }
}
