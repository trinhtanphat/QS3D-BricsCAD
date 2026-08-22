using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Licensed V25 proof for #1505 / LOCAL-003. The companion PowerShell runner opens a
    /// guarded disposable DWG copy and this command exercises the real snapshot reader and
    /// StructuralSolidBuilder. Marker output is intentionally sanitized: no handles, paths,
    /// project ids, source coordinates, or exception messages are emitted.
    /// </summary>
    public sealed class CurvedStructuralRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURVED_STRUCTURAL_RESULT";
        private const string NonceVariable = "QS3D_CURVED_STRUCTURAL_NONCE";
        private const string SourceShaVariable = "QS3D_CURVED_STRUCTURAL_SOURCE_SHA";
        private const string ResultFileName = "curved-structural-runtime-result.txt";

        private sealed class SourceReference
        {
            public ObjectId ObjectId { get; set; }
            public string Handle { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public ElementCategory Category { get; set; }
            public double HeightM { get; set; }
            public double? ExpectedLengthM { get; set; }
            public double? ExpectedAreaM2 { get; set; }
        }

        private sealed class ProbeSources
        {
            public List<SourceReference> Positive { get; } = new List<SourceReference>();
            public SourceReference ClosedBeamPolyline { get; set; } = null!;
            public SourceReference NonWcsBeamCircle { get; set; } = null!;
        }

        private sealed class Measurement
        {
            public string Name { get; set; } = string.Empty;
            public double? LengthM { get; set; }
            public double? AreaM2 { get; set; }
            public double VolumeM3 { get; set; }
            public double MinimumXM { get; set; }
            public double MaximumXM { get; set; }
            public double MinimumYM { get; set; }
            public double MaximumYM { get; set; }
            public double MinimumZM { get; set; }
            public double MaximumZM { get; set; }
        }

        [CommandMethod("QS3DCURVEDSTRUCTURALPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curved structural runtime probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            var failureStage = "context";
            var activeCase = "none";
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Curved structural runtime nonce is invalid.");
                var sourceSha = (Environment.GetEnvironmentVariable(SourceShaVariable) ?? string.Empty).Trim().ToLowerInvariant();
                if (sourceSha.Length != 40 || sourceSha.Any(x => !Uri.IsHexDigit(x)))
                    throw new InvalidOperationException("Curved structural runtime source SHA is invalid.");
                RequireAssemblyRevision(typeof(CurvedStructuralRuntimeProbeCommands).Assembly, sourceSha, "QS3D.BricsCAD.V25");
                RequireAssemblyRevision(typeof(ProjectState).Assembly, sourceSha, "QS3D.Core");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Curved structural runtime result already exists.");
                if (!Environment.Is64BitProcess) throw new InvalidOperationException("Curved structural runtime probe requires a 64-bit BricsCAD process.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!document.Name.EndsWith(".curved-structural-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Curved structural runtime probe requires a guarded disposable drawing copy.");
                if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                    throw new InvalidOperationException("Curved structural runtime probe requires a supported native drawing unit.");

                failureStage = "source_creation";
                var sources = CreateSources(document);
                var project = new ProjectState("curved-structural-runtime", "Curved Structural Runtime Probe");
                project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
                project.ActiveFloorId = "L0";
                DrawingUnitResolutionPolicy.BindQuantityUnit(
                    project.Metadata,
                    false,
                    nativeUnit,
                    DrawingUnitResolutionSource.NativeInsunits);

                var measurements = new List<Measurement>();
                failureStage = "positive_cases";
                foreach (var source in sources.Positive)
                {
                    activeCase = source.Name;
                    measurements.Add(ExercisePositive(document, project, source));
                }

                failureStage = "fail_closed";
                activeCase = "closed_beam_polyline";
                VerifyFailClosed(document, project, sources.ClosedBeamPolyline);
                activeCase = "non_wcs_beam_circle";
                VerifyFailClosed(document, project, sources.NonWcsBeamCircle);

                failureStage = "marker";
                var lines = new List<string>
                {
                    "status=PASS",
                    "command=QS3DCURVEDSTRUCTURALPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "source_sha=" + sourceSha,
                    "schema=QS3D_CURVED_STRUCTURAL_RUNTIME_V1",
                    "is_64bit=true",
                    "native_drawing_unit=" + nativeUnit.ToString(),
                    "positive_case_count=" + measurements.Count.ToString(CultureInfo.InvariantCulture),
                    "rebuild_count=" + measurements.Count.ToString(CultureInfo.InvariantCulture),
                    "closed_beam_polyline_fail_closed=true",
                    "non_wcs_beam_circle_fail_closed=true"
                };
                foreach (var measurement in measurements) AddMeasurement(lines, measurement);
                WriteMarkerAtomic(resultPath, lines);
                document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                document.Editor.WriteMessage("\nQS3D curved structural runtime probe PASS.");
            }
            catch (System.Exception error)
            {
                TryWriteFailure(requestedPath, failureStage, activeCase, error);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D curved structural runtime probe FAIL. See the local qualification marker.");
            }
        }

        private static ProbeSources CreateSources(Document document)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var unitsPerMeter = CadGeometryGuard.ToDrawingUnits(document, 1d, "curved structural probe meter scale");
                var result = new ProbeSources();

                result.Positive.Add(AppendLine(
                    document, transaction, modelSpace, "beam_line", ElementCategory.Beam, .5d,
                    AtMeters(unitsPerMeter, 0d, 0d, 0d), AtMeters(unitsPerMeter, 4d, 0d, 0d), 4d));
                result.Positive.Add(AppendArc(
                    document, transaction, modelSpace, "beam_arc", ElementCategory.Beam, .5d,
                    AtMeters(unitsPerMeter, 10d, 0d, 0d), 3d * unitsPerMeter, 0d, Math.PI / 2d, 3d * Math.PI / 2d));
                result.Positive.Add(AppendCircle(
                    document, transaction, modelSpace, "beam_circle", ElementCategory.Beam, .5d,
                    AtMeters(unitsPerMeter, 20d, 0d, 0d), Vector3d.ZAxis, 2d * unitsPerMeter, 4d * Math.PI, null));
                result.Positive.Add(AppendPolyline(
                    document, transaction, modelSpace, "beam_polyline_straight", ElementCategory.Beam, .5d, 0d,
                    unitsPerMeter,
                    new[] { new Point2d(30d, 0d), new Point2d(34d, 0d), new Point2d(36d, 2d) },
                    new[] { 0d, 0d },
                    4d + Math.Sqrt(8d)));
                result.Positive.Add(AppendPolyline(
                    document, transaction, modelSpace, "beam_polyline_curved", ElementCategory.Beam, .5d, 0d,
                    unitsPerMeter,
                    new[] { new Point2d(40d, 0d), new Point2d(44d, 0d), new Point2d(46d, 2d) },
                    new[] { Math.Tan(Math.PI / 8d), 0d },
                    Math.PI * Math.Sqrt(2d) + Math.Sqrt(8d)));
                result.Positive.Add(AppendCircle(
                    document, transaction, modelSpace, "slab_circle", ElementCategory.Slab, .2d,
                    AtMeters(unitsPerMeter, 0d, 10d, 0d), Vector3d.ZAxis, 2d * unitsPerMeter, null, 4d * Math.PI));
                result.Positive.Add(AppendCircle(
                    document, transaction, modelSpace, "column_circle", ElementCategory.Column, 3d,
                    AtMeters(unitsPerMeter, 10d, 10d, 0d), Vector3d.ZAxis, .4d * unitsPerMeter, null, .16d * Math.PI));

                result.ClosedBeamPolyline = AppendPolyline(
                    document, transaction, modelSpace, "closed_beam_polyline", ElementCategory.Beam, .5d, 0d,
                    unitsPerMeter,
                    new[] { new Point2d(20d, 10d), new Point2d(23d, 10d), new Point2d(23d, 13d) },
                    new[] { 0d, 0d },
                    null,
                    true);
                result.NonWcsBeamCircle = AppendCircle(
                    document, transaction, modelSpace, "non_wcs_beam_circle", ElementCategory.Beam, .5d,
                    AtMeters(unitsPerMeter, 30d, 10d, 0d), Vector3d.YAxis, 1d * unitsPerMeter, null, null);

                transaction.Commit();
                return result;
            }
        }

        private static Measurement ExercisePositive(Document document, ProjectState project, SourceReference source)
        {
            var snapshots = EntitySnapshotReader.ReadHandles(document, new[] { source.Handle });
            Require(snapshots.Count == 1, source.Name + " snapshot count");
            var snapshot = snapshots[0];
            Require(!snapshot.HasQs3dGeneratedOwnershipMarker, source.Name + " source must not carry a generated ownership marker");
            double? observedLengthM = null;
            double? observedAreaM2 = null;
            var unitsPerMeter = CadGeometryGuard.ToDrawingUnits(document, 1d, source.Name + "/meter scale");
            var capturedLengthDrawing = snapshot.LengthDrawingUnits;
            if (source.ExpectedLengthM.HasValue)
            {
                Require(capturedLengthDrawing.HasValue, source.Name + " captured Length");
                observedLengthM = CadGeometryGuard.ToMeters(document, capturedLengthDrawing.GetValueOrDefault(), source.Name + "/captured Length");
                RequireNear(source.ExpectedLengthM.Value, observedLengthM.Value, source.Name + " captured Length");
            }
            var capturedAreaDrawing = snapshot.AreaDrawingUnitsSquared;
            if (source.ExpectedAreaM2.HasValue)
            {
                Require(capturedAreaDrawing.HasValue, source.Name + " captured Area");
                observedAreaM2 = capturedAreaDrawing.GetValueOrDefault() / (unitsPerMeter * unitsPerMeter);
                RequireFinitePositive(observedAreaM2.Value, source.Name + " captured Area");
                RequireNear(source.ExpectedAreaM2.Value, observedAreaM2.Value, source.Name + " captured Area");
            }

            var element = AddElement(project, source.Name, source.Category, source);
            Configure(element, source.Category, source.HeightM);
            Select(document, source.ObjectId);
            Require(StructuralSolidBuilder.BuildSelected(document, project, source.Category) == 1, source.Name + " first build count");
            var firstHandle = Property(element, "GeneratedSolidHandle");
            Require(firstHandle.Length > 0, source.Name + " first generated ownership");
            var first = ReadGenerated(document, firstHandle, source.Name, source.HeightM);

            Select(document, source.ObjectId);
            Require(StructuralSolidBuilder.BuildSelected(document, project, source.Category) == 1, source.Name + " rebuild count");
            var secondHandle = Property(element, "GeneratedSolidHandle");
            Require(secondHandle.Length > 0, source.Name + " rebuilt ownership");
            Require(!string.Equals(firstHandle, secondHandle, StringComparison.OrdinalIgnoreCase), source.Name + " replacement handle must advance");
            Require(CadHandleService.Resolve(document, new[] { firstHandle }).Count == 0, source.Name + " old generated solid must be retired");
            var second = ReadGenerated(document, secondHandle, source.Name + " rebuilt", source.HeightM);
            RequireNear(first.VolumeM3, second.VolumeM3, source.Name + " rebuild volume continuity");

            return new Measurement
            {
                Name = source.Name,
                LengthM = observedLengthM,
                AreaM2 = observedAreaM2,
                VolumeM3 = second.VolumeM3,
                MinimumXM = second.MinimumXM,
                MaximumXM = second.MaximumXM,
                MinimumYM = second.MinimumYM,
                MaximumYM = second.MaximumYM,
                MinimumZM = second.MinimumZM,
                MaximumZM = second.MaximumZM
            };
        }

        private static void VerifyFailClosed(Document document, ProjectState project, SourceReference source)
        {
            var element = AddElement(project, source.Name, source.Category, source);
            Configure(element, source.Category, source.HeightM);
            Select(document, source.ObjectId);
            var rejected = false;
            try
            {
                StructuralSolidBuilder.BuildSelected(document, project, source.Category);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Require(rejected, source.Name + " must fail closed");
            var restored = project.FindElement(element.Id);
            Require(restored != null, source.Name + " project rollback");
            Require(Property(restored!, "GeneratedSolidHandle").Length == 0, source.Name + " failure must not publish generated ownership");
            Require(CadHandleService.Resolve(document, new[] { source.Handle }).Count == 1, source.Name + " failure must preserve source");
        }

        private static Measurement ReadGenerated(Document document, string handle, string label, double expectedHeightM)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException(label + " must resolve to one generated Solid3d.");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException(label + " generated entity is not a Solid3d.");
                Require(GeneratedNativeSourceGuard.HasKnownOwnershipMarker(solid), label + " generated ownership marker");
                var extents = solid.GeometricExtents;
                var volumeDrawing = solid.MassProperties.Volume;
                var unitsPerMeter = CadGeometryGuard.ToDrawingUnits(document, 1d, label + "/meter scale");
                RequireFinitePositive(volumeDrawing, label + " generated volume");
                var result = new Measurement
                {
                    VolumeM3 = volumeDrawing / (unitsPerMeter * unitsPerMeter * unitsPerMeter),
                    MinimumXM = CadGeometryGuard.ToMeters(document, extents.MinPoint.X, label + "/minimum X"),
                    MaximumXM = CadGeometryGuard.ToMeters(document, extents.MaxPoint.X, label + "/maximum X"),
                    MinimumYM = CadGeometryGuard.ToMeters(document, extents.MinPoint.Y, label + "/minimum Y"),
                    MaximumYM = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Y, label + "/maximum Y"),
                    MinimumZM = CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, label + "/minimum Z"),
                    MaximumZM = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, label + "/maximum Z")
                };
                transaction.Commit();
                RequireFinitePositive(result.VolumeM3, label + " volume m3");
                Require(result.MaximumXM > result.MinimumXM, label + " X span");
                Require(result.MaximumYM > result.MinimumYM, label + " Y span");
                RequireNear(0d, result.MinimumZM, label + " minimum Z");
                RequireNear(expectedHeightM, result.MaximumZM, label + " maximum Z");
                return result;
            }
        }

        private static ProjectElement AddElement(ProjectState project, string id, ElementCategory category, SourceReference source)
        {
            var element = new ProjectElement(id, category, string.Empty, "L0", string.Empty);
            element.SourceHandles.Add(source.Handle);
            project.Elements.Add(element);
            return element;
        }

        private static void Configure(ProjectElement element, ElementCategory category, double heightM)
        {
            switch (category)
            {
                case ElementCategory.Beam:
                    Set(element, "WidthM", .3d);
                    Set(element, "HeightM", heightM);
                    break;
                case ElementCategory.Slab:
                    Set(element, "ThicknessM", heightM);
                    break;
                case ElementCategory.Column:
                    Set(element, "HeightM", heightM);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported curved structural probe category: " + category);
            }
        }

        private static SourceReference AppendLine(
            Document document, Transaction transaction, BlockTableRecord modelSpace,
            string name, ElementCategory category, double heightM, Point3d start, Point3d end, double expectedLengthM)
        {
            var line = new Line(start, end);
            line.SetDatabaseDefaults(document.Database);
            var id = modelSpace.AppendEntity(line);
            transaction.AddNewlyCreatedDBObject(line, true);
            return Reference(id, line.Handle.ToString(), name, category, heightM, expectedLengthM, null);
        }

        private static SourceReference AppendArc(
            Document document, Transaction transaction, BlockTableRecord modelSpace,
            string name, ElementCategory category, double heightM, Point3d center, double radius,
            double startAngle, double endAngle, double expectedLengthM)
        {
            var arc = new Arc(center, radius, startAngle, endAngle);
            arc.SetDatabaseDefaults(document.Database);
            var id = modelSpace.AppendEntity(arc);
            transaction.AddNewlyCreatedDBObject(arc, true);
            return Reference(id, arc.Handle.ToString(), name, category, heightM, expectedLengthM, null);
        }

        private static SourceReference AppendCircle(
            Document document, Transaction transaction, BlockTableRecord modelSpace,
            string name, ElementCategory category, double heightM, Point3d center, Vector3d normal, double radius,
            double? expectedLengthM, double? expectedAreaM2)
        {
            var circle = new Circle(center, normal, radius);
            circle.SetDatabaseDefaults(document.Database);
            var id = modelSpace.AppendEntity(circle);
            transaction.AddNewlyCreatedDBObject(circle, true);
            return Reference(id, circle.Handle.ToString(), name, category, heightM, expectedLengthM, expectedAreaM2);
        }

        private static SourceReference AppendPolyline(
            Document document, Transaction transaction, BlockTableRecord modelSpace,
            string name, ElementCategory category, double heightM, double elevationM, double unitsPerMeter,
            IReadOnlyList<Point2d> pointsM, IReadOnlyList<double> bulges, double? expectedLengthM, bool closed = false)
        {
            if (pointsM.Count < 2 || bulges.Count != pointsM.Count - 1)
                throw new InvalidOperationException(name + " polyline fixture is invalid.");
            var polyline = new Polyline();
            polyline.SetDatabaseDefaults(document.Database);
            polyline.Normal = Vector3d.ZAxis;
            polyline.Elevation = elevationM * unitsPerMeter;
            for (var index = 0; index < pointsM.Count; index++)
            {
                var point = pointsM[index];
                var bulge = index < bulges.Count ? bulges[index] : 0d;
                polyline.AddVertexAt(index, new Point2d(point.X * unitsPerMeter, point.Y * unitsPerMeter), bulge, 0d, 0d);
            }
            polyline.Closed = closed;
            var id = modelSpace.AppendEntity(polyline);
            transaction.AddNewlyCreatedDBObject(polyline, true);
            return Reference(id, polyline.Handle.ToString(), name, category, heightM, expectedLengthM, null);
        }

        private static SourceReference Reference(
            ObjectId objectId, string handle, string name, ElementCategory category, double heightM,
            double? expectedLengthM, double? expectedAreaM2) =>
            new SourceReference
            {
                ObjectId = objectId,
                Handle = handle,
                Name = name,
                Category = category,
                HeightM = heightM,
                ExpectedLengthM = expectedLengthM,
                ExpectedAreaM2 = expectedAreaM2
            };

        private static Point3d AtMeters(double unitsPerMeter, double x, double y, double z) =>
            new Point3d(x * unitsPerMeter, y * unitsPerMeter, z * unitsPerMeter);

        private static void Select(Document document, ObjectId id) => document.Editor.SetImpliedSelection(new[] { id });

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var value) ? (value ?? string.Empty).Trim() : string.Empty;

        private static void Set(ProjectElement element, string key, double value) =>
            element.Properties[key] = Number(value);

        private static void AddMeasurement(ICollection<string> lines, Measurement measurement)
        {
            var prefix = measurement.Name;
            if (measurement.LengthM.HasValue) lines.Add(prefix + "_length_m=" + Number(measurement.LengthM.Value));
            if (measurement.AreaM2.HasValue) lines.Add(prefix + "_area_m2=" + Number(measurement.AreaM2.Value));
            lines.Add(prefix + "_volume_m3=" + Number(measurement.VolumeM3));
            lines.Add(prefix + "_min_x_m=" + Number(measurement.MinimumXM));
            lines.Add(prefix + "_max_x_m=" + Number(measurement.MaximumXM));
            lines.Add(prefix + "_min_y_m=" + Number(measurement.MinimumYM));
            lines.Add(prefix + "_max_y_m=" + Number(measurement.MaximumYM));
            lines.Add(prefix + "_min_z_m=" + Number(measurement.MinimumZM));
            lines.Add(prefix + "_max_z_m=" + Number(measurement.MaximumZM));
        }

        private static void RequireNear(double expected, double actual, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual)) throw new InvalidOperationException(label + " must be finite.");
            var tolerance = Math.Max(1e-7d, Math.Max(Math.Abs(expected), Math.Abs(actual)) * 1e-6d);
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException(label + " mismatch.");
        }

        private static void RequireFinitePositive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new InvalidOperationException(label + " must be finite and positive.");
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Curved structural runtime assertion failed: " + label + ".");
        }

        private static void RequireAssemblyRevision(Assembly assembly, string sourceSha, string label)
        {
            var informationalVersion = assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .Select(x => x.InformationalVersion ?? string.Empty)
                .FirstOrDefault() ?? string.Empty;
            if (!informationalVersion.EndsWith("+" + sourceSha, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(label + " assembly revision does not match exact source SHA.");
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Curved structural runtime result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Curved structural runtime result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? requestedPath, string failureStage, string activeCase, System.Exception error)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length == 0 || File.Exists(normalized)) return;
                WriteMarkerAtomic(normalized, new[]
                {
                    "status=FAIL",
                    "command=QS3DCURVEDSTRUCTURALPROBE",
                    "error_stage=" + SafeStage(failureStage),
                    "error_case=" + SafeCase(activeCase),
                    "exception_type=" + OneLine(error.GetType().FullName ?? error.GetType().Name),
                    "exception_hresult=0x" + error.HResult.ToString("X8", CultureInfo.InvariantCulture)
                });
            }
            catch { }
        }

        private static string SafeStage(string value)
        {
            switch (value)
            {
                case "context":
                case "source_creation":
                case "positive_cases":
                case "fail_closed":
                case "marker":
                    return value;
                default:
                    return "context";
            }
        }

        private static string SafeCase(string value)
        {
            var allowed = new[]
            {
                "none", "beam_line", "beam_arc", "beam_circle", "beam_polyline_straight",
                "beam_polyline_curved", "slab_circle", "column_circle", "closed_beam_polyline",
                "non_wcs_beam_circle"
            };
            return allowed.Contains(value, StringComparer.Ordinal) ? value : "none";
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Curved structural runtime result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();

        private static string Number(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }
}
