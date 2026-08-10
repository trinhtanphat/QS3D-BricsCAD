using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedRebarOwnershipHealthSmoke
    {
        public static void Run()
        {
            CrossElementConflict();
            CrossKeySameElementConflict();
            DistinctHandlesAreClean();
        }

        private static void CrossElementConflict()
        {
            var project = new ProjectState("P", "P");
            var a = Element("A"); a.Properties["GeneratedRebarHandles"] = "AA";
            var b = Element("B"); b.Properties["GeneratedTieRebarHandles"] = "AA";
            project.Elements.Add(a); project.Elements.Add(b);
            var issues = new GeneratedRebarOwnershipHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT"));
        }

        private static void CrossKeySameElementConflict()
        {
            var project = new ProjectState("P", "P");
            var a = Element("A");
            a.Properties["GeneratedShapeRebarHandles"] = "BB";
            a.Properties["GeneratedTieRebarHandles"] = "BB";
            project.Elements.Add(a);
            var issues = new GeneratedRebarOwnershipHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT"));
        }

        private static void DistinctHandlesAreClean()
        {
            var project = new ProjectState("P", "P");
            var a = Element("A");
            a.Properties["GeneratedRebarHandles"] = "AA";
            a.Properties["GeneratedShapeRebarHandles"] = "BB";
            a.Properties["GeneratedTieRebarHandles"] = "CC";
            project.Elements.Add(a);
            var issues = new GeneratedRebarOwnershipHealthService().Inspect(project);
            True(issues.Count == 0);
        }

        private static ProjectElement Element(string id) => new ProjectElement(id, ElementCategory.Column, string.Empty, string.Empty, string.Empty);
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    }
}
