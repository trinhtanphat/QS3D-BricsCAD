using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditNumericNoOpSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GeometryNumericNoOpPreservesLexicalAndFreshnessState();
            NonGeometryNumericNoOpPreservesLexicalState();
            RealGeometryNumericChangeStillMutates();
        }

        private static void GeometryNumericNoOpPreservesLexicalAndFreshnessState()
        {
            var setup = NewWall("noop-geometry");
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.Properties["GeneratedSolidHandle"] = "A1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "WidthM",
                1d);

            if (changed.Count != 0)
                throw new InvalidOperationException("Bulk numeric x1 on an exact numeric value must report no changed elements.");
            if (!setup.Element.Properties.TryGetValue("WidthM", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk numeric x1 rewrote the geometry property's lexical representation.");
            if (setup.Project.ChangeVersion != beforeProjectVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 advanced project freshness for an exact numeric no-op.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 dirtied the element for an exact numeric no-op.");
            if (setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("Bulk numeric x1 marked generated solid output stale for an exact numeric no-op.");
        }

        private static void NonGeometryNumericNoOpPreservesLexicalState()
        {
            var setup = NewWall("noop-property");
            setup.Element.Properties["Scale"] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "Scale",
                1d);

            if (changed.Count != 0)
                throw new InvalidOperationException("Bulk numeric x1 on a non-geometry property must report no changed elements.");
            if (!setup.Element.Properties.TryGetValue("Scale", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk numeric x1 rewrote a non-geometry property's lexical representation.");
            if (setup.Project.ChangeVersion != beforeProjectVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 advanced project freshness for a non-geometry no-op.");
            if (setup.Element.Dirty != ElementDirtyFlags.None || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Bulk numeric x1 dirtied the element for a non-geometry no-op.");
        }

        private static void RealGeometryNumericChangeStillMutates()
        {
            var setup = NewWall("real-change");
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.Properties["GeneratedSolidHandle"] = "A1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeProjectVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "WidthM",
                2d);

            if (changed.Count != 1 || !string.Equals(changed[0], setup.Element.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("A real bulk numeric multiplication did not report the changed element.");
            if (!setup.Element.Properties.TryGetValue("WidthM", out var raw) || !string.Equals(raw, "2", StringComparison.Ordinal))
                throw new InvalidOperationException("A real bulk numeric multiplication did not persist the expected round-trip value.");
            if (setup.Project.ChangeVersion != checked(beforeProjectVersion + 1L))
                throw new InvalidOperationException("A real bulk numeric multiplication must advance project revision exactly once.");
            var requiredDirty = ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if ((setup.Element.Dirty & requiredDirty) != requiredDirty)
                throw new InvalidOperationException("A real geometry numeric multiplication did not preserve expected dirty flags.");
            if (!setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("A real geometry numeric multiplication did not mark generated solid output stale.");
        }

        private static Setup NewWall(string suffix)
        {
            var project = new ProjectState("P-BULK-NUMERIC-" + suffix, "Bulk numeric no-op");
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
