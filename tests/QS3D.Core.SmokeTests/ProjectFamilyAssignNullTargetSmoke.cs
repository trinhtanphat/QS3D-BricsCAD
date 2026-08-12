using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyAssignNullTargetSmoke
    {
        internal static void Run()
        {
            NullTargetFailsBeforeMutation();
            ValidAssignmentStillSucceedsOnce();
        }

        private static void NullTargetFailsBeforeMutation()
        {
            var project = Fixture(out var element);
            var beforeVersion = project.ChangeVersion;
            var beforeDirty = element.Dirty;
            var beforeFamily = element.FamilyId;
            var beforeMaterial = element.Properties["Material"];

            Throws<ArgumentException>(() => ProjectFamilyService.Assign(project, "FAM-NEW", new ProjectElement[] { element, null! }));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeDirty, element.Dirty);
            Equal(beforeFamily, element.FamilyId);
            Equal(beforeMaterial, element.Properties["Material"]);
        }

        private static void ValidAssignmentStillSucceedsOnce()
        {
            var project = Fixture(out var element);
            var beforeVersion = project.ChangeVersion;

            var changed = ProjectFamilyService.Assign(project, "FAM-NEW", new[] { element });

            Equal(1, changed);
            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal("FAM-NEW", element.FamilyId);
            Equal("C40", element.Properties["Material"]);
        }

        private static ProjectState Fixture(out ProjectElement element)
        {
            var project = new ProjectState("family-null-target", "Family Null Target");
            var oldFamily = new ProjectFamily("FAM-OLD", "Old Beam", ElementCategory.Beam);
            oldFamily.Properties["Material"] = "C30";
            var newFamily = new ProjectFamily("FAM-NEW", "New Beam", ElementCategory.Beam);
            newFamily.Properties["Material"] = "C40";
            project.Families.Add(oldFamily);
            project.Families.Add(newFamily);

            element = new ProjectElement("B1", ElementCategory.Beam, oldFamily.Id, string.Empty, string.Empty);
            element.Properties["Material"] = "C30";
            project.Elements.Add(element);
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class ProjectFamilyAssignNullTargetSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyAssignNullTargetSmoke.Run();
    }
}
