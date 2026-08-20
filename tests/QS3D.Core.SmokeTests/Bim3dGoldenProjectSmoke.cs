using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    /// <summary>
    /// Repository-owned synthetic customer golden path. All dimensions are deliberately simple
    /// and independently computable; no customer/private DWG or licensed host is involved.
    /// </summary>
    internal static class Bim3dGoldenProjectSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Run();
        }

        internal static void Run()
        {
            var project = BuildProject();
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());

            var regenerated = engine.RegenerateDirty(project);
            // Seven semantic elements are processed, then Door regeneration marks its host Wall quantity-dirty,
            // so the deterministic dependency-aware total includes one additional Wall pass.
            Equal(8, regenerated, "initial regeneration count");
            AssertExpectedQuantities(project);
            AssertReport(project);
            AssertIdentity(project);

            Equal(0, engine.RegenerateDirty(project), "clean repeated regeneration count");
            AssertExpectedQuantities(project);

            UnitParity();
            FailClosedSelection(project);
            RoundTripAndRecalculate(project);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("golden-project", "Synthetic BIM3D QS golden project")
            {
                DrawingFingerprint = "SYNTHETIC-GOLDEN-FP"
            };
            project.Floors.Add(new FloorDefinition("floor-01", "Level 01", 0d));
            project.Zones.Add(new ZoneDefinition("zone-a", "Zone A"));

            AddFamily(project, "fam-wall", "Wall 200", ElementCategory.ArchitecturalWall);
            AddFamily(project, "fam-door", "Door 900x2100", ElementCategory.Door);
            AddFamily(project, "fam-beam", "Beam 300x500", ElementCategory.Beam);
            AddFamily(project, "fam-column", "Column 400x400", ElementCategory.Column);
            AddFamily(project, "fam-slab", "Slab 150", ElementCategory.Slab);
            AddFamily(project, "fam-swall", "Structural Wall 250", ElementCategory.StructuralWall);
            AddFamily(project, "fam-foundation", "Pad Foundation", ElementCategory.Foundation);

            var wall = Element("W1", ElementCategory.ArchitecturalWall, "fam-wall", "A100");
            Set(wall, "LengthM", 5d); Set(wall, "HeightM", 3d); Set(wall, "ThicknessM", .2d);
            project.Elements.Add(wall);

            var door = Element("D1", ElementCategory.Door, "fam-door", "A110");
            Set(door, "WidthM", .9d); Set(door, "HeightM", 2.1d); door.Properties["HostWallId"] = wall.Id;
            door.DependsOn.Add(wall.Id);
            project.Elements.Add(door);

            var beam = Element("B1", ElementCategory.Beam, "fam-beam", "S100");
            Set(beam, "LengthM", 4d); Set(beam, "WidthM", .3d); Set(beam, "HeightM", .5d);
            project.Elements.Add(beam);

            var column = Element("C1", ElementCategory.Column, "fam-column", "S110");
            Set(column, "WidthM", .4d); Set(column, "DepthM", .4d); Set(column, "HeightM", 3d);
            project.Elements.Add(column);

            var slab = Element("SL1", ElementCategory.Slab, "fam-slab", "S120");
            Set(slab, "AreaM2", 20d); Set(slab, "ThicknessM", .15d); Set(slab, "PerimeterM", 18d);
            project.Elements.Add(slab);

            var structuralWall = Element("SW1", ElementCategory.StructuralWall, "fam-swall", "S130");
            Set(structuralWall, "LengthM", 4d); Set(structuralWall, "HeightM", 3d); Set(structuralWall, "ThicknessM", .25d);
            project.Elements.Add(structuralWall);

            var foundation = Element("F1", ElementCategory.Foundation, "fam-foundation", "S140");
            Set(foundation, "BaseAreaM2", 4d); Set(foundation, "ThicknessM", .5d); Set(foundation, "PerimeterM", 8d);
            project.Elements.Add(foundation);

            return project;
        }

        private static void AddFamily(ProjectState project, string id, string name, ElementCategory category)
        {
            var family = new ProjectFamily(id, name, category);
            family.Properties["Material"] = category == ElementCategory.Door ? "Timber" : "Concrete";
            project.Families.Add(family);
        }

        private static ProjectElement Element(string id, ElementCategory category, string familyId, string handle)
        {
            var element = new ProjectElement(id, category, familyId, "floor-01", "zone-a");
            element.SourceHandles.Add(handle);
            return element;
        }

        private static void Set(ProjectElement element, string key, double value)
        {
            element.Properties[key] = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void AssertExpectedQuantities(ProjectState project)
        {
            var wall = Required(project, "W1");
            Near(1.89d, Required(project, "D1").Quantities["OpeningAreaM2"], "door opening area");
            Near(13.11d, wall.Quantities["NetWallAreaM2"], "wall net area after linked door");
            Near(2.622d, wall.Quantities["NetVolumeM3"], "wall net volume after linked door");
            Near(.6d, Required(project, "B1").Quantities["NetVolumeM3"], "beam volume");
            Near(.48d, Required(project, "C1").Quantities["NetVolumeM3"], "column volume");
            Near(3d, Required(project, "SL1").Quantities["NetVolumeM3"], "slab volume");
            Near(3d, Required(project, "SW1").Quantities["NetVolumeM3"], "structural wall volume");
            Near(2d, Required(project, "F1").Quantities["NetVolumeM3"], "foundation volume");
        }

        private static void AssertReport(ProjectState project)
        {
            var detail = ProjectQuantityReportBuilder.Detail(project);
            Equal(7, detail.Count, "detail row count");
            var expectedProvenance = new Dictionary<ElementCategory, string[]>
            {
                [ElementCategory.ArchitecturalWall] = new[] { "A100" },
                [ElementCategory.Door] = new[] { "A110", "A100" },
                [ElementCategory.Beam] = new[] { "S100" },
                [ElementCategory.Column] = new[] { "S110" },
                [ElementCategory.Slab] = new[] { "S120" },
                [ElementCategory.StructuralWall] = new[] { "S130" },
                [ElementCategory.Foundation] = new[] { "S140" }
            };
            foreach (var pair in expectedProvenance)
            {
                var category = pair.Key;
                var row = detail.SingleOrDefault(x => string.Equals(x.Category, category.ToString(), StringComparison.Ordinal));
                if (row == null || row.ElementIds.Count != 1 || row.DrawingFingerprint != project.DrawingFingerprint)
                    throw new InvalidOperationException("Golden report identity provenance failed for " + category + ".");

                var actualHandles = new HashSet<string>(row.SourceHandles, StringComparer.OrdinalIgnoreCase);
                if (!actualHandles.SetEquals(pair.Value))
                    throw new InvalidOperationException(
                        "Golden report source provenance failed for " + category + ": expected [" +
                        string.Join(",", pair.Value) + "] but was [" + string.Join(",", actualHandles) + "].");
            }
        }

        private static void AssertIdentity(ProjectState project)
        {
            if (project.Elements.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != project.Elements.Count)
                throw new InvalidOperationException("Golden rebuild produced duplicate semantic element identity.");
            var handles = project.Elements.SelectMany(x => x.SourceHandles).ToList();
            if (handles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != handles.Count)
                throw new InvalidOperationException("Golden rebuild produced duplicate source ownership identity.");
        }

        private static void UnitParity()
        {
            var mm = new ProjectUnitPolicy(LengthUnit.Millimeter, 3);
            var meter = new ProjectUnitPolicy(LengthUnit.Meter, 3);
            Near(meter.ToMeters(5d), mm.ToMeters(5000d), "5 m / 5000 mm length parity");
            Near(meter.VolumeToCubicMeters(1d), mm.VolumeToCubicMeters(1_000_000_000d), "m3 / mm3 volume parity");
        }

        private static void FailClosedSelection(ProjectState project)
        {
            Throws<KeyNotFoundException>(() => ProjectQuantityReportBuilder.Detail(project, new[] { "missing-element" }));
        }

        private static void RoundTripAndRecalculate(ProjectState project)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-golden-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);
                Equal(project.Elements.Count, loaded.Elements.Count, "round-trip element count");
                AssertIdentity(loaded);
                AssertExpectedQuantities(loaded);

                foreach (var element in loaded.Elements)
                    element.MarkDirty(ElementDirtyFlags.Quantity);
                var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
                Equal(8, engine.RegenerateDirty(loaded), "post-round-trip regeneration count");
                AssertExpectedQuantities(loaded);
                AssertReport(loaded);
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
                SafeDelete(path + ".tmp");
            }
        }

        private static ProjectElement Required(ProjectState project, string id)
        {
            return project.FindElement(id) ?? throw new InvalidOperationException("Missing golden element " + id + ".");
        }

        private static void Near(double expected, double actual, string context)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException(context + " expected " + expected + " but was " + actual + ".");
        }

        private static void Equal(int expected, int actual, string context)
        {
            if (expected != actual)
                throw new InvalidOperationException(context + " expected " + expected + " but was " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}