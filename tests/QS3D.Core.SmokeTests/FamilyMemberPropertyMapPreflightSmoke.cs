using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyMemberPropertyMapPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SetPropertyRejectsPaddedMemberKeyBeforeMutation();
            RemovePropertyRejectsPaddedMemberKeyBeforeMutation();
            SetPropertyRejectsBlankMemberKeyBeforeMutation();
            CanonicalMemberMapStillUpdatesNormally();
        }

        private static void SetPropertyRejectsPaddedMemberKeyBeforeMutation()
        {
            var setup = Create();
            setup.Family.Properties["WidthM"] = "1.0";
            setup.Element.Properties[" WidthM "] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.SetProperty(setup.Project, setup.Family.Id, "WidthM", "2.0"));

            if (setup.Project.ChangeVersion != beforeVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Rejected SetProperty changed project persistence state for a malformed member property map.");
            if (!setup.Family.Properties.TryGetValue("WidthM", out var familyValue) || !string.Equals(familyValue, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected SetProperty changed the Family default before member-map validation.");
            if (!setup.Element.Properties.TryGetValue(" WidthM ", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal) ||
                setup.Element.Properties.ContainsKey("WidthM"))
                throw new InvalidOperationException("Rejected SetProperty changed malformed member property-map identity.");
            if (setup.Element.Dirty != beforeDirty || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Rejected SetProperty dirtied the malformed Family member.");
        }

        private static void RemovePropertyRejectsPaddedMemberKeyBeforeMutation()
        {
            var setup = Create();
            setup.Family.Properties["WidthM"] = "1.0";
            setup.Element.Properties[" WidthM "] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.RemoveProperty(setup.Project, setup.Family.Id, "WidthM"));

            if (setup.Project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException("Rejected RemoveProperty changed project revision for a malformed member property map.");
            if (!setup.Family.Properties.TryGetValue("WidthM", out var familyValue) || !string.Equals(familyValue, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected RemoveProperty removed the Family default before member-map validation.");
            if (!setup.Element.Properties.TryGetValue(" WidthM ", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected RemoveProperty changed malformed member property-map state.");
            if (setup.Element.Dirty != beforeDirty || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Rejected RemoveProperty dirtied the malformed Family member.");
        }

        private static void SetPropertyRejectsBlankMemberKeyBeforeMutation()
        {
            var setup = Create();
            setup.Family.Properties["WidthM"] = "1.0";
            setup.Element.Properties[string.Empty] = "legacy";
            var beforeVersion = setup.Project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.SetProperty(setup.Project, setup.Family.Id, "WidthM", "2.0"));

            if (setup.Project.ChangeVersion != beforeVersion || !setup.Element.Properties.ContainsKey(string.Empty))
                throw new InvalidOperationException("Blank member property-key rejection mutated project state.");
        }

        private static void CanonicalMemberMapStillUpdatesNormally()
        {
            var setup = Create();
            setup.Family.Properties["WidthM"] = "1.0";
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            var result = ProjectFamilyService.SetProperty(setup.Project, setup.Family.Id, "WidthM", "2.0");

            if (result.InheritedInstancesUpdated != 1 || result.OverridesPreserved != 0)
                throw new InvalidOperationException("Canonical member-map update reported unexpected propagation counts.");
            if (setup.Project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("Canonical member-map update did not advance project revision exactly once.");
            if (!setup.Family.Properties.TryGetValue("WidthM", out var familyValue) || !string.Equals(familyValue, "2.0", StringComparison.Ordinal) ||
                !setup.Element.Properties.TryGetValue("WidthM", out var elementValue) || !string.Equals(elementValue, "2.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical member-map update did not preserve ordinary Family inheritance behavior.");
        }

        private static Setup Create()
        {
            var project = new ProjectState("P-FAMILY-MEMBER-MAP", "Family member map preflight");
            var family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Setup(project, family, element);
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
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
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
