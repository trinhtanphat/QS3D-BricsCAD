using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyRenameFailureAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("FAMILY-RENAME-ATOMIC", "Family rename atomicity");
            var family = ProjectFamilyService.Create(project, "FAMILY-1", "Tường 200", ElementCategory.Wall);

            var beforeName = family.Name;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeFamilyCount = project.Families.Count;

            Throws<ArgumentException>(() => ProjectFamilyService.Rename(project, family.Id, "Tường\n300"));

            Equal(beforeName, family.Name, "Family name after rejected rename");
            Equal(beforeVersion, project.ChangeVersion, "project change version after rejected rename");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "project timestamp after rejected rename");
            Equal(beforeFamilyCount, project.Families.Count, "Family count after rejected rename");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectFamilyRenameFailureAtomicitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectFamilyRenameFailureAtomicitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
