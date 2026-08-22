using System;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogSmoke
    {
        public static void Run()
        {
            CustomRoundTripAndUpdate();
            RenamePreservesReferences();
            ReferencedMaterialsAreDiscovered();
            RenamePropagatesReferencesAndStaleState();
            RenameStalesInheritedConsumersButPreservesOverrides();
            ReferencedMaterialCannotBeDeleted();
            RejectsDuplicateBuiltInAndCorruptStorage();
        }

        private static void RenamePreservesReferences()
        {
            var project = new ProjectState("p-rename", "Rename Materials");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-panel", "Panel cũ", "m²", "");
            var family = new ProjectFamily("f-rename", "Vách", ElementCategory.GlassWall);
            family.Properties["Material"] = "Panel cũ";
            project.Families.Add(family);
            var element = new ProjectElement("e-rename", ElementCategory.GlassWall, "f-rename", "floor", "zone");
            element.Properties["CurtainFrameMaterial"] = "Panel cũ";
            project.Elements.Add(element);

            ProjectMaterialCatalog.UpsertCustom(project, "mat-panel", "Panel mới", "m²", "");

            if (family.Properties["Material"] != "Panel mới") throw new Exception("Family material reference was not renamed.");
            if (element.Properties["CurtainFrameMaterial"] != "Panel mới") throw new Exception("Instance material reference was not renamed.");
        }

        private static void CustomRoundTripAndUpdate()
        {
            var project = new ProjectState("p", "Materials");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-stone", "Đá tự nhiên", "m²", "Mặt hoàn thiện");
            var all = ProjectMaterialCatalog.GetAll(project);
            var stone = all.Single(x => x.Id == "mat-stone");
            if (stone.Name != "Đá tự nhiên" || stone.Unit != "m²" || stone.Description != "Mặt hoàn thiện" || stone.IsBuiltIn)
                throw new Exception("Custom material round-trip failed.");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-stone", "Đá tự nhiên", "m²", "Ốp tường");
            if (ProjectMaterialCatalog.GetCustom(project).Single().Description != "Ốp tường") throw new Exception("Custom material update failed.");
            if (!ProjectMaterialCatalog.DeleteCustom(project, "mat-stone")) throw new Exception("Custom material delete failed.");
            if (ProjectMaterialCatalog.GetCustom(project).Count != 0) throw new Exception("Custom material was not deleted.");
            if (project.Metadata.ContainsKey(ProjectMaterialCatalog.MetadataKey)) throw new Exception("Empty custom catalog metadata should be removed.");
        }

        private static void ReferencedMaterialsAreDiscovered()
        {
            var project = new ProjectState("p2", "Referenced Materials");
            var family = new ProjectFamily("f", "Vách Kính", ElementCategory.GlassWall);
            family.Properties["Material"] = "Kính Low-E";
            family.Properties["CurtainFrameMaterial"] = "Nhôm hệ 55";
            project.Families.Add(family);
            var element = new ProjectElement("e", ElementCategory.ArchitecturalWall, "fw", "floor", "zone");
            element.Properties["Material"] = "Gạch AAC";
            project.Elements.Add(element);
            var names = ProjectMaterialCatalog.ReferencedMaterialNames(project);
            if (!names.Contains("Kính Low-E") || !names.Contains("Nhôm hệ 55") || !names.Contains("Gạch AAC"))
                throw new Exception("Referenced material discovery failed.");
        }

        private static void RenamePropagatesReferencesAndStaleState()
        {
            var project = new ProjectState("p4", "Rename Materials");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-glass", "Kính A", "m²", "");
            var family = new ProjectFamily("f-glass", "Vách Kính", ElementCategory.GlassWall);
            family.Properties["Material"] = "Kính A";
            family.Properties["CurtainFrameMaterial"] = "Kính A";
            project.Families.Add(family);
            var element = new ProjectElement("e-glass", ElementCategory.GlassWall, family.Id, "floor", "zone");
            element.Properties["Material"] = "Kính A";
            element.Properties["CurtainFrameMaterial"] = "Kính A";
            element.Properties["GeneratedSolidHandle"] = "AA";
            element.Properties["GeneratedCurtainFrameHandles"] = "BB";
            project.Elements.Add(element);

            ProjectMaterialCatalog.UpsertCustom(project, "mat-glass", "Kính B", "m²", "Đổi tên");

            if (family.Properties["Material"] != "Kính B" || family.Properties["CurtainFrameMaterial"] != "Kính B")
                throw new Exception("Family material references were not renamed.");
            if (element.Properties["Material"] != "Kính B" || element.Properties["CurtainFrameMaterial"] != "Kính B")
                throw new Exception("Instance material references were not renamed.");
            if (!element.IsGeneratedSolidStale() || !element.IsGeneratedCurtainFrameStale())
                throw new Exception("Renaming an instance-referenced material must stale generated geometry outputs.");
        }

        private static void RenameStalesInheritedConsumersButPreservesOverrides()
        {
            var project = new ProjectState("p6", "Inherited Materials");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-inherit", "Vật liệu A", "m²", "");
            var family = new ProjectFamily("f-inherit", "Tường", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Vật liệu A";
            project.Families.Add(family);

            var inherited = new ProjectElement("e-inherit", ElementCategory.ArchitecturalWall, family.Id, "floor", "zone");
            inherited.Properties["GeneratedSolidHandle"] = "CC";
            project.Elements.Add(inherited);

            var overridden = new ProjectElement("e-override", ElementCategory.ArchitecturalWall, family.Id, "floor", "zone");
            overridden.Properties["Material"] = "Gạch";
            overridden.Properties["GeneratedSolidHandle"] = "DD";
            overridden.ClearGeneratedGeometryStale();
            project.Elements.Add(overridden);

            ProjectMaterialCatalog.UpsertCustom(project, "mat-inherit", "Vật liệu B", "m²", "");

            if (family.Properties["Material"] != "Vật liệu B") throw new Exception("Inherited family material was not renamed.");
            if (!inherited.IsGeneratedSolidStale()) throw new Exception("Inherited material consumers must become stale when the Family reference is renamed.");
            if (overridden.Properties["Material"] != "Gạch") throw new Exception("True instance material override must be preserved.");
            if (overridden.IsGeneratedSolidStale()) throw new Exception("Unchanged material override must not become stale solely because the Family material was renamed.");
        }

        private static void ReferencedMaterialCannotBeDeleted()
        {
            var project = new ProjectState("p5", "Referenced Delete");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-aac", "Gạch AAC", "m²", "");
            var family = new ProjectFamily("f-wall", "Tường", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Gạch AAC";
            project.Families.Add(family);
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.DeleteCustom(project, "mat-aac"));
            if (ProjectMaterialCatalog.GetCustom(project).Single().Name != "Gạch AAC")
                throw new Exception("Referenced material must remain in the catalog after rejected deletion.");

            family.Properties["Material"] = "Gạch";
            if (!ProjectMaterialCatalog.DeleteCustom(project, "mat-aac")) throw new Exception("Unreferenced custom material should be deletable.");
        }

        private static void RejectsDuplicateBuiltInAndCorruptStorage()
        {
            var project = new ProjectState("p3", "Bad Materials");
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.UpsertCustom(project, "custom", "Kính", "m²", ""));
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = "not-base64|still-bad|x|y";
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(project));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
