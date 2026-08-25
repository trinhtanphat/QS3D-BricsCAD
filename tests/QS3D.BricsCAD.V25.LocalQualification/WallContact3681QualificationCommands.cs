using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;
using Exception = System.Exception;

namespace QS3D.BricsCAD.V25.LocalQualification
{
    public sealed class WallContact3681QualificationCommands
    {
        private const string ResultEnvironmentVariable = "QS3D_3681_RESULT";
        private const double ToleranceM2 = 1e-6d;
        private const double ExpectedGrossM2 = 2.6688d;
        private const double ExpectedOneEndM2 = 0.1600d;
        private const double ExpectedPartialM2 = 0.0800d;
        private const double ExpectedTwoEndsM2 = 0.3200d;
        private const double ExpectedOneEndNetM2 = 2.5088d;
        private const double ExpectedTwoEndsNetM2 = 2.3488d;

        private sealed class SolidRef
        {
            public SolidRef(ObjectId id, string handle)
            {
                Id = id;
                Handle = handle;
            }

            public ObjectId Id { get; }
            public string Handle { get; }
        }

        private sealed class Measurement
        {
            public bool Available { get; set; }
            public double DeductionM2 { get; set; }
            public int CandidateSolidCount { get; set; }
            public int VerticalFaceSeedCount { get; set; }
            public int PositiveVolumeCutCount { get; set; }
            public int ContactProbeCutCount { get; set; }
            public int FailedNativeCutCount { get; set; }
            public double GrossVerticalAreaM2 { get; set; }
            public double ResidualVerticalAreaM2 { get; set; }
        }

        private struct SolidSpec
        {
            public SolidSpec(double x, double y, double z, double dx, double dy, double dz)
            {
                X = x;
                Y = y;
                Z = z;
                Dx = dx;
                Dy = dy;
                Dz = dz;
            }

            public double X;
            public double Y;
            public double Z;
            public double Dx;
            public double Dy;
            public double Dz;
        }

        [CommandMethod("QS3D3681GEOMETRY")]
        public void Geometry()
        {
            Execute("geometry", RunGeometryQualification);
        }

        [CommandMethod("QS3D3681PERSIST")]
        public void Persist()
        {
            Execute("persist", RunPersistenceSetup);
        }

        [CommandMethod("QS3D3681REOPEN")]
        public void Reopen()
        {
            Execute("reopen", RunColdReopenVerification);
        }

        private static IDictionary<string, string> RunGeometryQualification(Document document)
        {
            RequireMillimeterDrawing(document);
            var result = NewResult("geometry");

            var baseline = RunMeasureCase(document, new SolidSpec[0], 0d);
            RequireMeasurement("baseline", baseline, 0d, 0);
            result["case.baseline"] = "PASS";
            result["baseline.gross_m2"] = Format(baseline.GrossVerticalAreaM2);
            result["baseline.deduction_m2"] = Format(baseline.DeductionM2);

            var full = RunMeasureCase(document, new[]
            {
                new SolidSpec(-100d, -100d, 0d, 100d, 200d, 800d)
            }, ExpectedOneEndM2);
            RequireMeasurement("full_end", full, ExpectedOneEndM2, 1);
            result["case.full_end"] = "PASS";
            result["full_end.deduction_m2"] = Format(full.DeductionM2);
            result["full_end.contact_probe_cut_count"] = full.ContactProbeCutCount.ToString(CultureInfo.InvariantCulture);

            var partial = RunMeasureCase(document, new[]
            {
                new SolidSpec(-100d, -100d, 200d, 100d, 200d, 400d)
            }, ExpectedPartialM2);
            RequireMeasurement("partial_end", partial, ExpectedPartialM2, 1);
            result["case.partial_end"] = "PASS";
            result["partial_end.deduction_m2"] = Format(partial.DeductionM2);

            var union = RunMeasureCase(document, new[]
            {
                new SolidSpec(-100d, -100d, 0d, 100d, 200d, 500d),
                new SolidSpec(-100d, -100d, 300d, 100d, 200d, 500d)
            }, ExpectedOneEndM2);
            RequireMeasurement("multi_neighbor_union", union, ExpectedOneEndM2, 1);
            result["case.multi_neighbor_union"] = "PASS";
            result["multi_neighbor_union.deduction_m2"] = Format(union.DeductionM2);

            var top = RunMeasureCase(document, new[]
            {
                new SolidSpec(100d, -50d, 800d, 1268d, 100d, 100d)
            }, 0d);
            RequireMeasurement("top_bottom_exclusion", top, 0d, 0);
            result["case.top_bottom_exclusion"] = "PASS";
            result["top_bottom_exclusion.deduction_m2"] = Format(top.DeductionM2);

            var twoEnds = RunMeasureCase(document, new[]
            {
                new SolidSpec(-100d, -100d, 0d, 100d, 200d, 800d),
                new SolidSpec(1468d, -100d, 0d, 100d, 200d, 800d)
            }, ExpectedTwoEndsM2);
            RequireMeasurement("two_end_blt", twoEnds, ExpectedTwoEndsM2, 2);
            result["case.two_end_blt"] = "PASS";
            result["blt.gross_m2"] = Format(ExpectedGrossM2);
            result["blt.deduction_m2"] = Format(twoEnds.DeductionM2);
            result["blt.net_m2"] = Format(ExpectedTwoEndsNetM2);

            RunCaptureRefreshAndMissingTargetClear(document, result);
            RunReadOnlyMutationGuard(document, result);
            result["case.undo_redo"] = "PASS_NOT_APPLICABLE_READ_ONLY_MEASUREMENT";
            return result;
        }

