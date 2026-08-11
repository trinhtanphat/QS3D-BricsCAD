using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionInspectorSmoke
    {
        public static void Run()
        {
            CommonAndMixedValuesAreStable();
            ReferencePresenceCountsActualAssignments();
            FamilyDefaultsParticipateInEffectiveValues();
            InternalOwnershipPropertiesStayHidden();
            MissingSelectionFailsClosed();
            MissingSemanticReferenceFailsClosed();
            FamilyCategoryMismatchFailsClosed();
            DuplicateProjectIdentityFailsClosed();
            EmptySelectionIsSupported();
        }

        private static void CommonAndMixedValuesAreStable()
        {
            var project = BuildProject();
            var result = SemanticSelectionInspector.Inspect(project, new[] { "B-002", "B-001" });
            Equal(2, result.Count);
            Equal("B-001", result.ElementIds[0]);
            Equal("B-002", result.ElementIds[1]);
            Equal(false, result.HasMixedCategories);
            Equal(false, result.Family.IsMixed);
            Equal("FAM-B", result.Family.Value);
            Equal(false, result.Floor.IsMixed);
            Equal("F-02", result.Floor.Value);
            Equal(true, result.Zone.IsMixed);

            var thickness = result.Properties.Single(x => x.Name == "ThicknessM");
            Equal(false, thickness.IsMixed);
            Equal("0.3", thickness.Value);
            Equal(2, thickness.PresentCount);

            var mark = result.Properties.Single(x => x.Name == "Mark");
            Equal(true, mark.IsMixed);
            Equal(null, mark.Value);

            var note = result.Properties.Single(x => x.Name == "Note");
            Equal(true, note.IsMixed);
            Equal(1, note.PresentCount);

            var length = result.Quantities.Single(x => x.Name == "LengthM");
            Equal(true, length.IsMixed);
            Equal(2, length.PresentCount);
        }

        private static void ReferencePresenceCountsActualAssignments()
        {
            var project = BuildProject();
            project.Elements[0].ZoneId = string.Empty;
            var partial = SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" });
            Equal(true, partial.Zone.IsMixed);
            Equal(1, partial.Zone.PresentCount);
            Equal(null, partial.Zone.Value);

            project.Elements[1].ZoneId = "   ";
            var unassigned = SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" });
            Equal(false, unassigned.Zone.IsMixed);
            Equal(0, unassigned.Zone.PresentCount);
            Equal(string.Empty, unassigned.Zone.Value);
        }

        private static void FamilyDefaultsParticipateInEffectiveValues()
        {
            var project = BuildProject();
            var result = SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" });

            var fireRating = result.Properties.Single(x => x.Name == "FireRating");
            Equal(false, fireRating.IsMixed);
            Equal("R60", fireRating.Value);
            Equal(2, fireRating.PresentCount);

            var material = result.Properties.Single(x => x.Name == "Material");
            Equal(true, material.IsMixed);
            Equal(null, material.Value);
            Equal(2, material.PresentCount);
        }

        private static void InternalOwnershipPropertiesStayHidden()
        {
            var project = BuildProject();
            project.Elements[0].Properties["GeneratedSolidHandle"] = "AB12";
            project.Elements[0].Properties[ProjectElement.GeneratedGeometryStateKey] = "stale";
            project.Elements[0].Properties["PhysicalOpeningCutHandle"] = "CD34";
            project.Families[0].Properties["GeneratedFamilyHandle"] = "EF56";
            var result = SemanticSelectionInspector.Inspect(project, new[] { project.Elements[0].Id });
            if (result.Properties.Any(x => x.Name.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new Exception("Property inspector must not expose native ownership handles.");
            if (result.Properties.Any(x => x.Name.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Property inspector must not expose internal generated-state keys as editable semantic properties.");
            if (result.Properties.Any(x => x.Name.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Property inspector must not expose physical opening cut ownership state.");
        }

        private static void MissingSelectionFailsClosed()
        {
            var project = BuildProject();
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { "E-404" }),
                "Missing selected semantic IDs must fail closed.");
        }

        private static void MissingSemanticReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Elements[0].FloorId = "F-404";
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { project.Elements[0].Id }),
                "Missing selected floor references must fail closed.");
        }

        private static void FamilyCategoryMismatchFailsClosed()
        {
            var project = BuildProject();
            project.Families[0].Category = ElementCategory.Column;
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { project.Elements[0].Id }),
                "Selected element/family category mismatch must fail closed.");
        }

        private static void DuplicateProjectIdentityFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("b-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A"));
            MustFail(
                () => SemanticSelectionInspector.Inspect(project, new[] { "B-001" }),
                "Duplicate project element IDs must fail closed before inspection.");
        }

        private static void EmptySelectionIsSupported()
        {
            var result = SemanticSelectionInspector.Inspect(BuildProject(), Array.Empty<string>());
            Equal(0, result.Count);
            Equal(0, result.Categories.Count);
            Equal(false, result.Family.IsMixed);
            Equal(null, result.Family.Value);
            Equal(0, result.Properties.Count);
            Equal(0, result.Quantities.Count);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-PROP", "Property Inspector Smoke");
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Zones.Add(new ZoneDefinition("Z-B", "Zone B"));
            var family = new ProjectFamily("FAM-B", "Beam 300x500", ElementCategory.Beam);
            family.Properties["FireRating"] = "R60";
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var first = new ProjectElement("B-002", ElementCategory.Beam, "FAM-B", "F-02", "Z-B");
            first.SetProperty("ThicknessM", "0.3");
            first.SetProperty("Mark", "B2");
            first.SetProperty("Note", "Edge");
            first.SetProperty("Material", "C35");
            first.SetQuantity("LengthM", 5d);

            var second = new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-A");
            second.SetProperty("ThicknessM", "0.3");
            second.SetProperty("Mark", "B1");
            second.SetQuantity("LengthM", 6d);

            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
