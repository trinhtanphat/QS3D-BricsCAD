using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditIdTargetStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            IdSetPropertyRejectsSameIdReplacement();
            IdAssignFamilyRejectsSameIdReplacement();
            StableIdSetPropertyStillApplies();
        }

        private static void IdSetPropertyRejectsSameIdReplacement()
        {
            var project = Fixture(out var original);
            original.Properties["Note"] = "original";
            var beforeVersion = project.ChangeVersion;
            var index = project.Elements.IndexOf(original);

            IEnumerable<string> Targets()
            {
                var replacement = Beam("B1", "F1");
                replacement.Properties["Note"] = "replacement";
                project.Elements[index] = replacement;
                yield return "B1";
            }

            ThrowsInvalidOperation(() => new BulkEditService().SetProperty(project, Targets(), "Note", "edited"));
            Equal(beforeVersion, project.ChangeVersion, "rejected ID property edit revision");
            Equal("replacement", project.FindElement("B1")!.Properties["Note"], "replacement property after rejection");
            Equal("original", original.Properties["Note"], "detached original property after rejection");
        }

        private static void IdAssignFamilyRejectsSameIdReplacement()
        {
            var project = Fixture(out var original);
            var beforeVersion = project.ChangeVersion;
            var index = project.Elements.IndexOf(original);

            IEnumerable<string> Targets()
            {
                project.Elements[index] = Beam("B1", "F1");
                yield return "B1";
            }

            ThrowsInvalidOperation(() => new BulkEditService().AssignFamily(project, Targets(), "F2"));
            Equal(beforeVersion, project.ChangeVersion, "rejected ID Family assignment revision");
            Equal("F1", project.FindElement("B1")!.FamilyId, "replacement Family after rejection");
            Equal("F1", original.FamilyId, "detached original Family after rejection");
        }

        private static void StableIdSetPropertyStillApplies()
        {
            var project = Fixture(out var element);
            element.Properties["Note"] = "before";
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(project, new[] { "B1" }, "Note", "after");

            Equal(1, changed, "stable ID property edit changed count");
            Equal("after", element.Properties["Note"], "stable ID property edit value");
            if (project.ChangeVersion <= beforeVersion)
                throw new InvalidOperationException("Stable ID property edit must advance project revision.");
        }

        private static ProjectState Fixture(out ProjectElement element)
        {
            var project = new ProjectState("P-BULK-ID-STRUCTURAL", "Bulk ID target structural freshness");
            project.Families.Add(new ProjectFamily("F1", "Beam 1", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("F2", "Beam 2", ElementCategory.Beam));
            element = Beam("B1", "F1");
            project.Elements.Add(element);
            return project;
        }

        private static ProjectElement Beam(string id, string familyId) =>
            new ProjectElement(id, ElementCategory.Beam, familyId, string.Empty, string.Empty);

        private static void ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Expected bulk ID-target structural freshness rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