        private static IDictionary<string, string> RunPersistenceSetup(Document document)
        {
            RequireMillimeterDrawing(document);
            RequireSavedDrawing(document);
            ForgetProject(document);

            var created = new List<SolidRef>();
            try
            {
                var wallSolid = CreateBox(document, 0d, -100d, 0d, 1468d, 200d, 800d);
                var left = CreateBox(document, -100d, -100d, 0d, 100d, 200d, 800d);
                var right = CreateBox(document, 1468d, -100d, 0d, 100d, 200d, 800d);
                created.Add(wallSolid);
                created.Add(left);
                created.Add(right);

                var project = GetOrCreateProject(document);
                project.Elements.Clear();
                BindFixtureMillimeterUnit(project);
                var wall = NewWall("local-3681-wall", wallSolid.Handle);
                project.Elements.Add(wall);
                project.Elements.Add(NewNeighbor("local-3681-left", left.Handle));
                project.Elements.Add(NewNeighbor("local-3681-right", right.Handle));
                new StructuralRegenerator().Regenerate(project, wall);
                project.Touch();
                RefreshContacts(document, project);

                RequireQuantity(wall, "GrossFormworkM2", ExpectedGrossM2, "persist_gross");
                RequireQuantity(wall, "ConcreteContactDeductionM2", ExpectedTwoEndsM2, "persist_contact");
                RequireQuantity(wall, "FormworkM2", ExpectedTwoEndsNetM2, "persist_net");
                SaveProject(document);

                var result = NewResult("persist");
                result["case.save"] = "PASS";
                result["save.gross_m2"] = Format(wall.Quantities["GrossFormworkM2"]);
                result["save.deduction_m2"] = Format(wall.Quantities["ConcreteContactDeductionM2"]);
                result["save.net_m2"] = Format(wall.Quantities["FormworkM2"]);
                result["persisted_solid_count"] = "3";
                return result;
            }
            catch
            {
                Erase(document, created.Select(x => x.Id));
                throw;
            }
        }

