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
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-003 boundary probe. It proves legacy native Z on a
    /// disposable synthetic drawing and proves configured Level placement stays
    /// fail-closed before native replacement while qualification is incomplete.
    /// </summary>
    public sealed class LevelZRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_LEVEL_Z_RESULT";
        private const string NonceVariable = "QS3D_LEVEL_Z_NONCE";
        private const string ResultFileName = "level-z-runtime-result.txt";

        private sealed class VerticalBounds
        {
            public double MinZ { get; set; }
            public double MaxZ { get; set; }
        }

        [CommandMethod("QS3DLEVELZPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Level-Z runtime probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Level-Z runtime nonce is invalid.");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Level-Z runtime result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!document.Name.EndsWith(".level-z-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Level-Z runtime probe requires a guarded disposable drawing copy.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                if (project.Elements.Count != 0)
                    throw new InvalidOperationException("Level-Z runtime probe requires a project with no semantic elements.");

                var sourceId = CreateBeamSource(document);
                var sourceHandle = sourceId.Handle.ToString();
                var element = new ProjectElement("level-z-probe-beam", ElementCategory.Beam, string.Empty, project.ActiveFloorId, project.ActiveZoneId);
                element.SourceHandles.Add(sourceHandle);
                element.Properties["WidthM"] = "0.3";
                element.Properties["HeightM"] = "3";
                element.Properties["BottomOffsetM"] = "0.2";
                project.Elements.Add(element);
                project.Touch();

                document.Editor.SetImpliedSelection(new[] { sourceId });
                if (StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Beam) != 1)
                    throw new InvalidOperationException("Legacy Level-Z probe did not build exactly one Beam solid.");
                var generatedHandle = RequiredGeneratedHandle(element);
                var legacyBounds = ReadSolidBoundsM(document, generatedHandle);
                Near(0.2d, legacyBounds.MinZ, "legacy minimum Z");
                Near(3.2d, legacyBounds.MaxZ, "legacy maximum Z");

                ProjectFloorService.Create(project, "level-z-bottom", "Probe Bottom", 3d);
                ProjectFloorService.Create(project, "level-z-top", "Probe Top", 7d);
                if (ProjectFloorService.AssignBottomLevel(project, "level-z-bottom", new[] { element }) != 1 ||
                    ProjectFloorService.AssignTopLevel(project, "level-z-top", new[] { element }) != 1)
                    throw new InvalidOperationException("Level-Z probe could not assign its bounded Level references.");

                var blocked = false;
                document.Editor.SetImpliedSelection(new[] { sourceId });
                try { StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Beam); }
                catch (InvalidOperationException) { blocked = true; }
                if (!blocked) throw new InvalidOperationException("Unqualified Level placement unexpectedly reached native replacement.");

                var retainedHandle = RequiredGeneratedHandle(element);
                if (!string.Equals(generatedHandle, retainedHandle, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Fail-closed Level rebuild changed generated ownership.");
                var retainedBounds = ReadSolidBoundsM(document, retainedHandle);
                Near(legacyBounds.MinZ, retainedBounds.MinZ, "retained minimum Z");
                Near(legacyBounds.MaxZ, retainedBounds.MaxZ, "retained maximum Z");

                var live = CadHandleService.GetLiveSolidHandles(document, new[] { retainedHandle });
                if (live.Count != 1) throw new InvalidOperationException("Legacy Beam solid was not retained after the blocked rebuild.");
                var pendingIssues = new LevelReferenceHealthService().Inspect(project)
                    .Count(x => x.Code == "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING" &&
                                string.Equals(x.ElementId, element.Id, StringComparison.OrdinalIgnoreCase));
                if (pendingIssues != 1)
                    throw new InvalidOperationException("Level qualification health did not remain release-blocking.");

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DLEVELZPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_LEVEL_Z_RUNTIME_V1",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "legacy_solid_count=1",
                    "legacy_min_z_m=" + legacyBounds.MinZ.ToString("R", CultureInfo.InvariantCulture),
                    "legacy_max_z_m=" + legacyBounds.MaxZ.ToString("R", CultureInfo.InvariantCulture),
                    "level_rebuild_blocked=true",
                    "retained_solid_count=1",
                    "ownership_unchanged=true",
                    "pending_health_count=1",
                    "production_level_qualified=false"
                });
                document.Editor.WriteMessage("\nQS3D Level-Z boundary runtime probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(requestedPath);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Level-Z runtime probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static ObjectId CreateBeamSource(Document document)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var length = CadGeometryGuard.ToDrawingUnits(document, 5d, "Level-Z probe source length");
                var line = new Line(Point3d.Origin, new Point3d(length, 0d, 0d));
                try
                {
                    line.SetDatabaseDefaults(document.Database);
                    var id = modelSpace.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    transaction.Commit();
                    line = null!;
                    return id;
                }
                finally { line?.Dispose(); }
            }
        }

        private static string RequiredGeneratedHandle(ProjectElement element)
        {
            if (!element.Properties.TryGetValue("GeneratedSolidHandle", out var raw))
                throw new InvalidDataException("GeneratedSolidHandle is missing.");
            return CadHandleService.NormalizeHexHandle(raw)
                ?? throw new InvalidDataException("GeneratedSolidHandle is invalid.");
        }

        private static VerticalBounds ReadSolidBoundsM(Document document, string handle)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException("Level-Z probe could not resolve one generated solid.");
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased) throw new InvalidOperationException("Level-Z generated output is not a live Solid3d.");
                var extents = solid.GeometricExtents;
                var min = CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, "Level-Z minimum extent");
                var max = CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, "Level-Z maximum extent");
                transaction.Commit();
                return new VerticalBounds { MinZ = min, MaxZ = max };
            }
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-6d)
                throw new InvalidOperationException(label + " mismatch.");
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Level-Z runtime result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Level-Z runtime result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? requestedPath)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL",
                        "command=QS3DLEVELZPROBE",
                        "error_code=LEVEL_Z_RUNTIME_FAILED"
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Level-Z runtime result already exists.");
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
