using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyPropertyRemovalFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OrdinaryInheritedRemovalDoesNotDirtyGeometryOrStaleOutput();
            GeometryInheritedRemovalDirtiesGeometryAndStalesOutput();
            OutputOnlyInheritedRemovalStalesWithoutGeometryDirty();
            ExplicitOverrideRemainsUntouched();
        }

        private static void OrdinaryInheritedRemovalDoesNotDirtyGeometryOrStaleOutput()
        {
            var setup = Create("ordinary", "Scale", "1.0", "1.0");
            var beforeVersion = setup.Project.ChangeVersion;

            var result = ProjectFamilyService.RemoveProperty(setup.Project, setup.Family.Id, "Scale");

            AssertCounts(result, 1, 0, "ordinary inherited removal");
            AssertRemoved(setup, "Scale", "ordinary inherited removal");
            if (setup.Project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("Ordinary Family property removal must advance project revision exactly once.");
            var expected = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (setup.Element.Dirty != expected)
                throw new InvalidOperationException("Ordinary inherited Family property removal dirtied unexpected element surfaces: " + setup.Element.Dirty + ".");
            if (setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("Ordinary inherited Family property removal unnecessarily marked generated solid output stale.");
        }

        private static void GeometryInheritedRemovalDirtiesGeometryAndStalesOutput()
        {
            var setup = Create("geometry", "WidthM", "1.0", "1.0");

            var result = ProjectFamilyService.RemoveProperty(setup.Project, setup.Family.Id, "WidthM");

            AssertCounts(result, 1, 0, "geometry inherited removal");
            AssertRemoved(setup, "WidthM", "geometry inherited removal");
            var expected = ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if ((setup.Element.Dirty & expected) != expected)
                throw new InvalidOperationException("Geometry-affecting Family property removal did not preserve required dirty flags.");
            if (!setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("Geometry-affecting Family property removal did not mark generated solid output stale.");
        }

        private static void OutputOnlyInheritedRemovalStalesWithoutGeometryDirty()
        {
            var setup = Create("output", "Material", "Concrete", "Concrete");

            var result = ProjectFamilyService.RemoveProperty(setup.Project, setup.Family.Id, "Material");

            AssertCounts(result, 1, 0, "generated-output inherited removal");
            AssertRemoved(setup, "Material", "generated-output inherited removal");
            var expected = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (setup.Element.Dirty != expected)
                throw new InvalidOperationException("Generated-output-only Family property removal must not dirty Geometry: " + setup.Element.Dirty + ".");
            if (!setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("Generated-output-only Family property removal did not mark generated solid output stale.");
        }

        private static void ExplicitOverrideRemainsUntouched()
        {
            var setup = Create("override", "Scale", "1.0", "2.0");
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            var result = ProjectFamilyService.RemoveProperty(setup.Project, setup.Family.Id, "Scale");

            AssertCounts(result, 0, 1, "explicit override removal");
            if (setup.Family.Properties.ContainsKey("Scale"))
                throw new InvalidOperationException("Family property was not removed while preserving an explicit instance override.");
            if (!setup.Element.Properties.TryGetValue("Scale", out var raw) || !string.Equals(raw, "2.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Explicit instance override was changed while removing the Family default.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Preserved explicit override changed element freshness.");
            if (setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("Preserved explicit override unnecessarily marked generated solid output stale.");
        }

        private static Setup Create(string suffix, string key, string familyValue, string instanceValue)
        {
            var project = new ProjectState("P-FAMILY-REMOVE-" + suffix, "Family remove freshness");
            var family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
            family.Properties[key] = familyValue;
            project.Families.Add(family);

            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            element.Properties[key] = instanceValue;
            element.Properties["GeneratedSolidHandle"] = "A1";
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            return new Setup(project, family, element);
        }

        private static void AssertCounts(FamilyPropertyUpdateResult result, int inherited, int overrides, string label)
        {
            if (result.InheritedInstancesUpdated != inherited || result.OverridesPreserved != overrides)
                throw new InvalidOperationException(label + " reported unexpected Family property update counts.");
        }

        private static void AssertRemoved(Setup setup, string key, string label)
        {
            if (setup.Family.Properties.ContainsKey(key))
                throw new InvalidOperationException(label + " did not remove the Family property.");
            if (setup.Element.Properties.ContainsKey(key))
                throw new InvalidOperationException(label + " did not remove the inherited instance property.");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectFamily family, ProjectElement element)
            {
                Project = project;
                Family = family;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectFamily Family { get; }
            public ProjectElement Element { get; }
        }
    }
}