        private static IDictionary<string, string> RunColdReopenVerification(Document document)
        {
            RequireMillimeterDrawing(document);
            RequireSavedDrawing(document);
            ForgetProject(document);
            var project = GetOrCreateProject(document);
            var wall = project.Elements.SingleOrDefault(x => string.Equals(x.Id, "local-3681-wall", StringComparison.Ordinal));
            if (wall == null) throw new InvalidOperationException("cold_reopen_wall_missing");

            RefreshContacts(document, project);
            RequireQuantity(wall, "GrossFormworkM2", ExpectedGrossM2, "reopen_gross");
            RequireQuantity(wall, "ConcreteContactDeductionM2", ExpectedTwoEndsM2, "reopen_contact");
            RequireQuantity(wall, "FormworkM2", ExpectedTwoEndsNetM2, "reopen_net");

            var result = NewResult("reopen");
            result["case.cold_reopen"] = "PASS";
            result["reopen.gross_m2"] = Format(wall.Quantities["GrossFormworkM2"]);
            result["reopen.deduction_m2"] = Format(wall.Quantities["ConcreteContactDeductionM2"]);
            result["reopen.net_m2"] = Format(wall.Quantities["FormworkM2"]);
            return result;
        }

        private static void RunCaptureRefreshAndMissingTargetClear(Document document, IDictionary<string, string> result)
        {
            ForgetProject(document);
            var created = new List<SolidRef>();
            try
            {
                var wallSolid = CreateBox(document, 0d, -100d, 0d, 1468d, 200d, 800d);
                var near = CreateBox(document, -100d, -100d, 0d, 100d, 200d, 800d);
                var distant = CreateBox(document, 3000d, -100d, 0d, 100d, 200d, 800d);
                created.Add(wallSolid);
                created.Add(near);
                created.Add(distant);

                var project = GetOrCreateProject(document);
                project.Elements.Clear();
                BindFixtureMillimeterUnit(project);
                var wall = NewWall("local-3681-capture-wall", wallSolid.Handle);
                project.Elements.Add(wall);
                new StructuralRegenerator().Regenerate(project, wall);
                project.Touch();
                RefreshContacts(document, project);
                RequireQuantity(wall, "FormworkM2", ExpectedGrossM2, "capture_baseline");

                document.Editor.SetImpliedSelection(new[] { near.Id });
                var captured = CaptureSelection(document, ElementCategory.Column);
                if (captured != 1) throw new InvalidOperationException("capture_refresh_near_count");
                RequireContactProperty(wall, ExpectedOneEndM2, "capture_refresh_contact");
                RequireQuantity(wall, "FormworkM2", ExpectedOneEndNetM2, "capture_refresh_net");
                result["case.semantic_capture_refresh"] = "PASS";
                result["capture_refresh.deduction_m2"] = Format(ExpectedOneEndM2);
                result["capture_refresh.net_m2"] = Format(wall.Quantities["FormworkM2"]);

                Erase(document, new[] { wallSolid.Id });
                document.Editor.SetImpliedSelection(new[] { distant.Id });
                captured = CaptureSelection(document, ElementCategory.Column);
                if (captured != 1) throw new InvalidOperationException("missing_target_refresh_count");
                if (wall.Properties.ContainsKey("ConcreteContactAreaM2"))
                    throw new InvalidOperationException("missing_target_contact_not_cleared");
                RequireQuantity(wall, "FormworkM2", ExpectedGrossM2, "missing_target_net");
                result["case.stale_missing_brep_clear"] = "PASS";
                result["stale_clear.deduction_m2"] = "0";
                result["stale_clear.net_m2"] = Format(wall.Quantities["FormworkM2"]);
            }
            finally
            {
                try { document.Editor.SetImpliedSelection(new ObjectId[0]); } catch { }
                ForgetProject(document);
                Erase(document, created.Select(x => x.Id));
            }
        }

        private static void BindFixtureMillimeterUnit(ProjectState project)
        {
            const LengthUnit unit = LengthUnit.Millimeter;
            var hasElements = project.Elements.Count > 0;
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(project.Metadata, hasElements, unit);
            DrawingUnitResolutionPolicy.BindQuantityUnit(
                project.Metadata,
                hasElements,
                unit,
                DrawingUnitResolutionSource.ProjectOverride);
            DrawingUnitResolutionPolicy.SetProjectOverride(project.Metadata, unit);
            project.Touch();
        }

