using System;
using System.Reflection;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnershipIndexSmoke
    {
        public static void Run()
        {
            ResolvesCaseInsensitiveTrimmedHandle();
            SameLogicalAliasOnSameOwnerIsAllowed();
            DifferentOwnersFailClosed();
            DuplicateIdsFailClosedAtBuild();
            NullElementFailsClosedAtBuild();
            CorruptBlankIdFailsClosedAtBuild();
            DifferentLogicalSlotsOnSameOwnerFailClosed();
            BuiltIndexIsMembershipSnapshot();
        }

        private static void ResolvesCaseInsensitiveTrimmedHandle()
        {
            var project = NewProject();
            var owner = NewElement("E-1");
            owner.Properties["GeneratedSolidHandle"] = "AA01";
            project.Elements.Add(owner);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            if (!index.TryFindOwner(" aa01 ", out var actual, out var slot))
                throw new Exception("Generated ownership index did not resolve a case-insensitive trimmed handle.");
            if (!ReferenceEquals(actual, owner)) throw new Exception("Generated ownership index resolved the wrong owner.");
            if (!string.Equals(slot, "GeneratedSolidHandle", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Generated ownership index resolved the wrong owner slot.");
        }

        private static void SameLogicalAliasOnSameOwnerIsAllowed()
        {
            var project = NewProject();
            var owner = NewElement("E-1");
            owner.Properties["GeneratedSolidHandle"] = "AA01";
            owner.Properties["PhysicalOpeningCutSolidHandle"] = "AA01";
            project.Elements.Add(owner);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            if (!index.TryFindOwner("AA01", out var actual, out _))
                throw new Exception("Same-owner logical host-solid aliases should resolve.");
            if (!ReferenceEquals(actual, owner)) throw new Exception("Logical alias lookup resolved the wrong owner.");
        }

        private static void DifferentOwnersFailClosed()
        {
            var project = NewProject();
            var first = NewElement("E-1");
            var second = NewElement("E-2");
            first.Properties["GeneratedSolidHandle"] = "AA01";
            second.Properties["GeneratedSolidHandle"] = "AA01";
            project.Elements.Add(first);
            project.Elements.Add(second);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            ExpectInvalid(() => index.TryFindOwner("AA01", out _, out _), "different semantic owners");
        }

        private static void DuplicateIdsFailClosedAtBuild()
        {
            var project = NewProject();
            var first = NewElement(" E-1 ");
            var second = NewElement("e-1");
            first.Properties["GeneratedSolidHandle"] = "AA01";
            second.Properties["GeneratedSolidHandle"] = "AA01";
            project.Elements.Add(first);
            project.Elements.Add(second);

            ExpectInvalid(() => GeneratedHandleOwnershipIndex.Build(project), "trimmed case-insensitive duplicate semantic element IDs");
        }

        private static void NullElementFailsClosedAtBuild()
        {
            var project = NewProject();
            project.Elements.Add(null!);
            ExpectInvalid(() => GeneratedHandleOwnershipIndex.Build(project), "a null semantic element");
        }

        private static void CorruptBlankIdFailsClosedAtBuild()
        {
            var project = NewProject();
            var owner = NewElement("E-1");
            var idField = typeof(ProjectElement).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (idField == null) throw new Exception("Could not locate ProjectElement.Id backing field for corruption smoke.");
            idField.SetValue(owner, "   ");
            project.Elements.Add(owner);

            ExpectInvalid(() => GeneratedHandleOwnershipIndex.Build(project), "a corrupt blank semantic element ID");
        }

        private static void DifferentLogicalSlotsOnSameOwnerFailClosed()
        {
            var project = NewProject();
            var owner = NewElement("E-1");
            owner.Properties["GeneratedSolidHandle"] = "AA01";
            owner.Properties["GeneratedRebarHandles"] = "AA01";
            project.Elements.Add(owner);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            ExpectInvalid(() => index.TryFindOwner("AA01", out _, out _), "different logical owner slots");
        }

        private static void BuiltIndexIsMembershipSnapshot()
        {
            var project = NewProject();
            var owner = NewElement("E-1");
            owner.Properties["GeneratedSolidHandle"] = "AA01";
            project.Elements.Add(owner);

            var index = GeneratedHandleOwnershipIndex.Build(project);
            owner.Properties["GeneratedSolidHandle"] = "BB02";

            if (!index.TryFindOwner("AA01", out var actual, out _) || !ReferenceEquals(actual, owner))
                throw new Exception("Built ownership index should retain operation-snapshot membership.");
            if (index.TryFindOwner("BB02", out _, out _))
                throw new Exception("Built ownership index must not silently absorb later project mutations.");
        }

        private static ProjectState NewProject() => new ProjectState("P-OWN-INDEX", "Ownership Index Smoke");

        private static ProjectElement NewElement(string id) => new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);

        private static void ExpectInvalid(Action action, string scenario)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception("Generated ownership index must fail closed for " + scenario + ".");
        }
    }
}
