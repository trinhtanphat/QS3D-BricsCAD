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
using QS3D.Core.Services;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Licensed-runtime proof for LOCAL-003. The PowerShell runner opens only a
    /// disposable synthetic DWG and kills the host without saving it. The marker
    /// contains aggregate counts and Z measurements only; Handles and paths are
    /// deliberately excluded.
    /// </summary>
    public sealed class LevelZRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_LEVEL_Z_RESULT";
        private const string NonceVariable = "QS3D_LEVEL_Z_NONCE";
        private const string SourceShaVariable = "QS3D_LEVEL_Z_SOURCE_SHA";
        private const string ResultFileName = "level-z-runtime-result.txt";

        private sealed class SourceReference
        {
            public ObjectId ObjectId { get; set; }
            public string Handle { get; set; } = string.Empty;
        }

        private sealed class ProbeSources
        {
            public SourceReference LegacyWall { get; set; } = null!;
            public SourceReference BoundedWall { get; set; } = null!;
            public SourceReference Beam { get; set; } = null!;
            public SourceReference GlassWall { get; set; } = null!;
            public SourceReference HostOpening { get; set; } = null!;
            public SourceReference CurtainOpening { get; set; } = null!;
            public SourceReference TopOnlyWall { get; set; } = null!;
        }

        private sealed class ZRange
        {
            public double MinimumM { get; set; }
            public double MaximumM { get; set; }
        }

        [CommandMethod("QS3DLEVELZPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Level Z runtime probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            var failureCode = "LEVEL_Z_RUNTIME_CONTEXT_FAILED";
            ZRange? observedLegacyRange = null;
            ZRange? observedGlassRange = null;
            ZRange? observedFrameRange = null;
            ZRange? observedPanelRange = null;
            var hostBuildStage = string.Empty;
            var rebarStage = string.Empty;
            int? observedBeamRebarCount = null;
            int? observedBeamStirrupElementCount = null;
            int? observedBeamStirrupCount = null;
            ZRange? observedRebarRange = null;
            ZRange? observedStirrupRange = null;
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("Level Z runtime nonce is invalid.");
                var sourceSha = (Environment.GetEnvironmentVariable(SourceShaVariable) ?? string.Empty).Trim().ToLowerInvariant();
                if (sourceSha.Length != 40 || sourceSha.Any(x => !Uri.IsHexDigit(x)))
                    throw new InvalidOperationException("Level Z runtime source SHA is invalid.");
                RequireAssemblyRevision(typeof(LevelZRuntimeProbeCommands).Assembly, sourceSha, "QS3D.BricsCAD.V25");
                RequireAssemblyRevision(typeof(ProjectState).Assembly, sourceSha, "QS3D.Core");
                var resultPath = RequiredResultPath(requestedPath);
                if (File.Exists(resultPath)) throw new IOException("Level Z runtime result already exists.");
                if (!Environment.Is64BitProcess) throw new InvalidOperationException("Level Z runtime probe requires a 64-bit BricsCAD process.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                if (!document.Name.EndsWith(".level-z-probe-copy.dwg", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Level Z runtime probe requires a guarded disposable drawing copy.");
                failureCode = "LEVEL_Z_RUNTIME_SOURCE_FAILED";
                var sources = CreateSources(document);
                var topOnlyFailClosed = VerifyTopOnlyFailsBeforeMutation(document, sources.TopOnlyWall);
                var project = CreateProject(document);
                if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                    throw new InvalidOperationException("Level Z runtime probe requires a supported native drawing unit.");
                DrawingUnitResolutionPolicy.BindQuantityUnit(
                    project.Metadata,
                    false,
                    nativeUnit,
                    DrawingUnitResolutionSource.NativeInsunits);
                var legacyWall = AddElement(project, "legacy-wall", ElementCategory.ArchitecturalWall, sources.LegacyWall);
                Set(legacyWall, "ThicknessM", 0.2d);
                Set(legacyWall, "HeightM", 2.5d);
                Set(legacyWall, "BottomOffsetM", 0.2d);

                var boundedWall = AddElement(project, "bounded-wall", ElementCategory.ArchitecturalWall, sources.BoundedWall);
                Set(boundedWall, "ThicknessM", 0.2d);
                boundedWall.Properties["HeightM"] = "ignored-invalid-height";
                boundedWall.Properties["BottomOffsetM"] = "ignored-invalid-offset";
                AssignBounded(project, boundedWall, 0.1d, -0.2d);

                var beam = AddElement(project, "bottom-beam", ElementCategory.Beam, sources.Beam);
                Set(beam, "LengthM", 5d);
                Set(beam, "WidthM", 0.3d);
                Set(beam, "HeightM", 0.6d);
                beam.Properties["BottomOffsetM"] = "ignored-invalid-offset";
                beam.Properties["RebarNotation"] = "4D16";
                beam.Properties["RebarStirrupNotation"] = "D8@1000";
                Set(beam, "RebarCoverM", 0.04d);
                Set(beam, "RebarStirrupCoverM", 0.04d);
                Set(beam, "RebarBeamEndCoverM", 0.05d);
                Set(beam, "RebarStirrupEndCoverM", 0.05d);
                ProjectFloorService.AssignBottomLevel(project, "L1", new[] { beam });
                Set(beam, ProjectFloorService.BottomLevelOffsetKey, 0.25d);

                var glassWall = AddElement(project, "bounded-glass", ElementCategory.GlassWall, sources.GlassWall);
                Set(glassWall, "ThicknessM", 0.2d);
                glassWall.Properties["HeightM"] = "ignored-invalid-height";
                AssignBounded(project, glassWall, 0d, 0d);

                var hostOpening = AddElement(project, "bounded-door", ElementCategory.Door, sources.HostOpening);
                Set(hostOpening, "WidthM", 1d);
                hostOpening.Properties["HeightM"] = "ignored-invalid-height";
                Set(hostOpening, "BooleanClearanceM", 0.01d);
                hostOpening.Properties["HostWallId"] = boundedWall.Id;
                AssignBounded(project, hostOpening, 0.4d, -0.4d);

                var curtainOpening = AddElement(project, "curtain-door", ElementCategory.Door, sources.CurtainOpening);
                Set(curtainOpening, "WidthM", 1d);
                curtainOpening.Properties["HeightM"] = "ignored-invalid-height";
                curtainOpening.Properties["HostWallId"] = glassWall.Id;
                AssignBounded(project, curtainOpening, 0.4d, -0.4d);

                failureCode = "LEVEL_Z_RUNTIME_HOST_BUILD_FAILED";
                hostBuildStage = "legacy_wall_build";
                Select(document, sources.LegacyWall.ObjectId);
                Require(WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall) == 1, "legacy wall build count");
                hostBuildStage = "bounded_wall_build";
                Select(document, sources.BoundedWall.ObjectId);
                Require(WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall) == 1, "bounded wall build count");
                hostBuildStage = "glass_wall_build";
                Select(document, sources.GlassWall.ObjectId);
                Require(WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.GlassWall) == 1, "GlassWall build count");
                hostBuildStage = "beam_build";
                Select(document, sources.Beam.ObjectId);
                Require(StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Beam) == 1, "Beam build count");

                hostBuildStage = "range_read";
                var legacyRange = ReadZRange(document, Handles(legacyWall, "GeneratedSolidHandle"), "legacy wall");
                observedLegacyRange = legacyRange;
                var boundedRange = ReadZRange(document, Handles(boundedWall, "GeneratedSolidHandle"), "bounded wall");
                var beamRange = ReadZRange(document, Handles(beam, "GeneratedSolidHandle"), "Beam");
                var glassRange = ReadZRange(document, Handles(glassWall, "GeneratedSolidHandle"), "GlassWall");
                observedGlassRange = glassRange;
                RequireNear(1.2d, legacyRange.MinimumM, "legacy wall bottom");
                RequireNear(3.7d, legacyRange.MaximumM, "legacy wall top");
                RequireNear(3.1d, boundedRange.MinimumM, "bounded wall bottom");
                RequireNear(6.8d, boundedRange.MaximumM, "bounded wall top");
                RequireNear(3.25d, beamRange.MinimumM, "Bottom-only Beam bottom");
                RequireNear(3.85d, beamRange.MaximumM, "Bottom-only Beam top");
                RequireNear(3d, glassRange.MinimumM, "bounded GlassWall bottom");
                RequireNear(7d, glassRange.MaximumM, "bounded GlassWall top");

                failureCode = "LEVEL_Z_RUNTIME_OPENING_FAILED";
                var wallVolumeBefore = ReadSolidVolume(document, Handles(boundedWall, "GeneratedSolidHandle").Single(), "bounded wall before opening");
                Require(OpeningBooleanService.CutLinkedOpenings(document, project, new[] { hostOpening.Id }) == 1, "physical opening cut count");
                var wallVolumeAfter = ReadSolidVolume(document, Handles(boundedWall, "GeneratedSolidHandle").Single(), "bounded wall after opening");
                Require(wallVolumeAfter > 0d && wallVolumeAfter < wallVolumeBefore, "physical opening must reduce host volume");

                failureCode = "LEVEL_Z_RUNTIME_CURTAIN_FRAME_BUILD_FAILED";
                Select(document, sources.GlassWall.ObjectId);
                var frameResult = CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(document, project);
                Require(frameResult.Elements == 1 && frameResult.Frames > 0, "Curtain frame result");
                failureCode = "LEVEL_Z_RUNTIME_CURTAIN_PANEL_BUILD_FAILED";
                Select(document, sources.GlassWall.ObjectId);
                var panelResult = CurtainWallPanelSolidBuilder.BuildSelectedLineWalls(document, project);
                Require(panelResult.Elements == 1 && panelResult.Panels > 0, "Curtain panel result");
                failureCode = "LEVEL_Z_RUNTIME_CURTAIN_RANGE_FAILED";
                var frameRange = ReadZRange(document, Handles(glassWall, "GeneratedCurtainFrameHandles"), "Curtain frames");
                var panelRange = ReadZRange(document, Handles(glassWall, "GeneratedCurtainPanelHandles"), "Curtain panels");
                observedFrameRange = frameRange;
                observedPanelRange = panelRange;
                RequireContained(frameRange, glassRange, "Curtain frame Z");
                RequireContained(panelRange, glassRange, "Curtain panel Z");
                failureCode = "LEVEL_Z_RUNTIME_CURTAIN_MODE_FAILED";
                Require(string.Equals(Property(glassWall, "GeneratedCurtainFrameMode"), "LineFrameOverlay.OpeningAware", StringComparison.Ordinal), "Curtain frame opening-aware mode");
                Require(string.Equals(Property(glassWall, "GeneratedCurtainPanelMode"), "LinePanelSolids.OpeningAware", StringComparison.Ordinal), "Curtain panel opening-aware mode");

                failureCode = "LEVEL_Z_RUNTIME_REBAR_FAILED";
                rebarStage = "longitudinal_build";
                Select(document, sources.Beam.ObjectId);
                var rebarCount = BeamRebarSolidBuilder.BuildSelected(document, project, new[] { sources.Beam.ObjectId });
                observedBeamRebarCount = rebarCount;
                rebarStage = "longitudinal_count";
                Require(rebarCount == 4, "Beam longitudinal rebar count");
                rebarStage = "stirrup_build";
                Select(document, sources.Beam.ObjectId);
                var stirrupResult = BeamStirrupSolidBuilder.BuildSelected(document, project);
                observedBeamStirrupElementCount = stirrupResult.Elements;
                observedBeamStirrupCount = stirrupResult.Stirrups;
                rebarStage = "stirrup_count";
                Require(stirrupResult.Elements == 1 && stirrupResult.Stirrups > 0, "Beam stirrup result");
                rebarStage = "longitudinal_range_read";
                var rebarRange = ReadZRange(document, Handles(beam, "GeneratedRebarHandles"), "Beam rebar");
                observedRebarRange = rebarRange;
                rebarStage = "stirrup_range_read";
                var stirrupRange = ReadZRange(document, Handles(beam, "GeneratedBeamStirrupHandles"), "Beam stirrups");
                observedStirrupRange = stirrupRange;
                rebarStage = "longitudinal_containment";
                RequireContained(rebarRange, beamRange, "Beam rebar Z");
                rebarStage = "stirrup_containment";
                RequireContained(stirrupRange, beamRange, "Beam stirrup Z");
                rebarStage = "complete";

                failureCode = "LEVEL_Z_RUNTIME_LEVEL_EDIT_FAILED";
                new WallRegenerator().Regenerate(project, boundedWall);
                new StructuralRegenerator().Regenerate(project, beam);
                new OpeningRegenerator().Regenerate(project, hostOpening);
                RequireNear(3.7d, boundedWall.Quantities["HeightM"], "bounded wall quantity height");
                RequireNear(0.6d, beam.Quantities["HeightM"], "Bottom-only Beam quantity height");
                RequireNear(3.2d, hostOpening.Quantities["OpeningAreaM2"], "bounded opening quantity area");

                RequireSnapshot(legacyWall, "GeneratedSolid", 1.2d, 3.7d, "LegacySourceRelative");
                RequireSnapshot(boundedWall, "GeneratedSolid", 3.1d, 6.8d, "BottomTopLevels");
                RequireSnapshot(beam, "GeneratedSolid", 3.25d, 3.85d, "BottomLevel");
                RequireSnapshot(beam, "GeneratedRebar", 3.25d, 3.85d, "BottomLevel");
                RequireSnapshot(beam, "GeneratedBeamStirrup", 3.25d, 3.85d, "BottomLevel");
                RequireSnapshot(glassWall, "GeneratedCurtainFrame", 3d, 7d, "BottomTopLevels");
                RequireSnapshot(glassWall, "GeneratedCurtainPanel", 3d, 7d, "BottomTopLevels");

                var healthBefore = new LevelReferenceHealthService().Inspect(project)
                    .Count(x => x.Severity != HealthSeverity.Info);
                Require(healthBefore == 0, "Level health before edit");

                ProjectFloorService.Update(project, "L2", "Level 2", 7.2d);
                var topLevelStaleSnapshotCount = new LevelReferenceHealthService().Inspect(project)
                    .Count(x => x.Code == "LEVEL_NATIVE_VERTICAL_SNAPSHOT_STALE");
                Require(topLevelStaleSnapshotCount >= 2, "Top Level edit stale snapshot count");
                Require(boundedWall.IsGeneratedSolidStale(), "bounded wall stale after Level edit");
                Require(glassWall.IsGeneratedCurtainFrameStale(), "Curtain frame stale after Level edit");
                Require(glassWall.IsGeneratedCurtainPanelStale(), "Curtain panel stale after Level edit");

                ProjectFloorService.Update(project, "L1", "Level 1", 3.2d);
                var allStaleSnapshotCount = new LevelReferenceHealthService().Inspect(project)
                    .Count(x => x.Code == "LEVEL_NATIVE_VERTICAL_SNAPSHOT_STALE");
                Require(allStaleSnapshotCount >= topLevelStaleSnapshotCount, "Bottom Level edit stale snapshot count");
                Require(beam.IsGeneratedRebarStale(), "Beam rebar stale after Level edit");
                Require(beam.IsGeneratedBeamStirrupStale(), "Beam stirrup stale after Level edit");

                failureCode = "LEVEL_Z_RUNTIME_MARKER_FAILED";
                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DLEVELZPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "source_sha=" + sourceSha,
                    "schema=QS3D_LEVEL_Z_RUNTIME_V1",
                    "is_64bit=true",
                    "native_drawing_unit=" + nativeUnit.ToString(),
                    "legacy_wall_bottom_m=" + Number(legacyRange.MinimumM),
                    "legacy_wall_top_m=" + Number(legacyRange.MaximumM),
                    "bounded_wall_bottom_m=" + Number(boundedRange.MinimumM),
                    "bounded_wall_top_m=" + Number(boundedRange.MaximumM),
                    "bottom_beam_bottom_m=" + Number(beamRange.MinimumM),
                    "bottom_beam_top_m=" + Number(beamRange.MaximumM),
                    "physical_opening_volume_reduced=true",
                    "curtain_frame_count=" + frameResult.Frames.ToString(CultureInfo.InvariantCulture),
                    "curtain_panel_count=" + panelResult.Panels.ToString(CultureInfo.InvariantCulture),
                    "beam_rebar_count=" + rebarCount.ToString(CultureInfo.InvariantCulture),
                    "beam_stirrup_count=" + stirrupResult.Stirrups.ToString(CultureInfo.InvariantCulture),
                    "level_health_issue_count_before_edit=0",
                    "top_level_stale_snapshot_count_after_edit=" + topLevelStaleSnapshotCount.ToString(CultureInfo.InvariantCulture),
                    "stale_snapshot_count_after_edit=" + allStaleSnapshotCount.ToString(CultureInfo.InvariantCulture),
                    "level_edit_invalidation=true",
                    "top_only_fail_closed=" + (topOnlyFailClosed ? "true" : "false")
                });
                document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                document.Editor.WriteMessage("\nQS3D Level Z runtime probe PASS.");
            }
            catch (System.Exception error)
            {
                TryWriteFailure(
                    requestedPath,
                    failureCode,
                    observedLegacyRange,
                    observedGlassRange,
                    observedFrameRange,
                    observedPanelRange,
                    hostBuildStage,
                    rebarStage,
                    observedBeamRebarCount,
                    observedBeamStirrupElementCount,
                    observedBeamStirrupCount,
                    observedRebarRange,
                    observedStirrupRange,
                    error);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Level Z runtime probe FAIL. See the local qualification marker.");
            }
        }

        private static ProjectState CreateProject(Document document)
        {
            var project = ProjectContextCoordinator.GetOrCreate(document);
            if (project.Elements.Count != 0 ||
                project.FindFloor("L0") != null ||
                project.FindFloor("L1") != null ||
                project.FindFloor("L2") != null)
                throw new InvalidOperationException("Level Z runtime probe requires a fresh canonical project.");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
            project.Floors.Add(new FloorDefinition("L2", "Level 2", 7d));
            project.ActiveFloorId = "L0";
            return project;
        }

        private static ProjectState CreateProject(string id)
        {
            var project = new ProjectState(id, "Level Z Runtime Probe");
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3d));
            project.Floors.Add(new FloorDefinition("L2", "Level 2", 7d));
            project.ActiveFloorId = "L0";
            return project;
        }

        private static ProjectElement AddElement(ProjectState project, string id, ElementCategory category, SourceReference source)
        {
            var element = new ProjectElement(id, category, string.Empty, "L0", string.Empty);
            element.SourceHandles.Add(source.Handle);
            project.Elements.Add(element);
            return element;
        }

        private static void AssignBounded(ProjectState project, ProjectElement element, double bottomOffsetM, double topOffsetM)
        {
            Require(ProjectFloorService.AssignBottomLevel(project, "L1", new[] { element }) == 1, element.Id + " Bottom Level assignment");
            Require(ProjectFloorService.AssignTopLevel(project, "L2", new[] { element }) == 1, element.Id + " Top Level assignment");
            Set(element, ProjectFloorService.BottomLevelOffsetKey, bottomOffsetM);
            Set(element, ProjectFloorService.TopLevelOffsetKey, topOffsetM);
        }

        private static bool VerifyTopOnlyFailsBeforeMutation(Document document, SourceReference source)
        {
            var project = CreateProject("level-z-top-only");
            var element = AddElement(project, "top-only-wall", ElementCategory.ArchitecturalWall, source);
            Set(element, "ThicknessM", 0.2d);
            Set(element, "HeightM", 3d);
            element.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
            Select(document, source.ObjectId);
            try
            {
                WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall);
            }
            catch (InvalidOperationException)
            {
                var restored = project.FindElement(element.Id);
                Require(restored != null && !restored.Properties.ContainsKey("GeneratedSolidHandle"), "Top-only failure must not mutate native ownership");
                return true;
            }
            throw new InvalidOperationException("Top-only Level placement did not fail closed.");
        }

        private static ProbeSources CreateSources(Document document)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var unitsPerMeter = CadGeometryGuard.ToDrawingUnits(document, 1d, "Level Z probe meter scale");
                var result = new ProbeSources
                {
                    LegacyWall = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 0d, 0d, 1d), AtMeters(unitsPerMeter, 5d, 0d, 1d)),
                    BoundedWall = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 0d, 3d, 9d), AtMeters(unitsPerMeter, 5d, 3d, 9d)),
                    Beam = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 0d, 6d, 12d), AtMeters(unitsPerMeter, 5d, 6d, 12d)),
                    GlassWall = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 0d, 9d, 15d), AtMeters(unitsPerMeter, 5d, 9d, 15d)),
                    HostOpening = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 2d, 3d, 3.4d), AtMeters(unitsPerMeter, 3d, 3d, 3.4d)),
                    CurtainOpening = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 2d, 9d, 3.4d), AtMeters(unitsPerMeter, 3d, 9d, 3.4d)),
                    TopOnlyWall = AppendLine(document, transaction, modelSpace, AtMeters(unitsPerMeter, 0d, 12d, 0d), AtMeters(unitsPerMeter, 5d, 12d, 0d))
                };
                transaction.Commit();
                return result;
            }
        }

        private static Point3d AtMeters(double unitsPerMeter, double x, double y, double z) =>
            new Point3d(x * unitsPerMeter, y * unitsPerMeter, z * unitsPerMeter);

        private static SourceReference AppendLine(
            Document document,
            Transaction transaction,
            BlockTableRecord modelSpace,
            Point3d start,
            Point3d end)
        {
            var line = new Line(start, end);
            line.SetDatabaseDefaults(document.Database);
            var id = modelSpace.AppendEntity(line);
            transaction.AddNewlyCreatedDBObject(line, true);
            return new SourceReference { ObjectId = id, Handle = line.Handle.ToString() };
        }

        private static void Select(Document document, ObjectId id) => document.Editor.SetImpliedSelection(new[] { id });

        private static IReadOnlyList<string> Handles(ProjectElement element, string key)
        {
            var values = Property(element, key)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count == 0) throw new InvalidOperationException(element.Id + "/" + key + " is empty.");
            return values.AsReadOnly();
        }

        private static ZRange ReadZRange(Document document, IReadOnlyList<string> handles, string label)
        {
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != handles.Count) throw new InvalidOperationException(label + " live Solid3d count does not match ownership.");
            var minimum = double.PositiveInfinity;
            var maximum = double.NegativeInfinity;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased) throw new InvalidOperationException(label + " contains a non-live Solid3d.");
                    var extents = solid.GeometricExtents;
                    minimum = Math.Min(minimum, CadGeometryGuard.ToMeters(document, extents.MinPoint.Z, label + " minimum Z"));
                    maximum = Math.Max(maximum, CadGeometryGuard.ToMeters(document, extents.MaxPoint.Z, label + " maximum Z"));
                }
                transaction.Commit();
            }
            if (double.IsInfinity(minimum) || double.IsInfinity(maximum) || maximum <= minimum)
                throw new InvalidOperationException(label + " has invalid aggregate Z extents.");
            return new ZRange { MinimumM = minimum, MaximumM = maximum };
        }

        private static double ReadSolidVolume(Document document, string handle, string label)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new InvalidOperationException(label + " must resolve to one Solid3d.");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d
                    ?? throw new InvalidOperationException(label + " is not a Solid3d.");
                var volume = solid.MassProperties.Volume;
                transaction.Commit();
                if (double.IsNaN(volume) || double.IsInfinity(volume) || volume <= 0d)
                    throw new InvalidOperationException(label + " volume must be finite and positive.");
                return volume;
            }
        }

        private static void RequireSnapshot(ProjectElement element, string prefix, double bottomM, double topM, string mode)
        {
            RequireNear(bottomM, NumberProperty(element, prefix + "VerticalBottomM"), element.Id + "/" + prefix + " bottom snapshot");
            RequireNear(topM, NumberProperty(element, prefix + "VerticalTopM"), element.Id + "/" + prefix + " top snapshot");
            RequireNear(topM - bottomM, NumberProperty(element, prefix + "VerticalHeightM"), element.Id + "/" + prefix + " height snapshot");
            Require(string.Equals(Property(element, prefix + "VerticalMode"), mode, StringComparison.Ordinal), element.Id + "/" + prefix + " mode snapshot");
        }

        private static void RequireContained(ZRange inner, ZRange outer, string label)
        {
            const double toleranceM = 1e-6d;
            Require(inner.MinimumM >= outer.MinimumM - toleranceM, label + " minimum");
            Require(inner.MaximumM <= outer.MaximumM + toleranceM, label + " maximum");
        }

        private static void Set(ProjectElement element, string key, double value) =>
            element.Properties[key] = Number(value);

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var value) ? (value ?? string.Empty).Trim() : string.Empty;

        private static double NumberProperty(ProjectElement element, string key)
        {
            var raw = Property(element, key);
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(element.Id + "/" + key + " must be a finite invariant number.");
            return value;
        }

        private static void RequireNear(double expected, double actual, string label)
        {
            var tolerance = Math.Max(1e-7d, Math.Max(Math.Abs(expected), Math.Abs(actual)) * 1e-7d);
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException(label + " expected " + Number(expected) + " but was " + Number(actual) + ".");
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Level Z runtime assertion failed: " + label + ".");
        }

        private static void RequireAssemblyRevision(Assembly assembly, string sourceSha, string label)
        {
            RuntimeSourceIdentityGuard.RequireExactSourceLink(assembly, sourceSha, label);
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Level Z runtime result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("Level Z runtime result directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(
            string? requestedPath,
            string failureCode,
            ZRange? observedLegacyRange,
            ZRange? observedGlassRange,
            ZRange? observedFrameRange,
            ZRange? observedPanelRange,
            string hostBuildStage,
            string rebarStage,
            int? observedBeamRebarCount,
            int? observedBeamStirrupElementCount,
            int? observedBeamStirrupCount,
            ZRange? observedRebarRange,
            ZRange? observedStirrupRange,
            System.Exception error)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized))
                {
                    var lines = new List<string>
                    {
                        "status=FAIL",
                        "command=QS3DLEVELZPROBE",
                        "error_code=" + failureCode
                    };
                    if (observedLegacyRange != null)
                    {
                        lines.Add("observed_legacy_min_z_m=" + Number(observedLegacyRange.MinimumM));
                        lines.Add("observed_legacy_max_z_m=" + Number(observedLegacyRange.MaximumM));
                    }
                    AddObservedRange(lines, "glass", observedGlassRange);
                    AddObservedRange(lines, "frame", observedFrameRange);
                    AddObservedRange(lines, "panel", observedPanelRange);
                    if (string.Equals(failureCode, "LEVEL_Z_RUNTIME_HOST_BUILD_FAILED", StringComparison.Ordinal))
                        lines.Add("host_build_stage=" + RequireHostBuildStage(hostBuildStage));
                    if (string.Equals(failureCode, "LEVEL_Z_RUNTIME_REBAR_FAILED", StringComparison.Ordinal))
                    {
                        lines.Add("rebar_stage=" + RequireRebarStage(rebarStage));
                        AddObservedCount(lines, "observed_beam_rebar_count", observedBeamRebarCount);
                        AddObservedCount(lines, "observed_beam_stirrup_element_count", observedBeamStirrupElementCount);
                        AddObservedCount(lines, "observed_beam_stirrup_count", observedBeamStirrupCount);
                        AddObservedRange(lines, "rebar", observedRebarRange);
                        AddObservedRange(lines, "stirrup", observedStirrupRange);
                    }
                    if (string.Equals(failureCode, "LEVEL_Z_RUNTIME_HOST_BUILD_FAILED", StringComparison.Ordinal) ||
                        string.Equals(failureCode, "LEVEL_Z_RUNTIME_REBAR_FAILED", StringComparison.Ordinal))
                    {
                        lines.Add("exception_type=" + OneLine(error.GetType().FullName ?? error.GetType().Name));
                        lines.Add("exception_target=" + OneLine(error.TargetSite?.Name ?? string.Empty));
                        lines.Add("exception_hresult=0x" + error.HResult.ToString("X8", CultureInfo.InvariantCulture));
                    }
                    WriteMarkerAtomic(normalized, lines);
                }
            }
            catch { }
        }

        private static void AddObservedRange(List<string> lines, string prefix, ZRange? range)
        {
            if (range == null) return;
            lines.Add("observed_" + prefix + "_min_z_m=" + Number(range.MinimumM));
            lines.Add("observed_" + prefix + "_max_z_m=" + Number(range.MaximumM));
        }

        private static void AddObservedCount(List<string> lines, string key, int? value)
        {
            if (!value.HasValue) return;
            lines.Add(key + "=" + value.Value.ToString(CultureInfo.InvariantCulture));
        }

        private static string RequireHostBuildStage(string value)
        {
            switch (value)
            {
                case "legacy_wall_build":
                case "bounded_wall_build":
                case "glass_wall_build":
                case "beam_build":
                case "range_read":
                    return value;
                default:
                    return "legacy_wall_build";
            }
        }

        private static string RequireRebarStage(string value)
        {
            switch (value)
            {
                case "longitudinal_build":
                case "longitudinal_count":
                case "stirrup_build":
                case "stirrup_count":
                case "longitudinal_range_read":
                case "stirrup_range_read":
                case "longitudinal_containment":
                case "stirrup_containment":
                case "complete":
                    return value;
                default:
                    return "longitudinal_build";
            }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("Level Z runtime result already exists.");
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

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}