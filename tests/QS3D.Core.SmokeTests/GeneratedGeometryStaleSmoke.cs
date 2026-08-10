using System;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedGeometryStaleSmoke
    {
        public static void Run()
        {
            GeneratedOutputsBecomeStaleAfterSemanticEdit();
            ReplacedHandleAutoResolvesOnlyItsOwnStaleKind();
            ExplicitClearPreservesOtherStaleKinds();
            StaleHealthReportsAllGeneratedKinds();
            ElementsWithoutGeneratedOutputsRemainFresh();
        }

        private static void GeneratedOutputsBecomeStaleAfterSemanticEdit()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB;BC";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            element.Properties["GeneratedTieRebarHandles"] = "DD";
            element.Properties["GeneratedBeamStirrupHandles"] = "EE";
            element.MarkGeneratedGeometryStale("Thickness changed");

            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedRebarStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedTieRebarStale());
            True(element.IsGeneratedBeamStirrupStale());
            True(element.IsGeneratedGeometryStale());
            Equal("Thickness changed", element.Properties[ProjectElement.GeneratedGeometryStaleReasonKey]);
        }

        private static void ReplacedHandleAutoResolvesOnlyItsOwnStaleKind()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedRebarHandles"] = "BB";
            element.Properties["GeneratedShapeRebarHandles"] = "CC";
            element.Properties["GeneratedTieRebarHandles"] = "DD";
            element.Properties["GeneratedBeamStirrupHandles"] = "EE";
            element.MarkGeneratedGeometryStale("Family changed");

            element.Properties["GeneratedRebarHandles"] = "BD";
            False(element.IsGeneratedRebarStale());
            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedTieRebarStale());
            True(element.IsGeneratedBeamStirrupStale());
            True(element.IsGeneratedGeometryStale());

            element.Properties["GeneratedSolidHandle"] = "AD";
            element.Properties["GeneratedShapeRebarHandles"] = "CD";
            element.Properties["GeneratedTieRebarHandles"] = "DE";
            True(element.IsGeneratedBeamStirrupStale());
            element.Properties["GeneratedBeamStirrupHandles"] = "EF";
            False(element.IsGeneratedGeometryStale());
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
        }

        private static void ExplicitClearPreservesOtherStaleKinds()
        {
            var element = Element();
            element.Properties["GeneratedSolidHandle"] = "10";
            element.Properties["GeneratedRebarHandles"] = "20";
            element.Properties["GeneratedShapeRebarHandles"] = "30";
            element.Properties["GeneratedTieRebarHandles"] = "40";
            element.Properties["GeneratedBeamStirrupHandles"] = "50";
            element.MarkGeneratedGeometryStale("Instance changed");

            element.ClearGeneratedRebarStale();
            False(element.IsGeneratedRebarStale());
            True(element.IsGeneratedSolidStale());
            True(element.IsGeneratedShapeRebarStale());
            True(element.IsGeneratedTieRebarStale());
            True(element.IsGeneratedBeamStirrupStale());

            element.ClearGeneratedSolidStale();
            element.ClearGeneratedShapeRebarStale();
            element.ClearGeneratedTieRebarStale();
            True(element.IsGeneratedBeamStirrupStale());
            element.ClearGeneratedBeamStirrupStale();
            False(element.IsGeneratedGeometryStale());
        }

        private static void StaleHealthReportsAllGeneratedKinds()
        {
            var project = new ProjectState("STALE");
            var element = Element();
            project.Elements.Add(element);
            element.Properties["GeneratedSolidHandle"] = "10";
            element.Properties["GeneratedRebarHandles"] = "20";
            element.Properties["GeneratedShapeRebarHandles"] = "30";
            element.Properties["GeneratedTieRebarHandles"] = "40";
            element.Properties["GeneratedBeamStirrupHandles"] = "50";
            element.MarkGeneratedGeometryStale("source moved");
            var issues = new GeneratedGeometryStaleHealthService().Inspect(project);
            Equal(5, issues.Count);
            Contains(issues, "GENERATED_SOLID_STALE");
            Contains(issues, "REBAR_GENERATED_STALE");
            Contains(issues, "SHAPE_REBAR_GENERATED_STALE");
            Contains(issues, "TIE_REBAR_GENERATED_STALE");
            Contains(issues, "BEAM_STIRRUP_GENERATED_STALE");
        }

        private static void ElementsWithoutGeneratedOutputsRemainFresh()
        {
            var element = Element();
            element.MarkGeneratedGeometryStale("No generated geometry");
            False(element.IsGeneratedGeometryStale());
            False(element.Properties.ContainsKey(ProjectElement.GeneratedGeometryStateKey));
        }

        private static void Contains(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues) if (string.Equals(issue.Code, code, StringComparison.Ordinal)) return;
            throw new Exception("Expected issue code " + code + ".");
        }

        private static ProjectElement Element() => new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    }
}