        private static void RunReadOnlyMutationGuard(Document document, IDictionary<string, string> result)
        {
            var created = new List<SolidRef>();
            try
            {
                var wall = CreateBox(document, 0d, -100d, 0d, 1468d, 200d, 800d);
                var neighbor = CreateBox(document, -100d, -100d, 0d, 100d, 200d, 800d);
                created.Add(wall);
                created.Add(neighbor);
                var beforeWall = ReadVolume(document, wall.Id);
                var beforeNeighbor = ReadVolume(document, neighbor.Id);

                var project = NewProject(wall.Handle, new[] { neighbor.Handle });
                var semanticWall = project.Elements.Single(x => x.Category == ElementCategory.StructuralWall);
                var first = Measure(document, project, semanticWall);
                var second = Measure(document, project, semanticWall);
                RequireMeasurement("read_only_first", first, ExpectedOneEndM2, 1);
                RequireMeasurement("read_only_second", second, ExpectedOneEndM2, 1);

                var afterWall = ReadVolume(document, wall.Id);
                var afterNeighbor = ReadVolume(document, neighbor.Id);
                if (!Near(beforeWall, afterWall, 1e-3d) || !Near(beforeNeighbor, afterNeighbor, 1e-3d))
                    throw new InvalidOperationException("measurement_mutated_native_solids");
                result["case.measurement_read_only"] = "PASS";
            }
            finally
            {
                Erase(document, created.Select(x => x.Id));
            }
        }

        private static Measurement RunMeasureCase(Document document, IEnumerable<SolidSpec> candidateSpecs, double expected)
        {
            var created = new List<SolidRef>();
            try
            {
                var wall = CreateBox(document, 0d, -100d, 0d, 1468d, 200d, 800d);
                created.Add(wall);
                var candidates = new List<SolidRef>();
                foreach (var spec in candidateSpecs)
                {
                    var candidate = CreateBox(document, spec.X, spec.Y, spec.Z, spec.Dx, spec.Dy, spec.Dz);
                    candidates.Add(candidate);
                    created.Add(candidate);
                }

                var project = NewProject(wall.Handle, candidates.Select(x => x.Handle));
                var semanticWall = project.Elements.Single(x => x.Category == ElementCategory.StructuralWall);
                var measurement = Measure(document, project, semanticWall);
                if (!Near(measurement.DeductionM2, expected, ToleranceM2))
                    throw new InvalidOperationException("unexpected_contact_deduction");
                return measurement;
            }
            finally
            {
                Erase(document, created.Select(x => x.Id));
            }
        }

        private static ProjectState NewProject(string wallHandle, IEnumerable<string> candidateHandles)
        {
            var project = new ProjectState("local-3681-" + Guid.NewGuid().ToString("N"), "LOCAL 3681");
            BindFixtureMillimeterUnit(project);
            var wall = NewWall("wall", wallHandle);
            project.Elements.Add(wall);
            var index = 0;
            foreach (var handle in candidateHandles)
                project.Elements.Add(NewNeighbor("neighbor-" + (++index).ToString(CultureInfo.InvariantCulture), handle));
            return project;
        }

        private static ProjectElement NewWall(string id, string handle)
        {
            var wall = new ProjectElement(id, ElementCategory.StructuralWall);
            wall.SourceHandles.Add(handle);
            wall.Properties["LengthM"] = "1.468";
            wall.Properties["ThicknessM"] = "0.2";
            wall.Properties["HeightM"] = "0.8";
            return wall;
        }

        private static ProjectElement NewNeighbor(string id, string handle)
        {
            var candidate = new ProjectElement(id, ElementCategory.Column);
            candidate.SourceHandles.Add(handle);
            return candidate;
        }

        private static Measurement Measure(Document document, ProjectState project, ProjectElement wall)
        {
            var serviceType = ProductAssembly().GetType("QS3D.BricsCAD.V25.Reporting.StructuralWallConcreteContactService", true);
            var method = serviceType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(x => x.Name == "TryMeasureM2" && x.GetParameters().Length == 5);
            var args = new object[] { document, project, wall, 0d, null! };
            var available = (bool)method.Invoke(null, args)!;
            var diagnostics = args[4];
            return new Measurement
            {
                Available = available,
                DeductionM2 = (double)args[3],
                CandidateSolidCount = ReadInt(diagnostics, "CandidateSolidCount"),
                VerticalFaceSeedCount = ReadInt(diagnostics, "VerticalFaceSeedCount"),
                PositiveVolumeCutCount = ReadInt(diagnostics, "PositiveVolumeCutCount"),
                ContactProbeCutCount = ReadInt(diagnostics, "ContactProbeCutCount"),
                FailedNativeCutCount = ReadInt(diagnostics, "FailedNativeCutCount"),
                GrossVerticalAreaM2 = ReadDouble(diagnostics, "GrossVerticalAreaM2"),
                ResidualVerticalAreaM2 = ReadDouble(diagnostics, "ResidualVerticalAreaM2")
            };
        }

