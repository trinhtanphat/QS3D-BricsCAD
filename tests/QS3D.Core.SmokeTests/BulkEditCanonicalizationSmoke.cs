using System;
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

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
