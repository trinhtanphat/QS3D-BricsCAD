using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionNumericNoOpSmoke
    {
        private const string PropertyName = "Scale";

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InheritedNoOpDoesNotMaterializeOverride();
            InstanceNoOpPreservesLexicalValue();
            RealInheritedChangeStillWritesOverride();
        }

        private static void InheritedNoOpDoesNotMaterializeOverride()
        {
            var setup = CreateInheritedProject();
            var beforeVersion = setup.Project.ChangeVersion;
            var result = new SemanticSelectionBulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element.Id },
                PropertyName,
                1d);

            if (result.SelectedCount != 1 || result.ChangedCount != 0)
                throw new InvalidOperationException("Numeric x1 on an inherited value must be reported as a selection no-op.");
            if (setup.Project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException("Numeric x1 on an inherited value changed the project revision.");
            if (setup.Element.Properties.ContainsKey(PropertyName))
                throw new InvalidOperationException("Numeric x1 materialized an instance override for an inherited Family value.");
            if (!string.Equals(setup.Family.Properties[PropertyName], "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Numeric x1 changed the inherited Family property text.");
        }

        private static void InstanceNoOpPreservesLexicalValue()
        {
            var setup = CreateInheritedProject();
            setup.Element.Properties[PropertyName] = "1.0";
            var beforeVersion = setup.Project.ChangeVersion;
            var result = new SemanticSelectionBulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element.Id },
                PropertyName,
                1d);

            if (result.ChangedCount != 0)
                throw new InvalidOperationException("Numeric x1 on an instance value must be reported as a selection no-op.");
            if (setup.Project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException("Numeric x1 on an instance value changed the project revision.");
            if (!setup.Element.Properties.TryGetValue(PropertyName, out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Numeric x1 rewrote the existing instance property's lexical representation.");
        }

        private static void RealInheritedChangeStillWritesOverride()
        {
            var setup = CreateInheritedProject();
            var beforeVersion = setup.Project.ChangeVersion;
            var result = new SemanticSelectionBulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element.Id },
                PropertyName,
                2d);

            if (result.SelectedCount != 1 || result.ChangedCount != 1 ||
                result.ChangedElementIds.Count != 1 ||
                !string.Equals(result.ChangedElementIds[0], setup.Element.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("A real numeric multiplication must report the changed semantic element.");
            if (setup.Project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("A real numeric multiplication must advance the project revision exactly once.");
            if (!setup.Element.Properties.TryGetValue(PropertyName, out var raw) || !string.Equals(raw, "2", StringComparison.Ordinal))
                throw new InvalidOperationException("A real inherited numeric multiplication did not write the expected instance override.");
            if (!string.Equals(setup.Family.Properties[PropertyName], "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("A real instance override must not rewrite the source Family property.");
        }

        private static Setup CreateInheritedProject()
        {
            var project = new ProjectState("P-SEL-NOOP", "Selection numeric no-op");
            var family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
            family.Properties[PropertyName] = "1.0";
            project.Families.Add(family);

            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Setup(project, family, element);
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