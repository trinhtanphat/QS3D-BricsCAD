using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStandaloneHandleNumericIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LongitudinalAliasesCollapseToOneHandle();
            ShapeAliasesCollapseToOneHandle();
            TieAliasesCollapseToOneHandle();
            LongitudinalSourceAliasFailsVisible();
            CrossOwnerAliasesFailVisible();
            OptionalPrefixValidityRemainsUnchanged();
            DistinctHandlesRemainDistinct();
        }

        private static void LongitudinalAliasesCollapseToOneHandle()
        {
            var project = Project("LONG-DUP");
            var element = Longitudinal("E-1", "A;0A", "2");
            project.Elements.Add(element);
            var issues = new GeneratedRebarHealthService().Inspect(project);
            Require(issues, element.Id, "DUPLICATE_REBAR_GENERATED_HANDLE");
            Require(issues, element.Id, "REBAR_GENERATED_COUNT_MISMATCH");
        }

        private static void ShapeAliasesCollapseToOneHandle()
        {
            var project = Project("SHAPE-DUP");
            var element = new ProjectElement("E-1", ElementCategory.Wall);
            element.Properties["GeneratedShapeRebarHandles"] = "B;00B";
            element.Properties["GeneratedShapeRebarCount"] = "2";
            project.Elements.Add(element);
            var issues = new GeneratedRebarHealthService().InspectShape(project);
            Require(issues, element.Id, "DUPLICATE_SHAPE_REBAR_GENERATED_HANDLE");
            Require(issues, element.Id, "SHAPE_REBAR_GENERATED_COUNT_MISMATCH");
        }

        private static void TieAliasesCollapseToOneHandle()
        {
            var project = Project("TIE-DUP");
            var element = Tie("E-1", "C;000C", "2");
            project.Elements.Add(element);
            var issues = new GeneratedTieRebarHealthService().Inspect(project);
            Require(issues, element.Id, "DUPLICATE_TIE_REBAR_GENERATED_HANDLE");
            Require(issues, element.Id, "TIE_REBAR_GENERATED_COUNT_MISMATCH");
        }

        private static void LongitudinalSourceAliasFailsVisible()
        {
            var project = Project("SOURCE");
            var element = Longitudinal("E-1", "D", "1");
            element.SourceHandles.Add("00D");
            project.Elements.Add(element);
            Require(new GeneratedRebarHealthService().Inspect(project), element.Id, "REBAR_GENERATED_HANDLE_IN_SOURCE");
        }

        private static void CrossOwnerAliasesFailVisible()
        {
            var project = Project("CROSS");
            var longitudinal = Longitudinal("E-1", "E", "1");
            var tie = Tie("E-2", "000E", "1");
            project.Elements.Add(longitudinal);
            project.Elements.Add(tie);

            Require(new GeneratedRebarHealthService().Inspect(project), longitudinal.Id, "REBAR_GENERATED_OWNERSHIP_CONFLICT");
            Require(new GeneratedTieRebarHealthService().Inspect(project), tie.Id, "TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT");
        }

        private static void OptionalPrefixValidityRemainsUnchanged()
        {
            var project = Project("PREFIX");
            var element = Longitudinal("E-1", "0xA", "0");
            project.Elements.Add(element);
            var issues = new GeneratedRebarHealthService().Inspect(project);
            Require(issues, element.Id, "INVALID_REBAR_GENERATED_HANDLE");
            EnsureAbsent(issues, "DUPLICATE_REBAR_GENERATED_HANDLE", "0x validity is explicitly outside this lane and must not be reclassified as a duplicate.");
        }

        private static void DistinctHandlesRemainDistinct()
        {
            var project = Project("DISTINCT");
            var element = Longitudinal("E-1", "A;B", "2");
            project.Elements.Add(element);
            var issues = new GeneratedRebarHealthService().Inspect(project);
            EnsureAbsent(issues, "DUPLICATE_REBAR_GENERATED_HANDLE", "Distinct CAD handles must remain distinct.");
            EnsureAbsent(issues, "REBAR_GENERATED_COUNT_MISMATCH", "Distinct valid handles must preserve the persisted count.");
        }

        private static ProjectElement Longitudinal(string id, string handles, string count)
        {
            var element = new ProjectElement(id, ElementCategory.Column);
            element.Properties["GeneratedRebarHandles"] = handles;
            element.Properties["GeneratedRebarCount"] = count;
            element.Properties["GeneratedRebarDiameterMm"] = "10";
            return element;
        }

        private static ProjectElement Tie(string id, string handles, string count)
        {
            var element = new ProjectElement(id, ElementCategory.Column);
            element.Properties["GeneratedTieRebarHandles"] = handles;
            element.Properties["GeneratedTieRebarCount"] = count;
            element.Properties["GeneratedTieRebarDiameterMm"] = "10";
            element.Properties["GeneratedTieRebarActualSpacingM"] = "0.2";
            element.Properties["GeneratedTieRebarCoverM"] = "0.05";
            element.Properties["GeneratedTieRebarMode"] = "ColumnRectangularTies";
            return element;
        }

        private static ProjectState Project(string suffix) =>
            new ProjectState("P-REBAR-STANDALONE-ID-" + suffix, "Standalone Rebar numeric identity smoke");

        private static void Require(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                throw new InvalidOperationException("Expected standalone rebar numeric-identity issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
        }
    }
}
