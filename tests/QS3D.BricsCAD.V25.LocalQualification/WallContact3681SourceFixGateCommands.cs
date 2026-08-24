using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;
using Exception = System.Exception;

namespace QS3D.BricsCAD.V25.LocalQualification
{
    public sealed class WallContact3681SourceFixGateCommands
    {
        private const string ResultEnvironmentVariable = "QS3D_3681_RESULT";
        private const double ToleranceM2 = 1e-6d;
        private const double ExpectedGrossM2 = 2.6688d;
        private const double ExpectedOneEndM2 = 0.1600d;
        private const double ExpectedOneEndNetM2 = 2.5088d;

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
            public int VerticalFaceSeedCount { get; set; }
            public int PositiveVolumeCutCount { get; set; }
            public int ContactProbeCutCount { get; set; }
            public int FailedNativeCutCount { get; set; }
            public double GrossVerticalAreaM2 { get; set; }
            public double ResidualVerticalAreaM2 { get; set; }
        }

        [CommandMethod("QS3D3681SOURCEFIXGATE")]
        public void SourceFixGate()
        {
            Execute(RunGate);
        }

        private static IDictionary<string, string> RunGate(Document document)
        {
            RequireMillimeterDrawing(document);
            var result = NewResult();

            var touching = RunMeasureCase(document, -100d, 100d);
            RequireCommon("touching_one_end", touching);
            if (touching.PositiveVolumeCutCount != 0)
                throw new InvalidOperationException("touching_one_end_unexpected_volume_cut");
            if (touching.ContactProbeCutCount < 1)
                throw new InvalidOperationException("touching_one_end_contact_probe_not_used");
            result["case.touching_one_end"] = "PASS";
            result["touching.gross_m2"] = Format(touching.GrossVerticalAreaM2);
            result["touching.deduction_m2"] = Format(touching.DeductionM2);
            result["touching.residual_m2"] = Format(touching.ResidualVerticalAreaM2);
            result["touching.volume_cuts"] = touching.PositiveVolumeCutCount.ToString(CultureInfo.InvariantCulture);
            result["touching.contact_cuts"] = touching.ContactProbeCutCount.ToString(CultureInfo.InvariantCulture);
            result["touching.failed_native"] = touching.FailedNativeCutCount.ToString(CultureInfo.InvariantCulture);

            var penetration = RunMeasureCase(document, -100d, 150d);
            RequireCommon("penetration_005m", penetration);
            if (penetration.PositiveVolumeCutCount < 1)
                throw new InvalidOperationException("penetration_005m_positive_volume_path_not_used");
            result["case.penetration_005m"] = "PASS";
            result["penetration.gross_m2"] = Format(penetration.GrossVerticalAreaM2);
            result["penetration.deduction_m2"] = Format(penetration.DeductionM2);
            result["penetration.residual_m2"] = Format(penetration.ResidualVerticalAreaM2);
            result["penetration.volume_cuts"] = penetration.PositiveVolumeCutCount.ToString(CultureInfo.InvariantCulture);
            result["penetration.contact_cuts"] = penetration.ContactProbeCutCount.ToString(CultureInfo.InvariantCulture);
            result["penetration.failed_native"] = penetration.FailedNativeCutCount.ToString(CultureInfo.InvariantCulture);

            return result;
        }

        private static Measurement RunMeasureCase(Document document, double neighborX, double neighborDx)
        {
            var created = new List<ObjectId>();
            try
            {
                var wall = CreateBox(document, 0d, -100d, 0d, 1468d, 200d, 800d);
                var neighbor = CreateBox(document, neighborX, -100d, 0d, neighborDx, 200d, 800d);
                created.Add(wall.Id);
                created.Add(neighbor.Id);

                var project = new ProjectState("local-3681-gate-" + Guid.NewGuid().ToString("N"), "LOCAL 3681 SOURCE FIX GATE");
                var semanticWall = new ProjectElement("wall", ElementCategory.StructuralWall);
                semanticWall.SourceHandles.Add(wall.Handle);
                semanticWall.Properties["LengthM"] = "1.468";
                semanticWall.Properties["ThicknessM"] = "0.2";
                semanticWall.Properties["HeightM"] = "0.8";
                project.Elements.Add(semanticWall);

                var semanticNeighbor = new ProjectElement("neighbor", ElementCategory.Column);
                semanticNeighbor.SourceHandles.Add(neighbor.Handle);
                project.Elements.Add(semanticNeighbor);

                return Measure(document, project, semanticWall);
            }
            finally
            {
                Erase(document, created);
            }
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
                VerticalFaceSeedCount = ReadInt(diagnostics, "VerticalFaceSeedCount"),
                PositiveVolumeCutCount = ReadInt(diagnostics, "PositiveVolumeCutCount"),
                ContactProbeCutCount = ReadInt(diagnostics, "ContactProbeCutCount"),
                FailedNativeCutCount = ReadInt(diagnostics, "FailedNativeCutCount"),
                GrossVerticalAreaM2 = ReadDouble(diagnostics, "GrossVerticalAreaM2"),
                ResidualVerticalAreaM2 = ReadDouble(diagnostics, "ResidualVerticalAreaM2")
            };
        }

        private static void RequireCommon(string label, Measurement measurement)
        {
            if (!measurement.Available) throw new InvalidOperationException(label + "_unavailable");
            if (measurement.VerticalFaceSeedCount < 4) throw new InvalidOperationException(label + "_face_seed_count");
            if (measurement.FailedNativeCutCount != 0) throw new InvalidOperationException(label + "_native_failure");
            if (!Near(measurement.GrossVerticalAreaM2, ExpectedGrossM2)) throw new InvalidOperationException(label + "_gross_area");
            if (!Near(measurement.DeductionM2, ExpectedOneEndM2)) throw new InvalidOperationException(label + "_deduction");
            if (!Near(measurement.ResidualVerticalAreaM2, ExpectedOneEndNetM2)) throw new InvalidOperationException(label + "_residual");
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
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, z)));
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

        private static void RequireMillimeterDrawing(Document document)
        {
            if ((int)document.Database.Insunits != 4)
                throw new InvalidOperationException("drawing_units_must_be_millimeters");
        }

        private static bool Near(double left, double right)
        {
            return Math.Abs(left - right) <= ToleranceM2;
        }

        private static string Format(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, string> NewResult()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["schema"] = "qs3d-local-3681-source-fix-gate-v1",
                ["phase"] = "source_fix_gate",
                ["status"] = "PASS"
            };
        }

        private static void Execute(Func<Document, IDictionary<string, string>> action)
        {
            var path = Environment.GetEnvironmentVariable(ResultEnvironmentVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
            {
                try { Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D 3681 SOURCE FIX GATE FAIL result_path_missing."); } catch { }
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
                    ["schema"] = "qs3d-local-3681-source-fix-gate-v1",
                    ["phase"] = "source_fix_gate",
                    ["status"] = "FAIL",
                    ["error_type"] = error.GetType().Name,
                    ["error_code"] = SafeCode(Unwrap(error).Message)
                };
            }

            WriteMarker(path, result);
            try
            {
                var status = result.TryGetValue("status", out var value) ? value : "FAIL";
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D 3681 SOURCE FIX GATE " + status + ".");
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
