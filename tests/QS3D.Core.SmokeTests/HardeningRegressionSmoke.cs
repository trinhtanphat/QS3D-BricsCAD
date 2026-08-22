using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Formulas;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HardeningRegressionSmoke
    {
        public static void Run()
        {
            SemanticQuantityHardening();
            RebarSpacingRequiresDistribution();
            ModelHealthReferenceIntegrity();
            ModelHealthDimensionIntegrity();
            FamilyChangeNotification();
            FormulaEvaluatorIsConcurrent();
            FormulaEvaluatorHasResourceGuards();
            ProjectLockCreatesParentDirectory();
            BulkEditRejectsNullIds();
            QsdbRejectsDtd();
            FailedRegenerationRemainsDirty();
        }

        private static void SemanticQuantityHardening()
        {
            var project = NewProject();
            var wall = new ProjectElement("W-HARD", ElementCategory.ArchitecturalWall, "wall", "f", "z");
            wall.Properties["LengthM"] = "NaN";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.2";
            new WallRegenerator().Regenerate(project, wall);
            Near(0d, wall.Quantities["LengthM"]);
            Near(0d, wall.Quantities["GrossWallAreaM2"]);

            wall.Properties["LengthM"] = "2";
            wall.Properties["HeightM"] = "2";
            wall.Properties["OpeningAreaM2"] = "99";
            new WallRegenerator().Regenerate(project, wall);
            Near(4d, wall.Quantities["GrossWallAreaM2"]);
            Near(4d, wall.Quantities["OpeningAreaM2"]);
            Near(0d, wall.Quantities["NetWallAreaM2"]);

            var opening = new ProjectElement("O-HARD", ElementCategory.WallOpening, "opening", "f", "z");
            opening.Properties["WidthM"] = "-1";
            opening.Properties["HeightM"] = "-2";
            new OpeningRegenerator().Regenerate(project, opening);
            Near(0d, opening.Quantities["OpeningAreaM2"]);

            var skirting = new ProjectElement("S-HARD", ElementCategory.Skirting, "skirting", "f", "z");
            skirting.Properties["PerimeterM"] = "10";
            skirting.Properties["DoorWidthM"] = "-2";
            new RoomRegenerator().Regenerate(project, skirting);
            Near(10d, skirting.Quantities["SkirtingLengthM"]);
        }

        private static void RebarSpacingRequiresDistribution()
        {
            Throws<InvalidOperationException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "S1", Notation = "D8@150", CuttingLengthM = 4d, DistributionLengthM = 0d }
            }));
        }

        private static void ModelHealthReferenceIntegrity()
        {
            var project = NewProject();
            project.ActiveFloorId = "missing-floor";

            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, "wall", "f", "z");
            project.Elements.Add(wall);

            var door = new ProjectElement("D1", ElementCategory.Door, "door", "f", "z");
            door.Properties["HostWallId"] = "missing-wall";
            door.DependsOn.Add("missing-wall");
            project.Elements.Add(door);

            var mismatched = new ProjectElement("BAD-FAMILY", ElementCategory.Slab, "wall", "f", "z");
            project.Elements.Add(mismatched);

            var beam = new ProjectElement("B1", ElementCategory.Beam, "beam", "f", "z");
            beam.Properties["LengthM"] = "5";
            beam.Properties["RebarNotation"] = "D8@150";
            project.Elements.Add(beam);

            var issues = new ModelHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "INVALID_ACTIVE_FLOOR"));
            True(issues.Any(x => x.Code == "INVALID_HOST" && x.ElementId == "D1"));
            True(issues.Any(x => x.Code == "MISSING_DEPENDENCY" && x.ElementId == "D1"));
            True(issues.Any(x => x.Code == "FAMILY_CATEGORY_MISMATCH" && x.ElementId == "BAD-FAMILY"));
            True(issues.Any(x => x.Code == "REBAR_DISTRIBUTION_MISSING" && x.ElementId == "B1"));
        }

        private static void ModelHealthDimensionIntegrity()
        {
            var project = NewProject();

            var beam = new ProjectElement("B-DIM", ElementCategory.Beam, "beam", "f", "z");
            beam.Properties["LengthM"] = "5";
            beam.Properties["WidthM"] = "NaN";
            beam.Properties["HeightM"] = "-0.5";
            project.Elements.Add(beam);

            var slabFamily = new ProjectFamily("slab", "Slab", ElementCategory.Slab); slabFamily.Properties["Material"] = "Concrete"; project.Families.Add(slabFamily);
            var slab = new ProjectElement("S-DIM", ElementCategory.Slab, "slab", "f", "z");
            slab.Properties["AreaM2"] = "12";
            project.Elements.Add(slab);

            var foundationFamily = new ProjectFamily("foundation", "Foundation", ElementCategory.Foundation); foundationFamily.Properties["Material"] = "Concrete"; project.Families.Add(foundationFamily);
            var foundation = new ProjectElement("F-DIM", ElementCategory.Foundation, "foundation", "f", "z");
            foundation.Properties["AreaM2"] = "4";
            foundation.Properties["ThicknessM"] = "0.5";
            project.Elements.Add(foundation);

            var issues = new ModelHealthService().Inspect(project);
            True(issues.Any(x => x.Code == "INVALID_DIMENSION" && x.ElementId == "B-DIM" && x.Message.Contains("WidthM")));
            True(issues.Any(x => x.Code == "INVALID_DIMENSION" && x.ElementId == "B-DIM" && x.Message.Contains("HeightM")));
            True(issues.Any(x => x.Code == "MISSING_DIMENSION" && x.ElementId == "S-DIM" && x.Message.Contains("ThicknessM")));
            True(!issues.Any(x => (x.Code == "MISSING_DIMENSION" || x.Code == "INVALID_DIMENSION") && x.ElementId == "F-DIM"));
        }

        private static void FamilyChangeNotification()
        {
            var family = new ProjectFamily("fam", "Old", ElementCategory.Room);
            var changed = string.Empty;
            family.PropertyChanged += (_, e) => changed = e.PropertyName ?? string.Empty;
            family.Name = "  New Name  ";
            Equal("New Name", family.Name);
            Equal("Name", changed);
            Throws<ArgumentException>(() => family.Name = "   ");
        }

        private static void FormulaEvaluatorIsConcurrent()
        {
            var evaluator = new ExpressionEvaluator();
            Parallel.For(0, 512, i =>
            {
                var value = i / 4d;
                var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["Value"] = value };
                var actual = evaluator.Evaluate("round(max(Value*2, 3.14159), 2)", variables);
                var expected = Math.Round(Math.Max(value * 2d, 3.14159d), 2);
                Near(expected, actual);
            });
            Near(1002d, evaluator.Evaluate("1e3 + 2"));
        }

        private static void FormulaEvaluatorHasResourceGuards()
        {
            var evaluator = new ExpressionEvaluator();
            Throws<InvalidOperationException>(() => evaluator.Evaluate(new string('1', 5000)));
            var nested = new string('(', 80) + "1" + new string(')', 80);
            Throws<InvalidOperationException>(() => evaluator.Evaluate(nested));
            Throws<InvalidOperationException>(() => evaluator.Evaluate("round(1, 1e100)"));
        }

        private static void ProjectLockCreatesParentDirectory()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-lock-parent-" + Guid.NewGuid().ToString("N"));
            var projectPath = Path.Combine(root, "nested", "project.qsdb");
            var lockPath = projectPath + ".lock";
            try
            {
                using (ProjectFileLock.Acquire(projectPath))
                {
                    True(Directory.Exists(Path.GetDirectoryName(projectPath)));
                    True(File.Exists(lockPath));
                }
                True(!File.Exists(lockPath));
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void BulkEditRejectsNullIds()
        {
            var project = new ProjectState("bulk-hardening", "Bulk Hardening");
            var family = new ProjectFamily("room", "Room", ElementCategory.Room);
            project.Families.Add(family);
            Throws<ArgumentNullException>(() => new BulkEditService().AssignFamily(project, null!, family.Id));
        }

        private static void QsdbRejectsDtd()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-dtd-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<!DOCTYPE qs3d [<!ENTITY injected \"bad\">]>" +
                    "<qs3d schema=\"2\" projectId=\"p\" name=\"&injected;\" updatedUtc=\"2026-08-10T00:00:00.0000000Z\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                    "<metadata/><zones/><floors/><families/><elements/></qs3d>", Encoding.UTF8);
                var rejected = false;
                try { new QsdbProjectStore().Load(path); }
                catch (XmlException) { rejected = true; }
                catch (InvalidDataException) { rejected = true; }
                True(rejected);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void FailedRegenerationRemainsDirty()
        {
            var project = new ProjectState("regen-hardening", "Regeneration Hardening");
            var element = new ProjectElement("Q1", ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            element.MarkDirty(ElementDirtyFlags.Quantity);
            project.Elements.Add(element);
            var engine = new RegenerationEngine(new DependencyGraph(), new IElementRegenerator[] { new ThrowingRegenerator() });
            Throws<InvalidOperationException>(() => engine.RegenerateDirty(project));
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);
            True(element.Quantities.ContainsKey("Partial"));
        }

        private sealed class ThrowingRegenerator : IElementRegenerator
        {
            public bool CanRegenerate(ElementCategory category) => category == ElementCategory.CustomQuantity;

            public void Regenerate(ProjectState project, ProjectElement element)
            {
                element.SetQuantity("Partial", 1d);
                throw new InvalidOperationException("Intentional hardening smoke failure.");
            }
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Hardening");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";

            var wall = new ProjectFamily("wall", "Wall", ElementCategory.ArchitecturalWall); wall.Properties["Material"] = "Brick"; project.Families.Add(wall);
            var opening = new ProjectFamily("opening", "Opening", ElementCategory.WallOpening); project.Families.Add(opening);
            var door = new ProjectFamily("door", "Door", ElementCategory.Door); door.Properties["Material"] = "Wood"; project.Families.Add(door);
            var skirting = new ProjectFamily("skirting", "Skirting", ElementCategory.Skirting); skirting.Properties["Material"] = "Tile"; project.Families.Add(skirting);
            var beam = new ProjectFamily("beam", "Beam", ElementCategory.Beam); beam.Properties["Material"] = "Concrete"; project.Families.Add(beam);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
