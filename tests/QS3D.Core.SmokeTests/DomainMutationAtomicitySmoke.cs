using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class DomainMutationAtomicitySmoke
    {
        public static void Run()
        {
            FamilyPropertyOverflowDoesNotCommit();
            FloorAssignmentOverflowDoesNotCommit();
            ZoneAssignmentOverflowDoesNotCommit();
            ActiveFamilyOverflowDoesNotCommit();
            GridRenumberOverflowDoesNotCommit();
            MaterialRenameOverflowDoesNotCommit();
            MaterialDeleteOverflowDoesNotCommit();
        }

        private static void FamilyPropertyOverflowDoesNotCommit()
        {
            var source = new ProjectState("P-FAMILY-DOMAIN-ATOMIC", "Family domain atomicity");
            var family = new ProjectFamily("F-A", "Wall A", ElementCategory.ArchitecturalWall);
            family.Properties["WidthM"] = "0.2";
            source.Families.Add(family);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(wall);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => ProjectFamilyService.SetProperty(project, "F-A", "WidthM", "0.3"));

            family = project.FindFamily("F-A") ?? throw new Exception("Failed family property mutation lost the family.");
            wall = RequiredElement(project, "W1");
            Equal("0.2", family.Properties["WidthM"], "Failed family property mutation changed the family default.");
            Equal("0.2", wall.Properties["WidthM"], "Failed family property mutation changed the inherited instance value.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed family property mutation changed instance dirty flags.");
            AssertPersistenceUnchanged(project, beforeUtc, "family property mutation");
        }

        private static void FloorAssignmentOverflowDoesNotCommit()
        {
            var source = new ProjectState("P-FLOOR-DOMAIN-ATOMIC", "Floor domain atomicity");
            source.Floors.Add(new FloorDefinition("L1", "Level 1", 0d));
            source.Floors.Add(new FloorDefinition("L2", "Level 2", 3d));
            source.ActiveFloorId = "L1";
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, "L1", string.Empty);
            wall.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(wall);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => ProjectFloorService.Assign(project, "L2", new[] { RequiredElement(project, "W1") }));

            wall = RequiredElement(project, "W1");
            Equal("L1", wall.FloorId, "Failed floor assignment changed FloorId.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed floor assignment changed dirty flags.");
            AssertPersistenceUnchanged(project, beforeUtc, "floor assignment");
        }

        private static void ZoneAssignmentOverflowDoesNotCommit()
        {
            var source = new ProjectState("P-ZONE-DOMAIN-ATOMIC", "Zone domain atomicity");
            source.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            source.Zones.Add(new ZoneDefinition("Z2", "Zone 2"));
            source.ActiveZoneId = "Z1";
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, "Z1");
            wall.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(wall);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => ProjectZoneService.Assign(project, "Z2", new[] { RequiredElement(project, "W1") }));

            wall = RequiredElement(project, "W1");
            Equal("Z1", wall.ZoneId, "Failed zone assignment changed ZoneId.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed zone assignment changed dirty flags.");
            AssertPersistenceUnchanged(project, beforeUtc, "zone assignment");
        }

        private static void ActiveFamilyOverflowDoesNotCommit()
        {
            var source = new ProjectState("P-ACTIVE-FAMILY-ATOMIC", "Active family atomicity");
            source.Families.Add(new ProjectFamily("F-A", "Wall A", ElementCategory.ArchitecturalWall));
            source.Families.Add(new ProjectFamily("F-B", "Wall B", ElementCategory.ArchitecturalWall));
            source.Metadata["ActiveFamilyId"] = "F-A";
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => ProjectFamilyActivationService.SetActive(project, "F-B"));

            Equal("F-A", project.Metadata["ActiveFamilyId"], "Failed active-family mutation changed metadata.");
            AssertPersistenceUnchanged(project, beforeUtc, "active family mutation");
        }

        private static void GridRenumberOverflowDoesNotCommit()
        {
            var source = new ProjectState("P-GRID-DOMAIN-ATOMIC", "Grid domain atomicity");
            var grid = new ProjectElement("G1", ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
            grid.Properties[GridNamingService.GridLabelKey] = "OLD";
            grid.Properties[GridNamingService.GridSequenceIndexKey] = "9";
            grid.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(grid);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => GridNamingService.Renumber(
                project,
                new[] { "G1" },
                new GridNamingOptions { Sequence = GridLabelSequence.Numeric, StartIndex = 1 }));

            grid = RequiredElement(project, "G1");
            Equal("OLD", grid.Properties[GridNamingService.GridLabelKey], "Failed Grid renumber changed the label.");
            Equal("9", grid.Properties[GridNamingService.GridSequenceIndexKey], "Failed Grid renumber changed the sequence index.");
            Equal(ElementDirtyFlags.None, grid.Dirty, "Failed Grid renumber changed dirty flags.");
            AssertPersistenceUnchanged(project, beforeUtc, "Grid renumber");
        }

        private static void MaterialRenameOverflowDoesNotCommit()
        {
            var source = MaterialProject();
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => ProjectMaterialCatalog.UpsertCustom(project, "MAT-1", "New Material", "m²", "renamed"));

            var material = ProjectMaterialCatalog.GetCustom(project).Single(x => string.Equals(x.Id, "MAT-1", StringComparison.OrdinalIgnoreCase));
            Equal("Old Material", material.Name, "Failed material rename changed the catalog entry.");
            var family = project.FindFamily("F-MAT") ?? throw new Exception("Failed material rename lost the family.");
            Equal("Old Material", family.Properties["Material"], "Failed material rename changed the family reference.");
            var wall = RequiredElement(project, "W-MAT");
            Equal("Old Material", wall.Properties["Material"], "Failed material rename changed the instance reference.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed material rename changed instance dirty flags.");
            AssertPersistenceUnchanged(project, beforeUtc, "material rename");
        }

        private static void MaterialDeleteOverflowDoesNotCommit()
        {
            var source = new ProjectState("P-MATERIAL-DELETE-ATOMIC", "Material delete atomicity");
            ProjectMaterialCatalog.UpsertCustom(source, "MAT-DELETE", "Delete Candidate", "kg", string.Empty);
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            Throws<OverflowException>(() => ProjectMaterialCatalog.DeleteCustom(project, "MAT-DELETE"));

            var material = ProjectMaterialCatalog.GetCustom(project).Single(x => string.Equals(x.Id, "MAT-DELETE", StringComparison.OrdinalIgnoreCase));
            Equal("Delete Candidate", material.Name, "Failed material delete removed the catalog entry.");
            AssertPersistenceUnchanged(project, beforeUtc, "material delete");
        }

        private static ProjectState MaterialProject()
        {
            var project = new ProjectState("P-MATERIAL-DOMAIN-ATOMIC", "Material domain atomicity");
            ProjectMaterialCatalog.UpsertCustom(project, "MAT-1", "Old Material", "m²", "original");
            var family = new ProjectFamily("F-MAT", "Material wall", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Old Material";
            project.Families.Add(family);
            var wall = new ProjectElement("W-MAT", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            wall.Properties["Material"] = "Old Material";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            return project;
        }

        private static void AssertPersistenceUnchanged(ProjectState project, DateTime beforeUtc, string operation)
        {
            Equal(long.MaxValue, project.ChangeVersion, "Failed " + operation + " changed the maximum project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed " + operation + " changed UpdatedUtc.");
        }

        private static ProjectState AtVersion(ProjectState source, long version)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-domain-atomicity-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(source, path);
                var document = XDocument.Load(path);
                var root = document.Root ?? throw new Exception("QSDB fixture has no root element.");
                root.SetAttributeValue("changeVersion", version.ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);
                return store.Load(path);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static ProjectElement RequiredElement(ProjectState project, string id) =>
            project.FindElement(id) ?? throw new Exception("Missing fixture element: " + id);

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
