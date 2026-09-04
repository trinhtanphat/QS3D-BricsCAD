using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionInspectorStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ReplacedElementDuringSelectionEnumerationFailsClosed();
            ReplacedFamilyDuringSelectionEnumerationFailsClosed();
            StableInspectionStillUsesCanonicalInstances();
        }

        private static void ReplacedElementDuringSelectionEnumerationFailsClosed()
        {
            var project = CreateProject(out var family, out var element);
            var beforeVersion = project.ChangeVersion;

            ThrowsStructuralOwnershipFreshness(() => SemanticSelectionInspector.Inspect(
                project,
                ReplaceElementAndYield(project, family, element)));

            Equal(beforeVersion, project.ChangeVersion, "element replacement change version");
            Equal(1, project.Elements.Count, "element replacement count");
            False(ReferenceEquals(project.Elements[0], element), "element replacement ownership");
        }

        private static void ReplacedFamilyDuringSelectionEnumerationFailsClosed()
        {
            var project = CreateProject(out var family, out var element);
            var beforeVersion = project.ChangeVersion;

            ThrowsProjectGenerationFreshness(() => SemanticSelectionInspector.Inspect(
                project,
                ReplaceFamilyAndYield(project, family, element.Id)));

            Equal(checked(beforeVersion + 2L), project.ChangeVersion, "family replacement change version");
            Equal(1, project.Families.Count, "family replacement count");
            False(ReferenceEquals(project.Families[0], family), "family replacement ownership");
        }

        private static void StableInspectionStillUsesCanonicalInstances()
        {
            var project = CreateProject(out var family, out var element);
            var beforeVersion = project.ChangeVersion;

            var inspection = SemanticSelectionInspector.Inspect(project, new[] { element.Id });

            Equal(beforeVersion, project.ChangeVersion, "stable change version");
            Equal(1, inspection.Count, "stable selected count");
            Equal(element.Id, inspection.ElementIds[0], "stable element id");
            Equal(family.Id, inspection.Family.Value ?? string.Empty, "stable family id");
            Equal("100", FindProperty(inspection, "Width"), "stable family property");
            Equal("A", FindProperty(inspection, "Mark"), "stable element property");
        }

        private static ProjectState CreateProject(out ProjectFamily family, out ProjectElement element)
        {
            var project = new ProjectState("SEL-STRUCT", "Selection structural freshness");
            family = new ProjectFamily("F1", "Family 1", ElementCategory.CustomQuantity);
            family.Properties["Width"] = "100";
            element = new ProjectElement("E1", ElementCategory.CustomQuantity, family.Id, string.Empty, string.Empty);
            element.Properties["Mark"] = "A";
            project.Families.Add(family);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> ReplaceElementAndYield(
            ProjectState project,
            ProjectFamily family,
            ProjectElement original)
        {
            project.Elements.Remove(original);
            var replacement = new ProjectElement(original.Id, original.Category, family.Id, string.Empty, string.Empty);
            replacement.Properties["Mark"] = "replacement";
            project.Elements.Add(replacement);
            yield return original.Id;
        }

        private static IEnumerable<string> ReplaceFamilyAndYield(
            ProjectState project,
            ProjectFamily original,
            string elementId)
        {
            project.Families.Remove(original);
            var replacement = new ProjectFamily(original.Id, "Replacement family", original.Category);
            replacement.Properties["Width"] = "999";
            project.Families.Add(replacement);
            yield return elementId;
        }

        private static string FindProperty(SemanticSelectionInspection inspection, string name)
        {
            foreach (var property in inspection.Properties)
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value ?? string.Empty;
            throw new InvalidOperationException("Missing semantic selection property: " + name + ".");
        }

        private static void ThrowsStructuralOwnershipFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project semantic ownership changed while inspecting semantic selection; retry the inspection.";
                if (string.Equals(ex.Message, expected, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected semantic selection structural ownership error.", ex);
            }
            throw new InvalidOperationException("Expected semantic selection structural ownership rejection.");
        }

        private static void ThrowsProjectGenerationFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project state changed while materializing semantic selection ids.";
                if (string.Equals(ex.Message, expected, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected semantic selection project-generation freshness error.", ex);
            }
            throw new InvalidOperationException("Expected semantic selection project-generation freshness rejection.");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException("SemanticSelectionInspectorStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "SemanticSelectionInspectorStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
