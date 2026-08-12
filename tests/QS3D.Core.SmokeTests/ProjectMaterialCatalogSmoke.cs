using System;
using System.Linq;
using System.Text;
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
            RenameStalesInheritedConsumerWithPaddedFamilyId();
            RenameRejectsCorruptReferenceGraphBeforeMutation();
            ReferencedMaterialCannotBeDeleted();
            RejectsDuplicateBuiltInAndCorruptStorage();
            RejectsStoredBuiltInShadowing();
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

        private static void RenameStalesInheritedConsumerWithPaddedFamilyId()
        {
            var project = new ProjectState("p-padded-family", "Padded inherited material FamilyId");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-padded", "Vật liệu cũ", "m²", "");
            var family = new ProjectFamily("f-padded", "Tường padded", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Vật liệu cũ";
            project.Families.Add(family);

            var inherited = new ProjectElement("e-padded", ElementCategory.ArchitecturalWall, family.Id, "floor", "zone");
            inherited.FamilyId = "  " + family.Id + "  ";
            inherited.Properties["GeneratedSolidHandle"] = "EE";
            inherited.ClearGeneratedGeometryStale();
            inherited.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(inherited);

            ProjectMaterialCatalog.UpsertCustom(project, "mat-padded", "Vật liệu mới", "m²", "");

            if (family.Properties["Material"] != "Vật liệu mới") throw new Exception("Family material reference was not renamed for the padded FamilyId regression.");
            if (!inherited.IsGeneratedSolidStale()) throw new Exception("Padded but semantically identical FamilyId must still stale inherited material consumers.");
            if ((inherited.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity)) != (ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity))
                throw new Exception("Padded FamilyId inherited consumer must be dirtied for Properties and Quantity.");
            if (inherited.FamilyId != "  " + family.Id + "  ") throw new Exception("Material rename must not rewrite the stored FamilyId while canonicalizing lookup identity.");
        }

        private static void RenameRejectsCorruptReferenceGraphBeforeMutation()
        {
            var project = new ProjectState("p-atomic", "Material atomicity");
            ProjectMaterialCatalog.UpsertCustom(project, "mat-atomic", "Vật liệu cũ", "m²", "");
            var family = new ProjectFamily("f-atomic", "Tường", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Vật liệu cũ";
            project.Families.Add(family);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.UpsertCustom(project, "mat-atomic", "Vật liệu mới", "m²", ""));
            if (ProjectMaterialCatalog.GetCustom(project).Single().Name != "Vật liệu cũ")
                throw new Exception("Rejected material rename must not partially rewrite catalog metadata.");
            if (family.Properties["Material"] != "Vật liệu cũ")
                throw new Exception("Rejected material rename must not partially rewrite Family references.");

            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.ReferencedMaterialNames(project));
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.DeleteCustom(project, "mat-atomic"));
            if (ProjectMaterialCatalog.GetCustom(project).Single().Name != "Vật liệu cũ")
                throw new Exception("Rejected material delete must preserve catalog metadata.");
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

        private static void RejectsStoredBuiltInShadowing()
        {
            var project = new ProjectState("p-shadow", "Stored material shadowing");
            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("builtin-concrete", "Bê tông giả", "m³", "legacy/tampered id collision");
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetCustom(project));

            project.Metadata[ProjectMaterialCatalog.MetadataKey] = Record("custom-shadow", "Bê tông", "m³", "legacy/tampered name collision");
            Throws<InvalidOperationException>(() => ProjectMaterialCatalog.GetAll(project));
        }

        private static string Record(string id, string name, string unit, string description)
        {
            return string.Join("|", Encode(id), Encode(name), Encode(unit), Encode(description));
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}