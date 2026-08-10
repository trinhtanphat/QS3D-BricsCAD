using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyServiceSmoke
    {
        public static void Run()
        {
            PropertyUpdatesPreserveOverrides();
            FamilyAssignmentDropsOldInheritedDefaultsButKeepsOverrides();
            FamilyAssignmentRejectsSpoofedSameIdElement();
            DuplicateRenameDeleteGuards();
        }

        private static void PropertyUpdatesPreserveOverrides()
        {
            var project = new ProjectState("p", "Families");
            var family = ProjectFamilyService.Create(project, "f1", "Tường 200", ElementCategory.ArchitecturalWall);
            ProjectFamilyService.SetProperty(project, family.Id, "ThicknessM", "0.2");
            var inherited = new ProjectElement("i1", ElementCategory.ArchitecturalWall, family.Id, "floor", "zone");
            inherited.Properties["ThicknessM"] = "0.2";
            inherited.Properties["GeneratedSolidHandle"] = "AA";
            inherited.ClearGeneratedGeometryStale();
            inherited.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(inherited);
            var overridden = new ProjectElement("i2", ElementCategory.ArchitecturalWall, family.Id, "floor", "zone");
            overridden.Properties["ThicknessM"] = "0.25";
            project.Elements.Add(overridden);

            var result = ProjectFamilyService.SetProperty(project, family.Id, "ThicknessM", "0.22");
            if (result.InheritedInstancesUpdated != 1 || result.OverridesPreserved != 1) throw new Exception("Family propagation counts failed.");
            if (inherited.Properties["ThicknessM"] != "0.22") throw new Exception("Inherited instance did not update.");
            if (overridden.Properties["ThicknessM"] != "0.25") throw new Exception("Instance override was overwritten.");
            if (!inherited.IsGeneratedSolidStale()) throw new Exception("Inherited geometry-affecting family update must stale generated solid.");

            var remove = ProjectFamilyService.RemoveProperty(project, family.Id, "ThicknessM");
            if (remove.InheritedInstancesUpdated != 1 || remove.OverridesPreserved != 1) throw new Exception("Family property removal counts failed.");
            if (inherited.Properties.ContainsKey("ThicknessM")) throw new Exception("Inherited property copy was not removed.");
            if (overridden.Properties["ThicknessM"] != "0.25") throw new Exception("Override must survive family property removal.");
        }

        private static void FamilyAssignmentDropsOldInheritedDefaultsButKeepsOverrides()
        {
            var project = new ProjectState("p2", "Assign family");
            var oldFamily = ProjectFamilyService.Create(project, "old", "Cột 400", ElementCategory.Column);
            oldFamily.Properties["WidthM"] = "0.4";
            oldFamily.Properties["Material"] = "Bê tông";
            var nextFamily = ProjectFamilyService.Create(project, "next", "Cột 500", ElementCategory.Column);
            nextFamily.Properties["WidthM"] = "0.5";
            nextFamily.Properties["Material"] = "Bê tông C40";
            nextFamily.Properties["DepthM"] = "0.5";
            var element = new ProjectElement("c1", ElementCategory.Column, oldFamily.Id, "floor", "zone");
            element.Properties["WidthM"] = "0.4";
            element.Properties["Material"] = "Bê tông đặc biệt";
            project.Elements.Add(element);

            var changed = ProjectFamilyService.Assign(project, nextFamily.Id, new[] { element, element });
            if (changed != 1 || element.FamilyId != nextFamily.Id) throw new Exception("Family assignment failed.");
            if (element.Properties["WidthM"] != "0.5") throw new Exception("Old inherited WidthM did not switch to new Family default.");
            if (element.Properties["DepthM"] != "0.5") throw new Exception("New Family default was not added.");
            if (element.Properties["Material"] != "Bê tông đặc biệt") throw new Exception("Explicit instance override did not survive Family assignment.");
            if (element.Dirty != ElementDirtyFlags.All) throw new Exception("Family assignment must dirty all semantic outputs.");

            var wrong = ProjectFamilyService.Create(project, "wall", "Tường", ElementCategory.ArchitecturalWall);
            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(project, wrong.Id, new[] { element }));
        }

        private static void FamilyAssignmentRejectsSpoofedSameIdElement()
        {
            var project = new ProjectState("p-spoof", "Family ownership");
            var oldFamily = ProjectFamilyService.Create(project, "old", "Cột cũ", ElementCategory.Column);
            var nextFamily = ProjectFamilyService.Create(project, "next", "Cột mới", ElementCategory.Column);
            var owned = new ProjectElement("same-id", ElementCategory.Column, oldFamily.Id, "floor", "zone");
            project.Elements.Add(owned);
            var spoofed = new ProjectElement("same-id", ElementCategory.Column, oldFamily.Id, "floor", "zone");

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(project, nextFamily.Id, new[] { spoofed }));
            if (owned.FamilyId != oldFamily.Id) throw new Exception("Rejected spoofed Family assignment must not mutate the project-owned element.");
            if (spoofed.FamilyId != oldFamily.Id) throw new Exception("Rejected spoofed Family assignment must not mutate the foreign element.");
        }

        private static void DuplicateRenameDeleteGuards()
        {
            var project = new ProjectState("p3", "Family guards");
            var family = ProjectFamilyService.Create(project, "f1", "Vách Kính A", ElementCategory.GlassWall);
            family.Properties["Material"] = "Kính";
            var clone = ProjectFamilyService.Duplicate(project, family.Id, "f2", "Vách Kính B");
            if (clone.Properties["Material"] != "Kính") throw new Exception("Family duplicate did not copy properties.");
            Throws<InvalidOperationException>(() => ProjectFamilyService.Rename(project, clone.Id, "vách kính a"));
            project.Metadata["ActiveFamilyId"] = family.Id;
            Throws<InvalidOperationException>(() => ProjectFamilyService.Delete(project, family.Id));
            project.Metadata["ActiveFamilyId"] = clone.Id;
            var element = new ProjectElement("g", ElementCategory.GlassWall, family.Id, "floor", "zone");
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => ProjectFamilyService.Delete(project, family.Id));
            project.Elements.Clear();
            if (!ProjectFamilyService.Delete(project, family.Id)) throw new Exception("Unused non-active Family delete failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
