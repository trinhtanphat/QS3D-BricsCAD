using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyPropertyMapCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SetPropertyRejectsPaddedExistingKeyBeforeMutation();
            RemovePropertyRejectsPaddedExistingKeyBeforeNoOp();
            SetPropertyRejectsBlankExistingKeyBeforeMutation();
            CanonicalSetPropertyStillWorks();
        }

        private static void SetPropertyRejectsPaddedExistingKeyBeforeMutation()
        {
            var setup = Create();
            InjectLegacyFamilyProperty(setup.Family, " WidthM ", "1.0");
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeUpdatedUtc = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.SetProperty(setup.Project, setup.Family.Id, "WidthM", "2.0"));

            if (setup.Project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException("Rejected SetProperty changed project revision for a malformed Family property map.");
            if (!setup.Family.Properties.TryGetValue(" WidthM ", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal) ||
                setup.Family.Properties.ContainsKey("WidthM"))
                throw new InvalidOperationException("Rejected SetProperty changed malformed Family property-map identity.");
            if (!setup.Element.Properties.TryGetValue("WidthM", out var elementValue) || !string.Equals(elementValue, "1.0", StringComparison.Ordinal) ||
                setup.Element.Dirty != beforeDirty || setup.Element.UpdatedUtc != beforeUpdatedUtc)
                throw new InvalidOperationException("Rejected SetProperty changed inherited instance state.");
        }

        private static void RemovePropertyRejectsPaddedExistingKeyBeforeNoOp()
        {
            var setup = Create();
            InjectLegacyFamilyProperty(setup.Family, " WidthM ", "1.0");
            var beforeVersion = setup.Project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.RemoveProperty(setup.Project, setup.Family.Id, "WidthM"));

            if (setup.Project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException("Rejected RemoveProperty changed project revision for a malformed Family property map.");
            if (!setup.Family.Properties.TryGetValue(" WidthM ", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected RemoveProperty changed malformed Family property-map state.");
        }

        private static void SetPropertyRejectsBlankExistingKeyBeforeMutation()
        {
            var setup = Create();
            InjectLegacyFamilyProperty(setup.Family, string.Empty, "legacy");
            var beforeVersion = setup.Project.ChangeVersion;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.SetProperty(setup.Project, setup.Family.Id, "WidthM", "2.0"));

            if (setup.Project.ChangeVersion != beforeVersion || !setup.Family.Properties.ContainsKey(string.Empty))
                throw new InvalidOperationException("Blank-key Family property rejection mutated project state.");
        }

        private static void CanonicalSetPropertyStillWorks()
        {
            var setup = Create();
            setup.Family.Properties["WidthM"] = "1.0";
            setup.Element.Properties["WidthM"] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            var result = ProjectFamilyService.SetProperty(setup.Project, setup.Family.Id, "WidthM", "2.0");

            if (result.InheritedInstancesUpdated != 1 || result.OverridesPreserved != 0)
                throw new InvalidOperationException("Canonical Family property update reported unexpected propagation counts.");
            if (setup.Project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("Canonical Family property update did not advance project revision exactly once.");
            if (!setup.Family.Properties.TryGetValue("WidthM", out var familyValue) || !string.Equals(familyValue, "2.0", StringComparison.Ordinal) ||
                !setup.Element.Properties.TryGetValue("WidthM", out var elementValue) || !string.Equals(elementValue, "2.0", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical Family property update did not propagate to the inherited instance.");
        }

        private static Setup Create()
        {
            var project = new ProjectState("P-FAMILY-PROPERTY-MAP", "Family property map");
            var family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Setup(project, family, element);
        }

        private static void InjectLegacyFamilyProperty(ProjectFamily family, string key, string value)
        {
            var innerField = family.Properties.GetType().GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Legacy Family fixture could not locate the property backing dictionary.");
            var inner = innerField.GetValue(family.Properties) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Legacy Family fixture property backing dictionary had an unexpected type.");
            inner[key] = value;
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
