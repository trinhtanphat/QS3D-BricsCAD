using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyAssignmentAtomicitySmoke
    {
        public static void Run()
        {
            ActiveFamilyMutationAdvancesExactlyOnce();
            SetActiveUsesLastAvailableRevision();
            DuplicatePreviousFamilyBlocksWholeAssignmentBatch();
            DuplicatePreviousFamilyBlocksBulkEditBatch();
            DanglingPreviousFamilyBlocksWholeAssignmentBatch();
            DanglingPreviousFamilyBlocksBulkEditBatch();
            SemanticallyIdenticalTargetAssignmentIsNoOp();
            MalformedPreviousFamilyBlocksWholeAssignmentBeforeMutation();
            LazyAssignmentTargetsRejectStaleProjectInput();
            CorruptProjectElementListBlocksPropertyPropagationBeforeMutation();
            CorruptProjectElementListBlocksFamilyDeleteBeforeMutation();
            UndefinedProjectFamilyCategoryFailsClosed();
            UndefinedFamilyDefinitionCategoryFailsClosed();
        }

        private static void ActiveFamilyMutationAdvancesExactlyOnce()
        {
            var project = new ProjectState("family-activation-revision", "Family activation revision");
            var family = new ProjectFamily("F1", "Family", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);

            var beforeSetVersion = project.ChangeVersion;
            ProjectFamilyActivationService.SetActive(project, family.Id);
            if (project.ChangeVersion != beforeSetVersion + 1L)
                throw new Exception("Setting the active Family must advance project ChangeVersion exactly once.");
            Equal(family.Id, project.Metadata["ActiveFamilyId"], "Active Family metadata mismatch after SetActive.");

            var afterSetVersion = project.ChangeVersion;
            var afterSetUpdatedUtc = project.UpdatedUtc;
            ProjectFamilyActivationService.SetActive(project, family.Id);
            if (project.ChangeVersion != afterSetVersion || project.UpdatedUtc != afterSetUpdatedUtc)
                throw new Exception("Setting the already-active Family must remain revision-neutral.");

            project.Families.Clear();
            var beforeClearVersion = project.ChangeVersion;
            ProjectFamilyActivationService.ClearIfMissing(project);
            if (project.ChangeVersion != beforeClearVersion + 1L)
                throw new Exception("Clearing a missing active Family must advance project ChangeVersion exactly once.");
            if (project.Metadata.ContainsKey("ActiveFamilyId"))
                throw new Exception("ClearIfMissing retained stale active Family metadata.");
        }

        private static void SetActiveUsesLastAvailableRevision()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-family-activation-revision-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = new ProjectState("family-activation-ceiling", "Family activation ceiling");
                project.Families.Add(new ProjectFamily("F1", "Family", ElementCategory.ArchitecturalWall));
                var store = new QsdbProjectStore();
                store.Save(project, path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new Exception("Serialized QSDB root was not found for Family activation revision-ceiling fixture.");
                root.SetAttributeValue(
                    "changeVersion",
                    (long.MaxValue - 1L).ToString(System.Globalization.CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var loaded = store.Load(path);
                if (loaded.ChangeVersion != long.MaxValue - 1L)
                    throw new Exception("Family activation revision-ceiling fixture did not restore the persisted ChangeVersion.");
                if (loaded.Metadata.ContainsKey("ActiveFamilyId"))
                    throw new Exception("Family activation revision-ceiling fixture unexpectedly started with active Family metadata.");

                ProjectFamilyActivationService.SetActive(loaded, "F1");

                if (loaded.ChangeVersion != long.MaxValue)
                    throw new Exception("SetActive did not consume exactly the final available project revision.");
                Equal("F1", loaded.Metadata["ActiveFamilyId"], "SetActive did not persist active Family metadata at the revision ceiling.");
                var active = ProjectFamilyActivationService.GetActive(loaded);
                if (active == null || !string.Equals(active.Id, "F1", StringComparison.Ordinal))
                    throw new Exception("SetActive did not resolve the persisted active Family at the revision ceiling.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void DuplicatePreviousFamilyBlocksWholeAssignmentBatch()
        {
            var setup = CreateDuplicatePreviousFamilyProject("family-atomic");
            var beforeUpdated = setup.Project.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(setup.Project, setup.Target.Id, new[] { setup.First, setup.Second }));
            AssertUnchanged(setup, beforeUpdated, "ProjectFamilyService.Assign");
        }

        private static void DuplicatePreviousFamilyBlocksBulkEditBatch()
        {
            var setup = CreateDuplicatePreviousFamilyProject("bulk-family-atomic");
            var beforeUpdated = setup.Project.UpdatedUtc;

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(setup.Project, new[] { setup.First.Id, setup.Second.Id }, setup.Target.Id));
            AssertUnchanged(setup, beforeUpdated, "BulkEditService.AssignFamily");
        }

        private static void DanglingPreviousFamilyBlocksWholeAssignmentBatch()
        {
            var setup = CreateDanglingPreviousFamilyProject("family-dangling-atomic");
            var beforeUpdated = setup.Project.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(setup.Project, setup.Target.Id, new[] { setup.First, setup.Second }));
            AssertDanglingUnchanged(setup, beforeUpdated, "ProjectFamilyService.Assign");
        }

        private static void DanglingPreviousFamilyBlocksBulkEditBatch()
        {
            var setup = CreateDanglingPreviousFamilyProject("bulk-family-dangling-atomic");
            var beforeUpdated = setup.Project.UpdatedUtc;

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(setup.Project, new[] { setup.First.Id, setup.Second.Id }, setup.Target.Id));
            AssertDanglingUnchanged(setup, beforeUpdated, "BulkEditService.AssignFamily");
        }

        private static void SemanticallyIdenticalTargetAssignmentIsNoOp()
        {
            var project = new ProjectState("family-canonical-noop", "Canonical family assignment no-op");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            project.Families.Add(target);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, target.Id, string.Empty, string.Empty);
            element.FamilyId = "  target  ";
            Equal("target", element.FamilyId, "FamilyId setter must canonicalize padded assignment input.");
            SetRawFamilyId(element, "  target  ");
            element.Properties["InstanceOverride"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeProjectVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;
            var beforeElementUpdated = element.UpdatedUtc;
            var beforeDirty = element.Dirty;

            var changed = ProjectFamilyService.Assign(project, " target ", new[] { element });

            if (changed != 0) throw new Exception("Semantically identical target Family assignment must report zero changes.");
            Equal("  target  ", element.FamilyId, "Canonical no-op assignment rewrote the stored FamilyId.");
            Equal("keep", element.Properties["InstanceOverride"], "Canonical no-op assignment changed instance properties.");
            if (element.Properties.Count != 1) throw new Exception("Canonical no-op assignment changed the element property set.");
            if (element.Dirty != beforeDirty || element.UpdatedUtc != beforeElementUpdated)
                throw new Exception("Canonical no-op assignment dirtied or timestamped the element.");
            if (project.ChangeVersion != beforeProjectVersion || project.UpdatedUtc != beforeProjectUpdated)
                throw new Exception("Canonical no-op assignment touched project persistence state.");
        }

        private static void MalformedPreviousFamilyBlocksWholeAssignmentBeforeMutation()
        {
            var project = new ProjectState("family-previous-malformed", "Malformed previous family atomicity");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            var previous = new ProjectFamily("PREV", "Previous", ElementCategory.ArchitecturalWall);
            previous.Properties[" ThicknessM "] = "0.2";
            project.Families.Add(target);
            project.Families.Add(previous);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties[" ThicknessM "] = "0.2";
            element.Properties["InstanceOverride"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeUpdated = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;
            var beforeElementUpdated = element.UpdatedUtc;
            var beforeDirty = element.Dirty;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(project, target.Id, new[] { element }));

            Equal(previous.Id, element.FamilyId, "Rejected previous-Family corruption changed FamilyId.");
            Equal("0.2", element.Properties[" ThicknessM "], "Rejected previous-Family corruption changed inherited property data.");
            Equal("keep", element.Properties["InstanceOverride"], "Rejected previous-Family corruption changed instance override data.");
            if (element.Properties.Count != 2) throw new Exception("Rejected previous-Family corruption changed the element property set.");
            if (element.Dirty != beforeDirty || element.UpdatedUtc != beforeElementUpdated)
                throw new Exception("Rejected previous-Family corruption dirtied or timestamped the element.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdated)
                throw new Exception("Rejected previous-Family corruption touched project persistence state.");
        }

        private static void LazyAssignmentTargetsRejectStaleProjectInput()
        {
            var project = new ProjectState("family-stale-input", "Family stale input");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            var previous = new ProjectFamily("PREV", "Previous", ElementCategory.ArchitecturalWall);
            previous.Properties["ThicknessM"] = "0.2";
            project.Families.Add(target);
            project.Families.Add(previous);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties["ThicknessM"] = "0.2";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeVersion = project.ChangeVersion;
            var beforeElementUpdated = element.UpdatedUtc;
            var beforeDirty = element.Dirty;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(project, target.Id, TouchProjectWhileEnumerating(project, element)));

            if (project.ChangeVersion != beforeVersion + 1)
                throw new Exception("Rejected stale Family assignment must preserve only the caller's deliberate project mutation.");
            Equal(previous.Id, element.FamilyId, "Rejected stale Family assignment changed FamilyId.");
            Equal("0.2", element.Properties["ThicknessM"], "Rejected stale Family assignment changed inherited properties.");
            if (element.Dirty != beforeDirty || element.UpdatedUtc != beforeElementUpdated)
                throw new Exception("Rejected stale Family assignment dirtied or timestamped the element.");
        }

        private static IEnumerable<ProjectElement> TouchProjectWhileEnumerating(ProjectState project, ProjectElement element)
        {
            project.Touch();
            yield return element;
        }

        private static void CorruptProjectElementListBlocksPropertyPropagationBeforeMutation()
        {
            var project = new ProjectState("family-property-atomic", "Family property atomicity");
            var family = new ProjectFamily("F1", "Family", ElementCategory.ArchitecturalWall);
            family.Properties["WidthM"] = "0.2";
            project.Families.Add(family);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => ProjectFamilyService.SetProperty(project, family.Id, "WidthM", "0.3"));
            Equal("0.2", family.Properties["WidthM"], "Family property mutated before corrupt member list validation completed.");
        }

        private static void CorruptProjectElementListBlocksFamilyDeleteBeforeMutation()
        {
            var project = new ProjectState("family-delete-atomic", "Family delete atomicity");
            var family = new ProjectFamily("F1", "Family", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("e1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            var beforeUpdated = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Delete(project, family.Id));

            if (project.Families.Count != 1 || !ReferenceEquals(project.Families[0], family))
                throw new Exception("Family delete mutated catalog membership before corrupt semantic element validation completed.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdated)
                throw new Exception("Family delete touched project persistence state before corrupt semantic element validation completed.");
        }

        private static void UndefinedProjectFamilyCategoryFailsClosed()
        {
            var invalid = (ElementCategory)int.MaxValue;
            Throws<ArgumentOutOfRangeException>(() => new ProjectFamily("BAD", "Invalid", invalid));

            var family = new ProjectFamily("GOOD", "Valid", ElementCategory.Room);
            Throws<ArgumentOutOfRangeException>(() => family.Category = invalid);
            if (family.Category != ElementCategory.Room)
                throw new Exception("Rejected ProjectFamily category assignment mutated the previous category.");
        }

        private static void UndefinedFamilyDefinitionCategoryFailsClosed()
        {
            var invalid = (ElementCategory)int.MaxValue;
            Throws<ArgumentOutOfRangeException>(() => new FamilyDefinition("Invalid", invalid));

            var family = new FamilyDefinition("Valid", ElementCategory.Room);
            Throws<ArgumentOutOfRangeException>(() => family.Category = invalid);
            if (family.Category != ElementCategory.Room)
                throw new Exception("Rejected FamilyDefinition category assignment mutated the previous category.");
        }

        private static Setup CreateDuplicatePreviousFamilyProject(string id)
        {
            var project = new ProjectState(id, "Family atomicity");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            var previous = new ProjectFamily("PREV", "Previous", ElementCategory.ArchitecturalWall);
            previous.Properties["ThicknessM"] = "0.2";
            project.Families.Add(target);
            project.Families.Add(previous);
            project.Families.Add(new ProjectFamily("DUP", "Duplicate A", ElementCategory.ArchitecturalWall));
            project.Families.Add(new ProjectFamily("dup", "Duplicate B", ElementCategory.ArchitecturalWall));

            var first = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            first.Properties["ThicknessM"] = "0.2";
            var second = new ProjectElement("E2", ElementCategory.ArchitecturalWall, "DUP", string.Empty, string.Empty);
            project.Elements.Add(first);
            project.Elements.Add(second);
            return new Setup(project, target, previous, first, second);
        }

        private static Setup CreateDanglingPreviousFamilyProject(string id)
        {
            var project = new ProjectState(id, "Dangling family atomicity");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            var previous = new ProjectFamily("PREV", "Previous", ElementCategory.ArchitecturalWall);
            previous.Properties["ThicknessM"] = "0.2";
            project.Families.Add(target);
            project.Families.Add(previous);

            var first = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            first.Properties["ThicknessM"] = "0.2";
            var second = new ProjectElement("E2", ElementCategory.ArchitecturalWall, "MISSING", string.Empty, string.Empty);
            second.Properties["ThicknessM"] = "legacy";
            project.Elements.Add(first);
            project.Elements.Add(second);
            return new Setup(project, target, previous, first, second);
        }

        private static void AssertUnchanged(Setup setup, DateTime beforeUpdated, string operation)
        {
            Equal(setup.Previous.Id, setup.First.FamilyId, operation + " changed the first element before later duplicate-family validation failed.");
            Equal("0.2", setup.First.Properties["ThicknessM"], operation + " changed inherited properties before whole-batch validation completed.");
            Equal("DUP", setup.Second.FamilyId, operation + " changed the second element despite failed batch.");
            if (setup.Project.UpdatedUtc != beforeUpdated) throw new Exception(operation + " touched project timestamp on a rejected batch.");
        }

        private static void AssertDanglingUnchanged(Setup setup, DateTime beforeUpdated, string operation)
        {
            Equal(setup.Previous.Id, setup.First.FamilyId, operation + " changed the first element before dangling-family validation completed.");
            Equal("0.2", setup.First.Properties["ThicknessM"], operation + " changed inherited properties before dangling-family validation completed.");
            Equal("MISSING", setup.Second.FamilyId, operation + " overwrote a dangling family reference instead of failing closed.");
            Equal("legacy", setup.Second.Properties["ThicknessM"], operation + " changed ambiguous properties on a dangling family reference.");
            if (setup.Project.UpdatedUtc != beforeUpdated) throw new Exception(operation + " touched project timestamp on a rejected dangling-family batch.");
        }

        private static void SetRawFamilyId(ProjectElement element, string value)
        {
            var field = typeof(ProjectElement).GetField("_familyId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement FamilyId backing field was not found for the raw no-op fixture.");
            if (field.FieldType != typeof(string))
                throw new Exception("ProjectElement FamilyId backing field must remain a string.");
            field.SetValue(element, value);
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectFamily target, ProjectFamily previous, ProjectElement first, ProjectElement second)
            {
                Project = project;
                Target = target;
                Previous = previous;
                First = first;
                Second = second;
            }

            public ProjectState Project { get; }
            public ProjectFamily Target { get; }
            public ProjectFamily Previous { get; }
            public ProjectElement First { get; }
            public ProjectElement Second { get; }
        }
    }
}
