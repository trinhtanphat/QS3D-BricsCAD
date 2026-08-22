using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// LOCAL-004 P05 read-only probe for a real manual endpoint-grip edit of the authoritative
    /// Beam LINE. The native grip interaction is intentionally performed by the licensed local
    /// operator; this probe only verifies the production source/semantic/generated lifecycle.
    /// </summary>
    public sealed class SourceReconcileNativeGripRuntimeProbeCommands
    {
        private const string Schema = "QS3D_SOURCE_RECONCILE_NATIVE_GRIP_RUNTIME_V1";
        private const string Boundary = "LOCAL_004_P05_MANUAL_GRIP_CANCEL_COMMIT";
        private const double MetricTolerance = 1e-8d;
        private const double NativeTolerance = 1e-5d;
        private static readonly object Sync = new object();
        private static ProbeState? _state;

        [CommandMethod("QS3DSRGRIPP05BASELINE", CommandFlags.Modal)]
        public void Baseline() => Execute("baseline", () =>
        {
            var context = Context();
            var owner = FindUniqueBeam(context.Document, context.Project, 5d);
            RequireSource(context.Document, owner, 5d);
            RequireSemantic(owner, 5d);
            RequireQuantities(owner, 5d);
            var host = RequireHost(context.Document, context.Project, owner, 5d);

            lock (Sync)
            {
                _state = new ProbeState(
                    context.Document,
                    context.Project.ProjectId,
                    owner.Id,
                    owner.SourceHandles.Single(),
                    host.Handle,
                    "BASELINED");
            }

            SetSourceSelection(context.Document, owner.SourceHandles.Single());
            WritePass(context.Document.Editor, "baseline", "source=FIVE_METERS|semantic=FIVE_METERS|generated=BASELINE_LIVE");
        });

        [CommandMethod("QS3DSRGRIPP05SELECT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void SelectSource() => Execute("select_source", () =>
        {
            var context = Context();
            var state = State(context);
            SetSourceSelection(context.Document, state.SourceHandle);
            WritePass(context.Document.Editor, "select_source", "selection=AUTHORITATIVE_SOURCE");
        });

        [CommandMethod("QS3DSRGRIPP05CANCELCHECK", CommandFlags.Modal)]
        public void CancelCheck() => Execute("cancel_check", () =>
        {
            var context = Context();
            var state = State(context, "BASELINED");
            var owner = Owner(context, state);

            RequireSource(context.Document, owner, 5d);
            RequireSemantic(owner, 5d);
            RequireQuantities(owner, 5d);
            var host = RequireHost(context.Document, context.Project, owner, 5d);
            if (!string.Equals(host.Handle, state.BaselineHostHandle, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("CANCEL_MUTATED_GENERATED_OUTPUT");

            state.Phase = "CANCEL_VERIFIED";
            SetSourceSelection(context.Document, state.SourceHandle);
            WritePass(context.Document.Editor, "cancel_check",
                "manual_grip_cancel_verified=true|source=FIVE_METERS|semantic=FIVE_METERS|generated=BASELINE_LIVE");
        });

        [CommandMethod("QS3DSRGRIPP05EDITCHECK", CommandFlags.Modal)]
        public void EditCheck() => Execute("edit_check", () =>
        {
            var context = Context();
            var state = State(context, "CANCEL_VERIFIED");
            var owner = Owner(context, state);

            RequireSource(context.Document, owner, 8d);
            // Before production reconcile the semantic/quantity/native output must still be the
            // last committed 5 m state. A native grip edit may mutate only the authoritative source.
            RequireSemantic(owner, 5d);
            RequireQuantities(owner, 5d);
            var host = RequireHost(context.Document, context.Project, owner, 5d);
            if (!string.Equals(host.Handle, state.BaselineHostHandle, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("PRE_SYNC_OUTPUT_MUTATED");

            state.Phase = "EDIT_VERIFIED";
            WritePass(context.Document.Editor, "edit_check",
                "manual_grip_commit_verified=true|source=EIGHT_METERS|semantic=FIVE_METERS|generated=BASELINE_LIVE");
        });

        [CommandMethod("QS3DSRGRIPP05SYNCCHECK", CommandFlags.Modal)]
        public void SyncCheck() => Execute("sync_check", () =>
        {
            var context = Context();
            var state = State(context, "EDIT_VERIFIED");
            var owner = Owner(context, state);

            RequireSource(context.Document, owner, 8d);
            RequireSemantic(owner, 8d);
            RequireQuantities(owner, 8d);
            if (owner.Properties.Keys.Any(IsGeneratedHostKey))
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");
            if (CadHandleService.GetLiveHandles(context.Document, new[] { state.BaselineHostHandle }).Count != 0)
                throw new ProbeFailure("GENERATED_INVALIDATION_REJECTED");

            state.Phase = "SYNC_VERIFIED";
            SetSourceSelection(context.Document, state.SourceHandle);
            WritePass(context.Document.Editor, "sync_check",
                "source_reconcile_verified=true|source=EIGHT_METERS|semantic=EIGHT_METERS|baseline_generated=INVALIDATED");
        });

        [CommandMethod("QS3DSRGRIPP05FINAL", CommandFlags.Modal)]
        public void Final() => Execute("final", () =>
        {
            var context = Context();
            var state = State(context, "SYNC_VERIFIED");
            var owner = Owner(context, state);

            RequireSource(context.Document, owner, 8d);
            RequireSemantic(owner, 8d);
            RequireQuantities(owner, 8d);
            var host = RequireHost(context.Document, context.Project, owner, 8d);
            if (string.Equals(host.Handle, state.BaselineHostHandle, StringComparison.OrdinalIgnoreCase))
                throw new ProbeFailure("GENERATED_REPLACEMENT_REJECTED");

            state.Phase = "FINAL_VERIFIED";
            WritePass(context.Document.Editor, "final",
                "rebuild_verified=true|replacement_generated=true|final_length=EIGHT_METERS");
        });

        [CommandMethod("QS3DSRGRIPP05REOPEN", CommandFlags.Modal)]
        public void Reopen() => Execute("reopen", () =>
        {
            var context = Context();
            var owner = FindUniqueBeam(context.Document, context.Project, 8d);
            RequireSource(context.Document, owner, 8d);
            RequireSemantic(owner, 8d);
            RequireQuantities(owner, 8d);
            RequireHost(context.Document, context.Project, owner, 8d);

            // A true cold reopen necessarily starts with fresh in-memory probe state. This command
            // therefore proves persisted final-state continuity only; it must not reassert the
            // manual cancel/commit/reconcile/rebuild phases that were observed before restart.
            context.Document.Editor.WriteMessage(
                "\n" + Schema +
                "|status=PASS" +
                "|qualification_boundary=" + Boundary +
                "|phase=reopen" +
                "|cold_reopen_verified=true" +
                "|prior_sequence_reasserted=false" +
                "|qualification_requires_prior_markers=true" +
                "|source_type=LINE_BEAM" +
                "|final_length_class=EIGHT_METERS" +
                "|error_code=NONE");
        });

        private static void Execute(string phase, Action action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try { action(); }
            catch (ProbeFailure failure)
            {
                document.Editor.WriteMessage(
                    "\n" + Schema + "|status=FAIL|qualification_boundary=" + Boundary +
                    "|phase=" + phase + "|error_code=" + failure.Code);
            }
            catch
            {
                document.Editor.WriteMessage(
                    "\n" + Schema + "|status=FAIL|qualification_boundary=" + Boundary +
                    "|phase=" + phase + "|error_code=STATE_REJECTED");
            }
        }

        private static ProbeContext Context()
        {
            var document = Application.DocumentManager.MdiActiveDocument
                ?? throw new ProbeFailure("ACTIVE_DOCUMENT_MISSING");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new ProbeFailure("PROJECT_MISSING");
            return new ProbeContext(document, project);
        }

        private static ProbeState State(ProbeContext context, string? phase = null)
        {
            ProbeState state;
            lock (Sync) state = _state ?? throw new ProbeFailure("SEQUENCE_NOT_INITIALIZED");
            if (!ReferenceEquals(state.Document, context.Document) ||
                !string.Equals(state.ProjectId, context.Project.ProjectId, StringComparison.Ordinal))
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
            var matches = project.Elements
                .Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Count == 1)
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

        private static void RequireSource(Document document, ProjectElement owner, double expectedLengthM)
        {
            var id = ResolveSource(document, owner.SourceHandles.Single());
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                    ?? throw new ProbeFailure("SOURCE_TYPE_REJECTED");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.StartPoint.X), 0d, "source start X");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.StartPoint.Y), 0d, "source start Y");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.EndPoint.X), expectedLengthM, "source end X");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.EndPoint.Y), 0d, "source end Y");
                RequireNear(CadUnitService.DrawingUnitsToMeters(document, line.Length), expectedLengthM, "source length");
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

        private static HostState RequireHost(Document document, ProjectState project, ProjectElement owner, double lengthM)
        {
            if (!owner.Properties.TryGetValue("GeneratedSolidHandle", out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => CadHandleService.NormalizeHexHandle(x) ?? throw new ProbeFailure("GENERATED_HANDLE_REJECTED"))
                .ToList();
            if (handles.Count != 1) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
            var ids = CadHandleService.Resolve(document, handles);
            if (ids.Count != 1) throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased || !GeneratedGeometryService.HasMatchingOwnership(solid, project, owner))
                    throw new ProbeFailure("GENERATED_OWNERSHIP_REJECTED");
                var unit = CadUnitService.MetersToDrawingUnits(document, 1d);
                var volumeM3 = Math.Abs(solid.MassProperties.Volume) / (unit * unit * unit);
                RequireNearNative(volumeM3, lengthM * .15d, "host volume");
                return new HostState(handles[0]);
            }
        }

        private static bool IsGeneratedHostKey(string key) =>
            key.StartsWith("GeneratedSolid", StringComparison.OrdinalIgnoreCase);

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

        private static bool Near(double actual, double expected, double tolerance) => Math.Abs(actual - expected) <= tolerance;

        private static void RequireNear(double actual, double expected, string label)
        {
            if (!Near(actual, expected, MetricTolerance))
                throw new ProbeFailure("METRIC_REJECTED_" + SanitizeLabel(label));
        }

        private static void RequireNearNative(double actual, double expected, string label)
        {
            if (!Near(actual, expected, NativeTolerance))
                throw new ProbeFailure("NATIVE_METRIC_REJECTED_" + SanitizeLabel(label));
        }

        private static string SanitizeLabel(string label) =>
            new string(label.ToUpperInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        private static void WritePass(Editor editor, string phase, string details)
        {
            editor.WriteMessage(
                "\n" + Schema + "|status=PASS|qualification_boundary=" + Boundary +
                "|phase=" + phase + "|" + details + "|error_code=NONE");
        }

        private sealed class ProbeContext
        {
            internal ProbeContext(Document document, ProjectState project)
            {
                Document = document;
                Project = project;
            }
            internal Document Document { get; }
            internal ProjectState Project { get; }
        }

        private sealed class ProbeState
        {
            internal ProbeState(Document document, string projectId, string ownerId, string sourceHandle,
                string baselineHostHandle, string phase)
            {
                Document = document;
                ProjectId = projectId;
                OwnerId = ownerId;
                SourceHandle = sourceHandle;
                BaselineHostHandle = baselineHostHandle;
                Phase = phase;
            }
            internal Document Document { get; }
            internal string ProjectId { get; }
            internal string OwnerId { get; }
            internal string SourceHandle { get; }
            internal string BaselineHostHandle { get; }
            internal string Phase { get; set; }
        }

        private sealed class HostState
        {
            internal HostState(string handle) { Handle = handle; }
            internal string Handle { get; }
        }

        private sealed class ProbeFailure : Exception
        {
            internal ProbeFailure(string code) : base(code) { Code = code; }
            internal string Code { get; }
        }
    }
}
