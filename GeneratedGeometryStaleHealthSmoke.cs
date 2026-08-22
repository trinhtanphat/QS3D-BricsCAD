using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGeometryStaleHealthSmoke
    {
        public static void Run()
        {
            ReportsEachStaleOutputKind();
            ReplacedHandlesResolveTheirOwnWarnings();
        }

        private static void ReportsEachStaleOutputKind()
        {
            var project = new ProjectState("stale-health", "Stale Health");
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            project.Elements.Add(element);
            element.MarkGeneratedGeometryStale("Width changed");

            var issues = new GeneratedGeometryStaleHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E1"));
            True(issues.Any(x => x.Code == "REBAR_GENERATED_STALE" && x.ElementId == "E1"));
            True(issues.Any(x => x.Code == "SHAPE_REBAR_GENERATED_STALE" && x.ElementId == "E1"));
            True(issues.All(x => x.Message.IndexOf("Width changed", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static void ReplacedHandlesResolveTheirOwnWarnings()
        {
            var project = new ProjectState("stale-health-rebuild", "Stale Health Rebuild");
            var element = new ProjectElement("E2", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "10";
            element.Properties["GeneratedRebarHandles"] = "20";
            element.Properties["GeneratedShapeRebarHandles"] = "30";
            project.Elements.Add(element);
            element.MarkGeneratedGeometryStale("Property changed");

            element.Properties["GeneratedRebarHandles"] = "21";
            var afterColumn = new GeneratedGeometryStaleHealthService().Inspect(project);
            False(afterColumn.Any(x => x.Code == "REBAR_GENERATED_STALE"));
            True(afterColumn.Any(x => x.Code == "GENERATED_SOLID_STALE"));
            True(afterColumn.Any(x => x.Code == "SHAPE_REBAR_GENERATED_STALE"));

            element.Properties["GeneratedSolidHandle"] = "11";
            element.Properties["GeneratedShapeRebarHandles"] = "31";
            var fresh = new GeneratedGeometryStaleHealthService().Inspect(project);
            Equal(0, fresh.Count);
        }

        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    }
}
