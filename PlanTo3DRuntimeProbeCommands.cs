using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-014 P01 probe. It seeds two simple LINE sources in a
    /// disposable synthetic drawing, invokes the real quick Plan-to-3D command,
    /// and emits only sanitized aggregate evidence. It is not a user command and
    /// does not qualify the advanced/cancel/rollback matrix.
    /// </summary>
    public sealed class PlanTo3DRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_PLAN_TO_3D_RESULT";
        private const string NonceVariable = "QS3D_PLAN_TO_3D_NONCE";
        private const string ResultFileName = "plan-to-3d-runtime-result.txt";
        private const double DrawingUnitsPerMeter = 1000d;
        private const double ToleranceM = 0.000001d;

        private sealed class ProbeFailureException : InvalidOperationException
        {
            public ProbeFailureException(string code) : base(code) => Code = code;
            public string Code { get; }
        }

        private sealed class SeedLine
        {
            public SeedLine(ObjectId id, Point3d start, Point3d end)
            {
                Id = id;
                Start = start;
                End = end;
                Handle = CadHandleService.NormalizeHexHandle(id.Handle.ToString())
                    ?? throw new InvalidDataException("Seed LINE produced an invalid CAD handle.");
            }

            public ObjectId Id { get; }
            public Point3d Start { get; }
            public Point3d End { get; }
            public string Handle { get; }
            public double LengthM => Start.DistanceTo(End) / DrawingUnitsPerMeter;
        }

        [CommandMethod("QS3DPLAN2DPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Plan-to-3D runtime probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            var failureCode = "PLAN_TO_3D_RUNTIME_CONTEXT_FAILED";
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Plan-to-3D runtime nonce is invalid.");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Plan-to-3D runtime result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!document.Name.EndsWith(".plan-to-3d-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Plan-to-3D runtime probe requires a guarded disposable drawing copy.");
                RequireModelSpace(document);
                if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit) || nativeUnit != LengthUnit.Millimeter)
                    throw new InvalidOperationException("Plan-to-3D P01 requires a native millimeter drawing.");
                if (ProjectContextCoordinator.TryGetReadOnly(document, out var existingProject) && existingProject.Elements.Count != 0)
                    throw new InvalidOperationException("Plan-to-3D P01 requires a drawing without existing semantic elements.");

                failureCode = "PLAN_TO_3D_RUNTIME_SEED_FAILED";
                var seeds = CreateSeedLines(document);
                if (seeds.Count != 2) throw new InvalidOperationException("Plan-to-3D P01 must seed exactly two LINE sources.");
                document.Editor.SetImpliedSelection(seeds.Select(x => x.Id).ToArray());

                failureCode = "PLAN_TO_3D_RUNTIME_COMMAND_FAILED";
                new PlanTo3DCommands().Convert2D();

                failureCode = "PLAN_TO_3D_RUNTIME_PROJECT_FAILED";
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Plan-to-3D did not create its canonical project.");
                var walls = project.Elements.Where(x =>
                    x.Category == ElementCategory.ArchitecturalWall &&
                    x.Properties.TryGetValue("QS3D.PlanTo3D", out var marker) &&
                    string.Equals((marker ?? string.Empty).Trim(), "1", StringComparison.Ordinal)).ToList();
                if (walls.Count != seeds.Count || project.Elements.Count != seeds.Count)
                    throw new InvalidOperationException("Plan-to-3D did not create exactly one semantic wall per seed source.");

                failureCode = "PLAN_TO_3D_RUNTIME_SOURCE_GEOMETRY_FAILED";
                RequireSeedGeometryUnchanged(document, seeds);
                var seedHandles = new HashSet<string>(seeds.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var claimedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var generatedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var wall in walls)
                {
                    failureCode = "PLAN_TO_3D_RUNTIME_SOURCE_OWNERSHIP_FAILED";
                    if (wall.SourceHandles.Count != 1)
                        throw new InvalidOperationException("A Plan-to-3D wall does not own exactly one source LINE.");
                    var sourceHandle = CadHandleService.NormalizeHexHandle(wall.SourceHandles[0])
                        ?? throw new InvalidDataException("A Plan-to-3D source handle is invalid.");
                    if (!seedHandles.Contains(sourceHandle) || !claimedSources.Add(sourceHandle))
                        throw new InvalidOperationException("Plan-to-3D source ownership is incomplete or duplicated.");

                    failureCode = "PLAN_TO_3D_RUNTIME_FALLBACK_VALUES_FAILED";
                    RequireNumber(wall, "ThicknessM", 0.2d);
                    RequireNumber(wall, "HeightM", 3d);
                    RequireNumber(wall, "BottomOffsetM", 0d);

                    failureCode = "PLAN_TO_3D_RUNTIME_GENERATED_METADATA_FAILED";
                    if (!wall.Properties.TryGetValue("GeneratedSolidHandle", out var rawGenerated))
                        throw new InvalidOperationException("A Plan-to-3D wall is missing GeneratedSolidHandle.");
                    var generatedHandle = CadHandleService.NormalizeHexHandle(rawGenerated)
                        ?? throw new InvalidDataException("A Plan-to-3D generated handle is invalid.");
                    if (seedHandles.Contains(generatedHandle) || !generatedHandles.Add(generatedHandle))
                        throw new InvalidOperationException("Plan-to-3D generated ownership overlaps or is duplicated.");

                    failureCode = "PLAN_TO_3D_RUNTIME_GENERATED_OWNERSHIP_FAILED";
                    var owned = GeneratedGeometryService.FindMatchingOwnedHandles(
                        document,
                        project.ProjectId,
                        wall.Id,
                        wall.Category)
                        .Select(CadHandleService.NormalizeHexHandle)
                        .Where(x => x != null)
                        .Cast<string>()
                        .ToList();
                    if (owned.Count != 1 || !string.Equals(owned[0], generatedHandle, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Generated metadata and native ownership marker disagree.");

                    failureCode = "PLAN_TO_3D_RUNTIME_NATIVE_BOUNDS_FAILED";
                    var seed = seeds.Single(x => string.Equals(x.Handle, sourceHandle, StringComparison.OrdinalIgnoreCase));
                    RequireGeneratedSolidBounds(document, project, wall, generatedHandle, seed.LengthM);
                }
                failureCode = "PLAN_TO_3D_RUNTIME_SOURCE_SET_FAILED";
                if (!claimedSources.SetEquals(seedHandles))
                    throw new InvalidOperationException("Plan-to-3D did not retain every seeded source as canonical provenance.");

                failureCode = "PLAN_TO_3D_RUNTIME_HEALTH_FAILED";
                var liveSources = new HashSet<string>(claimedSources, StringComparer.OrdinalIgnoreCase);
                var liveGenerated = CadHandleService.GetLiveSolidHandles(document, generatedHandles);
                if (liveGenerated.Count != generatedHandles.Count)
                    throw new InvalidOperationException("One or more Plan-to-3D generated outputs are not live Solid3d objects.");
                var coreHealthErrors = new ModelHealthService()
                    .Inspect(project, liveSources, liveGenerated)
                    .Count(x => x.Severity == HealthSeverity.Error);
                var runtimeHealthErrors = GeneratedSolidRuntimeHealthService
                    .Inspect(document, project)
                    .Count(x => x.Severity == HealthSeverity.Error);
                if (coreHealthErrors != 0 || runtimeHealthErrors != 0)
                    throw new InvalidOperationException("Plan-to-3D P01 health contains blocking errors.");

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DPLAN2DPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_PLAN_TO_3D_RUNTIME_V1",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "native_unit=Millimeter",
                    "source_line_count=" + seeds.Count.ToString(CultureInfo.InvariantCulture),
                    "semantic_wall_count=" + walls.Count.ToString(CultureInfo.InvariantCulture),
                    "generated_solid_count=" + liveGenerated.Count.ToString(CultureInfo.InvariantCulture),
                    "fallback_thickness_m=0.2",
                    "fallback_height_m=3",
                    "fallback_bottom_offset_m=0",
                    "source_geometry_retained=true",
                    "ownership_sets_disjoint=true",
                    "native_bounds_verified=true",
                    "core_health_error_count=0",
                    "runtime_health_error_count=0",
                    "qualification_boundary=P01_QUICK_POSITIVE_ONLY",
                    "production_local014_qualified=false"
                });
                document.Editor.WriteMessage("\nQS3D Plan-to-3D P01 runtime probe PASS.");
            }
            catch (System.Exception error)
            {
                if (error is ProbeFailureException probeFailure) failureCode = probeFailure.Code;
                TryWriteFailure(requestedPath, nonce, failureCode);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Plan-to-3D P01 runtime probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static IReadOnlyList<SeedLine> CreateSeedLines(Document document)
        {
            var specs = new[]
            {
                new[] { new Point3d(0d, 0d, 0d), new Point3d(4000d, 0d, 0d) },
                new[] { new Point3d(0d, 2000d, 0d), new Point3d(2500d, 2000d, 0d) }
            };
            var result = new List<SeedLine>(specs.Length);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var spec in specs)
                {
                    var line = new Line(spec[0], spec[1]);
                    var id = modelSpace.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    result.Add(new SeedLine(id, spec[0], spec[1]));
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private static void RequireSeedGeometryUnchanged(Document document, IEnumerable<SeedLine> seeds)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var seed in seeds)
                {
                    var line = transaction.GetObject(seed.Id, OpenMode.ForRead, false) as Line;
                    if (line == null || line.IsErased || !Near(line.StartPoint, seed.Start) || !Near(line.EndPoint, seed.End))
                        throw new InvalidOperationException("Plan-to-3D changed or erased a user-owned source LINE.");
                }
                transaction.Commit();
            }
        }

        private static void RequireGeneratedSolidBounds(
            Document document,
            ProjectState project,
            ProjectElement wall,
            string generatedHandle,
            double expectedLengthM)
        {
            var ids = CadHandleService.Resolve(document, new[] { generatedHandle });
            if (ids.Count != 1) throw new ProbeFailureException("PLAN_TO_3D_RUNTIME_NATIVE_HANDLE_RESOLUTION_FAILED");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased)
                    throw new ProbeFailureException("PLAN_TO_3D_RUNTIME_NATIVE_SOLID_TYPE_FAILED");
                if (!GeneratedGeometryService.HasMatchingOwnership(solid, project, wall))
                    throw new ProbeFailureException("PLAN_TO_3D_RUNTIME_NATIVE_XDATA_FAILED");
                var extents = solid.GeometricExtents;
                RequireNear(expectedLengthM, (extents.MaxPoint.X - extents.MinPoint.X) / DrawingUnitsPerMeter, "PLAN_TO_3D_RUNTIME_NATIVE_LENGTH_FAILED");
                RequireNear(0.2d, (extents.MaxPoint.Y - extents.MinPoint.Y) / DrawingUnitsPerMeter, "PLAN_TO_3D_RUNTIME_NATIVE_THICKNESS_FAILED");
                RequireNear(0d, extents.MinPoint.Z / DrawingUnitsPerMeter, "PLAN_TO_3D_RUNTIME_NATIVE_MIN_Z_FAILED");
                RequireNear(3d, extents.MaxPoint.Z / DrawingUnitsPerMeter, "PLAN_TO_3D_RUNTIME_NATIVE_MAX_Z_FAILED");
                transaction.Commit();
            }
        }

        private static void RequireNear(double expected, double actual, string failureCode)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > ToleranceM)
                throw new ProbeFailureException(failureCode);
        }

        private static void RequireNumber(ProjectElement element, string key, double expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException("Plan-to-3D property " + key + " is missing or invalid.");
            Near(expected, value, key);
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                if (document.Database.CurrentSpaceId != blockTable[BlockTableRecord.ModelSpace])
                    throw new InvalidOperationException("Plan-to-3D P01 requires Model Space.");
                transaction.Commit();
            }
        }

        private static bool Near(Point3d left, Point3d right) => left.DistanceTo(right) <= 0.000001d;

        private static void Near(double expected, double actual, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > ToleranceM)
                throw new InvalidOperationException(label + " expected " + expected.ToString("R", CultureInfo.InvariantCulture) + ".");
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D runtime result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Plan-to-3D runtime result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? requestedPath, string nonce, string failureCode)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length == 0 || File.Exists(normalized)) return;
                var lines = new List<string>
                {
                    "status=FAIL",
                    "command=QS3DPLAN2DPROBE",
                    "schema=QS3D_PLAN_TO_3D_RUNTIME_V1",
                    "error_code=" + OneLine(failureCode)
                };
                if (Guid.TryParseExact(nonce, "N", out _)) lines.Add("nonce=" + nonce);
                WriteMarkerAtomic(normalized, lines);
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Plan-to-3D runtime result already exists.");
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
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
