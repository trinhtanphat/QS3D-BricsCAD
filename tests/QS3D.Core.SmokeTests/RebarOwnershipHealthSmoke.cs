using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarOwnershipHealthSmoke
    {
        public static void Run()
        {
            RebarCannotClaimHostGeneratedSolid();
            ShapeCannotClaimAnotherElementsSource();
            ShapeHealthSeesColumnRebarConflict();
        }

        private static void RebarCannotClaimHostGeneratedSolid()
        {
            var project = new ProjectState("ownership-host", "Ownership Host");
            var host = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            host.Properties["GeneratedSolidHandle"] = "AB";
            project.Elements.Add(host);
            var column = new ProjectElement("C1", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            column.Properties["GeneratedRebarHandles"] = "AB";
            column.Properties["GeneratedRebarCount"] = "1";
            column.Properties["GeneratedRebarDiameterMm"] = "16";
            project.Elements.Add(column);

            var issues = new GeneratedRebarHealthService().Inspect(project);
            True(issues.Any(x => x.ElementId == "C1" && x.Code == "REBAR_GENERATED_OWNERSHIP_CONFLICT"));
        }

        private static void ShapeCannotClaimAnotherElementsSource()
        {
            var project = new ProjectState("ownership-source", "Ownership Source");
            var sourceOwner = new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            sourceOwner.SourceHandles.Add("CD");
            project.Elements.Add(sourceOwner);
            var shaped = new ProjectElement("B2", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            shaped.Properties["GeneratedShapeRebarHandles"] = "CD";
            shaped.Properties["GeneratedShapeRebarCount"] = "1";
            project.Elements.Add(shaped);

            var issues = new GeneratedRebarHealthService().InspectShape(project);
            True(issues.Any(x => x.ElementId == "B2" && x.Code == "SHAPE_REBAR_GENERATED_OWNERSHIP_CONFLICT"));
        }

        private static void ShapeHealthSeesColumnRebarConflict()
        {
            var project = new ProjectState("ownership-cross", "Ownership Cross");
            var column = new ProjectElement("C1", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            column.Properties["GeneratedRebarHandles"] = "EF";
            column.Properties["GeneratedRebarCount"] = "1";
            column.Properties["GeneratedRebarDiameterMm"] = "20";
            project.Elements.Add(column);
            var shaped = new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            shaped.Properties["GeneratedShapeRebarHandles"] = "EF";
            shaped.Properties["GeneratedShapeRebarCount"] = "1";
            project.Elements.Add(shaped);

            var issues = new GeneratedRebarHealthService().InspectShape(project);
            True(issues.Any(x => x.ElementId == "B1" && x.Code == "SHAPE_REBAR_GENERATED_OWNERSHIP_CONFLICT"));
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
