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
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only LOCAL-004 P02 probe for one production Direct Draw Slab whose
    /// authoritative closed POLYLINE is edited by BricsCAD's native top-level STRETCH.
    /// The probe only validates state and manages implied selection; production
    /// QS3DSYNCSOURCE/QS3DBUILD3D/QS3DSAVE own every semantic/native mutation.
    /// </summary>
    public sealed class SourceReconcileNativePolylineEditRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_RESULT";
        private const string PhaseVariable = "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_PHASE_RESULT";
        private const string NonceVariable = "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_NONCE";
        private const string DrawingVariable = "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_DWG";
        private const string ResultFileName = "source-reconcile-native-polyline-result.txt";
        private const string PhaseFileName = "source-reconcile-native-polyline-session1.txt";
        private const string Schema = "QS3D_SOURCE_RECONCILE_NATIVE_POLYLINE_RUNTIME_V1";
        private const string Boundary = "LOCAL_004_P02_CLOSED_POLYLINE_VERTEX";
        private const double MetricToleranceM = 1e-8d;
        private const double NativeToleranceM = 1e-6d;
        private static readonly object Sync = new object();
        private static SequenceState? _state;

        [CommandMethod("QS3DSRPOLYPREPARE", CommandFlags.Modal)]
        public void Prepare()
        {
            Execute("prepare", () =>
            {
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Initial);
                RequireSemanticMetrics(owner, ExpectedStage.Initial);
                RequireQuantities(owner, ExpectedStage.Initial);
                var generated = RequireGenerated(context.Document, context.Project, owner, ExpectedStage.Initial);
                lock (Sync)
                {
                    _state = new SequenceState(
                        context.Document,
                        context.Project.ProjectId,
                        owner.Id,
                        owner.SourceHandles.Single(),
                        context.Nonce,
                        generated);
                }

                // Direct Draw intentionally selects its generated solid. Native STRETCH must
                // derive vertex membership only from the runner's explicit crossing window.
                context.Document.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
            });
        }

        [CommandMethod("QS3DSRPOLYSTRETCHCHECK", CommandFlags.Modal)]
        public void CheckNativeStretch()
        {
            Execute("native_stretch", () =>
            {
                var context = Context();
                var state = State(context, "PREPARED");
                var owner = Owner(context, state);
                try { RequireGeometry(context.Document, owner, ExpectedStage.Stretched); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_GEOMETRY_REJECTED"); }
                try { RequireSemanticMetrics(owner, ExpectedStage.Initial); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_SEMANTIC_REJECTED"); }
                try { RequireQuantities(owner, ExpectedStage.Initial); }
                catch { throw new ProbeFailure("NATIVE_STRETCH_QUANTITY_REJECTED"); }
                try
                {
                    var current = RequireGenerated(context.Document, context.Project, owner, ExpectedStage.Initial);
                    if (!SameGenerated(state.InitialGenerated, current))
                        throw new ProbeFailure("GENERATED_MUTATED_BY_NATIVE_STRETCH");
                }
                catch
                {
                    throw new ProbeFailure("GENERATED_MUTATED_BY_NATIVE_STRETCH");
                }

                state.NativeVertexStretchVerified = true;
                state.PreSyncIsolationVerified = true;
                state.Phase = "STRETCHED";
            });
        }

        [CommandMethod("QS3DSRPOLYSELECT", CommandFlags.Modal | CommandFlags.UsePickSet)]
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

        [CommandMethod("QS3DSRPOLYSYNCCHECK", CommandFlags.Modal)]
        public void CheckReconcile()
        {
            Execute("check_reconcile", () =>
            {
                var context = Context();
                var state = State(context, "STRETCHED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticMetrics(owner, ExpectedStage.Stretched);
                RequireQuantities(owner, ExpectedStage.Stretched);
                RequireNoGenerated(context.Document, owner, state.InitialGenerated.Handle);
                state.AreaPerimeterReconcileVerified = true;
                state.QuantityRecalculationVerified = true;
                state.GeneratedInvalidationVerified = true;
                state.Phase = "SYNCED";
            });
        }

        [CommandMethod("QS3DSRPOLYFINAL", CommandFlags.Modal)]
        public void FinalizeSessionOne()
        {
            Execute("final_rebuild", () =>
            {
                var context = Context();
                var state = State(context, "SYNCED");
                var owner = Owner(context, state);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticMetrics(owner, ExpectedStage.Stretched);
                RequireQuantities(owner, ExpectedStage.Stretched);
                var generated = RequireGenerated(context.Document, context.Project, owner, ExpectedStage.Stretched);
                if (string.Equals(generated.Handle, state.InitialGenerated.Handle, StringComparison.OrdinalIgnoreCase))
                    throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");
                RequireScopedHealth(context.Document, context.Project, owner, generated.Handle);
                state.GeneratedRebuildVerified = true;
                state.NativeSolidBoundsVerified = true;
                state.ScopedHealthVerified = true;
                state.Phase = "FINAL_REBUILT";
                WriteMarkerAtomic(RequiredPath(PhaseVariable, PhaseFileName), EvidenceLines(
                    "PASS",
                    context.Nonce,
                    coldReopenVerified: false,
                    state));
            });
        }

        [CommandMethod("QS3DSRPOLYREOPEN", CommandFlags.Modal)]
        public void Reopen()
        {
            Execute("cold_reopen", () =>
            {
                var context = Context(requireState: false);
                var owner = FindUniqueOwner(context.Document, context.Project, ExpectedStage.Stretched);
                RequireGeometry(context.Document, owner, ExpectedStage.Stretched);
                RequireSemanticMetrics(owner, ExpectedStage.Stretched);
                RequireQuantities(owner, ExpectedStage.Stretched);
                var generated = RequireGenerated(context.Document, context.Project, owner, ExpectedStage.Stretched);
                RequireScopedHealth(context.Document, context.Project, owner, generated.Handle);
                var phase = ReadPhaseEvidence(context.Nonce);
                WriteMarkerAtomic(RequiredPath(ResultVariable, ResultFileName), new[]
                {
                    "status=PASS",
                    "command=QS3DSRPOLYREOPEN",
                    "nonce=" + context.Nonce,
                    "schema=" + Schema,
                    "qualification_boundary=" + Boundary,
                    "production_local004_p02_qualified=true",
                    "native_vertex_stretch_verified=" + phase["native_vertex_stretch_verified"],
                    "pre_sync_isolation_verified=" + phase["pre_sync_isolation_verified"],
                    "area_perimeter_reconcile_verified=" + phase["area_perimeter_reconcile_verified"],
                    "quantity_recalculation_verified=" + phase["quantity_recalculation_verified"],
                    "generated_invalidation_verified=" + phase["generated_invalidation_verified"],
                    "generated_rebuild_verified=" + phase["generated_rebuild_verified"],
                    "native_solid_bounds_verified=" + phase["native_solid_bounds_verified"],
                    "scoped_health_verified=" + phase["scoped_health_verified"],
                    "cold_reopen_verified=true",
                    "source_type=POLYLINE",
                    "edit_command=STRETCH",
                    "final_geometry_class=QUADRILATERAL_13_5_M2",
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
            if (owner == null || owner.Category != ElementCategory.Slab ||
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
                element.Category == ElementCategory.Slab && element.SourceHandles.Count == 1))
            {
                try
                {
                    RequireGeometry(document, candidate, stage);
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
                var polyline = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Polyline;
                if (polyline == null || polyline.IsErased)
                    throw new ProbeFailure("SOURCE_TYPE_REJECTED");
            }
            return ids[0];
        }

        private static void RequireGeometry(Document document, ProjectElement owner, ExpectedStage stage)
        {
            var id = ResolveSource(document, owner.SourceHandles.Single());
            var expected = Geometry(stage);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline
                    ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                if (!polyline.Closed || polyline.NumberOfVertices != expected.Points.Count)
                    throw new ProbeFailure("SOURCE_GEOMETRY_REJECTED");
                RequireNear(polyline.Normal.X, 0d, MetricToleranceM, "normal X");
                RequireNear(polyline.Normal.Y, 0d, MetricToleranceM, "normal Y");
                RequireNear(polyline.Normal.Z, 1d, MetricToleranceM, "normal Z");
                RequireNear(Meters(document, polyline.Elevation), 0d, MetricToleranceM, "elevation");
                for (var index = 0; index < expected.Points.Count; index++)
                {
                    var point = polyline.GetPoint3dAt(index);
                    RequireNear(Meters(document, point.X), expected.Points[index].X, MetricToleranceM, "vertex X");
                    RequireNear(Meters(document, point.Y), expected.Points[index].Y, MetricToleranceM, "vertex Y");
                    RequireNear(Meters(document, point.Z), 0d, MetricToleranceM, "vertex Z");
                    RequireNear(polyline.GetBulgeAt(index), 0d, MetricToleranceM, "vertex bulge");
                }
                var unitsPerMeter = Drawing(document, 1d);
                RequireNear(polyline.Area / (unitsPerMeter * unitsPerMeter), expected.AreaM2, MetricToleranceM, "area");
                RequireNear(Meters(document, polyline.Length), expected.PerimeterM, MetricToleranceM, "perimeter");
            }
        }

        private static ExpectedGeometry Geometry(ExpectedStage stage)
        {
            if (stage == ExpectedStage.Initial)
                return new ExpectedGeometry(
                    new[] { new Point2d(0d, 0d), new Point2d(4d, 0d), new Point2d(4d, 3d), new Point2d(0d, 3d) },
                    12d,
                    14d,
                    1.44d,
                    4d);
            if (stage == ExpectedStage.Stretched)
                return new ExpectedGeometry(
                    new[] { new Point2d(0d, 0d), new Point2d(4d, 0d), new Point2d(5d, 3d), new Point2d(0d, 3d) },
                    13.5d,
                    12d + Math.Sqrt(10d),
                    1.62d,
                    5d);
            throw new ProbeFailure("EXPECTED_GEOMETRY_REJECTED");
        }

        private static void RequireSemanticMetrics(ProjectElement owner, ExpectedStage stage)
        {
            var expected = Geometry(stage);
            RequireProperty(owner, "AreaM2", expected.AreaM2);
            RequireProperty(owner, "LengthM", expected.PerimeterM);
            RequireProperty(owner, "PerimeterM", expected.PerimeterM);
            RequireProperty(owner, "ThicknessM", 0.12d);
        }

        private static void RequireQuantities(ProjectElement owner, ExpectedStage stage)
        {
            var expected = Geometry(stage);
            RequireQuantity(owner, "AreaM2", expected.AreaM2);
            RequireQuantity(owner, "NetAreaM2", expected.AreaM2);
            RequireQuantity(owner, "GrossVolumeM3", expected.VolumeM3);
            RequireQuantity(owner, "NetVolumeM3", expected.VolumeM3);
            RequireQuantity(owner, "FormworkM2", expected.AreaM2 + 0.12d * expected.PerimeterM);
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
                throw new ProbeFailure("QUANTITIES_REJECTED");
            RequireNear(value, expected, MetricToleranceM, key);
        }

        private static GeneratedSnapshot RequireGenerated(
            Document document,
            ProjectState project,
            ProjectElement owner,
            ExpectedStage stage)
        {
            if (!owner.Properties.TryGetValue("GeneratedSolidHandle", out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => CadHandleService.NormalizeHexHandle(value)
                    ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (handles.Count != 1) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != 1) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var expected = Geometry(stage);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased || !GeneratedGeometryService.HasMatchingOwnership(solid, project, owner))
                    throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
                var extents = solid.GeometricExtents;
                var unitsPerMeter = Drawing(document, 1d);
                var volumeM3 = Math.Abs(solid.MassProperties.Volume) /
                    (unitsPerMeter * unitsPerMeter * unitsPerMeter);
                var snapshot = new GeneratedSnapshot(
                    handles[0],
                    volumeM3,
                    Meters(document, extents.MinPoint.X),
                    Meters(document, extents.MaxPoint.X),
                    Meters(document, extents.MinPoint.Y),
                    Meters(document, extents.MaxPoint.Y),
                    Meters(document, extents.MinPoint.Z),
                    Meters(document, extents.MaxPoint.Z));
                RequireGeneratedGeometry(snapshot, expected);
                return snapshot;
            }
        }

        private static void RequireGeneratedGeometry(GeneratedSnapshot snapshot, ExpectedGeometry expected)
        {
            RequireNear(snapshot.VolumeM3, expected.VolumeM3, NativeToleranceM, "solid volume");
            RequireNear(snapshot.MinimumXM, 0d, NativeToleranceM, "solid minimum X");
            RequireNear(snapshot.MaximumXM, expected.MaximumXM, NativeToleranceM, "solid maximum X");
            RequireNear(snapshot.MinimumYM, 0d, NativeToleranceM, "solid minimum Y");
            RequireNear(snapshot.MaximumYM, 3d, NativeToleranceM, "solid maximum Y");
            RequireNear(snapshot.MinimumZM, 0d, NativeToleranceM, "solid minimum Z");
            RequireNear(snapshot.MaximumZM, 0.12d, NativeToleranceM, "solid maximum Z");
        }

        private static bool SameGenerated(GeneratedSnapshot left, GeneratedSnapshot right) =>
            string.Equals(left.Handle, right.Handle, StringComparison.OrdinalIgnoreCase) &&
            Near(left.VolumeM3, right.VolumeM3, NativeToleranceM) &&
            Near(left.MinimumXM, right.MinimumXM, NativeToleranceM) &&
            Near(left.MaximumXM, right.MaximumXM, NativeToleranceM) &&
            Near(left.MinimumYM, right.MinimumYM, NativeToleranceM) &&
            Near(left.MaximumYM, right.MaximumYM, NativeToleranceM) &&
            Near(left.MinimumZM, right.MinimumZM, NativeToleranceM) &&
            Near(left.MaximumZM, right.MaximumZM, NativeToleranceM);

        private static void RequireNoGenerated(Document document, ProjectElement owner, string previousHandle)
        {
            if (owner.Properties.TryGetValue("GeneratedSolidHandle", out var raw) && !string.IsNullOrWhiteSpace(raw))
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
            if (CadHandleService.GetLiveHandles(document, new[] { previousHandle }).Count != 0)
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
        }

        private static void RequireScopedHealth(
            Document document,
            ProjectState project,
            ProjectElement owner,
            string generatedHandle)
        {
            var sourceHandles = new HashSet<string>(owner.SourceHandles, StringComparer.OrdinalIgnoreCase);
            var generatedHandles = new HashSet<string>(new[] { generatedHandle }, StringComparer.OrdinalIgnoreCase);
            var coreIssues = new ModelHealthService()
                .Inspect(project, sourceHandles, generatedHandles)
                .Count(issue => issue.Severity != HealthSeverity.Info &&
                    (string.IsNullOrEmpty(issue.ElementId) ||
                     string.Equals(issue.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase)));
            var runtimeIssues = GeneratedSolidRuntimeHealthService
                .Inspect(document, project)
                .Count(issue => issue.Severity != HealthSeverity.Info &&
                    (string.IsNullOrEmpty(issue.ElementId) ||
                     string.Equals(issue.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase)));
            if (coreIssues != 0 || runtimeIssues != 0)
                throw new ProbeFailure("HEALTH_REJECTED");
        }

        private static double Drawing(Document document, double meters) =>
            CadUnitService.MetersToDrawingUnits(document, meters);

        private static double Meters(Document document, double drawingUnits) =>
            CadUnitService.DrawingUnitsToMeters(document, drawingUnits);

        private static bool Near(double actual, double expected, double tolerance) =>
            !double.IsNaN(actual) && !double.IsInfinity(actual) && Math.Abs(actual - expected) <= tolerance;

        private static void RequireNear(double actual, double expected, double tolerance, string label)
        {
            if (!Near(actual, expected, tolerance))
                throw new InvalidOperationException("Native POLYLINE probe mismatch at " + label + ".");
        }

        private static IReadOnlyList<string> EvidenceLines(
            string status,
            string nonce,
            bool coldReopenVerified,
            SequenceState state) =>
            new[]
            {
                "status=" + status,
                "command=QS3DSRPOLYFINAL",
                "nonce=" + nonce,
                "schema=" + Schema,
                "qualification_boundary=" + Boundary,
                "production_local004_p02_qualified=false",
                "native_vertex_stretch_verified=" + Boolean(state.NativeVertexStretchVerified),
                "pre_sync_isolation_verified=" + Boolean(state.PreSyncIsolationVerified),
                "area_perimeter_reconcile_verified=" + Boolean(state.AreaPerimeterReconcileVerified),
                "quantity_recalculation_verified=" + Boolean(state.QuantityRecalculationVerified),
                "generated_invalidation_verified=" + Boolean(state.GeneratedInvalidationVerified),
                "generated_rebuild_verified=" + Boolean(state.GeneratedRebuildVerified),
                "native_solid_bounds_verified=" + Boolean(state.NativeSolidBoundsVerified),
                "scoped_health_verified=" + Boolean(state.ScopedHealthVerified),
                "cold_reopen_verified=" + Boolean(coldReopenVerified),
                "source_type=POLYLINE",
                "edit_command=STRETCH",
                "final_geometry_class=QUADRILATERAL_13_5_M2",
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
                ["production_local004_p02_qualified"] = "false",
                ["native_vertex_stretch_verified"] = "true",
                ["pre_sync_isolation_verified"] = "true",
                ["area_perimeter_reconcile_verified"] = "true",
                ["quantity_recalculation_verified"] = "true",
                ["generated_invalidation_verified"] = "true",
                ["generated_rebuild_verified"] = "true",
                ["native_solid_bounds_verified"] = "true",
                ["scoped_health_verified"] = "true",
                ["cold_reopen_verified"] = "false",
                ["source_type"] = "POLYLINE",
                ["edit_command"] = "STRETCH",
                ["final_geometry_class"] = "QUADRILATERAL_13_5_M2",
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
                    "command=QS3DSRPOLYREOPEN",
                    "nonce=" + nonce,
                    "schema=" + Schema,
                    "qualification_boundary=" + Boundary,
                    "production_local004_p02_qualified=false",
                    "error_code=SOURCE_RECONCILE_NATIVE_POLYLINE_RUNTIME_FAILED",
                    "failure_phase=" + OneLine(phase),
                    "failure_code=" + OneLine(code)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("Native POLYLINE probe marker already exists.");
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

        private enum ExpectedStage { Initial, Stretched }

        private sealed class ExpectedGeometry
        {
            public ExpectedGeometry(IReadOnlyList<Point2d> points, double areaM2, double perimeterM, double volumeM3, double maximumXM)
            { Points = points; AreaM2 = areaM2; PerimeterM = perimeterM; VolumeM3 = volumeM3; MaximumXM = maximumXM; }
            public IReadOnlyList<Point2d> Points { get; }
            public double AreaM2 { get; }
            public double PerimeterM { get; }
            public double VolumeM3 { get; }
            public double MaximumXM { get; }
        }

        private sealed class GeneratedSnapshot
        {
            public GeneratedSnapshot(
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
                GeneratedSnapshot initialGenerated)
            {
                Document = document;
                ProjectId = projectId;
                OwnerId = ownerId;
                SourceHandle = sourceHandle;
                Nonce = nonce;
                InitialGenerated = initialGenerated;
            }
            public Document Document { get; }
            public string ProjectId { get; }
            public string OwnerId { get; }
            public string SourceHandle { get; }
            public string Nonce { get; }
            public GeneratedSnapshot InitialGenerated { get; }
            public string Phase { get; set; } = "PREPARED";
            public bool NativeVertexStretchVerified { get; set; }
            public bool PreSyncIsolationVerified { get; set; }
            public bool AreaPerimeterReconcileVerified { get; set; }
            public bool QuantityRecalculationVerified { get; set; }
            public bool GeneratedInvalidationVerified { get; set; }
            public bool GeneratedRebuildVerified { get; set; }
            public bool NativeSolidBoundsVerified { get; set; }
            public bool ScopedHealthVerified { get; set; }
        }

        private sealed class ProbeFailure : InvalidOperationException
        {
            public ProbeFailure(string code) : base("Native POLYLINE source-reconcile probe state rejected.")
            { Code = code; }
            public string Code { get; }
        }
    }
}