        private static void RequireMeasurement(string label, Measurement measurement, double expectedDeduction, int minimumProbeCuts)
        {
            if (!measurement.Available) throw new InvalidOperationException(label + "_unavailable");
            if (measurement.VerticalFaceSeedCount < 4) throw new InvalidOperationException(label + "_face_seed_count");
            if (measurement.FailedNativeCutCount != 0) throw new InvalidOperationException(label + "_native_failure");
            if (measurement.ContactProbeCutCount < minimumProbeCuts) throw new InvalidOperationException(label + "_contact_probe_not_used");
            if (!Near(measurement.GrossVerticalAreaM2, ExpectedGrossM2, ToleranceM2))
                throw new InvalidOperationException(label + "_gross_area");
            if (!Near(measurement.DeductionM2, expectedDeduction, ToleranceM2))
                throw new InvalidOperationException(label + "_deduction");
        }

        private static SolidRef CreateBox(Document document, double x, double y, double z, double dx, double dy, double dz)
        {
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var space = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var solid = new Solid3d();
                solid.SetDatabaseDefaults();
                solid.CreateBox(dx, dy, dz);
                var extents = solid.GeometricExtents;
                var desiredMin = new Point3d(x, y, z);
                solid.TransformBy(Matrix3d.Displacement(desiredMin - extents.MinPoint));
                var id = space.AppendEntity(solid);
                transaction.AddNewlyCreatedDBObject(solid, true);
                var handle = solid.Handle.ToString();
                transaction.Commit();
                return new SolidRef(id, handle);
            }
        }

