using System;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationRevisionSmoke
    {
        public static void Run()
        {
            DanglingActiveFamilyRepairTouchesOnce();
            BlankActiveFamilyRepairTouchesOnce();
            ValidActiveFamilyIsNoOp();
            MissingActiveFamilyKeyIsNoOp();
            NearRevisionCeilingRepairsOnce();
            RevisionCeilingFailsBeforeRemoval();
        }

        private static void DanglingActiveFamilyRepairTouchesOnce()
        {
            var project = NewProject("active-family-dangling");
            project.Metadata["ActiveFamilyId"] = "missing-family";
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            False(project.Metadata.ContainsKey("ActiveFamilyId"), "Dangling ActiveFamilyId was not removed.");
            Equal(before + 1L, project.ChangeVersion, "Dangling ActiveFamilyId repair must advance ChangeVersion exactly once.");
        }

        private static void BlankActiveFamilyRepairTouchesOnce()
        {
            var project = NewProject("active-family-blank");
            project.Metadata["ActiveFamilyId"] = "   ";
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            False(project.Metadata.ContainsKey("ActiveFamilyId"), "Blank ActiveFamilyId was not removed.");
            Equal(before + 1L, project.ChangeVersion, "Blank ActiveFamilyId repair must advance ChangeVersion exactly once.");
        }

        private static void ValidActiveFamilyIsNoOp()
        {
            var project = NewProject("active-family-valid");
            var family = new ProjectFamily("family-ok", "D300x500", ElementCategory.Beam);
            project.Families.Add(family);
            ProjectFamilyActivationService.SetActive(project, family.Id);
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(before, project.ChangeVersion, "Valid ActiveFamilyId repair path must be a no-op.");
            Equal(family.Id, project.Metadata["ActiveFamilyId"], "Valid ActiveFamilyId was changed unexpectedly.");
        }

        private static void MissingActiveFamilyKeyIsNoOp()
        {
            var project = NewProject("active-family-absent");
            var before = project.ChangeVersion;

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(before, project.ChangeVersion, "Missing ActiveFamilyId key must be a no-op.");
        }

        private static void NearRevisionCeilingRepairsOnce()
        {
            var project = NewProject("active-family-near-ceiling");
            project.Metadata["ActiveFamilyId"] = "missing-family";
            SetChangeVersion(project, long.MaxValue - 1L);

            ProjectFamilyActivationService.ClearIfMissing(project);

            Equal(long.MaxValue, project.ChangeVersion, "Near-ceiling repair must consume exactly the final supported revision.");
            False(project.Metadata.ContainsKey("ActiveFamilyId"), "Near-ceiling repair failed to remove the dangling ActiveFamilyId.");
        }

        private static void RevisionCeilingFailsBeforeRemoval()
        {
            var project = NewProject("active-family-ceiling");
            project.Metadata["ActiveFamilyId"] = "missing-family";
            SetChangeVersion(project, long.MaxValue);

            Throws<InvalidOperationException>(() => ProjectFamilyActivationService.ClearIfMissing(project));

            Equal(long.MaxValue, project.ChangeVersion, "Revision-ceiling failure changed ChangeVersion.");
            True(project.Metadata.TryGetValue("ActiveFamilyId", out var value), "Revision-ceiling failure partially removed ActiveFamilyId.");
            Equal("missing-family", value, "Revision-ceiling failure changed ActiveFamilyId.");
        }

        private static ProjectState NewProject(string id) => new ProjectState(id, id);

        private static void SetChangeVersion(ProjectState project, long value)
        {
            var property = typeof(ProjectState).GetProperty(nameof(ProjectState.ChangeVersion), BindingFlags.Instance | BindingFlags.Public)
                ?? throw new Exception("ProjectState.ChangeVersion property was not found.");
            var setter = property.GetSetMethod(true)
                ?? throw new Exception("ProjectState.ChangeVersion private setter was not found.");
            setter.Invoke(project, new object[] { value });
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void False(bool condition, string message) => True(!condition, message);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
