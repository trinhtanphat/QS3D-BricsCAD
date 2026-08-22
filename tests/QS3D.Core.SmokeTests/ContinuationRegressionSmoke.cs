using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using QS3D.Core.Takeoff;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ContinuationRegressionSmoke
    {
        public static void Run()
        {
            LegacyMigrationMarksElementsDirty();
            FamilyAssignmentRefreshesInheritedDefaults();
            QsdbRejectsNonFiniteStateBeforeReplace();
            LegacyWallCalculatorRejectsNonFiniteValues();
            QuantityEngineRejectsInvalidSnapshotMetrics();
            ReportingRejectsNonFiniteState();
        }

        private static void LegacyMigrationMarksElementsDirty()
        {
            var path = Temp("legacy-element", ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"z\" activeFloorId=\"f\">" +
                    "<metadata/><zones><zone id=\"z\" name=\"Zone\"/></zones><floors><floor id=\"f\" name=\"Floor\" elevationM=\"0\"/></floors>" +
                    "<families><family id=\"wall\" name=\"Wall\" category=\"ArchitecturalWall\"><properties/></family></families>" +
                    "<elements><element id=\"W1\" category=\"ArchitecturalWall\" familyId=\"wall\" floorId=\"f\" zoneId=\"z\" drawingFingerprint=\"\">" +
                    "<handles/><dependencies/><properties><p name=\"LengthM\" value=\"5\"/></properties><quantities><q name=\"NetVolumeM3\" value=\"99\"/></quantities>" +
                    "</element></elements></qs3d>", Encoding.UTF8);
                var element = new QsdbProjectStore().Load(path).Elements.Single();
                Equal(ElementDirtyFlags.All, element.Dirty);
                Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), element.UpdatedUtc);
            }
            finally { Delete(path); }
        }

        private static void FamilyAssignmentRefreshesInheritedDefaults()
        {
            var project = NewProject();
            var oldFamily = new ProjectFamily("old", "Old", ElementCategory.ArchitecturalWall);
            oldFamily.Properties["ThicknessM"] = "0.2";
            oldFamily.Properties["Material"] = "Brick";
            oldFamily.Properties["LegacyOnly"] = "remove-me";
            var newFamily = new ProjectFamily("new", "New", ElementCategory.ArchitecturalWall);
            newFamily.Properties["ThicknessM"] = "0.3";
            newFamily.Properties["Material"] = "Concrete";
            newFamily.Properties["HeightM"] = "3.6";
            project.Families.Add(oldFamily);
            project.Families.Add(newFamily);

            var element = new ProjectElement("W1", ElementCategory.ArchitecturalWall, oldFamily.Id, "f", "z");
            element.Properties["ThicknessM"] = "0.2";
            element.Properties["Material"] = "Custom Finish";
            element.Properties["LegacyOnly"] = "remove-me";
            element.Properties["InstanceNote"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            Equal(1, new BulkEditService().AssignFamily(project, new[] { element.Id }, newFamily.Id));
            Equal(newFamily.Id, element.FamilyId);
            Equal("0.3", element.Properties["ThicknessM"]);
            Equal("3.6", element.Properties["HeightM"]);
            Equal("Custom Finish", element.Properties["Material"]);
            Equal("keep", element.Properties["InstanceNote"]);
            True(!element.Properties.ContainsKey("LegacyOnly"));
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);
            Equal(0, new BulkEditService().AssignFamily(project, new[] { element.Id }, newFamily.Id));
        }

        private static void QsdbRejectsNonFiniteStateBeforeReplace()
        {
            var path = Temp("finite-save", ".qsdb");
            try
            {
                var project = NewProject();
                var store = new QsdbProjectStore();
                store.Save(project, path);

                Throws<ArgumentOutOfRangeException>(() => project.Floors[0].ElevationM = double.NaN);
                Near(0d, project.Floors[0].ElevationM);

                var invalid = new ProjectElement("BAD-NAN", ElementCategory.CustomQuantity, string.Empty, "f", "z");
                invalid.Quantities["Count"] = double.NaN;
                project.Elements.Add(invalid);
                Throws<InvalidDataException>(() => store.Save(project, path));

                var restored = store.Load(path);
                Near(0d, restored.Floors.Single().ElevationM);
                True(restored.FindElement("BAD-NAN") == null);
            }
            finally { Delete(path); Delete(path + ".bak"); }
        }

        private static void LegacyWallCalculatorRejectsNonFiniteValues()
        {
            Throws<ArgumentOutOfRangeException>(() => WallQuantityCalculator.Calculate(double.NaN, 3d, .2d));
            Throws<ArgumentOutOfRangeException>(() => WallQuantityCalculator.Calculate(5d, 3d, .2d, new[] { new OpeningCut { WidthM = double.PositiveInfinity, HeightM = 2d } }));
            Throws<OverflowException>(() => WallQuantityCalculator.Calculate(double.MaxValue, 2d, .2d));
        }

        private static void QuantityEngineRejectsInvalidSnapshotMetrics()
        {
            var invalidLength = new EntitySnapshot("A", "Line", "0");
            Throws<ArgumentOutOfRangeException>(() => invalidLength.LengthDrawingUnits = double.NaN);

            var invalidArea = new EntitySnapshot("B", "Polyline", "0");
            Throws<ArgumentOutOfRangeException>(() => invalidArea.AreaDrawingUnitsSquared = -1d);
            SetLegacyMetric(invalidArea, "_areaDrawingUnitsSquared", -1d);
            Throws<InvalidOperationException>(() => QuantityEngine.Calculate(invalidArea, TakeoffKind.Area, DrawingUnit.Meter));
        }

        private static void SetLegacyMetric(EntitySnapshot snapshot, string fieldName, double value)
        {
            var field = typeof(EntitySnapshot).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("Expected private legacy metric field '" + fieldName + "'.");
            field.SetValue(snapshot, value);
        }

        private static void ReportingRejectsNonFiniteState()
        {
            var project = NewProject();
            var family = new ProjectFamily("wall", "Wall", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            var element = new ProjectElement("W-RPT", ElementCategory.ArchitecturalWall, family.Id, "f", "z");
            element.Quantities["LengthM"] = double.NaN;
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project));

            var legacyFamily = new FamilyDefinition("Legacy", ElementCategory.ArchitecturalWall);
            var legacy = new ElementInstance("LEGACY", legacyFamily, "Floor");
            Throws<ArgumentOutOfRangeException>(() => legacy.GrossConcreteM3 = double.PositiveInfinity);

            var badRow = new QuantityReportRow { Count = 1, LengthM = double.NaN };
            Throws<InvalidOperationException>(() => QuantityReportTotals.FromRows(new[] { badRow }));
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Continuation");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            return project;
        }

        private static string Temp(string prefix, string extension) => Path.Combine(Path.GetTempPath(), "qs3d-" + prefix + "-" + Guid.NewGuid().ToString("N") + extension);
        private static void Delete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