        private static void Erase(Document document, IEnumerable<ObjectId> ids)
        {
            var unique = ids.Where(x => !x.IsNull).Distinct().ToList();
            if (unique.Count == 0) return;
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in unique)
                {
                    try
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                        if (entity != null && !entity.IsErased) entity.Erase();
                    }
                    catch { }
                }
                transaction.Commit();
            }
        }

        private static double ReadVolume(Document document, ObjectId id)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased) throw new InvalidOperationException("solid_missing");
                return Math.Abs(solid.MassProperties.Volume);
            }
        }

        private static int CaptureSelection(Document document, ElementCategory category)
        {
            var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.Services.SemanticCaptureService", true);
            var method = type.GetMethod("Capture", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Document), typeof(ElementCategory) }, null);
            if (method == null) throw new MissingMethodException(type.FullName, "Capture");
            return (int)method.Invoke(null, new object[] { document, category })!;
        }

        private static void RefreshContacts(Document document, ProjectState project)
        {
            var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.Services.SemanticCaptureService", true);
            var method = type.GetMethod("RefreshStructuralWallConcreteContacts", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(type.FullName, "RefreshStructuralWallConcreteContacts");
            method.Invoke(null, new object[] { document, project });
        }

        private static ProjectState GetOrCreateProject(Document document)
        {
            var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.ProjectContextCoordinator", true);
            var method = type.GetMethod("GetOrCreate", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Document) }, null);
            if (method == null) throw new MissingMethodException(type.FullName, "GetOrCreate");
            return (ProjectState)method.Invoke(null, new object[] { document })!;
        }

        private static void SaveProject(Document document)
        {
            var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.ProjectContextCoordinator", true);
            var method = type.GetMethod("Save", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Document) }, null);
            if (method == null) throw new MissingMethodException(type.FullName, "Save");
            method.Invoke(null, new object[] { document });
        }

        private static void ForgetProject(Document document)
        {
            try
            {
                var type = ProductAssembly().GetType("QS3D.BricsCAD.V25.ProjectContextCoordinator", true);
                var method = type.GetMethod("Forget", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Document) }, null);
                method?.Invoke(null, new object[] { document });
            }
            catch { }
        }

        private static Assembly ProductAssembly()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(x => string.Equals(x.GetName().Name, "QS3D.BricsCAD.V25", StringComparison.OrdinalIgnoreCase));
            if (assembly == null) throw new InvalidOperationException("product_assembly_not_loaded");
            return assembly;
        }

        private static int ReadInt(object diagnostics, string name)
        {
            if (diagnostics == null) throw new InvalidOperationException("diagnostics_missing");
            var property = diagnostics.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) throw new MissingMemberException(diagnostics.GetType().FullName, name);
            return Convert.ToInt32(property.GetValue(diagnostics, null), CultureInfo.InvariantCulture);
        }

        private static double ReadDouble(object diagnostics, string name)
        {
            if (diagnostics == null) throw new InvalidOperationException("diagnostics_missing");
            var property = diagnostics.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) throw new MissingMemberException(diagnostics.GetType().FullName, name);
            return Convert.ToDouble(property.GetValue(diagnostics, null), CultureInfo.InvariantCulture);
        }

        private static void RequireContactProperty(ProjectElement wall, double expected, string label)
        {
            if (!wall.Properties.TryGetValue("ConcreteContactAreaM2", out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var actual) ||
                !Near(actual, expected, ToleranceM2))
                throw new InvalidOperationException(label);
        }

        private static void RequireQuantity(ProjectElement element, string key, double expected, string label)
        {
            if (!element.Quantities.TryGetValue(key, out var actual) || !Near(actual, expected, ToleranceM2))
                throw new InvalidOperationException(label);
        }

        private static void RequireMillimeterDrawing(Document document)
        {
            if ((int)document.Database.Insunits != 4)
                throw new InvalidOperationException("drawing_units_must_be_millimeters");
        }

        private static void RequireSavedDrawing(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || !Path.IsPathRooted(name) || !name.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("saved_scratch_drawing_required");
        }

        private static bool Near(double left, double right, double tolerance)
        {
            return Math.Abs(left - right) <= tolerance;
        }

        private static string Format(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, string> NewResult(string phase)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["schema"] = "qs3d-local-3681-v1",
                ["phase"] = phase,
                ["status"] = "PASS"
            };
        }

        private static void Execute(string phase, Func<Document, IDictionary<string, string>> action)
        {
            var path = Environment.GetEnvironmentVariable(ResultEnvironmentVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
            {
                try { Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D 3681 LOCAL FAIL result_path_missing."); } catch { }
                return;
            }

            IDictionary<string, string> result;
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null) throw new InvalidOperationException("active_document_missing");
                result = action(document);
            }
            catch (Exception error)
            {
                result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["schema"] = "qs3d-local-3681-v1",
                    ["phase"] = phase,
                    ["status"] = "FAIL",
                    ["error_type"] = error.GetType().Name,
                    ["error_code"] = SafeCode(Unwrap(error).Message)
                };
            }

            WriteMarker(path, result);
            try
            {
                var status = result.TryGetValue("status", out var value) ? value : "FAIL";
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D 3681 LOCAL " + status + " phase=" + phase + ".");
            }
            catch { }
        }

        private static Exception Unwrap(Exception error)
        {
            while (error is TargetInvocationException invocation && invocation.InnerException != null)
                error = invocation.InnerException;
            return error;
        }

        private static string SafeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var chars = value.Trim().Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').Take(120).ToArray();
            return new string(chars);
        }

        private static void WriteMarker(string path, IDictionary<string, string> values)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("result_directory_missing");
            Directory.CreateDirectory(directory);
            var temp = path + ".tmp";
            using (var writer = new StreamWriter(temp, false, new System.Text.UTF8Encoding(false)))
            {
                foreach (var pair in values.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (pair.Key.IndexOfAny(new[] { '\r', '\n', '=' }) >= 0) throw new InvalidOperationException("invalid_marker_key");
                    var safeValue = (pair.Value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("=", ":");
                    writer.WriteLine(pair.Key + "=" + safeValue);
                }
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
