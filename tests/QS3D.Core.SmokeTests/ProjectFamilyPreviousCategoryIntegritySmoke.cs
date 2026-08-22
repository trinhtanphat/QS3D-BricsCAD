using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyPreviousCategoryIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CrossCategoryPreviousFamilyFailsBeforeMutation();
        }

        private static void CrossCategoryPreviousFamilyFailsBeforeMutation()
        {
            var project = new ProjectState("family-previous-category", "Previous Family category integrity");
            var target = new ProjectFamily("TARGET", "Target wall", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.30";
            var previous = new ProjectFamily("ROOM", "Wrong previous Family", ElementCategory.Room);
            previous.Properties["ThicknessM"] = "0.20";
            project.Families.Add(target);
            project.Families.Add(previous);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties["ThicknessM"] = "0.20";
            element.Properties["InstanceOverride"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeProjectVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;
            var beforeElementUpdated = element.UpdatedUtc;
            var beforeDirty = element.Dirty;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(project, target.Id, new[] { element }));

            Equal(previous.Id, element.FamilyId, "Rejected cross-category previous Family changed FamilyId.");
            Equal("0.20", element.Properties["ThicknessM"], "Rejected cross-category previous Family removed inherited data.");
            Equal("keep", element.Properties["InstanceOverride"], "Rejected cross-category previous Family changed instance override data.");
            if (element.Properties.Count != 2)
                throw new Exception("Rejected cross-category previous Family changed the element property set.");
            if (element.Dirty != beforeDirty || element.UpdatedUtc != beforeElementUpdated)
                throw new Exception("Rejected cross-category previous Family dirtied or timestamped the element.");
            if (project.ChangeVersion != beforeProjectVersion || project.UpdatedUtc != beforeProjectUpdated)
                throw new Exception("Rejected cross-category previous Family touched project persistence state.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
