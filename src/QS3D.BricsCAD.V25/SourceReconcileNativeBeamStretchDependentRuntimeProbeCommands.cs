using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 P04 probe. Production commands author a Beam,
    /// native STRETCH edits only the authoritative LINE, Source Reconcile invalidates
    /// stale output, and production builders recreate host/longitudinal/stirrup output.
    /// </summary>
    public sealed class SourceReconcileNativeBeamStretchDependentRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RESULT";
        private const string PhaseVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_PHASE_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_NONCE";
        private const string DrawingVariable = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_DWG";
        private const string ResultFileName = "source-reconcile-native-beam-stretch-dependent-result.txt";
        private const string PhaseFileName = "source-reconcile-native-beam-stretch-dependent-session1.txt";
        private const string Schema = "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RUNTIME_V1";
        private const string Boundary = "LOCAL_004_P04_BEAM_DEPENDENT_STRETCH";
        private const double MetricTolerance = 1e-8d;
        private const double NativeTolerance = 1e-5d;
        private static readonly object Sync = new object();
        private static ProbeState? _state;

        [CommandMethod("QS3DSRBEAMP04PREPARE", CommandFlags.Modal)]
        public void Prepare() => Execute("prepare", () =>
        {
            var context = Context(false);
            var owner = FindUniqueBeam(context.Document, context.Project, 5d);
            RequireSemantic(owner, 5d);
            RequireQuantities(owner, 5d);
            RequireHost(context.Document, context.Project, owner, 5d);
            if (owner.Properties.Keys.Any(IsDependentGeneratedKey))
                throw new ProbeFailure("GENERATED_METADATA_REJECTED");

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

            lock (Sync)
            {
                _state = new ProbeState(context.Document, context.Project.ProjectId, owner.Id,
                    owner.SourceHandles.Single(), context.Nonce);
            }
            SetSourceSelection(context.Document, owner.SourceHandles.Single());
        });

        [CommandMethod("QS3DSRBEAMP04SELECT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SelectSource() => Execute("select_source", () =>
        {
            var context = Context();
            var state = State(context);
            SetSourceSelection(context.Document, state.SourceHandle);
        });

        [CommandMethod("QS3DSRBEAMP04BASELINE", CommandFlags.Modal)]
        public void Baseline() => Execute("dependent_baseline", () =>
        {
            var context = Context();
            var state = State(context, "PREPARED");
            var owner = Owner(context, state);
            RequireSource(context.Document, owner, 5d);
            RequireSemantic(owner, 5d);
            RequireQuantities(owner, 5d);
            RequireFixture(owner);
            var output = RequireOutputs(context.Document, context.Project, owner, 5d, 6);
            RequireStirrupMetadata(owner, 6, .9884d);
            state.BaselineHandles = output.Handles;
            state.BaselineVerified = true;
            state.Phase = "BASELINED";
            SetSourceSelection(context.Document, state.SourceHandle);
        });

        [CommandMethod("QS3DSRBEAMP04STRETCHCHECK", CommandFlags.Modal)]
        public void StretchCheck() => Execute("native_stretch", () =>
        {
            var context = Context();
            var state = State(context, "BASELINED");
            var owner = Owner(context, state);
            RequireSource(context.Document, owner, 8d);
            // Native edit must not mutate semantic or generated state before reconcile.
            RequireSemantic(owner, 5d);
            RequireQuantities(owner, 5d);
            RequireFixture(owner);
            var output = RequireOutputs(context.Document, context.Project, owner, 5d, 6);
            if (!SetEquals(state.RequiredBaselineHandles, output.Handles))
                throw new ProbeFailure("GENERATED_MUTATED_BY_NATIVE_STRETCH");
            state.NativeStretchVerified = true;
            state.PreSyncIsolationVerified = true;
            state.Phase = "STRETCHED";
        });

        [CommandMethod("QS3DSRBEAMP04SYNCCHECK", CommandFlags.Modal)]
        public void SyncCheck() => Execute("check_reconcile", () =>
        {
            var context = Context();
            var state = State(context, "STRETCHED");
            var owner = Owner(context, state);
            RequireSource(context.Document, owner, 8d);
            RequireSemantic(owner, 8d);
            RequireQuantities(owner, 8d);
            RequireFixture(owner);
            if (owner.Properties.Keys.Any(IsAnyGeneratedKey))
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
            if (CadHandleService.GetLiveHandles(context.Document, state.RequiredBaselineHandles).Count != 0)
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
            state.ReconcileVerified = true;
            state.InvalidationVerified = true;
            state.Phase = "SYNCED";
            SetSourceSelection(context.Document, state.SourceHandle);
        });

        [CommandMethod("QS3DSRBEAMP04FINAL", CommandFlags.Modal)]
        public void Final() => Execute("final_rebuild", () =>
        {
            var context = Context();
            var state = State(context, "SYNCED");
            var owner = Owner(context, state);
            RequireSource(context.Document, owner, 8d);
            RequireSemantic(owner, 8d);
            RequireQuantities(owner, 8d);
            RequireFixture(owner);
            var output = RequireOutputs(context.Document, context.Project, owner, 8d, 9);
            RequireStirrupMetadata(owner, 9, .99275d);
            RequireLongitudinalExtent(output.Rebar, 8d);
            if (state.RequiredBaselineHandles.Intersect(output.Handles, StringComparer.OrdinalIgnoreCase).Any())
                throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");
            state.DependentRebuildVerified = true;
            state.StirrupRedistributionVerified = true;
            state.LongitudinalExtentVerified = true;
            state.Phase = "FINAL_REBUILT";
            WriteMarker(RequiredPath(PhaseVariable, PhaseFileName), Evidence(context.Nonce, state, false, false));
        });

        [CommandMethod("QS3DSRBEAMP04REOPEN", CommandFlags.Modal)]
        public void Reopen() => Execute("cold_reopen", () =>
        {
            var context = Context(false);
            var owner = FindUniqueBeam(context.Document, context.Project, 8d);
            RequireSource(context.Document, owner, 8d);
            RequireSemantic(owner, 8d);
            RequireQuantities(owner, 8d);
            RequireFixture(owner);
            var output = RequireOutputs(context.Document, context.Project, owner, 8d, 9);
            RequireStirrupMetadata(owner, 9, .99275d);
            RequireLongitudinalExtent(output.Rebar, 8d);
            var phase = ReadPhase(context.Nonce);
            WriteMarker(RequiredPath(ResultVariable, ResultFileName), new[]
            {
                "status=PASS",
                "command=QS3DSRBEAMP04REOPEN",
                "nonce=" + context.Nonce,
                "schema=" + Schema,
                "qualification_boundary=" + Boundary,
                "production_local004_p04_qualified=true",
                "baseline_verified=" + phase["baseline_verified"],
                "native_stretch_verified=" + phase["native_stretch_verified"],
                "pre_sync_output_isolation_verified=" + phase["pre_sync_output_isolation_verified"],
                "source_reconcile_verified=" + phase["source_reconcile_verified"],
                "dependent_invalidation_verified=" + phase["dependent_invalidation_verified"],
                "dependent_rebuild_verified=" + phase["dependent_rebuild_verified"],
                "stirrup_redistribution_verified=" + phase["stirrup_redistribution_verified"],
                "longitudinal_extent_verified=" + phase["longitudinal_extent_verified"],
                "cold_reopen_verified=true",
                "source_type=LINE_BEAM",
                "edit_command=STRETCH",
                "final_length_class=EIGHT_METERS",
                "stirrup_count_class=NINE_AT_D8_1000",
                "output_families=HOST_LONGITUDINAL_STIRRUP",
                "error_code=NONE"
            });
        });

        private static void Execute(string phase, Action action)
        {
            try { action(); }
            catch (ProbeFailure failure) { TryFailure(phase, failure.Code); }
            catch { TryFailure(phase, "STATE_REJECTED"); }
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

        private static ProbeState State(ProbeContext context, string? phase = null)
        {
            ProbeState state;
            lock (Sync) state = _state ?? throw new ProbeFailure("SEQUENCE_NOT_INITIALIZED");
            if (!ReferenceEquals(state.Document, context.Document) ||
                !string.Equals(state.ProjectId, context.Project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(state.Nonce, context.Nonce, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_CONTEXT_CHANGED");
            if (phase != null && !string.Equals(state.Phase, phase, StringComparison.Ordinal))
                throw new ProbeFailure("SEQUENCE_ORDER_REJECTED");
            return state;
        }

        private static ProjectElement Owner(ProbeContext context, ProbeState state)
        {
            var owner = context.Project.FindElement(state.OwnerId);
            if (owner == null || owner.Category != ElementCategory.Beam || owner.SourceHandles.Count != 1 ||
                !string.Equals(owner.SourceHandles[0], state.SourceHandle, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("SOURCE_OWNER_REJECTED");
            return owner;
        }

        private static ProjectElement FindUniqueBeam(Document document, ProjectState project, double lengthM)
        {
            var matches = project.Elements.Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Count == 1)
                .Where(x => TrySourceLength(document, x.SourceHandles[0], out var length) && Near(length, lengthM, MetricTolerance))
                .ToList();
            if (matches.Count != 1) throw new ProbeFailure("SOURCE_OWNER_REJECTED");
            return matches[0];
        }

        private static bool TrySourceLength(Document document, string handle, out double lengthM)
        {
            lengthM = 0d;
            try
            {
                var id = ResolveSource(document, handle);
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                    if (line == null || line.IsErased) return false;
                    lengthM = CadUnitService.DrawingUnitsToMeters(document, line.Length);
                    return true;
                }
            }
            catch { return false; }
        }

        private static ObjectId ResolveSource(Document document, string handle)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count != 1) throw new ProbeFailure("SOURCE_MISSING");
            return ids[0];
        }

        private static void SetSourceSelection(Document document, string handle)
        {
            var id = ResolveSource(document, handle);
            document.Editor.SetImpliedSelection(new[] { id });
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null ||
                selection.Value.GetObjectIds().Length != 1 || selection.Value.GetObjectIds()[0] != id)
                throw new ProbeFailure("SELECTION_REJECTED");
        }

        private static void RequireSource(Document document, ProjectElement owner, double lengthM)
        {
            var id = ResolveSource(document, owner.SourceHandles.Single());
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                    ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.StartPoint.X), 0d, "source start X");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.StartPoint.Y), 0d, "source start Y");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.EndPoint.X), lengthM, "source end X");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.EndPoint.Y), 0d, "source end Y");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.Length), lengthM, "source length");
            }
        }

        private static void RequireSemantic(ProjectElement owner, double lengthM)
        {
            RequireProperty(owner, "LengthM", lengthM);
            RequireProperty(owner, "WidthM", .3d);
            RequireProperty(owner, "HeightM", .5d);
            RequireProperty(owner, "BottomOffsetM", 0d);
        }

        private static void RequireQuantities(ProjectElement owner, double lengthM)
        {
            RequireQuantity(owner, "LengthM", lengthM);
            RequireQuantity(owner, "HeightM", .5d);
            RequireQuantity(owner, "CrossSectionAreaM2", .15d);
            RequireQuantity(owner, "GrossVolumeM3", lengthM * .15d);
            RequireQuantity(owner, "NetVolumeM3", lengthM * .15d);
            RequireQuantity(owner, "FormworkM2", lengthM * 1.3d);
        }

        private static void RequireFixture(ProjectElement owner)
        {
            if (!owner.Properties.TryGetValue("RebarNotation", out var rebar) || rebar != "4D16" ||
                !owner.Properties.TryGetValue("RebarStirrupNotation", out var stirrup) || stirrup != "D8@1000")
                throw new ProbeFailure("FIXTURE_CONFIGURATION_REJECTED");
        }

        private static void RequireProperty(ProjectElement owner, string key, double expected)
        {
            if (!owner.Properties.TryGetValue(key, out var raw) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new ProbeFailure("SEMANTIC_METRICS_REJECTED");
            RequireNear(value, expected, key);
        }

        private static void RequireQuantity(ProjectElement owner, string key, double expected)
        {
            if (!owner.Quantities.TryGetValue(key, out var value))
                throw new ProbeFailure("SEMANTIC_METRICS_REJECTED");
            RequireNear(value, expected, key);
        }

        private static OutputState RequireOutputs(Document document, ProjectState project, ProjectElement owner, double lengthM, int stirrupCount)
        {
            var host = RequireHost(document, project, owner, lengthM);
            var rebar = RequireOwnedSet(document, project, owner, "GeneratedRebarHandles", "GeneratedRebarCount", 4);
            var stirrups = RequireOwnedSet(document, project, owner, "GeneratedBeamStirrupHandles", "GeneratedBeamStirrupCount", stirrupCount);
            RequireContained(host, rebar, "REBAR_HOST_CONTAINMENT_REJECTED");
            RequireContained(host, stirrups, "STIRRUP_HOST_CONTAINMENT_REJECTED");
            return new OutputState(host, rebar, stirrups);
        }

        private static SolidState RequireHost(Document document, ProjectState project, ProjectElement owner, double lengthM)
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
                var state = Snapshot(document, solid, handles[0]);
                RequireNearNative(state.VolumeM3, lengthM * .15d, "host volume");
                RequireNearNative(state.MinX, 0d, "host min X");
                RequireNearNative(state.MaxX, lengthM, "host max X");
                RequireNearNative(state.MinY, -.15d, "host min Y");
                RequireNearNative(state.MaxY, .15d, "host max Y");
                RequireNearNative(state.MinZ, 0d, "host min Z");
                RequireNearNative(state.MaxZ, .5d, "host max Z");
                return state;
            }
        }

        private static IReadOnlyList<SolidState> RequireOwnedSet(Document document, ProjectState project, ProjectElement owner,
            string handlesKey, string countKey, int expectedCount)
        {
            if (!owner.Properties.TryGetValue(handlesKey, out var raw) || string.IsNullOrWhiteSpace(raw) ||
                !owner.Properties.TryGetValue(countKey, out var countRaw) ||
                !int.TryParse(countRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count != expectedCount)
                throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            var handles = ParseHandles(raw);
            if (handles.Count != expectedCount) throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != expectedCount) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var result = new List<SolidState>();
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
            return result.AsReadOnly();
        }

        private static void RequireStirrupMetadata(ProjectElement owner, int count, double actualSpacingM)
        {
            if (!owner.Properties.TryGetValue("GeneratedBeamStirrupCount", out var rawCount) ||
                !int.TryParse(rawCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var recorded) || recorded != count ||
                !owner.Properties.TryGetValue("GeneratedBeamStirrupActualSpacingM", out var rawSpacing) ||
                !double.TryParse(rawSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var spacing))
                throw new ProbeFailure("STIRRUP_REDISTRIBUTION_REJECTED");
            RequireNearNative(spacing, actualSpacingM, "stirrup actual spacing");
        }

        private static void RequireLongitudinalExtent(IReadOnlyList<SolidState> bars, double lengthM)
        {
            if (bars.Count != 4) throw new ProbeFailure("OUTPUT_COUNT_REJECTED");
            var minX = bars.Min(x => x.MinX);
            var maxX = bars.Max(x => x.MaxX);
            RequireNearNative(minX, .04d, "longitudinal minimum X");
            RequireNearNative(maxX, lengthM - .04d, "longitudinal maximum X");
        }

        private static SolidState Snapshot(Document document, Solid3d solid, string handle)
        {
            var e = solid.GeometricExtents;
            var u = CadUnitService.MetersToDrawingUnits(document, 1d);
            return new SolidState(CadHandleService.NormalizeHexHandle(handle) ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"),
                Math.Abs(solid.MassProperties.Volume) / (u * u * u),
                CadUnitService.DrawingUnitsToMeters(document, e.MinPoint.X), CadUnitService.DrawingUnitsToMeters(document, e.MaxPoint.X),
                CadUnitService.DrawingUnitsToMeters(document, e.MinPoint.Y), CadUnitService.DrawingUnitsToMeters(document, e.MaxPoint.Y),
                CadUnitService.DrawingUnitsToMeters(document, e.MinPoint.Z), CadUnitService.DrawingUnitsToMeters(document, e.MaxPoint.Z));
        }

        private static void RequireContained(SolidState host, IEnumerable<SolidState> children, string code)
        {
            foreach (var child in children)
            {
                if (child.MinX < host.MinX - NativeTolerance || child.MaxX > host.MaxX + NativeTolerance ||
                    child.MinY < host.MinY - NativeTolerance || child.MaxY > host.MaxY + NativeTolerance ||
                    child.MinZ < host.MinZ - NativeTolerance || child.MaxZ > host.MaxZ + NativeTolerance)
                    throw new ProbeFailure(code);
            }
        }

        private static IReadOnlyList<string> ParseHandles(string raw)
        {
            var result = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CadHandleService.NormalizeHexHandle(x) ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"))
                .ToList();
            if (result.Count != result.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new ProbeFailure("GENERATED_HANDLE_REJECTED");
            return result.AsReadOnly();
        }

        private static bool SetEquals(IReadOnlyList<string> left, IReadOnlyList<string> right) =>
            new HashSet<string>(left, StringComparer.OrdinalIgnoreCase).SetEquals(right);

        private static bool IsAnyGeneratedKey(string key) =>
            key.StartsWith("GeneratedSolid", StringComparison.OrdinalIgnoreCase) || IsDependentGeneratedKey(key);

        private static bool IsDependentGeneratedKey(string key) =>
            key.StartsWith("GeneratedRebar", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("GeneratedBeamStirrup", StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<string> Evidence(string nonce, ProbeState state, bool cold, bool qualified) => new[]
        {
            "status=PASS", "command=QS3DSRBEAMP04FINAL", "nonce=" + nonce, "schema=" + Schema,
            "qualification_boundary=" + Boundary,
            "production_local004_p04_qualified=" + B(qualified),
            "baseline_verified=" + B(state.BaselineVerified),
            "native_stretch_verified=" + B(state.NativeStretchVerified),
            "pre_sync_output_isolation_verified=" + B(state.PreSyncIsolationVerified),
            "source_reconcile_verified=" + B(state.ReconcileVerified),
            "dependent_invalidation_verified=" + B(state.InvalidationVerified),
            "dependent_rebuild_verified=" + B(state.DependentRebuildVerified),
            "stirrup_redistribution_verified=" + B(state.StirrupRedistributionVerified),
            "longitudinal_extent_verified=" + B(state.LongitudinalExtentVerified),
            "cold_reopen_verified=" + B(cold),
            "source_type=LINE_BEAM", "edit_command=STRETCH", "final_length_class=EIGHT_METERS",
            "stirrup_count_class=NINE_AT_D8_1000", "output_families=HOST_LONGITUDINAL_STIRRUP", "error_code=NONE"
        };

        private static Dictionary<string, string> ReadPhase(string nonce)
        {
            var path = RequiredPath(PhaseVariable, PhaseFileName);
            if (!File.Exists(path)) throw new ProbeFailure("PHASE_EVIDENCE_MISSING");
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path, new UTF8Encoding(false, true)))
            {
                var i = line.IndexOf('=');
                if (i <= 0 || i == line.Length - 1 || map.ContainsKey(line.Substring(0, i)))
                    throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
                map.Add(line.Substring(0, i), line.Substring(i + 1));
            }
            var requiredTrue = new[] { "baseline_verified", "native_stretch_verified", "pre_sync_output_isolation_verified",
                "source_reconcile_verified", "dependent_invalidation_verified", "dependent_rebuild_verified",
                "stirrup_redistribution_verified", "longitudinal_extent_verified" };
            if (!map.TryGetValue("status", out var status) || status != "PASS" ||
                !map.TryGetValue("nonce", out var savedNonce) || savedNonce != nonce ||
                !map.TryGetValue("schema", out var schema) || schema != Schema ||
                !map.TryGetValue("qualification_boundary", out var boundary) || boundary != Boundary ||
                requiredTrue.Any(key => !map.TryGetValue(key, out var value) || value != "true"))
                throw new ProbeFailure("PHASE_EVIDENCE_REJECTED");
            return map;
        }

        private static string RequiredNonce()
        {
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            if (!Guid.TryParseExact(nonce, "N", out _)) throw new ProbeFailure("AUTOMATION_CONTEXT_REJECTED");
            return nonce;
        }

        private static string RequiredPath(string variable, string fileName)
        {
            var raw = (Environment.GetEnvironmentVariable(variable) ?? string.Empty).Trim();
            if (raw.Length == 0) throw new ProbeFailure("RESULT_PATH_REJECTED");
            var path = Path.GetFullPath(raw);
            if (!string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)) || !Directory.Exists(Path.GetDirectoryName(path)))
                throw new ProbeFailure("RESULT_PATH_REJECTED");
            return path;
        }

        private static void RequireExactDocument(Document document)
        {
            var expectedRaw = Environment.GetEnvironmentVariable(DrawingVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(expectedRaw)) throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
            var expected = Path.GetFullPath(expectedRaw);
            var actual = Path.GetFullPath(document.Name ?? string.Empty);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("DOCUMENT_PATH_REJECTED");
        }

        private static void TryFailure(string phase, string code)
        {
            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _)) return;
                var path = RequiredPath(ResultVariable, ResultFileName);
                if (File.Exists(path)) return;
                WriteMarker(path, new[] { "status=FAIL", "command=QS3DSRBEAMP04REOPEN", "nonce=" + nonce,
                    "schema=" + Schema, "qualification_boundary=" + Boundary, "production_local004_p04_qualified=false",
                    "error_code=SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RUNTIME_FAILED",
                    "failure_phase=" + OneLine(phase), "failure_code=" + OneLine(code) });
            }
            catch { }
        }

        private static void WriteMarker(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("P04 marker already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush(); stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
        }

        private static void RequireNear(double actual, double expected, string label)
        {
            if (!Near(actual, expected, MetricTolerance)) throw new ProbeFailure("SEMANTIC_METRICS_REJECTED");
        }

        private static void RequireNearNative(double actual, double expected, string label)
        {
            if (!Near(actual, expected, NativeTolerance)) throw new ProbeFailure("OUTPUT_GEOMETRY_REJECTED");
        }

        private static bool Near(double a, double b, double tolerance) =>
            !double.IsNaN(a) && !double.IsInfinity(a) && Math.Abs(a - b) <= tolerance;
        private static string B(bool value) => value ? "true" : "false";
        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private sealed class ProbeContext
        {
            public ProbeContext(Document document, ProjectState project, string nonce) { Document = document; Project = project; Nonce = nonce; }
            public Document Document { get; }
            public ProjectState Project { get; }
            public string Nonce { get; }
        }

        private sealed class ProbeState
        {
            public ProbeState(Document document, string projectId, string ownerId, string sourceHandle, string nonce)
            { Document = document; ProjectId = projectId; OwnerId = ownerId; SourceHandle = sourceHandle; Nonce = nonce; }
            public Document Document { get; }
            public string ProjectId { get; }
            public string OwnerId { get; }
            public string SourceHandle { get; }
            public string Nonce { get; }
            public string Phase { get; set; } = "PREPARED";
            public IReadOnlyList<string>? BaselineHandles { get; set; }
            public IReadOnlyList<string> RequiredBaselineHandles => BaselineHandles ?? throw new ProbeFailure("SEQUENCE_ORDER_REJECTED");
            public bool BaselineVerified { get; set; }
            public bool NativeStretchVerified { get; set; }
            public bool PreSyncIsolationVerified { get; set; }
            public bool ReconcileVerified { get; set; }
            public bool InvalidationVerified { get; set; }
            public bool DependentRebuildVerified { get; set; }
            public bool StirrupRedistributionVerified { get; set; }
            public bool LongitudinalExtentVerified { get; set; }
        }

        private sealed class SolidState
        {
            public SolidState(string handle, double volumeM3, double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
            { Handle = handle; VolumeM3 = volumeM3; MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY; MinZ = minZ; MaxZ = maxZ; }
            public string Handle { get; }
            public double VolumeM3 { get; }
            public double MinX { get; }
            public double MaxX { get; }
            public double MinY { get; }
            public double MaxY { get; }
            public double MinZ { get; }
            public double MaxZ { get; }
        }

        private sealed class OutputState
        {
            public OutputState(SolidState host, IReadOnlyList<SolidState> rebar, IReadOnlyList<SolidState> stirrups)
            {
                Host = host; Rebar = rebar; Stirrups = stirrups;
                Handles = new[] { host.Handle }.Concat(rebar.Select(x => x.Handle)).Concat(stirrups.Select(x => x.Handle)).ToList().AsReadOnly();
            }
            public SolidState Host { get; }
            public IReadOnlyList<SolidState> Rebar { get; }
            public IReadOnlyList<SolidState> Stirrups { get; }
            public IReadOnlyList<string> Handles { get; }
        }

        private sealed class ProbeFailure : InvalidOperationException
        {
            public ProbeFailure(string code) : base("Beam STRETCH dependent probe rejected state.") { Code = code; }
            public string Code { get; }
        }
    }
}
