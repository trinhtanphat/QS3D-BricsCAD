using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyPropertyCanonicalitySmoke
    {
        public static void Run()
        {
            MalformedTargetDefaultsFailBeforeMutation();
            MalformedPreviousDefaultsFailBeforeMutation();
            OverBoundTargetValueFailsBeforeMutation();
            ValidAssignmentPreservesInheritanceAndOverrides();
        }

        private static void MalformedTargetDefaultsFailBeforeMutation()
        {
            var project = NewProject(out var previous, out var target, out var element);
            InjectLegacyFamilyProperty(target, " ThicknessM ", "0.30");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeElementUpdatedUtc = element.UpdatedUtc;
            var beforeFamilyId = element.FamilyId;
            var beforeMaterial = element.Properties["Material"];
            var beforeDirty = element.Dirty;

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id));

            Equal(beforeFamilyId, element.FamilyId, "Malformed target Family changed element FamilyId.");
            Equal(beforeMaterial, element.Properties["Material"], "Malformed target Family changed instance properties.");
            Require(!element.Properties.ContainsKey(" ThicknessM "), "Malformed target Family property leaked into the instance.");
            Require(element.Dirty == beforeDirty, "Malformed target Family dirtied the instance before rejection.");
            Require(element.UpdatedUtc == beforeElementUpdatedUtc, "Malformed target Family changed element UpdatedUtc before rejection.");
            Require(project.ChangeVersion == beforeVersion, "Malformed target Family changed project ChangeVersion before rejection.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Malformed target Family changed project UpdatedUtc before rejection.");
            Require(ReferenceEquals(project.FindFamily(previous.Id), previous), "Malformed target rejection changed previous Family ownership.");
        }

        private static void MalformedPreviousDefaultsFailBeforeMutation()
        {
            var project = NewProject(out var previous, out var target, out var element);
            InjectLegacyFamilyProperty(previous, " OldDefault ", "legacy");
            element.Properties[" OldDefault "] = "legacy";
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeElementUpdatedUtc = element.UpdatedUtc;
            var beforeFamilyId = element.FamilyId;
            var beforeDirty = element.Dirty;
            var beforePropertyCount = element.Properties.Count;

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id));

            Equal(beforeFamilyId, element.FamilyId, "Malformed previous Family changed element FamilyId.");
            Require(element.Properties.Count == beforePropertyCount && element.Properties[" OldDefault "] == "legacy",
                "Malformed previous Family changed instance properties before rejection.");
            Require(element.Dirty == beforeDirty, "Malformed previous Family dirtied the instance before rejection.");
            Require(element.UpdatedUtc == beforeElementUpdatedUtc, "Malformed previous Family changed element UpdatedUtc before rejection.");
            Require(project.ChangeVersion == beforeVersion, "Malformed previous Family changed project ChangeVersion before rejection.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Malformed previous Family changed project UpdatedUtc before rejection.");
        }

        private static void OverBoundTargetValueFailsBeforeMutation()
        {
            var project = NewProject(out _, out var target, out var element);
            target.Properties["Description"] = new string('x', 1001);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeFamilyId = element.FamilyId;
            var beforeDirty = element.Dirty;

            Throws<ArgumentException>(() => new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id));

            Equal(beforeFamilyId, element.FamilyId, "Over-bound target Family value changed element FamilyId.");
            Require(!element.Properties.ContainsKey("Description"), "Over-bound target Family value leaked into the instance.");
            Require(element.Dirty == beforeDirty, "Over-bound target Family value dirtied the instance before rejection.");
            Require(project.ChangeVersion == beforeVersion, "Over-bound target Family value changed project ChangeVersion before rejection.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Over-bound target Family value changed project UpdatedUtc before rejection.");
        }

        private static void ValidAssignmentPreservesInheritanceAndOverrides()
        {
            var project = new ProjectState("bulk-family-property-valid", "Bulk Family property valid");
            var previous = new ProjectFamily("F-OLD", "Old", ElementCategory.ArchitecturalWall);
            previous.Properties["Inherited"] = "old-inherited";
            previous.Properties["RemovedInherited"] = "old-remove";
            previous.Properties["Override"] = "old-default";
            var target = new ProjectFamily("F-NEW", "New", ElementCategory.ArchitecturalWall);
            target.Properties["Inherited"] = "new-inherited";
            target.Properties["Added"] = "new-added";
            target.Properties["Override"] = "new-default";
            project.Families.Add(previous);
            project.Families.Add(target);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties["Inherited"] = "old-inherited";
            element.Properties["RemovedInherited"] = "old-remove";
            element.Properties["Override"] = "explicit-override";
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);

            Require(changed == 1, "Valid bulk Family assignment did not report exactly one changed element.");
            Equal(target.Id, element.FamilyId, "Valid bulk Family assignment did not update FamilyId.");
            Equal("new-inherited", element.Properties["Inherited"], "Inherited previous default did not adopt target default.");
            Require(!element.Properties.ContainsKey("RemovedInherited"), "Inherited previous default absent from target was not removed.");
            Equal("explicit-override", element.Properties["Override"], "Explicit instance override was not preserved.");
            Equal("new-added", element.Properties["Added"], "Target default absent from instance was not applied.");
            var required = ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry;
            Require((element.Dirty & required) == required, "Valid bulk Family assignment lost required dirty flags.");
            Require(project.ChangeVersion == checked(beforeVersion + 1L), "Valid bulk Family assignment did not touch project exactly once.");
        }

        private static ProjectState NewProject(out ProjectFamily previous, out ProjectFamily target, out ProjectElement element)
        {
            var project = new ProjectState("bulk-family-property-invalid", "Bulk Family property invalid");
            previous = new ProjectFamily("F-OLD", "Old", ElementCategory.ArchitecturalWall);
            previous.Properties["Material"] = "Old";
            target = new ProjectFamily("F-NEW", "New", ElementCategory.ArchitecturalWall);
            target.Properties["Material"] = "New";
            project.Families.Add(previous);
            project.Families.Add(target);
            element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties["Material"] = "Old";
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            return project;
        }

        private static void InjectLegacyFamilyProperty(ProjectFamily family, string key, string value)
        {
            var innerField = family.Properties.GetType().GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("Legacy Family fixture could not locate the property backing dictionary.");
            var inner = innerField.GetValue(family.Properties) as Dictionary<string, string>
                ?? throw new Exception("Legacy Family fixture property backing dictionary had an unexpected type.");
            inner[key] = value;
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected='" + expected + "', actual='" + actual + "'.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
