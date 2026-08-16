using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditCanonicalizationSmoke
    {
        public static void Run()
        {
            SetPropertyUsesCanonicalKeyAndGeometryDirtyPolicy();
            MultiplyNumericPropertyUsesCanonicalKey();
            CorruptProjectFailsBeforeBulkMutation();
            ObjectBasedBulkEditsRejectNullTargets();
            IdBasedBulkEditsRejectIncompleteTargetSets();
            IdBasedBulkEditsRejectNonCanonicalTargetIds();
            FamilyAssignmentRejectsIncompatibleBatch();
        }

        private static void SetPropertyUsesCanonicalKeyAndGeometryDirtyPolicy()
        {
            var project = new ProjectState("P1", "Bulk");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);

            var changed = new BulkEditService().SetProperty(project, new[] { wall }, " WidthM ", "0.25");
            if (changed.Count != 1 || changed[0] != "W1") throw new Exception("Bulk set must report the canonical owned element once.");
            if (!wall.Properties.TryGetValue("WidthM", out var width) || width != "0.25") throw new Exception("Bulk set must write the canonical trimmed property key.");
            if (wall.Properties.Keys.Any(key => key != key.Trim())) throw new Exception("Bulk set must not create padded property keys.");
            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0) throw new Exception("Canonical geometry property bulk set must mark generated geometry dirty.");
        }

        private static void MultiplyNumericPropertyUsesCanonicalKey()
        {
            var project = new ProjectState("P1", "Bulk");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);

            var changed = new BulkEditService().MultiplyNumericProperty(project, new[] { wall }, " WidthM ", 2d);
            if (changed.Count != 1 || changed[0] != "W1") throw new Exception("Bulk multiply must report the canonical owned element once.");
            if (!wall.Properties.TryGetValue("WidthM", out var width) || width != "0.4") throw new Exception("Bulk multiply must read/write the canonical trimmed property key.");
            if (wall.Properties.Keys.Any(key => key != key.Trim())) throw new Exception("Bulk multiply must not create padded property keys.");
            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0) throw new Exception("Canonical geometry property bulk multiply must mark generated geometry dirty.");
        }

        private static void CorruptProjectFailsBeforeBulkMutation()
        {
            var project = new ProjectState("P-CORRUPT", "Bulk atomicity");
            var familyA = new ProjectFamily("F-A", "Tường A", ElementCategory.ArchitecturalWall);
            var familyB = new ProjectFamily("F-B", "Tường B", ElementCategory.ArchitecturalWall);
            project.Families.Add(familyA);
            project.Families.Add(familyB);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, familyA.Id, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => new BulkEditService().SetProperty(project, new[] { wall }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2") throw new Exception("Rejected bulk set must not partially mutate a target.");

            Throws<InvalidOperationException>(() => new BulkEditService().MultiplyNumericProperty(project, new[] { wall }, "WidthM", 2d));
            if (wall.Properties["WidthM"] != "0.2") throw new Exception("Rejected bulk multiply must not partially mutate a target.");

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(project, new[] { wall.Id }, familyB.Id));
            if (wall.FamilyId != familyA.Id) throw new Exception("Rejected bulk family assignment must not partially mutate a target.");
        }

        private static void ObjectBasedBulkEditsRejectNullTargets()
        {
            var project = new ProjectState("P-OBJECT-NULL", "Bulk object target atomicity");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            var service = new BulkEditService();
            var version = project.ChangeVersion;
            var dirty = wall.Dirty;
            var targets = new ProjectElement[] { wall, null! };

            Throws<InvalidOperationException>(() => service.SetProperty(project, targets, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || wall.Dirty != dirty || project.ChangeVersion != version)
                throw new Exception("Null object target must reject bulk set before any semantic mutation.");

            Throws<InvalidOperationException>(() => service.MultiplyNumericProperty(project, targets, "WidthM", 2d));
            if (wall.Properties["WidthM"] != "0.2" || wall.Dirty != dirty || project.ChangeVersion != version)
                throw new Exception("Null object target must reject bulk multiply before any semantic mutation.");
        }

        private static void IdBasedBulkEditsRejectIncompleteTargetSets()
        {
            var project = new ProjectState("P-ID", "Bulk target identity");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            var service = new BulkEditService();
            var version = project.ChangeVersion;

            Throws<KeyNotFoundException>(() => service.SetProperty(project, new[] { "W1", "W404" }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || project.ChangeVersion != version)
                throw new Exception("Missing bulk target must reject the whole batch before mutation.");

            Throws<ArgumentException>(() => service.SetProperty(project, new[] { "W1", "   " }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || project.ChangeVersion != version)
                throw new Exception("Blank bulk target must reject the whole batch before mutation.");

            Throws<InvalidOperationException>(() => service.SetProperty(project, new[] { "W1", "w1" }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || project.ChangeVersion != version)
                throw new Exception("Duplicate bulk target must reject the whole batch before mutation.");
        }

        private static void IdBasedBulkEditsRejectNonCanonicalTargetIds()
        {
            var project = new ProjectState("P-ID-CANONICAL", "Bulk target canonicality");
            var familyA = new ProjectFamily("F-A", "Wall A", ElementCategory.ArchitecturalWall);
            var familyB = new ProjectFamily("F-B", "Wall B", ElementCategory.ArchitecturalWall);
            project.Families.Add(familyA);
            project.Families.Add(familyB);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, familyA.Id, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            var service = new BulkEditService();
            var initialVersion = project.ChangeVersion;
            var initialDirty = wall.Dirty;

            foreach (var padded in new[] { " W1", "W1 ", " W1 ", "\tW1", "W1\t" })
            {
                Throws<ArgumentException>(() => service.SetProperty(project, new[] { padded }, "WidthM", "0.25"));
                if (wall.Properties["WidthM"] != "0.2" || wall.FamilyId != familyA.Id || wall.Dirty != initialDirty || project.ChangeVersion != initialVersion)
                    throw new Exception("Non-canonical bulk target id must reject property edit before any semantic mutation: " + padded);

                Throws<ArgumentException>(() => service.AssignFamily(project, new[] { padded }, familyB.Id));
                if (wall.Properties["WidthM"] != "0.2" || wall.FamilyId != familyA.Id || wall.Dirty != initialDirty || project.ChangeVersion != initialVersion)
                    throw new Exception("Non-canonical bulk target id must reject family assignment before any semantic mutation: " + padded);
            }

            var changed = service.SetProperty(project, new[] { "w1" }, "WidthM", "0.25");
            if (changed != 1 || wall.Properties["WidthM"] != "0.25")
                throw new Exception("Canonical case-insensitive target identity must remain supported for bulk property edits.");

            var assigned = service.AssignFamily(project, new[] { "w1" }, familyB.Id);
            if (assigned != 1 || wall.FamilyId != familyB.Id)
                throw new Exception("Canonical case-insensitive target identity must remain supported for bulk family assignment.");
        }

        private static void FamilyAssignmentRejectsIncompatibleBatch()
        {
            var project = new ProjectState("P-CATEGORY", "Bulk family category atomicity");
            var wallA = new ProjectFamily("FW-A", "Wall A", ElementCategory.ArchitecturalWall);
            var wallB = new ProjectFamily("FW-B", "Wall B", ElementCategory.ArchitecturalWall);
            var columnFamily = new ProjectFamily("FC", "Column", ElementCategory.Column);
            project.Families.Add(wallA);
            project.Families.Add(wallB);
            project.Families.Add(columnFamily);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, wallA.Id, string.Empty, string.Empty);
            var column = new ProjectElement("C1", ElementCategory.Column, columnFamily.Id, string.Empty, string.Empty);
            project.Elements.Add(wall);
            project.Elements.Add(column);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(project, new[] { wall.Id, column.Id }, wallB.Id));
            if (wall.FamilyId != wallA.Id || column.FamilyId != columnFamily.Id || project.ChangeVersion != version)
                throw new Exception("Incompatible family assignment must reject the whole batch without silently skipping targets.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}