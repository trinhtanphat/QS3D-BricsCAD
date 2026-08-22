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
            ReportsNonCanonicalStaleStateTokensWithoutMutation();
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

        private static void ReportsNonCanonicalStaleStateTokensWithoutMutation()
        {
            var project = new ProjectState("stale-health-canonicality", "Stale Health Canonicality");
            var canonical = AddSolidState(project, "E3", "stale", "A1", true);
            var uppercase = AddSolidState(project, "E4", "STALE", "A2", true);
            var mixedCase = AddSolidState(project, "E5", "StAlE", "A3", true);
            var padded = AddSolidState(project, "E6", " stale ", "A4", true);
            var missingSnapshot = AddSolidState(project, "E7", "STALE", "A5", false);

            var canonicalBefore = Snapshot(canonical);
            var uppercaseBefore = Snapshot(uppercase);
            var mixedCaseBefore = Snapshot(mixedCase);
            var paddedBefore = Snapshot(padded);
            var missingBefore = Snapshot(missingSnapshot);
            var service = new GeneratedGeometryStaleHealthService();

            var first = service.Inspect(project);
            var second = service.Inspect(project);

            False(first.Any(x => x.Code == "GENERATED_STALE_STATE_NON_CANONICAL" && x.ElementId == "E3"));
            True(first.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E3"));

            True(first.Any(x => x.Code == "GENERATED_STALE_STATE_NON_CANONICAL" && x.ElementId == "E4"));
            True(first.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E4"));
            True(first.Any(x => x.Code == "GENERATED_STALE_STATE_NON_CANONICAL" && x.ElementId == "E5"));
            True(first.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E5"));

            True(first.Any(x => x.Code == "GENERATED_STALE_STATE_NON_CANONICAL" && x.ElementId == "E6"));
            False(first.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E6"));

            True(first.Any(x => x.Code == "GENERATED_STALE_STATE_NON_CANONICAL" && x.ElementId == "E7"));
            True(first.Any(x => x.Code == "GENERATED_STALE_METADATA_INVALID" && x.ElementId == "E7"));
            False(first.Any(x => x.Code == "GENERATED_SOLID_STALE" && x.ElementId == "E7"));

            Equal(first.Count, second.Count);
            Equal(canonicalBefore, Snapshot(canonical));
            Equal(uppercaseBefore, Snapshot(uppercase));
            Equal(mixedCaseBefore, Snapshot(mixedCase));
            Equal(paddedBefore, Snapshot(padded));
            Equal(missingBefore, Snapshot(missingSnapshot));
        }

        private static ProjectElement AddSolidState(
            ProjectState project,
            string elementId,
            string state,
            string handle,
            bool includeSnapshot)
        {
            var element = new ProjectElement(elementId, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = handle;
            element.Properties[ProjectElement.GeneratedSolidStateKey] = state;
            if (includeSnapshot)
                element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey] = handle;
            project.Elements.Add(element);
            return element;
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
