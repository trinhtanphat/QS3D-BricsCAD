using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-014/P02 preparation and verification. Production
    /// QS3DCONVERT2D/QS3DPLAN2WALLS commands remain the mutation boundary; these
    /// helpers only seed/select repository-synthetic state and inspect aggregates.
    /// </summary>
    public sealed class PlanTo3DP02RuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_PLAN_TO_3D_P02_RESULT";
        private const string NonceVariable = "QS3D_PLAN_TO_3D_P02_NONCE";
        private const string ResultFileName = "plan-to-3d-p02-runtime-result.txt";
        private const string PreferredFamilyId = "local014-p02-preferred-wall";
        private const double DrawingUnitsPerMeter = 1000d;
        private const double PreferredThicknessM = 0.31d;
        private const double PreferredHeightM = 4.2d;
        private const double PreferredBottomOffsetM = 0.45d;
        private const double ToleranceM = 0.000001d;

        private enum ProbeSourceKind
        {
            QuickLine,
            AliasOpenPolyline,
            UnrelatedLine
        }

        private sealed class ProbeSource
        {
            public ProbeSource(
                ObjectId id,
                ProbeSourceKind kind,
                IReadOnlyList<Point3d> points,
                string handle)
            {
                Id = id;
                Kind = kind;
                Points = points;
                Handle = handle;
            }

            public ObjectId Id { get; }
            public ProbeSourceKind Kind { get; }
            public IReadOnlyList<Point3d> Points { get; }
            public string Handle { get; }
            public double LengthM => Points[0].DistanceTo(Points[Points.Count - 1]) / DrawingUnitsPerMeter;
        }

        private sealed class ElementSnapshot
        {
            public ElementSnapshot(ProjectElement element)
            {
                Element = element;
                Category = element.Category;
                FamilyId = element.FamilyId;
                FloorId = element.FloorId;
                ZoneId = element.ZoneId;
                DrawingFingerprint = element.DrawingFingerprint;
                SourceHandles = element.SourceHandles.ToList().AsReadOnly();
                DependsOn = element.DependsOn.ToList().AsReadOnly();
                Properties = new Dictionary<string, string>(element.Properties, StringComparer.OrdinalIgnoreCase);
                Quantities = new Dictionary<string, double>(element.Quantities, StringComparer.OrdinalIgnoreCase);
                Dirty = element.Dirty;
                UpdatedUtc = element.UpdatedUtc;
            }

            public ProjectElement Element { get; }
            public ElementCategory Category { get; }
            public string FamilyId { get; }
            public string FloorId { get; }
            public string ZoneId { get; }
            public string DrawingFingerprint { get; }
            public IReadOnlyList<string> SourceHandles { get; }
            public IReadOnlyList<string> DependsOn { get; }
            public IReadOnlyDictionary<string, string> Properties { get; }
            public IReadOnlyDictionary<string, double> Quantities { get; }
            public ElementDirtyFlags Dirty { get; }
            public DateTime UpdatedUtc { get; }
        }

        private sealed class ProbeState
        {
            public Document Document { get; set; } = null!;
            public ProjectState Project { get; set; } = null!;
            public ProbeSource QuickLine { get; set; } = null!;
            public ProbeSource AliasPolyline { get; set; } = null!;
            public ProbeSource UnrelatedLine { get; set; } = null!;
            public ElementSnapshot Unrelated { get; set; } = null!;
        }

        private static ProbeState? _state;

        [CommandMethod("QS3DPLAN2DP02PREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireNonce(nonce);
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Plan-to-3D P02 result already exists.");
                if (_state != null) throw new InvalidOperationException("Plan-to-3D P02 state is already prepared.");

                var document = RequiredDocument();
                RequireModelSpace(document);
                if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit) || nativeUnit != LengthUnit.Millimeter)
                    throw new InvalidOperationException("Plan-to-3D P02 requires a native millimeter drawing.");

                var project = ProjectContextCoordinator.TryGetReadOnly(document, out var existing)
                    ? existing
                    : ProjectContextCoordinator.GetOrCreate(document);
                if (project.Elements.Count != 0)
                    throw new InvalidOperationException("Plan-to-3D P02 requires a project without existing semantic elements.");
                if (project.FindFamily(PreferredFamilyId) != null)
                    throw new InvalidOperationException("Plan-to-3D P02 preferred Family already exists.");

                var family = ProjectFamilyService.Create(
                    project,
                    PreferredFamilyId,
                    "LOCAL-014 P02 Preferred Wall",
                    ElementCategory.ArchitecturalWall);
                ProjectFamilyService.SetProperty(project, family.Id, "ThicknessM", PreferredThicknessM.ToString("R", CultureInfo.InvariantCulture));
                ProjectFamilyService.SetProperty(project, family.Id, "HeightM", PreferredHeightM.ToString("R", CultureInfo.InvariantCulture));
                ProjectFamilyService.SetProperty(project, family.Id, "BottomOffsetM", PreferredBottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                ProjectFamilyActivationService.SetActive(project, family.Id);

                var sources = CreateSources(document);
                var quickLine = sources.Single(x => x.Kind == ProbeSourceKind.QuickLine);
                var aliasPolyline = sources.Single(x => x.Kind == ProbeSourceKind.AliasOpenPolyline);
                var unrelatedLine = sources.Single(x => x.Kind == ProbeSourceKind.UnrelatedLine);

                document.Editor.SetImpliedSelection(new[] { unrelatedLine.Id });
                if (SemanticCaptureService.Capture(document, ElementCategory.Beam) != 1)
                    throw new InvalidOperationException("Plan-to-3D P02 could not seed one unrelated semantic element.");
                var unrelated = project.Elements.SingleOrDefault(x =>
                    x.Category == ElementCategory.Beam &&
                    x.SourceHandles.Any(h => string.Equals(h, unrelatedLine.Handle, StringComparison.OrdinalIgnoreCase)))
                    ?? throw new InvalidOperationException("Plan-to-3D P02 unrelated semantic element is missing.");
                unrelated.SetProperty("LOCAL014.P02.Unrelated", "dirty");
                unrelated.MarkClean(ElementDirtyFlags.All);
                unrelated.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
                project.Touch();
                ProjectFamilyActivationService.SetActive(project, family.Id);

                _state = new ProbeState
                {
                    Document = document,
                    Project = project,
                    QuickLine = quickLine,
                    AliasPolyline = aliasPolyline,
                    UnrelatedLine = unrelatedLine,
                    Unrelated = new ElementSnapshot(unrelated)
                };
                document.Editor.SetImpliedSelection(new[] { quickLine.Id });
                document.Editor.WriteMessage("\nQS3D Plan-to-3D P02 synthetic preparation ready.");
            }
            catch
            {
                _state = null;
                TryWriteFailure(requestedPath, nonce, "PLAN_TO_3D_P02_PREPARE_FAILED");
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Plan-to-3D P02 preparation FAIL. See the local qualification result.");
                throw;
            }
        }

        [CommandMethod("QS3DPLAN2DP02SELECTALIAS", CommandFlags.Modal)]
        public void SelectAliasSource()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireNonce(nonce);
                var state = RequiredState();
                var project = RequiredProject(state);
                var firstWalls = PlanWalls(project);
                if (firstWalls.Count != 1 || project.Elements.Count != 2)
                    throw new InvalidOperationException("The first Plan-to-3D quick command did not create exactly one wall.");
                var first = RequireWallForSource(firstWalls, state.QuickLine.Handle);
                RequirePreferredFamily(first);
                RequireGeneratedHandle(state.Document, project, first, state.QuickLine.Handle);
                if (project.Elements.Any(x => x.SourceHandles.Any(h =>
                    string.Equals(h, state.AliasPolyline.Handle, StringComparison.OrdinalIgnoreCase))))
                    throw new InvalidOperationException("The alias POLYLINE was captured before the alias command.");
                RequireUnrelatedState(state);
                RequireSourcesUnchanged(state);
                state.Document.Editor.SetImpliedSelection(new[] { state.AliasPolyline.Id });
                state.Document.Editor.WriteMessage("\nQS3D Plan-to-3D P02 alias source selected.");
            }
            catch
            {
                _state = null;
                TryWriteFailure(requestedPath, nonce, "PLAN_TO_3D_P02_FIRST_QUICK_FAILED");
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Plan-to-3D P02 first quick command FAIL. See the local qualification result.");
                throw;
            }
        }

        [CommandMethod("QS3DPLAN2DP02VERIFY", CommandFlags.Modal)]
        public void Verify()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            var failureCode = "PLAN_TO_3D_P02_ALIAS_QUICK_FAILED";
            try
            {
                RequireNonce(nonce);
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Plan-to-3D P02 result already exists.");
                var state = RequiredState();
                var project = RequiredProject(state);

                failureCode = "PLAN_TO_3D_P02_PROJECT_FAILED";
                var walls = PlanWalls(project);
                if (walls.Count != 2 || project.Elements.Count != 3)
                    throw new InvalidOperationException("Plan-to-3D P02 did not create exactly two walls plus one unrelated element.");
                var quickWall = RequireWallForSource(walls, state.QuickLine.Handle);
                var aliasWall = RequireWallForSource(walls, state.AliasPolyline.Handle);

                failureCode = "PLAN_TO_3D_P02_FAMILY_FAILED";
                RequirePreferredFamily(quickWall);
                RequirePreferredFamily(aliasWall);

                failureCode = "PLAN_TO_3D_P02_UNRELATED_STATE_FAILED";
                RequireUnrelatedState(state);

                failureCode = "PLAN_TO_3D_P02_SOURCE_GEOMETRY_FAILED";
                RequireSourcesUnchanged(state);

                failureCode = "PLAN_TO_3D_P02_GENERATED_OWNERSHIP_FAILED";
                var sourceHandles = new HashSet<string>(new[]
                {
                    state.QuickLine.Handle,
                    state.AliasPolyline.Handle,
                    state.UnrelatedLine.Handle
                }, StringComparer.OrdinalIgnoreCase);
                var generatedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                generatedHandles.Add(RequireGeneratedHandle(state.Document, project, quickWall, state.QuickLine.Handle));
                generatedHandles.Add(RequireGeneratedHandle(state.Document, project, aliasWall, state.AliasPolyline.Handle));
                if (generatedHandles.Count != 2 || generatedHandles.Overlaps(sourceHandles))
                    throw new InvalidOperationException("Plan-to-3D P02 ownership sets overlap or are incomplete.");

                failureCode = "PLAN_TO_3D_P02_NATIVE_BOUNDS_FAILED";
                RequireGeneratedSolidBounds(state.Document, project, quickWall, generatedHandles, state.QuickLine.LengthM);
                RequireGeneratedSolidBounds(state.Document, project, aliasWall, generatedHandles, state.AliasPolyline.LengthM);

                failureCode = "PLAN_TO_3D_P02_HEALTH_FAILED";
                var liveGenerated = CadHandleService.GetLiveSolidHandles(state.Document, generatedHandles);
                if (liveGenerated.Count != generatedHandles.Count)
                    throw new InvalidOperationException("One or more Plan-to-3D P02 generated outputs are not live Solid3d objects.");
                var wallIds = new HashSet<string>(walls.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                var coreHealthErrors = new ModelHealthService()
                    .Inspect(project, sourceHandles, liveGenerated)
                    .Count(x => x.Severity == HealthSeverity.Error && wallIds.Contains(x.ElementId));
                var runtimeHealthErrors = GeneratedSolidRuntimeHealthService
                    .Inspect(state.Document, project)
                    .Count(x => x.Severity == HealthSeverity.Error && wallIds.Contains(x.ElementId));
                if (coreHealthErrors != 0 || runtimeHealthErrors != 0)
                    throw new InvalidOperationException("Plan-to-3D P02 wall-scoped Health contains blocking errors.");

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DPLAN2DP02VERIFY",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_PLAN_TO_3D_P02_RUNTIME_V1",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "native_unit=Millimeter",
                    "quick_command_count=2",
                    "source_line_count=1",
                    "source_open_polyline_count=1",
                    "semantic_wall_count=2",
                    "generated_solid_count=2",
                    "preferred_family_applied_count=2",
                    "preferred_thickness_m=0.31",
                    "preferred_height_m=4.2",
                    "preferred_bottom_offset_m=0.45",
                    "unrelated_dirty_preserved=true",
                    "source_geometry_retained=true",
                    "ownership_sets_disjoint=true",
                    "native_bounds_verified=true",
                    "wall_scoped_core_health_error_count=0",
                    "wall_scoped_runtime_health_error_count=0",
                    "qualification_boundary=P02_QUICK_ALIAS_POLYLINE_FAMILY_DIRTY_ONLY",
                    "production_local014_qualified=false"
                });
                _state = null;
                state.Document.Editor.WriteMessage("\nQS3D Plan-to-3D P02 runtime probe PASS.");
            }
            catch
            {
                _state = null;
                TryWriteFailure(requestedPath, nonce, failureCode);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Plan-to-3D P02 runtime probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static ProbeState RequiredState()
        {
            var state = _state ?? throw new InvalidOperationException("Plan-to-3D P02 state was not prepared.");
            var active = RequiredDocument();
            if (!ReferenceEquals(active, state.Document))
                throw new InvalidOperationException("Plan-to-3D P02 active document changed.");
            RequireModelSpace(active);
            return state;
        }

        private static ProjectState RequiredProject(ProbeState state)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(state.Document, out var project) ||
                !ReferenceEquals(project, state.Project))
                throw new InvalidOperationException("Plan-to-3D P02 canonical project changed.");
            var active = ProjectFamilyActivationService.GetActive(project);
            if (active == null || !string.Equals(active.Id, PreferredFamilyId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 preferred Family is no longer active.");
            return project;
        }

        private static IReadOnlyList<ProbeSource> CreateSources(Document document)
        {
            var result = new List<ProbeSource>(3);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var lineStart = new Point3d(0d, 0d, 0d);
                var lineEnd = new Point3d(4000d, 0d, 0d);
                var line = new Line(lineStart, lineEnd);
                var lineId = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                result.Add(new ProbeSource(
                    lineId,
                    ProbeSourceKind.QuickLine,
                    new[] { lineStart, lineEnd },
                    RequiredHandle(line.Handle.ToString())));

                var polyline = new Polyline();
                var polyStart = new Point3d(0d, 2000d, 0d);
                var polyEnd = new Point3d(3600d, 2000d, 0d);
                polyline.AddVertexAt(0, new Point2d(polyStart.X, polyStart.Y), 0d, 0d, 0d);
                polyline.AddVertexAt(1, new Point2d(polyEnd.X, polyEnd.Y), 0d, 0d, 0d);
                polyline.Closed = false;
                polyline.Elevation = 0d;
                var polylineId = modelSpace.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                result.Add(new ProbeSource(
                    polylineId,
                    ProbeSourceKind.AliasOpenPolyline,
                    new[] { polyStart, polyEnd },
                    RequiredHandle(polyline.Handle.ToString())));

                var unrelatedStart = new Point3d(0d, 4000d, 0d);
                var unrelatedEnd = new Point3d(3000d, 4000d, 0d);
                var unrelated = new Line(unrelatedStart, unrelatedEnd);
                var unrelatedId = modelSpace.AppendEntity(unrelated);
                transaction.AddNewlyCreatedDBObject(unrelated, true);
                result.Add(new ProbeSource(
                    unrelatedId,
                    ProbeSourceKind.UnrelatedLine,
                    new[] { unrelatedStart, unrelatedEnd },
                    RequiredHandle(unrelated.Handle.ToString())));

                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> PlanWalls(ProjectState project) =>
            project.Elements.Where(x =>
                x.Category == ElementCategory.ArchitecturalWall &&
                x.Properties.TryGetValue("QS3D.PlanTo3D", out var marker) &&
                string.Equals((marker ?? string.Empty).Trim(), "1", StringComparison.Ordinal))
                .ToList()
                .AsReadOnly();

        private static ProjectElement RequireWallForSource(IEnumerable<ProjectElement> walls, string sourceHandle)
        {
            var matches = walls.Where(x =>
                x.SourceHandles.Count == 1 &&
                string.Equals(RequiredHandle(x.SourceHandles[0]), sourceHandle, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Plan-to-3D P02 source does not have exactly one semantic wall.");
            return matches[0];
        }

        private static void RequirePreferredFamily(ProjectElement wall)
        {
            if (!string.Equals(wall.FamilyId, PreferredFamilyId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 wall did not use the preferred Family.");
            RequireNumber(wall, "ThicknessM", PreferredThicknessM);
            RequireNumber(wall, "HeightM", PreferredHeightM);
            RequireNumber(wall, "BottomOffsetM", PreferredBottomOffsetM);
        }

        private static string RequireGeneratedHandle(
            Document document,
            ProjectState project,
            ProjectElement wall,
            string sourceHandle)
        {
            if (!wall.Properties.TryGetValue("GeneratedSolidHandle", out var rawGenerated))
                throw new InvalidOperationException("A Plan-to-3D P02 wall is missing GeneratedSolidHandle.");
            var generatedHandle = RequiredHandle(rawGenerated);
            if (string.Equals(generatedHandle, sourceHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 generated ownership overlaps its source.");
            var owned = GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, wall.Id, wall.Category)
                .Select(CadHandleService.NormalizeHexHandle)
                .Where(x => x != null)
                .Cast<string>()
                .ToList();
            if (owned.Count != 1 || !string.Equals(owned[0], generatedHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 metadata and native ownership marker disagree.");
            return generatedHandle;
        }

        private static void RequireGeneratedSolidBounds(
            Document document,
            ProjectState project,
            ProjectElement wall,
            IEnumerable<string> generatedHandles,
            double expectedLengthM)
        {
            var generated = RequiredHandle(wall.Properties["GeneratedSolidHandle"]);
            if (!generatedHandles.Contains(generated, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 generated handle is outside the expected set.");
            var ids = CadHandleService.Resolve(document, new[] { generated });
            if (ids.Count != 1) throw new InvalidOperationException("Plan-to-3D P02 generated handle did not resolve once.");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased || !GeneratedGeometryService.HasMatchingOwnership(solid, project, wall))
                    throw new InvalidOperationException("Plan-to-3D P02 generated entity is not the expected owned Solid3d.");
                var extents = solid.GeometricExtents;
                RequireNear(expectedLengthM, (extents.MaxPoint.X - extents.MinPoint.X) / DrawingUnitsPerMeter);
                RequireNear(PreferredThicknessM, (extents.MaxPoint.Y - extents.MinPoint.Y) / DrawingUnitsPerMeter);
                RequireNear(PreferredBottomOffsetM, extents.MinPoint.Z / DrawingUnitsPerMeter);
                RequireNear(PreferredBottomOffsetM + PreferredHeightM, extents.MaxPoint.Z / DrawingUnitsPerMeter);
                transaction.Commit();
            }
        }

        private static void RequireSourcesUnchanged(ProbeState state)
        {
            using (var transaction = state.Document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                RequireLine(transaction, state.QuickLine);
                RequirePolyline(transaction, state.AliasPolyline);
                RequireLine(transaction, state.UnrelatedLine);
                transaction.Commit();
            }
        }

        private static void RequireLine(Transaction transaction, ProbeSource source)
        {
            var line = transaction.GetObject(source.Id, OpenMode.ForRead, false) as Line;
            if (line == null || line.IsErased ||
                !string.Equals(RequiredHandle(line.Handle.ToString()), source.Handle, StringComparison.OrdinalIgnoreCase) ||
                !Near(line.StartPoint, source.Points[0]) ||
                !Near(line.EndPoint, source.Points[1]))
                throw new InvalidOperationException("Plan-to-3D P02 changed or erased a seeded LINE.");
        }

        private static void RequirePolyline(Transaction transaction, ProbeSource source)
        {
            var polyline = transaction.GetObject(source.Id, OpenMode.ForRead, false) as Polyline;
            if (polyline == null || polyline.IsErased || polyline.Closed || polyline.NumberOfVertices != 2 ||
                !string.Equals(RequiredHandle(polyline.Handle.ToString()), source.Handle, StringComparison.OrdinalIgnoreCase) ||
                Math.Abs(polyline.Elevation) > ToleranceM ||
                Math.Abs(polyline.Normal.X) > ToleranceM ||
                Math.Abs(polyline.Normal.Y) > ToleranceM ||
                Math.Abs(polyline.Normal.Z - 1d) > ToleranceM)
                throw new InvalidOperationException("Plan-to-3D P02 changed or erased the open POLYLINE.");
            for (var index = 0; index < 2; index++)
            {
                var actual = polyline.GetPoint2dAt(index);
                var expected = source.Points[index];
                if (Math.Abs(actual.X - expected.X) > ToleranceM ||
                    Math.Abs(actual.Y - expected.Y) > ToleranceM ||
                    Math.Abs(polyline.GetBulgeAt(index)) > ToleranceM)
                    throw new InvalidOperationException("Plan-to-3D P02 open POLYLINE geometry changed.");
            }
        }

        private static void RequireUnrelatedState(ProbeState state)
        {
            var snapshot = state.Unrelated;
            var current = state.Project.Elements.SingleOrDefault(x => string.Equals(x.Id, snapshot.Element.Id, StringComparison.OrdinalIgnoreCase));
            if (!ReferenceEquals(current, snapshot.Element) || current == null ||
                current.Category != snapshot.Category ||
                !string.Equals(current.FamilyId, snapshot.FamilyId, StringComparison.Ordinal) ||
                !string.Equals(current.FloorId, snapshot.FloorId, StringComparison.Ordinal) ||
                !string.Equals(current.ZoneId, snapshot.ZoneId, StringComparison.Ordinal) ||
                !string.Equals(current.DrawingFingerprint, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                current.Dirty != snapshot.Dirty ||
                current.UpdatedUtc != snapshot.UpdatedUtc ||
                !current.SourceHandles.SequenceEqual(snapshot.SourceHandles, StringComparer.Ordinal) ||
                !current.DependsOn.SequenceEqual(snapshot.DependsOn, StringComparer.Ordinal) ||
                !DictionaryEquals(current.Properties, snapshot.Properties) ||
                !DictionaryEquals(current.Quantities, snapshot.Quantities))
                throw new InvalidOperationException("Plan-to-3D P02 changed unrelated semantic state.");
        }

        private static bool DictionaryEquals<T>(IDictionary<string, T> current, IReadOnlyDictionary<string, T> expected)
        {
            if (current.Count != expected.Count) return false;
            var comparer = EqualityComparer<T>.Default;
            foreach (var pair in expected)
                if (!current.TryGetValue(pair.Key, out var value) || !comparer.Equals(value, pair.Value)) return false;
            return true;
        }

        private static void RequireNumber(ProjectElement element, string key, double expected)
        {
            if (!element.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException("Plan-to-3D P02 required numeric property is missing or invalid.");
            RequireNear(expected, value);
        }

        private static void RequireNear(double expected, double actual)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > ToleranceM)
                throw new InvalidOperationException("Plan-to-3D P02 numeric assertion failed.");
        }

        private static bool Near(Point3d left, Point3d right) => left.DistanceTo(right) <= ToleranceM;

        private static string RequiredHandle(string value) =>
            CadHandleService.NormalizeHexHandle(value)
            ?? throw new InvalidDataException("Plan-to-3D P02 CAD handle is invalid.");

        private static Document RequiredDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("No active BricsCAD document is available.");
            if (!document.Name.EndsWith(".plan-to-3d-p02-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 requires a guarded disposable drawing copy.");
            return document;
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                if (!document.Database.CurrentSpaceId.Equals(blockTable[BlockTableRecord.ModelSpace]))
                    throw new InvalidOperationException("Plan-to-3D P02 requires Model Space.");
                transaction.Commit();
            }
            var zAxis = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d) ||
                Math.Abs(zAxis.X / length) > 1e-9d ||
                Math.Abs(zAxis.Y / length) > 1e-9d ||
                Math.Abs(zAxis.Z / length - 1d) > 1e-9d)
                throw new InvalidOperationException("Plan-to-3D P02 requires WCS-planar UCS.");
        }

        private static void RequireNonce(string nonce)
        {
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Plan-to-3D P02 runtime nonce is invalid.");
        }

        private static string RequiredResultPath(string? value)
        {
            var fullPath = Path.GetFullPath((value ?? string.Empty).Trim());
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plan-to-3D P02 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Plan-to-3D P02 result directory must already exist.");
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
                    "command=QS3DPLAN2DP02VERIFY",
                    "schema=QS3D_PLAN_TO_3D_P02_RUNTIME_V1",
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
            if (File.Exists(fullPath)) throw new IOException("Plan-to-3D P02 result already exists.");
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
