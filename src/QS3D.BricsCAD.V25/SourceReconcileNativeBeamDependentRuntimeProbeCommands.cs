using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 P03 probe for one production Direct Draw Beam whose
    /// authoritative LINE is edited by BricsCAD's native top-level MOVE. The probe
    /// supplies only bounded rebar fixture notation, validates state, and manages
    /// implied selection; production commands own all CAD generation and reconcile.
    /// </summary>
    public sealed class SourceReconcileNativeBeamDependentRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_RESULT";
        private const string PhaseVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_PHASE_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_NONCE";
        private const string DrawingVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_DWG";
        private const string ResultFileName = "source-reconcile-native-beam-dependent-result.txt";
        private const string PhaseFileName = "source-reconcile-native-beam-dependent-session1.txt";
        private const string Schema = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_RUNTIME_V1";
        private const string Boundary = "LOCAL_004_P03_BEAM_DEPENDENT_MOVE";
        private const double MetricToleranceM = 1e-8d;
        private const double NativeToleranceM = 1e-6d;
        private static readonly object Sync = new object();
        private static SequenceState? _state;

        [CommandMethod("QS3DSRBEAMP03PREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            Execute("prepare", () =>
            {
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Initial);
                RequireSemanticState(owner);
                RequireBeamQuantities(owner);
                RequireNoDependentRebarMetadata(owner);
                var initialHost = RequireHost(context.Document, context.Project, owner, ExpectedStage.Initial);

                var rollback = ProjectStateSnapshot.Capture(context.Project);
                try
                {
                    owner.SetProperty("RebarNotation", "4D16");
                    owner.SetProperty("RebarStirrupNotation", "D8@1000");
                    context.Project.Touch();
                }
                catch
                {
                    rollback.Restore(context.Project);
                    throw;
                }
                RequireFixtureConfiguration(owner);
                lock (Sync)
                {
                    _state = new SequenceState(
                        context.Document,
                        context.Project.ProjectId,
                        owner.Id,
                        owner.SourceHandles.Single(),
                        context.Nonce,
                        initialHost);
                    _state.FixtureConfigurationVerified = true;
                }
                SetExactSourceSelection(context.Document, owner.SourceHandles.Single());
            });
        }

        [CommandMethod("QS3DSRBEAMP03BASELINE", CommandFlags.Modal)]
        public void CaptureDependentBaseline()
        {
            Execute("dependent_baseline", () =>
            {
                var context = Context();
                var state = State(context, "PREPARED");
                var owner = Owner(context, state);
                RequireSourceGeometry(context.Document, owner, ExpectedStage.Initial);
                RequireSemanticState(owner);
                RequireBeamQuantities(owner);
                RequireFixtureConfiguration(owner);
                var baseline = RequireOutputs(context.Document, context.Project, owner, ExpectedStage.Initial);
                if (!SameSolid(state.InitialHost, baseline.Host))
                    throw new ProbeFailure("GENERATED_MUTATED_BY_NATIVE_MOVE");
                state.Baseline = baseline;
                state.DependentBaselineVerified = true;
                state.Phase = "BASELINED";
            });
        }

        [CommandMethod("QS3DSRBEAMP03SELECT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SelectSource()
        {
            Execute("select_source", () =>
            {
                var context = Context();
                var state = State(context);
                var id = ResolveSource(context.Document, state.SourceHandle);
                context.Document.Editor.SetImpliedSelection(new[] { id });
                var selected = context.Document.Editor.SelectImplied();
                if (selected.Status != PromptStatus.OK || selected.Value == null)
                    throw new ProbeFailure("SELECTION_REJECTED");
                var ids = selected.Value.GetObjectIds();
                if (ids.Length != 1 || ids[0] != id)
                    throw new ProbeFailure("SELECTION_REJECTED");
            });
        }

        [CommandMethod("QS3DSRBEAMP03MOVECHECK", CommandFlags.Modal)]
        public void CheckNativeMove()
        {
            Execute("native_move", () =>
            {
                var context = Context();
                var state = State(context, "BASELINED");
                var owner = Owner(context, state);
                try { RequireSourceGeometry(context.Document, owner, ExpectedStage.Moved); }
                catch { throw new ProbeFailure("NATIVE_MOVE_GEOMETRY_REJECTED"); }
                try
                {
                    RequireSemanticState(owner);
                    RequireBeamQuantities(owner);
                    RequireFixtureConfiguration(owner);
                }
                catch { throw new ProbeFailure("NATIVE_MOVE_SEMANTIC_REJECTED"); }

                var current = RequireOutputs(context.Document, context.Project, owner, ExpectedStage.Initial);
                if (!SameOutputs(state.RequiredBaseline, current))
                    throw new ProbeFailure("GENERATED_MUTATED_BY_NATIVE_MOVE");
                state.NativeBeamMoveVerified = true;
                state.PreSyncOutputIsolationVerified = true;
                state.Phase = "MOVED";
            });
        }

        [CommandMethod("QS3DSRBEAMP03SYNCCHECK", CommandFlags.Modal)]
        public void CheckReconcile()
        {
            Execute("check_reconcile", () =>
            {
                var context = Context();
                var state = State(context, "MOVED");
                var owner = Owner(context, state);
                RequireSourceGeometry(context.Document, owner, ExpectedStage.Moved);
                RequireSemanticState(owner);
                RequireBeamQuantities(owner);
                RequireFixtureConfiguration(owner);
                RequireNoGenerated(context.Document, owner, state.RequiredBaseline);
                state.SourceReconcileVerified = true;
                state.HostInvalidationVerified = true;
                state.RebarInvalidationVerified = true;
                state.StirrupInvalidationVerified = true;
                state.Phase = "SYNCED";
            });
        }

        [CommandMethod("QS3DSRBEAMP03FINAL", CommandFlags.Modal)]
        public void FinalizeSessionOne()
        {
            Execute("final_rebuild", () =>
            {
                var context = Context();
                var state = State(context, "SYNCED");
                var owner = Owner(context, state);
                RequireSourceGeometry(context.Document, owner, ExpectedStage.Moved);
                RequireSemanticState(owner);
                RequireBeamQuantities(owner);
                RequireFixtureConfiguration(owner);
                var rebuilt = RequireOutputs(context.Document, context.Project, owner, ExpectedStage.Moved);
                RequireTranslatedReplacement(state.RequiredBaseline, rebuilt);
                RequireScopedHealth(context.Document, context.Project, owner, rebuilt);
                state.DependentRebuildVerified = true;
                state.ScopedHealthVerified = true;
                state.Phase = "FINAL_REBUILT";
                WriteMarkerAtomic(RequiredPath(PhaseVariable, PhaseFileName), EvidenceLines(
                    "PASS",
                    context.Nonce,
                    coldReopenVerified: false,
                    state));
            });
        }

        [CommandMethod("QS3DSRBEAMP03REOPEN", CommandFlags.Modal)]
        public void Reopen()
        {
            Execute("cold_reopen", () =>
            {
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Moved);
                RequireSourceGeometry(context.Document, owner, ExpectedStage.Moved);
                RequireSemanticState(owner);
                RequireBeamQuantities(owner);
                RequireFixtureConfiguration(owner);
                var outputs = RequireOutputs(context.Document, context.Project, owner, ExpectedStage.Moved);
                RequireScopedHealth(context.Document, context.Project, owner, outputs);
                var phase = ReadPhaseEvidence(context.Nonce);
                WriteMarkerAtomic(RequiredPath(ResultVariable, ResultFileName), new[]
                {
                    "status=PASS",
                    "command=QS3DSRBEAMP03REOPEN",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=" + Boundary,
                    "production_local004_p03_qualified=true",
                    "fixture_configuration_verified=" + phase["fixture_configuration_verified"],
                    "dependent_baseline_verified=" + phase["dependent_baseline_verified"],
                    "native_beam_move_verified=" + phase["native_beam_move_verified"],
                    "pre_sync_output_isolation_verified=" + phase["pre_sync_output_isolation_verified"],
                    "source_reconcile_verified=" + phase["source_reconcile_verified"],
                    "host_invalidation_verified=" + phase["host_invalidation_verified"],
                    "rebar_invalidation_verified=" + phase["rebar_invalidation_verified"],
                    "stirrup_invalidation_verified=" + phase["stirrup_invalidation_verified"],
                    "dependent_rebuild_verified=" + phase["dependent_rebuild_verified"],
                    "scoped_health_verified=" + phase["scoped_health_verified"],
                    "cold_reopen_verified=true",
                    "source_type=LINE_BEAM",
                    "edit_command=MOVE",
                    "output_families=HOST_LONGITUDINAL_STIRRUP",
                    "error_code=NONE"
                });
            });
        }

        private static void Execute(string phase, Action action)
        {
            try { action(); }
            catch (ProbeFailure failure) { TryWriteFailure(phase, failure.Code); }
            catch { TryWriteFailure(phase, "STATE_REJECTED"); }
        }

        private static ProbeContext Context(bool requireState = true)
        {
            var nonce = RequiredNonce();
            RequiredPath(ResultVariable, ResultFileName);
            RequiredPath(PhaseVariable, PhaseFileName);
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new ProbeFailure("ACTIVE_DOCUMENT_MISSING");
            RequireExactDocument(document);
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new ProbeFailure("PROJECT_MISSING");
            var context = new ProbeContext(document, project, nonce);
            if (requireState) State(context);
            return context;
        }

        private static SequenceState State(ProbeContext context, string? expectedPhase = null)
        {
            SequenceState state;
            lock (Sync) state = _state ?? throw new ProbeFailure("SEQUENCE_NOT_INITIALIZED");
            if (!ReferenceEquals(state.Document, context.Document) ||
                !string.Equals(state.ProjectId, context.Project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(state.Nonce, context.Nonce, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_CONTEXT_CHANGED");
            if (expectedPhase != null && !string.Equals(state.Phase, expectedPhase, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_ORDER_REJECTED");
            return state;
        }

        private static ProjectElement Owner(ProbeContext context, SequenceState state)
        {
            var owner = context.Project.FindElement(state.OwnerId);
            if (owner == null || owner.Category != ElementCategory.Beam ||
                owner.SourceHandles.Count != 1 ||
                !string.Equals(owner.SourceHandles[0], state.SourceHandle, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("SOURCE_OWNER_REJECTED");
            ResolveSource(context.Document, state.SourceHandle);
            return owner;
        }

        private static ProjectElement FindUniqueOwner(Document document, ProjectState project, ExpectedStage stage)
        {
            var matches = new List<ProjectElement>();
            foreach (var candidate in project.Elements.Where(element =>
                element.Category == ElementCategory.Beam && element.SourceHandles.Count == 1))
            {
                try
                {
                    RequireSourceGeometry(document, candidate, stage);
                    matches.Add(candidate);
                }
                catch { }
            }
            if (matches.Count != 1) throw new ProbeFailure("SOURCE_OWNER_REJECTED");
            return matches[0];
        }

        private static ObjectId ResolveSource(Document document, string sourceHandle)
        {
            var ids = CadHandleService.Resolve(document, new[] { sourceHandle });
            if (ids.Count != 1) throw new ProbeFailure("SOURCE_MISSING");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Line;
                if (line == null || line.IsErased)
                    throw new ProbeFailure("SOURCE_TYPE_REJECTED");
            }
            return ids[0];
        }

        private static void SetExactSourceSelection(Document document, string sourceHandle)
        {
            var id = ResolveSource(document, sourceHandle);
            document.Editor.SetImpliedSelection(new[] { id });
            var selected = document.Editor.SelectImplied();
            if (selected.Status != PromptStatus.OK || selected.Value == null)
                throw new ProbeFailure("SELECTION_REJECTED");
            var ids = selected.Value.GetObjectIds();
            if (ids.Length != 1 || ids[0] != id)
                throw new ProbeFailure("SELECTION_REJECTED");
        }

        private static void RequireSourceGeometry(Document document, ProjectElement owner, ExpectedStage stage)
        {
            var id = ResolveSource(document, owner.SourceHandles.Single());
            var expectedY = stage == ExpectedStage.Initial ? 0d : 1d;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                    ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                RequireNear(Meters(document, line.StartPoint.X), 0d, MetricToleranceM, "source start X");
                RequireNear(Meters(document, line.StartPoint.Y), expectedY, MetricToleranceM, "source start Y");
                RequireNear(Meters(document, line.StartPoint.Z), 0d, MetricToleranceM, "source start Z");
                RequireNear(Meters(document, line.EndPoint.X), 5d, MetricToleranceM, "source end X");
                RequireNear(Meters(document, line.EndPoint.Y), expectedY, MetricToleranceM, "source end Y");
                RequireNear(Meters(document, line.EndPoint.Z), 0d, MetricToleranceM, "source end Z");
                RequireNear(Meters(document, line.Length), 5d, MetricToleranceM, "source length");
            }
        }

        private static void RequireSemanticState(ProjectElement owner)
        {
            RequireProperty(owner, "LengthM", 5d);
            RequireProperty(owner, "WidthM", .3d);
            RequireProperty(owner, "HeightM", .5d);
            RequireProperty(owner, "BottomOffsetM", 0d);
        }

        private static void RequireBeamQuantities(ProjectElement owner)
        {
            RequireQuantity(owner, "LengthM", 5d);
            RequireQuantity(owner, "HeightM", .5d);
            RequireQuantity(owner, "CrossSectionAreaM2", .15d);
            RequireQuantity(owner, "GrossVolumeM3", .75d);
            RequireQuantity(owner, "NetVolumeM3", .75d);
            RequireQuantity(owner, "FormworkM2", 6.5d);
        }

        private static void RequireFixtureConfiguration(ProjectElement owner)
        {
            if (!owner.Properties.TryGetValue("RebarNotation", out var rebar) ||
                !string.Equals(rebar, "4D16", StringComparison.Ordinal) ||
                !owner.Properties.TryGetValue("RebarStirrupNotation", out var stirrup) ||
                !string.Equals(stirrup, "D8@1000", StringComparison.Ordinal))
                throw new ProbeFailure("FIXTURE_CONFIGURATION_REJECTED");
        }

        private static void RequireNoDependentRebarMetadata(ProjectElement owner)
        {
            if (owner.Properties.Keys.Any(key =>
                key.StartsWith("GeneratedRebar", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("GeneratedBeamStirrup", StringComparison.OrdinalIgnoreCase)))
                throw new ProbeFailure("GENERATED_METADATA_REJECTED");
        }

        private static void RequireProperty(ProjectElement owner, string key, double expected)
        {
            if (!owner.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new ProbeFailure("SEMANTIC_METRICS_REJECTED");
            RequireNear(value, expected, MetricToleranceM, key);
        }

        private static void RequireQuantity(ProjectElement owner, string key, double expected)
        {
            if (!owner.Quantities.TryGetValue(key, out var value))
                throw new ProbeFailure("SEMANTIC_METRICS_REJECTED");
            RequireNear(value, expected, MetricToleranceM, key);
        }

        private static SolidSnapshot RequireHost(
            Document document,
            ProjectState project,
            ProjectElement owner,
            ExpectedStage stage)
        {
            if (!owner.Properties.TryGetValue("GeneratedSolidHandle", out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var handles = ParseHandles(raw);
            if (handles.Count != 1) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != 1) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased || !GeneratedGeometryService.HasMatchingOwnership(solid, project, owner))
                    throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
                var snapshot = Snapshot(document, solid, handles[0]);
                RequireHostGeometry(snapshot, stage);
                return snapshot;
            }
        }

        private static OutputSnapshot RequireOutputs(
            Document document,
            ProjectState project,
            ProjectElement owner,
            ExpectedStage stage)
        {
            var host = RequireHost(document, project, owner, stage);
            var rebar = RequireRebarSet(document, project, owner, "GeneratedRebarHandles", "GeneratedRebarCount", 4);
            var stirrups = RequireRebarSet(document, project, owner, "GeneratedBeamStirrupHandles", "GeneratedBeamStirrupCount", 6);
            RequireContained(host, rebar, "REBAR_HOST_CONTAINMENT_REJECTED");
            RequireContained(host, stirrups, "STIRRUP_HOST_CONTAINMENT_REJECTED");
            return new OutputSnapshot(host, rebar, stirrups);
        }

        private static IReadOnlyList<SolidSnapshot> RequireRebarSet(
            Document document,
            ProjectState project,
            ProjectElement owner,
            string handlesKey,
            string countKey,
            int expectedCount)
        {
            if (!owner.Properties.TryGetValue(handlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new ProbeFailure("GENERATED_METADATA_REJECTED");
            if (!owner.Properties.TryGetValue(countKey, out var rawCount) ||
                !int.TryParse(rawCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var recordedCount) ||
                recordedCount != expectedCount)
                throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            var handles = ParseHandles(raw);
            if (handles.Count != expectedCount) throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != expectedCount) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var result = new List<SolidSnapshot>(expectedCount);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased ||
                        !GeneratedRebarNativeOwnershipService.HasMatchingOwnership(solid, project, owner, handlesKey))
                        throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
                    result.Add(Snapshot(document, solid, id.Handle.ToString()));
                }
            }
            return result.OrderBy(item => item.Handle, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static IReadOnlyList<string> ParseHandles(string raw)
        {
            var result = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => CadHandleService.NormalizeHexHandle(value)
                    ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"))
                .ToList();
            if (result.Count != result.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new ProbeFailure("GENERATED_HANDLE_REJECTED");
            return result.AsReadOnly();
        }

        private static SolidSnapshot Snapshot(Document document, Solid3d solid, string handle)
        {
            var extents = solid.GeometricExtents;
            var unitsPerMeter = Drawing(document, 1d);
            return new SolidSnapshot(
                CadHandleService.NormalizeHexHandle(handle) ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"),
                Math.Abs(solid.MassProperties.Volume) / (unitsPerMeter * unitsPerMeter * unitsPerMeter),
                Meters(document, extents.MinPoint.X), Meters(document, extents.MaxPoint.X),
                Meters(document, extents.MinPoint.Y), Meters(document, extents.MaxPoint.Y),
                Meters(document, extents.MinPoint.Z), Meters(document, extents.MaxPoint.Z));
        }

        private static void RequireHostGeometry(SolidSnapshot snapshot, ExpectedStage stage)
        {
            var centerY = stage == ExpectedStage.Initial ? 0d : 1d;
            RequireNear(snapshot.VolumeM3, .75d, NativeToleranceM, "host volume");
            RequireNear(snapshot.MinimumXM, 0d, NativeToleranceM, "host minimum X");
            RequireNear(snapshot.MaximumXM, 5d, NativeToleranceM, "host maximum X");
            RequireNear(snapshot.MinimumYM, centerY - .15d, NativeToleranceM, "host minimum Y");
            RequireNear(snapshot.MaximumYM, centerY + .15d, NativeToleranceM, "host maximum Y");
            RequireNear(snapshot.MinimumZM, 0d, NativeToleranceM, "host minimum Z");
            RequireNear(snapshot.MaximumZM, .5d, NativeToleranceM, "host maximum Z");
        }

        private static void RequireContained(
            SolidSnapshot host,
            IEnumerable<SolidSnapshot> dependents,
            string failureCode)
        {
            foreach (var dependent in dependents)
            {
                if (dependent.MinimumXM < host.MinimumXM - NativeToleranceM ||
                    dependent.MaximumXM > host.MaximumXM + NativeToleranceM ||
                    dependent.MinimumYM < host.MinimumYM - NativeToleranceM ||
                    dependent.MaximumYM > host.MaximumYM + NativeToleranceM ||
                    dependent.MinimumZM < host.MinimumZM - NativeToleranceM ||
                    dependent.MaximumZM > host.MaximumZM + NativeToleranceM)
                    throw new ProbeFailure(failureCode);
            }
        }

        private static bool SameSolid(SolidSnapshot left, SolidSnapshot right) =>
            string.Equals(left.Handle, right.Handle, StringComparison.OrdinalIgnoreCase) &&
            Near(left.VolumeM3, right.VolumeM3, NativeToleranceM) &&
            Near(left.MinimumXM, right.MinimumXM, NativeToleranceM) &&
            Near(left.MaximumXM, right.MaximumXM, NativeToleranceM) &&
            Near(left.MinimumYM, right.MinimumYM, NativeToleranceM) &&
            Near(left.MaximumYM, right.MaximumYM, NativeToleranceM) &&
            Near(left.MinimumZM, right.MinimumZM, NativeToleranceM) &&
            Near(left.MaximumZM, right.MaximumZM, NativeToleranceM);

        private static bool SameOutputs(OutputSnapshot left, OutputSnapshot right) =>
            SameSolid(left.Host, right.Host) && SameSet(left.Rebar, right.Rebar) && SameSet(left.Stirrups, right.Stirrups);

        private static bool SameSet(IReadOnlyList<SolidSnapshot> left, IReadOnlyList<SolidSnapshot> right) =>
            left.Count == right.Count && left.All(item => right.Any(candidate => SameSolid(item, candidate)));

        private static void RequireNoGenerated(Document document, ProjectElement owner, OutputSnapshot previous)
        {
            if (owner.Properties.Keys.Any(key =>
                key.StartsWith("GeneratedSolid", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("GeneratedRebar", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("GeneratedBeamStirrup", StringComparison.OrdinalIgnoreCase)))
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
            if (CadHandleService.GetLiveHandles(document, previous.Handles).Count != 0)
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
        }

        private static void RequireTranslatedReplacement(OutputSnapshot previous, OutputSnapshot current)
        {
            if (previous.Handles.Intersect(current.Handles, StringComparer.OrdinalIgnoreCase).Any())
                throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");
            RequireTranslatedSolid(previous.Host, current.Host, 1d);
            RequireTranslatedSet(previous.Rebar, current.Rebar, 1d);
            RequireTranslatedSet(previous.Stirrups, current.Stirrups, 1d);
        }

        private static void RequireTranslatedSet(
            IReadOnlyList<SolidSnapshot> previous,
            IReadOnlyList<SolidSnapshot> current,
            double deltaY)
        {
            if (previous.Count != current.Count) throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            var previousVolumes = previous.Select(item => item.VolumeM3).OrderBy(value => value).ToArray();
            var currentVolumes = current.Select(item => item.VolumeM3).OrderBy(value => value).ToArray();
            for (var index = 0; index < previousVolumes.Length; index++)
                RequireNear(currentVolumes[index], previousVolumes[index], NativeToleranceM, "dependent volume");
            RequireTranslatedSolid(Bounds(previous), Bounds(current), deltaY);
        }

        private static SolidSnapshot Bounds(IReadOnlyList<SolidSnapshot> items)
        {
            if (items.Count == 0) throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            return new SolidSnapshot(
                string.Empty,
                items.Sum(item => item.VolumeM3),
                items.Min(item => item.MinimumXM), items.Max(item => item.MaximumXM),
                items.Min(item => item.MinimumYM), items.Max(item => item.MaximumYM),
                items.Min(item => item.MinimumZM), items.Max(item => item.MaximumZM));
        }

        private static void RequireTranslatedSolid(SolidSnapshot previous, SolidSnapshot current, double deltaY)
        {
            RequireNear(current.VolumeM3, previous.VolumeM3, NativeToleranceM, "translated volume");
            RequireNear(current.MinimumXM, previous.MinimumXM, NativeToleranceM, "translated minimum X");
            RequireNear(current.MaximumXM, previous.MaximumXM, NativeToleranceM, "translated maximum X");
            RequireNear(current.MinimumYM, previous.MinimumYM + deltaY, NativeToleranceM, "translated minimum Y");
            RequireNear(current.MaximumYM, previous.MaximumYM + deltaY, NativeToleranceM, "translated maximum Y");
            RequireNear(current.MinimumZM, previous.MinimumZM, NativeToleranceM, "translated minimum Z");
            RequireNear(current.MaximumZM, previous.MaximumZM, NativeToleranceM, "translated maximum Z");
        }

        private static void RequireScopedHealth(
            Document document,
            ProjectState project,
            ProjectElement owner,
            OutputSnapshot outputs)
        {
            var sourceHandles = new HashSet<string>(owner.SourceHandles, StringComparer.OrdinalIgnoreCase);
            var generatedHandles = new HashSet<string>(outputs.Handles, StringComparer.OrdinalIgnoreCase);
            var rebarHandles = new HashSet<string>(outputs.Rebar.Select(item => item.Handle), StringComparer.OrdinalIgnoreCase);
            var stirrupHandles = new HashSet<string>(outputs.Stirrups.Select(item => item.Handle), StringComparer.OrdinalIgnoreCase);
            var issues = new List<ModelHealthIssue>();
            issues.AddRange(new ModelHealthService()
                .Inspect(project, sourceHandles, generatedHandles)
                .Where(issue => Relevant(issue, owner)));
            issues.AddRange(GeneratedSolidRuntimeHealthService.Inspect(document, project).Where(issue => Relevant(issue, owner)));
            issues.AddRange(new GeneratedRebarHealthService().Inspect(project, rebarHandles).Where(issue => Relevant(issue, owner)));
            issues.AddRange(new GeneratedBeamStirrupHealthService().Inspect(project, stirrupHandles).Where(issue => Relevant(issue, owner)));
            issues.AddRange(new GeneratedRebarOwnershipHealthService().Inspect(project).Where(issue => Relevant(issue, owner)));
            issues.AddRange(new GeneratedRebarModeHealthService().Inspect(project).Where(issue => Relevant(issue, owner)));
            if (issues.Any(issue => issue.Severity != HealthSeverity.Info))
                throw new ProbeFailure("HEALTH_REJECTED");
        }

        private static bool Relevant(ModelHealthIssue issue, ProjectElement owner) =>
            string.IsNullOrEmpty(issue.ElementId) || string.Equals(issue.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase);

        private static double Drawing(Document document, double meters) =>
            CadUnitService.MetersToDrawingUnits(document, meters);

        private static double Meters(Document document, double drawingUnits) =>
            CadUnitService.DrawingUnitsToMeters(document, drawingUnits);

        private static bool Near(double actual, double expected, double tolerance) =>
            !double.IsNaN(actual) && !double.IsInfinity(actual) && Math.Abs(actual - expected) <= tolerance;

        private static void RequireNear(double actual, double expected, double tolerance, string label)
        {
            if (!Near(actual, expected, tolerance))
                throw new InvalidOperationException("Native Beam dependent probe mismatch at " + label + ".");
        }

        private static IReadOnlyList<string> EvidenceLines(
            string status,
            string nonce,
            bool coldReopenVerified,
            SequenceState state) =>
            new[]
            {
                "status=" + status,
                "command=QS3DSRBEAMP03FINAL",
                "nonce=" + nonce,
                "schema=" + Schema,
                "qualification_boundary=" + Boundary,
                "production_local004_p03_qualified=false",
                "fixture_configuration_verified=" + Boolean(state.FixtureConfigurationVerified),
                "dependent_baseline_verified=" + Boolean(state.DependentBaselineVerified),
                "native_beam_move_verified=" + Boolean(state.NativeBeamMoveVerified),
                "pre_sync_output_isolation_verified=" + Boolean(state.PreSyncOutputIsolationVerified),
                "source_reconcile_verified=" + Boolean(state.SourceReconcileVerified),
                "host_invalidation_verified=" + Boolean(state.HostInvalidationVerified),
                "rebar_invalidation_verified=" + Boolean(state.RebarInvalidationVerified),
                "stirrup_invalidation_verified=" + Boolean(state.StirrupInvalidationVerified),
                "dependent_rebuild_verified=" + Boolean(state.DependentRebuildVerified),
                "scoped_health_verified=" + Boolean(state.ScopedHealthVerified),
                "cold_reopen_verified=" + Boolean(coldReopenVerified),
                "source_type=LINE_BEAM",
                "edit_command=MOVE",
                "output_families=HOST_LONGITUDINAL_STIRRUP",
                "error_code=NONE"
            };

        private static Dictionary<string, string> ReadPhaseEvidence(string nonce)
        {
            var path = RequiredPath(PhaseVariable, PhaseFileName);
            if (!File.Exists(path)) throw new ProbeFailure("PHASE_EVIDENCE_MISSING");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path, new UTF8Encoding(false, true)))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0 || separator == line.Length - 1)
                    throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
                var key = line.Substring(0, separator);
                if (result.ContainsKey(key)) throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
                result.Add(key, line.Substring(separator + 1));
            }
            foreach (var pair in new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["status"] = "PASS",
                ["schema"] = Schema,
                ["qualification_boundary"] = Boundary,
                ["nonce"] = nonce,
                ["production_local004_p03_qualified"] = "false",
                ["fixture_configuration_verified"] = "true",
                ["dependent_baseline_verified"] = "true",
                ["native_beam_move_verified"] = "true",
                ["pre_sync_output_isolation_verified"] = "true",
                ["source_reconcile_verified"] = "true",
                ["host_invalidation_verified"] = "true",
                ["rebar_invalidation_verified"] = "true",
                ["stirrup_invalidation_verified"] = "true",
                ["dependent_rebuild_verified"] = "true",
                ["scoped_health_verified"] = "true",
                ["cold_reopen_verified"] = "false",
                ["source_type"] = "LINE_BEAM",
                ["edit_command"] = "MOVE",
                ["output_families"] = "HOST_LONGITUDINAL_STIRRUP",
                ["error_code"] = "NONE"
            })
            {
                if (!result.TryGetValue(pair.Key, out var value) ||
                    !string.Equals(value, pair.Value, StringComparison.Ordinal))
                    throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
            }
            return result;
        }

        private static string RequiredNonce()
        {
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new ProbeFailure("AUTOMATION_CONTEXT_REJECTED");
            return nonce;
        }

        private static string RequiredPath(string variable, string fileName)
        {
            var raw = (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new ProbeFailure("RESULT_PATH_REJECTED");
            var path = Path.GetFullPath(raw);
            if (!string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)) ||
                !Directory.Exists(Path.GetDirectoryName(path)))
                throw new ProbeFailure("RESULT_PATH_REJECTED");
            return path;
        }

        private static void RequireExactDocument(Document document)
        {
            var expected = Path.GetFullPath(Environment.GetEnvironmentVariable(DrawingVariable) ?? string.Empty);
            var actual = Path.GetFullPath(document.Name ?? string.Empty);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
        }

        private static void TryWriteFailure(string phase, string code)
        {
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _)) return;
                var path = RequiredPath(ResultVariable, ResultFileName);
                if (File.Exists(path)) return;
                WriteMarkerAtomic(path, new[]
                {
                    "status=FAIL",
                    "command=QS3DSRBEAMP03REOPEN",
                    "nonce=" + nonce,
                    "schema=" + Schema,
                    "qualification_boundary=" + Boundary,
                    "production_local004_p03_qualified=false",
                    "error_code=SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_RUNTIME_FAILED",
                    "failure_phase=" + OneLine(phase),
                    "failure_code=" + OneLine(code)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("Native Beam dependent probe marker already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string? value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private static string Boolean(bool value) => value ? "true" : "false";

        private enum ExpectedStage { Initial, Moved }

        private sealed class SolidSnapshot
        {
            public SolidSnapshot(
                string handle,
                double volumeM3,
                double minimumXM,
                double maximumXM,
                double minimumYM,
                double maximumYM,
                double minimumZM,
                double maximumZM)
            {
                Handle = handle;
                VolumeM3 = volumeM3;
                MinimumXM = minimumXM;
                MaximumXM = maximumXM;
                MinimumYM = minimumYM;
                MaximumYM = maximumYM;
                MinimumZM = minimumZM;
                MaximumZM = maximumZM;
            }
            public string Handle { get; }
            public double VolumeM3 { get; }
            public double MinimumXM { get; }
            public double MaximumXM { get; }
            public double MinimumYM { get; }
            public double MaximumYM { get; }
            public double MinimumZM { get; }
            public double MaximumZM { get; }
        }

        private sealed class OutputSnapshot
        {
            public OutputSnapshot(
                SolidSnapshot host,
                IReadOnlyList<SolidSnapshot> rebar,
                IReadOnlyList<SolidSnapshot> stirrups)
            {
                Host = host ?? throw new ArgumentNullException(nameof(host));
                Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
                Stirrups = stirrups ?? throw new ArgumentNullException(nameof(stirrups));
                Handles = new[] { host.Handle }
                    .Concat(rebar.Select(item => item.Handle))
                    .Concat(stirrups.Select(item => item.Handle))
                    .ToList()
                    .AsReadOnly();
            }
            public SolidSnapshot Host { get; }
            public IReadOnlyList<SolidSnapshot> Rebar { get; }
            public IReadOnlyList<SolidSnapshot> Stirrups { get; }
            public IReadOnlyList<string> Handles { get; }
        }

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce)
            { Document = document; Project = project; Nonce = nonce; }
            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
        }

        private sealed class SequenceState
        {
            public SequenceState(
                Document document,
                string projectId,
                string ownerId,
                string sourceHandle,
                string nonce,
                SolidSnapshot initialHost)
            {
                Document = document;
                ProjectId = projectId;
                OwnerId = ownerId;
                SourceHandle = sourceHandle;
                Nonce = nonce;
                InitialHost = initialHost;
            }
            public Document Document { get; }
            public string ProjectId { get; }
            public string OwnerId { get; }
            public string SourceHandle { get; }
            public string Nonce { get; }
            public SolidSnapshot InitialHost { get; }
            public OutputSnapshot? Baseline { get; set; }
            public OutputSnapshot RequiredBaseline => Baseline ?? throw new ProbeFailure("SEQUENCE_ORDER_REJECTED");
            public string Phase { get; set; } = "PREPARED";
            public bool FixtureConfigurationVerified { get; set; }
            public bool DependentBaselineVerified { get; set; }
            public bool NativeBeamMoveVerified { get; set; }
            public bool PreSyncOutputIsolationVerified { get; set; }
            public bool SourceReconcileVerified { get; set; }
            public bool HostInvalidationVerified { get; set; }
            public bool RebarInvalidationVerified { get; set; }
            public bool StirrupInvalidationVerified { get; set; }
            public bool DependentRebuildVerified { get; set; }
            public bool ScopedHealthVerified { get; set; }
        }

        private sealed class ProbeFailure : InvalidOperationException
        {
            public ProbeFailure(string code) : base("Native Beam dependent source-reconcile probe state rejected.")
            { Code = code; }
            public string Code { get; }
        }
    }
}
