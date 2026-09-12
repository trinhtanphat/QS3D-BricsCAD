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
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-003 matrix for every native host family touched by
    /// shared Level placement. It uses only disposable synthetic geometry and emits
    /// aggregate booleans/counts; no handles, paths, names or source geometry leak.
    /// </summary>
    public sealed class LevelZCompleteFamilyRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_LEVEL_Z_COMPLETE_FAMILY_RESULT";
        private const string NonceVariable = "QS3D_LEVEL_Z_COMPLETE_FAMILY_NONCE";
        private const string SourceShaVariable = "QS3D_LEVEL_Z_COMPLETE_FAMILY_SOURCE_SHA";
        private const string ResultFileName = "level-z-complete-family-result.txt";
        private const string Schema = "LOCAL_003_COMPLETE_FAMILY_RUNTIME_V1";
        private static readonly ElementCategory[] HostCategories =
        {
            ElementCategory.ArchitecturalWall,
            ElementCategory.GlassWall,
            ElementCategory.WallPier,
            ElementCategory.StructuralWall,
            ElementCategory.Beam,
            ElementCategory.Column,
            ElementCategory.Slab,
            ElementCategory.Foundation,
            ElementCategory.Stair,
            ElementCategory.Railing
        };

        private sealed class SourceRef
        {
            public ObjectId ObjectId { get; set; }
            public string Handle { get; set; } = string.Empty;
            public double SourceZM { get; set; }
        }

        private sealed class MatrixCase
        {
            public MatrixCase(ElementCategory category, string mode, SourceRef source, ProjectElement element, double expectedBottomM, double expectedTopM)
            {
                Category = category;
                Mode = mode;
                Source = source;
                Element = element;
                ExpectedBottomM = expectedBottomM;
                ExpectedTopM = expectedTopM;
            }

            public ElementCategory Category { get; }
            public string Mode { get; }
            public SourceRef Source { get; }
            public ProjectElement Element { get; }
            public double ExpectedBottomM { get; }
            public double ExpectedTopM { get; }
        }

        private sealed class ZRange
        {
            public ZRange(double minimumM, double maximumM) { MinimumM = minimumM; MaximumM = maximumM; }
            public double MinimumM { get; }
            public double MaximumM { get; }
        }
        [CommandMethod("QS3DLEVELZCOMPLETEFAMILY", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            try
            {
                var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Complete-family probe is automation-only.");
                var sourceSha = (Environment.GetEnvironmentVariable(SourceShaVariable) ?? string.Empty).Trim().ToLowerInvariant();
                if (sourceSha.Length != 40 || sourceSha.Any(character => !Uri.IsHexDigit(character)))
                    throw new InvalidOperationException("Complete-family source SHA is invalid.");
                RuntimeSourceIdentityGuard.RequireExactSourceLink(typeof(LevelZCompleteFamilyRuntimeProbeCommands).Assembly, sourceSha, "QS3D.BricsCAD.V25");
                RuntimeSourceIdentityGuard.RequireExactSourceLink(typeof(ProjectState).Assembly, sourceSha, "QS3D.Core");
                var resultPath = RequiredPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Complete-family marker already exists.");
                if (!Environment.Is64BitProcess) throw new InvalidOperationException("Complete-family probe requires x64 BricsCAD.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document.");
                if (!document.Name.EndsWith(".level-z-complete-family-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Complete-family probe requires the guarded disposable suffix.");
                if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                    throw new InvalidOperationException("Complete-family probe requires Millimeter or Meter native units.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                if (project.Elements.Count != 0 || project.Floors.Count != 0)
                    throw new InvalidOperationException("Complete-family probe requires a fresh project.");
                project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
                project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
                project.Floors.Add(new FloorDefinition("L2", "Level 2", 7d));
                project.ActiveFloorId = "L0";
                DrawingUnitResolutionPolicy.BindQuantityUnit(project.Metadata, false, nativeUnit, DrawingUnitResolutionSource.NativeInsunits);
                var sources = CreateMatrixSources(document);
                var matrix = new List<MatrixCase>();
                var index = 0;
                foreach (var category in HostCategories)
                {
                    var legacySource = sources[index++];
                    var bottomSource = sources[index++];
                    var boundedSource = sources[index++];
                    var legacyHeight = LegacyHeight(category);

                    var legacy = AddElement(project, category, "legacy-" + category, legacySource);
                    ConfigureDimensions(legacy, category, legacyHeight);
                    matrix.Add(new MatrixCase(category, "LegacySourceRelative", legacySource, legacy, legacySource.SourceZM, legacySource.SourceZM + legacyHeight));

                    var bottom = AddElement(project, category, "bottom-" + category, bottomSource);
                    ConfigureDimensions(bottom, category, legacyHeight);
                    Require(ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bottom }) == 1, category + " bottom-only assignment");
                    Set(bottom, ProjectFloorService.BottomLevelOffsetKey, 0.25d);
                    matrix.Add(new MatrixCase(category, "BottomLevel", bottomSource, bottom, 3.25d, 3.25d + legacyHeight));

                    var bounded = AddElement(project, category, "bounded-" + category, boundedSource);
                    ConfigureDimensions(bounded, category, legacyHeight);
                    Require(ProjectFloorService.AssignBottomLevel(project, "L1", new[] { bounded }) == 1, category + " bounded Bottom assignment");
                    Require(ProjectFloorService.AssignTopLevel(project, "L2", new[] { bounded }) == 1, category + " bounded Top assignment");
                    Set(bounded, ProjectFloorService.BottomLevelOffsetKey, 0.1d);
                    Set(bounded, ProjectFloorService.TopLevelOffsetKey, -0.2d);
                    matrix.Add(new MatrixCase(category, "BottomTopLevels", boundedSource, bounded, 3.1d, 6.8d));
                }

                foreach (var item in matrix)
                {
                    Select(document, item.Source.ObjectId);
                    Require(Build(document, project, item.Category) == 1, item.Category + "/" + item.Mode + " native build");
                    var range = ReadZRange(document, Handles(item.Element, "GeneratedSolidHandle"), item.Category + "/" + item.Mode);
                    RequireNear(item.ExpectedBottomM, range.MinimumM, item.Category + "/" + item.Mode + " bottom");
                    RequireNear(item.ExpectedTopM, range.MaximumM, item.Category + "/" + item.Mode + " top");
                    RequireSnapshot(item.Element, item.ExpectedBottomM, item.ExpectedTopM, item.Mode);
                }
                var hostedOpeningCount = VerifyStraightHostedOpenings(document, project);
                var topOnlyFailClosed = VerifyFailure(document, FailureKind.TopOnly, 220d);
                var missingLevelFailClosed = VerifyFailure(document, FailureKind.MissingLevel, 224d);
                var ambiguousLevelFailClosed = VerifyFailure(document, FailureKind.AmbiguousLevel, 228d);
                var nonFiniteOffsetFailClosed = VerifyFailure(document, FailureKind.NonFiniteOffset, 232d);
                var invalidVerticalRangeFailClosed = VerifyFailure(document, FailureKind.InvalidVerticalRange, 236d);

                var healthIssueCount = new LevelReferenceHealthService().Inspect(project)
                    .Count(issue => issue.Severity != HealthSeverity.Info);
                Require(healthIssueCount == 0, "complete-family Level health");

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DLEVELZCOMPLETEFAMILY",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "source_sha=" + sourceSha,
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_003_COMPLETE_FAMILY_HOSTS_ONLY",
                    "production_local003_qualified=false",
                    "is_64bit=true",
                    "native_drawing_unit=" + nativeUnit,
                    "host_family_count=10",
                    "family_case_count=" + matrix.Count.ToString(CultureInfo.InvariantCulture),
                    "legacy_family_count=10",
                    "bottom_only_family_count=10",
                    "bottom_top_family_count=10",
                    "hosted_opening_count=" + hostedOpeningCount.ToString(CultureInfo.InvariantCulture),
                    "door_straight_cut=true",
                    "wallopening_straight_cut=true",
                    "top_only_fail_closed=" + Boolean(topOnlyFailClosed),
                    "missing_level_fail_closed=" + Boolean(missingLevelFailClosed),
                    "ambiguous_level_fail_closed=" + Boolean(ambiguousLevelFailClosed),
                    "non_finite_offset_fail_closed=" + Boolean(nonFiniteOffsetFailClosed),
                    "invalid_vertical_range_fail_closed=" + Boolean(invalidVerticalRangeFailClosed),
                    "level_health_issue_count=0"
                });
                document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
            catch (Exception error)
            {
                TryWriteFailure(requestedPath, error);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D LOCAL-003 complete-family probe FAIL.");
            }
        }

        private enum FailureKind { TopOnly, MissingLevel, AmbiguousLevel, NonFiniteOffset, InvalidVerticalRange }
        private static IReadOnlyList<SourceRef> CreateMatrixSources(Document document)
        {
            var result = new List<SourceRef>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var units = CadGeometryGuard.ToDrawingUnits(document, 1d, "complete-family meter scale");
                var row = 0;
                foreach (var category in HostCategories)
                {
                    result.Add(AppendSource(document, transaction, modelSpace, category, units, row++ * 5d, 1d));
                    result.Add(AppendSource(document, transaction, modelSpace, category, units, row++ * 5d, 9d));
                    result.Add(AppendSource(document, transaction, modelSpace, category, units, row++ * 5d, 15d));
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private static SourceRef AppendSource(Document document, Transaction transaction, BlockTableRecord modelSpace,
            ElementCategory category, double units, double yM, double zM)
        {
            if (category == ElementCategory.Column || category == ElementCategory.Slab ||
                category == ElementCategory.Foundation || category == ElementCategory.Stair)
                return AppendRectangle(document, transaction, modelSpace, units, yM, zM);
            return AppendLine(document, transaction, modelSpace, units, 0d, yM, 5d, yM, zM);
        }

        private static SourceRef AppendLine(Document document, Transaction transaction, BlockTableRecord modelSpace,
            double units, double x1M, double y1M, double x2M, double y2M, double zM)
        {
            var line = new Line(new Point3d(x1M * units, y1M * units, zM * units), new Point3d(x2M * units, y2M * units, zM * units));
            line.SetDatabaseDefaults(document.Database);
            var id = modelSpace.AppendEntity(line);
            transaction.AddNewlyCreatedDBObject(line, true);
            return new SourceRef { ObjectId = id, Handle = line.Handle.ToString(), SourceZM = zM };
        }
        private static SourceRef AppendRectangle(Document document, Transaction transaction, BlockTableRecord modelSpace,
            double units, double yM, double zM)
        {
            var polyline = new Polyline();
            polyline.SetDatabaseDefaults(document.Database);
            polyline.AddVertexAt(0, new Point2d(0d * units, yM * units), 0d, 0d, 0d);
            polyline.AddVertexAt(1, new Point2d(2d * units, yM * units), 0d, 0d, 0d);
            polyline.AddVertexAt(2, new Point2d(2d * units, (yM + 2d) * units), 0d, 0d, 0d);
            polyline.AddVertexAt(3, new Point2d(0d * units, (yM + 2d) * units), 0d, 0d, 0d);
            polyline.Closed = true;
            polyline.Elevation = zM * units;
            var id = modelSpace.AppendEntity(polyline);
            transaction.AddNewlyCreatedDBObject(polyline, true);
            return new SourceRef { ObjectId = id, Handle = polyline.Handle.ToString(), SourceZM = zM };
        }

        private static SourceRef CreateLineSource(Document document, double yM, double zM, double x1M = 0d, double x2M = 5d)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var units = CadGeometryGuard.ToDrawingUnits(document, 1d, "complete-family extra meter scale");
                var source = AppendLine(document, transaction, modelSpace, units, x1M, yM, x2M, yM, zM);
                transaction.Commit();
                return source;
            }
        }

        private static ProjectElement AddElement(ProjectState project, ElementCategory category, string id, SourceRef source)
        {
            var element = new ProjectElement(id, category, string.Empty, "L0", string.Empty);
            element.SourceHandles.Add(source.Handle);
            project.Elements.Add(element);
            return element;
        }
        private static double LegacyHeight(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam: return 0.6d;
                case ElementCategory.Slab: return 0.2d;
                case ElementCategory.Foundation: return 0.5d;
                case ElementCategory.Stair: return 0.25d;
                case ElementCategory.Railing: return 1.1d;
                default: return 2.8d;
            }
        }

        private static void ConfigureDimensions(ProjectElement element, ElementCategory category, double heightM)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                    Set(element, "ThicknessM", 0.2d);
                    Set(element, "HeightM", heightM);
                    break;
                case ElementCategory.Beam:
                    Set(element, "WidthM", 0.3d);
                    Set(element, "HeightM", heightM);
                    break;
                case ElementCategory.Column:
                    Set(element, "HeightM", heightM);
                    break;
                case ElementCategory.Slab:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                    Set(element, "ThicknessM", heightM);
                    break;
                case ElementCategory.Railing:
                    Set(element, "ProfileWidthM", 0.05d);
                    Set(element, "HeightM", heightM);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported complete-family category: " + category);
            }
        }

        private static int Build(Document document, ProjectState project, ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                    return WallSolidBuilder.BuildSelectedLineWalls(document, project, category, false);
                case ElementCategory.WallPier:
                    return WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project);
                case ElementCategory.StructuralWall:
                case ElementCategory.Beam:
                case ElementCategory.Column:
                case ElementCategory.Slab:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                case ElementCategory.Railing:
                    return StructuralSolidBuilder.BuildSelected(document, project, category);
                default:
                    throw new InvalidOperationException("Unsupported complete-family build category: " + category);
            }
        }

        private static int VerifyStraightHostedOpenings(Document document, ProjectState project)
        {
            var host = project.FindElement("bounded-ArchitecturalWall")
                ?? throw new InvalidOperationException("Complete-family opening host is missing.");
            var doorSource = CreateLineSource(document, 10d, 3.4d, 1d, 1.8d);
            var wallOpeningSource = CreateLineSource(document, 10d, 3.4d, 3d, 3.8d);
            var door = AddOpening(project, ElementCategory.Door, "bounded-door", doorSource, host.Id);
            var wallOpening = AddOpening(project, ElementCategory.WallOpening, "bounded-wall-opening", wallOpeningSource, host.Id);
            var hostHandle = Handles(host, "GeneratedSolidHandle").Single();
            var before = ReadSolidVolume(document, hostHandle, "opening host before cuts");
            Require(OpeningBooleanService.CutLinkedOpenings(document, project, new[] { door.Id }) == 1, "Door straight cut");
            var afterDoor = ReadSolidVolume(document, hostHandle, "opening host after Door");
            Require(afterDoor > 0d && afterDoor < before, "Door must reduce host volume");
            Require(OpeningBooleanService.CutLinkedOpenings(document, project, new[] { wallOpening.Id }) == 1, "WallOpening straight cut");
            var afterWallOpening = ReadSolidVolume(document, hostHandle, "opening host after WallOpening");
            Require(afterWallOpening > 0d && afterWallOpening < afterDoor, "WallOpening must further reduce host volume");
            return 2;
        }

        private static ProjectElement AddOpening(ProjectState project, ElementCategory category, string id, SourceRef source, string hostId)
        {
            var opening = AddElement(project, category, id, source);
            opening.Properties["HostWallId"] = hostId;
            Set(opening, "WidthM", 0.8d);
            Set(opening, "BooleanClearanceM", 0.01d);
            Require(ProjectFloorService.AssignBottomLevel(project, "L1", new[] { opening }) == 1, id + " Bottom Level assignment");
            Require(ProjectFloorService.AssignTopLevel(project, "L2", new[] { opening }) == 1, id + " Top Level assignment");
            Set(opening, ProjectFloorService.BottomLevelOffsetKey, 0.4d);
            Set(opening, ProjectFloorService.TopLevelOffsetKey, -0.4d);
            return opening;
        }

        private static bool VerifyFailure(Document document, FailureKind kind, double yM)
        {
            var source = CreateLineSource(document, yM, 0d);
            var project = FailureProject(kind);
            var element = AddElement(project, ElementCategory.ArchitecturalWall, "failure-wall", source);
            ConfigureDimensions(element, ElementCategory.ArchitecturalWall, 2.8d);
            ConfigureFailure(project, element, kind);
            Select(document, source.ObjectId);
            try
            {
                WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall, false);
            }
            catch (InvalidOperationException)
            {
                var restored = project.FindElement(element.Id);
                Require(restored != null, "failure semantic element must remain present");
                Require(!restored!.Properties.ContainsKey("GeneratedSolidHandle"), kind + " must fail before generated ownership");
                return true;
            }
            throw new InvalidOperationException(kind + " did not fail closed.");
        }

        private static ProjectState FailureProject(FailureKind kind)
        {
            var project = new ProjectState("complete-family-failure-" + kind, "Complete Family Failure");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
            project.Floors.Add(new FloorDefinition("L2", "Level 2", 7d));
            project.ActiveFloorId = "L0";
            if (kind == FailureKind.AmbiguousLevel)
                project.Floors.Add(new FloorDefinition("L1", "Duplicate Level 1", 3.5d));
            return project;
        }

        private static void ConfigureFailure(ProjectState project, ProjectElement element, FailureKind kind)
        {
            switch (kind)
            {
                case FailureKind.TopOnly:
                    element.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
                    break;
                case FailureKind.MissingLevel:
                    element.Properties[ProjectFloorService.BottomLevelIdKey] = "missing-level";
                    break;
                case FailureKind.AmbiguousLevel:
                    element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
                    break;
                case FailureKind.NonFiniteOffset:
                    element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
                    element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "NaN";
                    break;
                case FailureKind.InvalidVerticalRange:
                    element.Properties[ProjectFloorService.BottomLevelIdKey] = "L2";
                    element.Properties[ProjectFloorService.TopLevelIdKey] = "L1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void Select(Document document, ObjectId id) =>
            document.Editor.SetImpliedSelection(new[] { id });

        private static IReadOnlyList<string> Handles(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(element.Id + "/" + key + " is empty.");
            var result = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (result.Count == 0) throw new InvalidOperationException(element.Id + "/" + key + " is empty.");
            return result.AsReadOnly();
        }

        private static ZRange ReadZRange(Document document, IReadOnlyList<string> handles, string label)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count) throw new InvalidOperationException(label + " generated Solid3d count changed.");
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased) throw new InvalidOperationException(label + " is not a live Solid3d.");
                    var extents = solid.GeometricExtents;
                    minimum = Math.Min(minimum, CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, label + " minimum Z"));
                    maximum = Math.Max(maximum, CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, label + " maximum Z"));
                }
                transaction.Commit();
            }
            if (double.IsInfinity(minimum) || double.IsInfinity(maximum) || maximum <= minimum)
                throw new InvalidOperationException(label + " aggregate Z range is invalid.");
            return new ZRange(minimum, maximum);
        }

        private static double ReadSolidVolume(Document document, string handle, string label)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException(label + " must resolve to one solid.");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException(label + " is not a Solid3d.");
                var volume = solid.MassProperties.Volume;
                transaction.Commit();
                if (double.IsNaN(volume) || double.IsInfinity(volume) || volume <= 0d)
                    throw new InvalidOperationException(label + " volume is invalid.");
                return volume;
            }
        }

        private static void RequireSnapshot(ProjectElement element, double bottomM, double topM, string mode)
        {
            RequireNear(bottomM, NumberProperty(element, "GeneratedSolidVerticalBottomM"), element.Id + " snapshot bottom");
            RequireNear(topM, NumberProperty(element, "GeneratedSolidVerticalTopM"), element.Id + " snapshot top");
            RequireNear(topM - bottomM, NumberProperty(element, "GeneratedSolidVerticalHeightM"), element.Id + " snapshot height");
            Require(string.Equals(Property(element, "GeneratedSolidVerticalMode"), mode, StringComparison.Ordinal),
                element.Id + " snapshot mode");
        }

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

        private static double NumberProperty(ProjectElement element, string key)
        {
            var raw = Property(element, key);
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(element.Id + "/" + key + " is not finite.");
            return value;
        }

        private static void Set(ProjectElement element, string key, double value) =>
            element.Properties[key] = Number(value);

        private static void RequireNear(double expected, double actual, string label)
        {
            var tolerance = Math.Max(1e-7d, Math.Max(Math.Abs(expected), Math.Abs(actual)) * 1e-7d);
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException(label + " expected " + Number(expected) + " but was " + Number(actual) + ".");
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Complete-family assertion failed: " + label + ".");
        }

        private static string RequiredPath(string? value)
        {
            var fullPath = Path.GetFullPath((value ?? string.Empty).Trim());
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new InvalidOperationException("Complete-family result path is invalid.");
            return fullPath;
        }

        private static void TryWriteFailure(string? requestedPath, Exception error)
        {
            try
            {
                var path = RequiredPath(requestedPath);
                if (File.Exists(path)) return;
                WriteMarkerAtomic(path, new[]
                {
                    "status=FAIL",
                    "command=QS3DLEVELZCOMPLETEFAMILY",
                    "schema=" + Schema,
                    "qualification_boundary=LOCAL_003_COMPLETE_FAMILY_HOSTS_ONLY",
                    "production_local003_qualified=false",
                    "error_code=LOCAL_003_COMPLETE_FAMILY_RUNTIME_FAILED",
                    "exception_type=" + OneLine(error.GetType().FullName ?? error.GetType().Name),
                    "exception_target=" + OneLine(error.TargetSite?.Name ?? string.Empty),
                    "exception_hresult=0x" + error.HResult.ToString("X8", CultureInfo.InvariantCulture)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            var fullPath = RequiredPath(path);
            if (File.Exists(fullPath)) throw new IOException("Complete-family result already exists.");
            var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temporary, fullPath);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Boolean(bool value) => value ? "true" : "false";
        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
