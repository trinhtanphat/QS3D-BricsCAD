using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationSmoke
    {
        public static void Run()
        {
            var project = new ProjectState("p", "Active family");
            var family = ProjectFamilyService.Create(project, "f1", "Tường 200", ElementCategory.ArchitecturalWall);
            if (ProjectFamilyActivationService.GetActive(project) != null) throw new Exception("Active Family should start empty.");
            ProjectFamilyActivationService.SetActive(project, family.Id);
            if (ProjectFamilyActivationService.GetActive(project)?.Id != family.Id) throw new Exception("Active Family was not resolved.");
            project.Families.Remove(family);
            ProjectFamilyActivationService.ClearIfMissing(project);
            if (project.Metadata.ContainsKey("ActiveFamilyId")) throw new Exception("Missing active Family metadata was not cleared.");
            Throws<InvalidOperationException>(() => ProjectFamilyActivationService.SetActive(project, family.Id));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
