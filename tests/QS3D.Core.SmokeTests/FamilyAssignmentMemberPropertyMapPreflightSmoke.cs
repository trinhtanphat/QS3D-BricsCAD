using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyAssignmentMemberPropertyMapPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AssignmentRejectsPaddedPendingMemberKeyBeforeMutation();
            AssignmentRejectsBlankPendingMemberKeyBeforeMutation();
            MalformedAlreadyAssignedMemberRemainsNoOp();
            CanonicalAssignmentStillPreservesInheritanceAndOverrides();
        }

        private static void AssignmentRejectsPaddedPendingMemberKeyBeforeMutation()
        {
            var setup = Create();
            setup.Previous.Properties["WidthM"] = "1.0";
            setup.Target.Properties["WidthM"] = "2.0";
            setup.Element.Properties[" WidthM "] = "1.0";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;
            var beforeFamilyId = setup.Element.FamilyId;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(setup.Project, setup.Target.Id, new[] { setup.Element }));

            if (setup.Project.ChangeVersion != beforeVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Rejected Family assignment changed project persistence state for a malformed pending member map.");
            if (!string.Equals(setup.Element.FamilyId, beforeFamilyId, StringComparison.Ordinal) ||
                setup.Element.Dirty != beforeDirty || setup.Element.UpdatedUtc != beforeElementUpdatedUtc)
                throw new InvalidOperationException("Rejected Family assignment changed pending member state.");
            if (!setup.Element.Properties.TryGetValue(" WidthM ", out var raw) || !string.Equals(raw, "1.0", StringComparison.Ordinal) ||
                setup.Element.Properties.ContainsKey("WidthM"))
                throw new InvalidOperationException("Rejected Family assignment changed malformed member property-map identity.");
        }

        private static void AssignmentRejectsBlankPendingMemberKeyBeforeMutation()
        {
            var setup = Create();
            setup.Target.Properties["WidthM"] = "2.0";
            setup.Element.Properties[string.Empty] = "legacy";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeFamilyId = setup.Element.FamilyId;
            var beforeDirty = setup.Element.Dirty;

            Throws<InvalidOperationException>(() =>
                ProjectFamilyService.Assign(setup.Project, setup.Target.Id, new[] { setup.Element }));

            if (setup.Project.ChangeVersion != beforeVersion ||
                !string.Equals(setup.Element.FamilyId, beforeFamilyId, StringComparison.Ordinal) ||
                setup.Element.Dirty != beforeDirty || !setup.Element.Properties.ContainsKey(string.Empty) ||
                setup.Element.Properties.ContainsKey("WidthM"))
                throw new InvalidOperationException("Blank pending member property-key rejection mutated assignment state.");
        }

        private static void MalformedAlreadyAssignedMemberRemainsNoOp()
        {
            var setup = Create();
            setup.Element.FamilyId = setup.Target.Id;
            setup.Element.Properties[" WidthM "] = "legacy";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdatedUtc = setup.Project.UpdatedUtc;
            var beforeElementUpdatedUtc = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;

            var changed = ProjectFamilyService.Assign(setup.Project, setup.Target.Id, new[] { setup.Element });

            if (changed != 0 || setup.Project.ChangeVersion != beforeVersion || setup.Project.UpdatedUtc != beforeProjectUpdatedUtc)
                throw new InvalidOperationException("Already-assigned malformed member no-op changed project state.");
            if (setup.Element.Dirty != beforeDirty || setup.Element.UpdatedUtc != beforeElementUpdatedUtc ||
                !setup.Element.Properties.ContainsKey(" WidthM "))
                throw new InvalidOperationException("Already-assigned malformed member no-op changed element state.");
        }

        private static void CanonicalAssignmentStillPreservesInheritanceAndOverrides()
        {
            var setup = Create();
            setup.Previous.Properties["Inherited"] = "old-inherited";
            setup.Previous.Properties["RemovedInherited"] = "old-remove";
            setup.Previous.Properties["Override"] = "old-default";
            setup.Target.Properties["Inherited"] = "new-inherited";
            setup.Target.Properties["Added"] = "new-added";
            setup.Target.Properties["Override"] = "new-default";
            setup.Element.Properties["Inherited"] = "old-inherited";
            setup.Element.Properties["RemovedInherited"] = "old-remove";
            setup.Element.Properties["Override"] = "explicit-override";
            setup.Element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = setup.Project.ChangeVersion;

            var changed = ProjectFamilyService.Assign(setup.Project, setup.Target.Id, new[] { setup.Element });

            if (changed != 1)
                throw new InvalidOperationException("Canonical Family assignment did not report exactly one changed member.");
            if (!string.Equals(setup.Element.FamilyId, setup.Target.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical Family assignment did not update FamilyId.");
            if (!setup.Element.Properties.TryGetValue("Inherited", out var inherited) || !string.Equals(inherited, "new-inherited", StringComparison.Ordinal) ||
                setup.Element.Properties.ContainsKey("RemovedInherited") ||
                !setup.Element.Properties.TryGetValue("Override", out var explicitOverride) || !string.Equals(explicitOverride, "explicit-override", StringComparison.Ordinal) ||
                !setup.Element.Properties.TryGetValue("Added", out var added) || !string.Equals(added, "new-added", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical Family assignment changed inheritance/override semantics.");
            if (setup.Project.ChangeVersion != checked(beforeVersion + 1L))
                throw new InvalidOperationException("Canonical Family assignment did not advance project revision exactly once.");
            if (setup.Element.Dirty != ElementDirtyFlags.All)
                throw new InvalidOperationException("Canonical Family assignment did not retain the expected dirty contract.");
        }

        private static Setup Create()
        {
            var project = new ProjectState("P-FAMILY-ASSIGN-MEMBER-MAP", "Family assignment member map preflight");
            var previous = new ProjectFamily("F-OLD", "Old", ElementCategory.ArchitecturalWall);
            var target = new ProjectFamily("F-NEW", "New", ElementCategory.ArchitecturalWall);
            project.Families.Add(previous);
            project.Families.Add(target);
            var element = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            return new Setup(project, previous, target, element);
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
            public Setup(ProjectState project, ProjectFamily previous, ProjectFamily target, ProjectElement element)
            {
                Project = project;
                Previous = previous;
                Target = target;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectFamily Previous { get; }
            public ProjectFamily Target { get; }
            public ProjectElement Element { get; }
        }
    }
}
