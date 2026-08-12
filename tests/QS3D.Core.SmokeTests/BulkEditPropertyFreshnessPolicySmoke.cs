using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditPropertyFreshnessPolicySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OrdinaryStringPropertyDoesNotStaleGeneratedOutput();
            OrdinaryNumericPropertyDoesNotStaleGeneratedOutput();
            OutputOnlyPropertyStalesWithoutGeometryDirty();
            GeometryPropertyStalesAndDirtiesGeometry();
        }

        private static void OrdinaryStringPropertyDoesNotStaleGeneratedOutput()
        {
            var setup = NewWall("ordinary-string");
            var beforeVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(
                setup.Project,
                new[] { setup.Element },
                "Scale",
                "2");

            RequireSingleChange(changed, setup.Element.Id, "ordinary string property");
            RequireValue(setup.Element, "Scale", "2", "ordinary string property");
            RequireDirty(setup.Element, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity, "ordinary string property");
            if (setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("An ordinary bulk string property change must not stale generated solid output.");
            RequireSingleProjectTouch(setup.Project, beforeVersion, "ordinary string property");
        }

        private static void OrdinaryNumericPropertyDoesNotStaleGeneratedOutput()
        {
            var setup = NewWall("ordinary-numeric");
            setup.Element.Properties["Scale"] = "2";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                new[] { setup.Element },
                "Scale",
                2d);

            RequireSingleChange(changed, setup.Element.Id, "ordinary numeric property");
            RequireValue(setup.Element, "Scale", "4", "ordinary numeric property");
            RequireDirty(setup.Element, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity, "ordinary numeric property");
            if (setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("An ordinary bulk numeric property change must not stale generated solid output.");
            RequireSingleProjectTouch(setup.Project, beforeVersion, "ordinary numeric property");
        }

        private static void OutputOnlyPropertyStalesWithoutGeometryDirty()
        {
            var setup = NewWall("output-only");
            var beforeVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(
                setup.Project,
                new[] { setup.Element },
                "Material",
                "Concrete-C30");

            RequireSingleChange(changed, setup.Element.Id, "generated-output-only property");
            RequireValue(setup.Element, "Material", "Concrete-C30", "generated-output-only property");
            RequireDirty(setup.Element, ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity, "generated-output-only property");
            if (!setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("A generated-output-only bulk property change must stale generated solid output.");
            RequireSingleProjectTouch(setup.Project, beforeVersion, "generated-output-only property");
        }

        private static void GeometryPropertyStalesAndDirtiesGeometry()
        {
            var setup = NewWall("geometry");
            var beforeVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(
                setup.Project,
                new[] { setup.Element },
                "WidthM",
                "0.25");

            RequireSingleChange(changed, setup.Element.Id, "geometry property");
            RequireValue(setup.Element, "WidthM", "0.25", "geometry property");
            RequireDirty(
                setup.Element,
                ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity,
                "geometry property");
            if (!setup.Element.IsGeneratedSolidStale())
                throw new InvalidOperationException("A geometry bulk property change must stale generated solid output.");
            RequireSingleProjectTouch(setup.Project, beforeVersion, "geometry property");
        }

        private static Setup NewWall(string suffix)
        {
            var project = new ProjectState("P-BULK-FRESHNESS-" + suffix, "Bulk property freshness");
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall);
            element.Properties["GeneratedSolidHandle"] = "A1";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireSingleChange(System.Collections.Generic.IReadOnlyList<string> changed, string elementId, string label)
        {
            if (changed.Count != 1 || !string.Equals(changed[0], elementId, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " did not report exactly the changed element.");
        }

        private static void RequireValue(ProjectElement element, string key, string expected, string label)
        {
            if (!element.Properties.TryGetValue(key, out var actual) || !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " did not persist the expected property value.");
        }

        private static void RequireDirty(ProjectElement element, ElementDirtyFlags expected, string label)
        {
            if (element.Dirty != expected)
                throw new InvalidOperationException(label + " produced unexpected dirty flags: " + element.Dirty + ".");
        }

        private static void RequireSingleProjectTouch(ProjectState project, long beforeVersion, string label)
        {
            if (project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException(label + " must advance project revision exactly once.");
        }

        private sealed class Setup
        {
            internal Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal ProjectElement Element { get; }
        }
    }
}
