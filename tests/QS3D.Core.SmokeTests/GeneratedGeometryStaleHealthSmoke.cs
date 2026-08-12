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
            ReportsEachStaleOutputKindWithoutMutation();
            ReplacedHandlesResolveWarningsWithoutMutation();
        }

        private static void ReportsEachStaleOutputKindWithoutMutation()
        {
            var project = new ProjectState("stale-health", "Stale Health");
            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            project.Elements.Add(element);
            element.MarkGeneratedGeometryStale("Width changed");

            var before = Snapshot(element);
            var updatedUtc = element.UpdatedUtc;
            var service = new GeneratedGeometryStaleHealthService();
            var first = service.Inspect(project);
            var second = service.Inspect(project);

            True(first.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E1"));
            True(first.Any(x => x.Code == "REBAR_GENERATED_STALE" && x.ElementId == "E1"));
            True(first.Any(x => x.Code == "SHAPE_REBAR_GENERATED_STALE" && x.ElementId == "E1"));
            True(first.All(x => x.Message.IndexOf("Width changed", StringComparison.OrdinalIgnoreCase) >= 0));
            Equal(first.Count, second.Count);
            Equal(before, Snapshot(element));
            Equal(updatedUtc, element.UpdatedUtc);
        }

        private static void ReplacedHandlesResolveWarningsWithoutMutation()
        {
            var project = new ProjectState("stale-health-rebuild", "Stale Health Rebuild");
            var element = new ProjectElement("E2", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "10";
            element.Properties["GeneratedRebarHandles"] = "20";
            element.Properties["GeneratedShapeRebarHandles"] = "30";
            project.Elements.Add(element);
            element.MarkGeneratedGeometryStale("Property changed");

            element.Properties["GeneratedRebarHandles"] = "21";
            var beforePartial = Snapshot(element);
            var service = new GeneratedGeometryStaleHealthService();
            var firstPartial = service.Inspect(project);
            var secondPartial = service.Inspect(project);
            False(firstPartial.Any(x => x.Code == "REBAR_GENERATED_STALE"));
            True(firstPartial.Any(x => x.Code == "GENERATED_SOLID_STALE"));
            True(firstPartial.Any(x => x.Code == "SHAPE_REBAR_GENERATED_STALE"));
            Equal(firstPartial.Count, secondPartial.Count);
            Equal(beforePartial, Snapshot(element));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedRebarStateKey));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedRebarStaleSnapshotKey));

            element.Properties["GeneratedSolidHandle"] = "11";
            element.Properties["GeneratedShapeRebarHandles"] = "31";
            var beforeFresh = Snapshot(element);
            var firstFresh = service.Inspect(project);
            var secondFresh = service.Inspect(project);
            Equal(0, firstFresh.Count);
            Equal(0, secondFresh.Count);
            Equal(beforeFresh, Snapshot(element));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStaleReasonKey));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStateKey));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedRebarStateKey));
            True(element.Properties.ContainsKey(ProjectElement.GeneratedShapeRebarStateKey));

            element.ClearGeneratedGeometryStale();
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStaleReasonKey));
            False(element.Properties.ContainsKey(ProjectElement.GeneratedSolidStateKey));
            False(element.Properties.ContainsKey(ProjectElement.GeneratedRebarStateKey));
            False(element.Properties.ContainsKey(ProjectElement.GeneratedShapeRebarStateKey));
        }

        private static string Snapshot(ProjectElement element)
        {
            return string.Join("\n", element.Properties
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Key + "=" + x.Value));
        }

        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    }
}
