using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditTargetEnumerationFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ObjectSetPropertyFailsClosedOnEnumerationMutation();
            NumericEditFailsClosedOnEnumerationMutation();
            IdSetPropertyFailsClosedOnEnumerationMutation();
            FamilyAssignmentFailsClosedOnEnumerationMutation();
            SideEffectFreeTargetsStillMutateNormally();
        }

        private static void ObjectSetPropertyFailsClosedOnEnumerationMutation()
        {
            var setup = NewWall("OBJECT-SET");
            setup.Element.Properties["WidthM"] = "1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            ThrowsFreshness(() => new BulkEditService().SetProperty(
                setup.Project,
                TouchAndYieldElement(setup.Project, setup.Element),
                "WidthM",
                "2"));

            Equal(checked(beforeVersion + 1L), setup.Project.ChangeVersion, "Caller enumeration should be the only project version change.");
            PropertyEquals(setup.Element, "WidthM", "1", "Object-target SetProperty mutated after its target enumeration became stale.");
            Equal(ElementDirtyFlags.None, setup.Element.Dirty, "Object-target SetProperty dirtied the element after stale target enumeration.");
        }

        private static void NumericEditFailsClosedOnEnumerationMutation()
        {
            var setup = NewWall("NUMERIC");
            setup.Element.Properties["WidthM"] = "2";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            ThrowsFreshness(() => new BulkEditService().MultiplyNumericProperty(
                setup.Project,
                TouchAndYieldElement(setup.Project, setup.Element),
                "WidthM",
                3d));

            Equal(checked(beforeVersion + 1L), setup.Project.ChangeVersion, "Caller enumeration should be the only numeric-edit version change.");
            PropertyEquals(setup.Element, "WidthM", "2", "Numeric bulk edit mutated after its target enumeration became stale.");
            Equal(ElementDirtyFlags.None, setup.Element.Dirty, "Numeric bulk edit dirtied the element after stale target enumeration.");
        }

        private static void IdSetPropertyFailsClosedOnEnumerationMutation()
        {
            var setup = NewWall("ID-SET");
            setup.Element.Properties["WidthM"] = "1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            ThrowsFreshness(() => new BulkEditService().SetProperty(
                setup.Project,
                TouchAndYieldId(setup.Project, setup.Element.Id),
                "WidthM",
                "2"));

            Equal(checked(beforeVersion + 1L), setup.Project.ChangeVersion, "Caller id enumeration should be the only project version change.");
            PropertyEquals(setup.Element, "WidthM", "1", "Id-target SetProperty mutated after its target enumeration became stale.");
            Equal(ElementDirtyFlags.None, setup.Element.Dirty, "Id-target SetProperty dirtied the element after stale target enumeration.");
        }

        private static void FamilyAssignmentFailsClosedOnEnumerationMutation()
        {
            var project = new ProjectState("P-BULK-FRESH-FAMILY", "Bulk target freshness");
            var oldFamily = new ProjectFamily("F-OLD", "Old", ElementCategory.ArchitecturalWall);
            oldFamily.Properties["WidthM"] = "1";
            var newFamily = new ProjectFamily("F-NEW", "New", ElementCategory.ArchitecturalWall);
            newFamily.Properties["WidthM"] = "2";
            project.Families.Add(oldFamily);
            project.Families.Add(newFamily);
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, oldFamily.Id, string.Empty, string.Empty);
            element.Properties["WidthM"] = "1";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => new BulkEditService().AssignFamily(
                project,
                TouchAndYieldId(project, element.Id),
                newFamily.Id));

            Equal(checked(beforeVersion + 1L), project.ChangeVersion, "Caller Family target enumeration should be the only project version change.");
            if (!string.Equals(element.FamilyId, oldFamily.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Family assignment changed FamilyId after its target enumeration became stale.");
            PropertyEquals(element, "WidthM", "1", "Family assignment changed inherited properties after stale target enumeration.");
            Equal(ElementDirtyFlags.None, element.Dirty, "Family assignment dirtied the element after stale target enumeration.");
        }

        private static void SideEffectFreeTargetsStillMutateNormally()
        {
            var setup = NewWall("NORMAL");
            setup.Element.Properties["WidthM"] = "1";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(
                setup.Project,
                new[] { setup.Element },
                "WidthM",
                "2");

            if (changed.Count != 1 || !string.Equals(changed[0], setup.Element.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Side-effect-free BulkEdit targets no longer report the normal changed element.");
            Equal(checked(beforeVersion + 1L), setup.Project.ChangeVersion, "Normal BulkEdit must advance the project version exactly once.");
            PropertyEquals(setup.Element, "WidthM", "2", "Normal BulkEdit no longer persists the requested property value.");
        }

        private static IEnumerable<ProjectElement> TouchAndYieldElement(ProjectState project, ProjectElement element)
        {
            project.Touch();
            yield return element;
        }

        private static IEnumerable<string> TouchAndYieldId(ProjectState project, string elementId)
        {
            project.Touch();
            yield return elementId;
        }

        private static Setup NewWall(string suffix)
        {
            var project = new ProjectState("P-BULK-FRESH-" + suffix, "Bulk target freshness");
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall);
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void ThrowsFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("enumerat", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("BulkEdit rejected stale target enumeration with the wrong contract message.", ex);
            }
            throw new InvalidOperationException("Expected BulkEdit to reject a target enumerable that changes ProjectState during enumeration.");
        }

        private static void PropertyEquals(ProjectElement element, string key, string expected, string message)
        {
            if (!element.Properties.TryGetValue(key, out var actual) || !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + "; Actual=" + actual + ".");
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