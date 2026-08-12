using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyCanonicalNoOpSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            DirectBulkAssignmentIsCanonicalNoOp();
            SelectionBulkAssignmentReportsCanonicalNoOp();
            GenuineBulkReassignmentStillChanges();
        }

        private static void DirectBulkAssignmentIsCanonicalNoOp()
        {
            var setup = CreateCanonicalNoOpProject("bulk-family-canonical-noop");
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdated = setup.Project.UpdatedUtc;
            var beforeElementUpdated = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;

            var changed = new BulkEditService().AssignFamily(setup.Project, new[] { setup.Element.Id }, " target ");

            if (changed != 0) throw new Exception("Canonical bulk Family assignment must report zero changes.");
            AssertNoOpState(setup, beforeVersion, beforeProjectUpdated, beforeElementUpdated, beforeDirty, "BulkEditService.AssignFamily");
        }

        private static void SelectionBulkAssignmentReportsCanonicalNoOp()
        {
            var setup = CreateCanonicalNoOpProject("selection-family-canonical-noop");
            var beforeVersion = setup.Project.ChangeVersion;
            var beforeProjectUpdated = setup.Project.UpdatedUtc;
            var beforeElementUpdated = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;

            var result = new SemanticSelectionBulkEditService().AssignFamily(setup.Project, new[] { setup.Element.Id }, " TARGET ");

            if (result.SelectedCount != 1 || result.ChangedCount != 0 || result.ChangedElementIds.Count != 0)
                throw new Exception("Selection Family assignment reported a false change for canonical target identity.");
            AssertNoOpState(setup, beforeVersion, beforeProjectUpdated, beforeElementUpdated, beforeDirty, "SemanticSelectionBulkEditService.AssignFamily");
        }

        private static void GenuineBulkReassignmentStillChanges()
        {
            var project = new ProjectState("bulk-family-real-change", "Bulk Family real change");
            var previous = new ProjectFamily("PREV", "Previous", ElementCategory.ArchitecturalWall);
            previous.Properties["ThicknessM"] = "0.2";
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            project.Families.Add(previous);
            project.Families.Add(target);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties["ThicknessM"] = "0.2";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);
            if (changed != 1) throw new Exception("Genuine bulk Family reassignment must report one change.");
            if (!string.Equals(element.FamilyId, target.Id, StringComparison.Ordinal)) throw new Exception("Genuine bulk Family reassignment did not store target identity.");
            if (!string.Equals(element.Properties["ThicknessM"], "0.3", StringComparison.Ordinal)) throw new Exception("Genuine bulk Family reassignment did not propagate target defaults.");
            if (element.Dirty == ElementDirtyFlags.None) throw new Exception("Genuine bulk Family reassignment did not dirty the element.");
            if (project.ChangeVersion <= beforeVersion) throw new Exception("Genuine bulk Family reassignment did not advance project ChangeVersion.");
        }

        private static Setup CreateCanonicalNoOpProject(string id)
        {
            var project = new ProjectState(id, "Canonical bulk Family no-op");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.3";
            project.Families.Add(target);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, target.Id, string.Empty, string.Empty);
            element.FamilyId = "  target  ";
            element.Properties["InstanceOverride"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void AssertNoOpState(
            Setup setup,
            long beforeVersion,
            DateTime beforeProjectUpdated,
            DateTime beforeElementUpdated,
            ElementDirtyFlags beforeDirty,
            string operation)
        {
            if (!string.Equals(setup.Element.FamilyId, "  target  ", StringComparison.Ordinal))
                throw new Exception(operation + " rewrote stored FamilyId during a canonical no-op.");
            if (!string.Equals(setup.Element.Properties["InstanceOverride"], "keep", StringComparison.Ordinal) || setup.Element.Properties.Count != 1)
                throw new Exception(operation + " changed instance properties during a canonical no-op.");
            if (setup.Element.Dirty != beforeDirty || setup.Element.UpdatedUtc != beforeElementUpdated)
                throw new Exception(operation + " dirtied or timestamped the element during a canonical no-op.");
            if (setup.Project.ChangeVersion != beforeVersion || setup.Project.UpdatedUtc != beforeProjectUpdated)
                throw new Exception(operation + " touched project persistence state during a canonical no-op.");
        }

        private sealed class Setup
        {
            internal Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            internal ProjectState Project { get; }
            internal ProjectElement Element { get; }
        }
    }
}