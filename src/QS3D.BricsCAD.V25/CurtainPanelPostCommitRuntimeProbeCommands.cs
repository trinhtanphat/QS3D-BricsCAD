using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Automation-only LOCAL-002/P09 probe for the two post-commit Curtain
    /// warning boundaries. Production QS3DCURTAIN3D performs every replacement.
    /// </summary>
    public sealed class CurtainPanelPostCommitRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_CURTAIN_PANEL_P09_RESULT";
        private const string NonceVariable = "QS3D_CURTAIN_PANEL_P09_NONCE";
        private const string ResultFileName = "curtain-panel-postcommit-runtime-result.txt";
        private const string FrameFingerprintKey = "GeneratedCurtainFrameLiveFingerprint";
        private const string PanelFingerprintKey = "GeneratedCurtainPanelLiveFingerprint";

        private enum SequenceStage
        {
            None, Seeded, Prepared, Baseline, FingerprintArmed, FingerprintVerified,
            CleanReady, CleanVerified, UiArmed, Complete
        }

        private sealed class OwnerOutput
        {
            public string SourceHandle { get; set; } = string.Empty;
            public IReadOnlyList<string> GeneratedHandles { get; set; } = Array.Empty<string>();
        }

        private sealed class SequenceState
        {
            public string Nonce { get; set; } = string.Empty;
            public SequenceStage Stage { get; set; }
            public string ElementId { get; set; } = string.Empty;
            public string SourceDigest { get; set; } = string.Empty;
            public OwnerOutput? Baseline { get; set; }
            public OwnerOutput? FingerprintOutput { get; set; }
            public OwnerOutput? CleanOutput { get; set; }
        }

        private static readonly object StateSync = new object();
        private static SequenceState? State;
        private static readonly HashSet<string> FailurePhases = new HashSet<string>(StringComparer.Ordinal)
        {
            "PROBE_AUTH", "SEED_LINE", "PREPARE_BASELINE", "VERIFY_BASELINE",
            "ARM_FINGERPRINT", "VERIFY_FINGERPRINT", "PREPARE_CLEAN", "VERIFY_CLEAN",
            "ARM_UI", "VERIFY_UI", "RESULT_PUBLISH"
        };
        private static readonly HashSet<string> FailureCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "STATE_REJECTED", "DATA_REJECTED", "IO_REJECTED", "OVERFLOW_REJECTED", "UNEXPECTED_REJECTED"
        };

        [CommandMethod("QS3DCURTAINP09SEED", CommandFlags.Modal)]
        public void SeedLine() => ExecuteStage("SEED_LINE", (document, _, nonce) =>
        {
            ObjectId id;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(Point3d.Origin, new Point3d(CadGeometryGuard.ToDrawingUnits(document, 5d, "P09 line length"), 0d, 0d));
                try
                {
                    line.SetDatabaseDefaults(document.Database);
                    id = modelSpace.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                    transaction.Commit();
                    line = null!;
                }
                finally { line?.Dispose(); }
            }
            CurtainWallPostCommitFailureInjection.RequireIdle();
            lock (StateSync) State = new SequenceState { Nonce = nonce, Stage = SequenceStage.Seeded };
            document.Editor.SetImpliedSelection(new[] { id });
        });

        [CommandMethod("QS3DCURTAINP09PREPARE", CommandFlags.Modal)]
        public void PrepareBaseline() => ExecuteStage("PREPARE_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Seeded);
            var owners = project.Elements.Where(x => x.Category == ElementCategory.GlassWall).Take(2).ToList();
            if (owners.Count != 1) throw new InvalidOperationException("P09 requires exactly one synthetic GlassWall.");
            var owner = owners[0];
            RequireLegacyNoLevel(owner);
            var sourceId = ResolveSingleSource(document, owner);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Line
                    ?? throw new InvalidOperationException("P09 source is not a LINE.");
                if (!Near(CadGeometryGuard.ToMeters(document, line.StartPoint.Y, "P09 source lane"), 0d))
                    throw new InvalidOperationException("P09 source is outside its synthetic lane.");
                transaction.Commit();
            }
            owner.SetProperty("CurtainMaxPanelWidthM", "0.9");
            project.Touch();
            state.ElementId = owner.Id;
            state.SourceDigest = CaptureSourceDigest(document, owner);
            state.Stage = SequenceStage.Prepared;
            document.Editor.SetImpliedSelection(new[] { sourceId });
        });

        [CommandMethod("QS3DCURTAINP09BASELINE", CommandFlags.Modal)]
        public void VerifyBaseline() => ExecuteStage("VERIFY_BASELINE", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Prepared);
            state.Baseline = CaptureOwner(document, project, RequiredOwner(project, state.ElementId), requireHealthy: true, requireFingerprints: true);
            RequireSourceUnchanged(document, project, state);
            state.Stage = SequenceStage.Baseline;
        });

        [CommandMethod("QS3DCURTAINP09ARMFINGERPRINT", CommandFlags.Modal)]
        public void ArmFingerprintFailure() => ExecuteStage("ARM_FINGERPRINT", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.Baseline);
            var owner = RequiredOwner(project, state.ElementId);
            owner.SetProperty("CurtainMaxPanelWidthM", "0.78");
            project.Touch();
            CurtainWallPostCommitFailureInjection.Arm(nonce, CurtainWallPostCommitFailureInjection.LiveFingerprint);
            SelectSource(document, owner);
            state.Stage = SequenceStage.FingerprintArmed;
        });

        [CommandMethod("QS3DCURTAINP09VERIFYFINGERPRINT", CommandFlags.Modal)]
        public void VerifyFingerprintFailure() => ExecuteStage("VERIFY_FINGERPRINT", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.FingerprintArmed);
            CurtainWallPostCommitFailureInjection.RequireConsumed(nonce, CurtainWallPostCommitFailureInjection.LiveFingerprint);
            var owner = RequiredOwner(project, state.ElementId);
            var current = CaptureOwner(document, project, owner, requireHealthy: false, requireFingerprints: false);
            RequireReplacement(document, RequiredOutput(state.Baseline), current);
            if (HasProperty(owner, FrameFingerprintKey) || HasProperty(owner, PanelFingerprintKey))
                throw new InvalidOperationException("P09 fingerprint injection did not leave both live fingerprints pending.");
            var issues = OwnerIssues(document, project, owner);
            if (issues.Any(x => x.Severity == HealthSeverity.Error) || issues.Count != 2 ||
                !issues.Any(x => string.Equals(x.Code, "CURTAIN_FRAME_LIVE_FINGERPRINT_MISSING", StringComparison.Ordinal)) ||
                !issues.Any(x => string.Equals(x.Code, "CURTAIN_PANEL_LIVE_FINGERPRINT_MISSING", StringComparison.Ordinal)))
                throw new InvalidOperationException("P09 fingerprint failure did not produce the exact Health review state.");
            RequireSourceUnchanged(document, project, state);
            state.FingerprintOutput = current;
            state.Stage = SequenceStage.FingerprintVerified;
        });

        [CommandMethod("QS3DCURTAINP09PRECLEAN", CommandFlags.Modal)]
        public void PrepareCleanRecovery() => ExecuteStage("PREPARE_CLEAN", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.FingerprintVerified);
            var owner = RequiredOwner(project, state.ElementId);
            owner.SetProperty("CurtainMaxPanelWidthM", "0.72");
            project.Touch();
            SelectSource(document, owner);
            state.Stage = SequenceStage.CleanReady;
        });

        [CommandMethod("QS3DCURTAINP09VERIFYCLEAN", CommandFlags.Modal)]
        public void VerifyCleanRecovery() => ExecuteStage("VERIFY_CLEAN", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.CleanReady);
            CurtainWallPostCommitFailureInjection.RequireIdle();
            var current = CaptureOwner(document, project, RequiredOwner(project, state.ElementId), requireHealthy: true, requireFingerprints: true);
            RequireReplacement(document, RequiredOutput(state.FingerprintOutput), current);
            RequireSourceUnchanged(document, project, state);
            state.CleanOutput = current;
            state.Stage = SequenceStage.CleanVerified;
        });

        [CommandMethod("QS3DCURTAINP09ARMUI", CommandFlags.Modal)]
        public void ArmUiFailure() => ExecuteStage("ARM_UI", (document, project, nonce) =>
        {
            var state = RequireState(nonce, SequenceStage.CleanVerified);
            var owner = RequiredOwner(project, state.ElementId);
            owner.SetProperty("CurtainMaxPanelWidthM", "0.66");
            project.Touch();
            CurtainWallPostCommitFailureInjection.Arm(nonce, CurtainWallPostCommitFailureInjection.UiRefresh);
            SelectSource(document, owner);
            state.Stage = SequenceStage.UiArmed;
        });

        [CommandMethod("QS3DCURTAINP09PROBE", CommandFlags.Modal)]
        public void VerifyUiFailure() => ExecuteStage("VERIFY_UI", (document, project, nonce) =>
        {
            var resultPath = RequiredResultPath(Environment.GetEnvironmentVariable(ResultVariable)!);
            if (File.Exists(resultPath)) throw new IOException("P09 result already exists.");
            var state = RequireState(nonce, SequenceStage.UiArmed);
            CurtainWallPostCommitFailureInjection.RequireConsumed(nonce, CurtainWallPostCommitFailureInjection.UiRefresh);
            var current = CaptureOwner(document, project, RequiredOwner(project, state.ElementId), requireHealthy: true, requireFingerprints: true);
            RequireReplacement(document, RequiredOutput(state.CleanOutput), current);
            RequireSourceUnchanged(document, project, state);
            state.Stage = SequenceStage.Complete;
            WriteMarkerAtomic(resultPath, new[]
            {
                "status=PASS", "command=QS3DCURTAINP09PROBE", "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                "nonce=" + nonce, "schema=QS3D_CURTAIN_PANEL_POSTCOMMIT_RUNTIME_V1",
                "qualification_boundary=LOCAL_002_P09_ONLY", "production_local002_qualified=false",
                "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"), "legacy_no_level=true",
                "postcommit_failure_count=2", "fingerprint_failure_committed=true",
                "fingerprint_old_set_removed=true", "fingerprint_new_set_complete=true",
                "fingerprint_health_review_required=true", "frame_fingerprint_missing_health=true",
                "panel_fingerprint_missing_health=true", "ui_failure_committed=true",
                "ui_old_set_removed=true", "ui_new_set_complete=true", "ui_health_issue_count=0",
                "source_geometry_preserved=true",
                "baseline_generated_count=" + RequiredOutput(state.Baseline).GeneratedHandles.Count.ToString(CultureInfo.InvariantCulture),
                "fingerprint_generated_count=" + RequiredOutput(state.FingerprintOutput).GeneratedHandles.Count.ToString(CultureInfo.InvariantCulture),
                "clean_generated_count=" + RequiredOutput(state.CleanOutput).GeneratedHandles.Count.ToString(CultureInfo.InvariantCulture),
                "ui_generated_count=" + current.GeneratedHandles.Count.ToString(CultureInfo.InvariantCulture)
            });
            document.Editor.WriteMessage("\nQS3D Curtain panel P09 post-commit warning probe PASS.");
        });

        private static SequenceState RequireState(string nonce, SequenceStage expected)
        {
            lock (StateSync)
            {
                if (State == null || !string.Equals(State.Nonce, nonce, StringComparison.Ordinal) || State.Stage != expected)
                    throw new InvalidOperationException("P09 runtime command sequence is invalid.");
                return State;
            }
        }

        private static void ExecuteStage(string phase, Action<Document, ProjectState, string> action)
        {
            var requestedPath = Environment.GetEnvironmentVariable(ResultVariable);
            var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
            try
            {
                RequireAutomation(requestedPath, nonce);
                var document = Application.DocumentManager.MdiActiveDocument ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                var project = ProjectContextCoordinator.TryGetReadOnly(document, out var existing) ? existing : ProjectContextCoordinator.GetOrCreate(document);
                action(document, project, nonce);
            }
            catch (Exception error)
            {
                CurtainWallPostCommitFailureInjection.Clear(nonce);
                TryWriteFailure(requestedPath, nonce, phase, FailureCode(error));
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D Curtain panel P09 probe stage failed. See the sanitized local result.");
                throw;
            }
        }

        private static OwnerOutput CaptureOwner(Document document, ProjectState project, ProjectElement owner, bool requireHealthy, bool requireFingerprints)
        {
            RequireLegacyNoLevel(owner);
            if (owner.IsGeneratedCurtainPanelStale()) throw new InvalidOperationException("P09 owner is stale.");
            if (!string.Equals(RequiredProperty(owner, "GeneratedCurtainPanelBuildState"), "Complete", StringComparison.Ordinal))
                throw new InvalidOperationException("P09 panel build state is not Complete.");
            var source = CanonicalHandles(owner.SourceHandles, "P09 source");
            var host = CanonicalHandle(RequiredProperty(owner, "GeneratedSolidHandle"), "P09 host");
            var frames = CanonicalHandles(SplitProperty(owner, "GeneratedCurtainFrameHandles"), "P09 frames");
            var panels = CanonicalHandles(SplitProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey), "P09 panels");
            var generated = new List<string> { host };
            generated.AddRange(frames);
            generated.AddRange(panels);
            if (source.Count != 1 || frames.Count == 0 || panels.Count == 0 || generated.Distinct(StringComparer.OrdinalIgnoreCase).Count() != generated.Count)
                throw new InvalidOperationException("P09 owner output is incomplete or ambiguous.");
            if (generated.Contains(source[0], StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("P09 generated ownership overlaps the source.");
            if (!int.TryParse(RequiredProperty(owner, "GeneratedCurtainPanelCount"), NumberStyles.None, CultureInfo.InvariantCulture, out var panelCount) || panelCount != panels.Count)
                throw new InvalidOperationException("P09 panel count metadata is inconsistent.");
            if (CadHandleService.GetLiveSolidHandles(document, generated).Count != generated.Count)
                throw new InvalidOperationException("P09 generated output is not completely live Solid3d geometry.");
            if (requireFingerprints && (!HasProperty(owner, FrameFingerprintKey) || !HasProperty(owner, PanelFingerprintKey)))
                throw new InvalidOperationException("P09 required live fingerprints are missing.");
            if (requireHealthy && OwnerIssues(document, project, owner).Count != 0)
                throw new InvalidOperationException("P09 owner has blocking or warning Health state.");
            return new OwnerOutput
            {
                SourceHandle = source[0],
                GeneratedHandles = generated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly()
            };
        }

        private static IReadOnlyList<ModelHealthIssue> OwnerIssues(Document document, ProjectState project, ProjectElement owner)
        {
            var panels = CanonicalHandles(SplitProperty(owner, GeneratedCurtainPanelHealthService.HandlesKey), "P09 panels");
            var livePanels = new HashSet<string>(CadHandleService.GetLiveSolidHandles(document, panels), StringComparer.OrdinalIgnoreCase);
            return new GeneratedCurtainPanelHealthService().Inspect(project, livePanels)
                .Concat(CurtainWallFrameLiveStateService.Inspect(document, project))
                .Concat(CurtainWallPanelLiveStateService.Inspect(document, project))
                .Concat(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project))
                .Where(x => string.Equals(x.ElementId, owner.Id, StringComparison.OrdinalIgnoreCase) && x.Severity != HealthSeverity.Info)
                .ToList().AsReadOnly();
        }

        private static void RequireReplacement(Document document, OwnerOutput previous, OwnerOutput current)
        {
            if (!string.Equals(previous.SourceHandle, current.SourceHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P09 source ownership changed during replacement.");
            if (CadHandleService.Resolve(document, previous.GeneratedHandles).Count != 0)
                throw new InvalidOperationException("P09 replacement left old generated output live.");
            if (current.GeneratedHandles.Intersect(previous.GeneratedHandles, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("P09 replacement reused an old generated handle.");
        }

        private static void RequireSourceUnchanged(Document document, ProjectState project, SequenceState state)
        {
            if (!string.Equals(CaptureSourceDigest(document, RequiredOwner(project, state.ElementId)), state.SourceDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("P09 source geometry changed during the post-commit matrix.");
        }

        private static string CaptureSourceDigest(Document document, ProjectElement owner)
        {
            var id = ResolveSingleSource(document, owner);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line
                    ?? throw new InvalidOperationException("P09 source is not a live LINE.");
                var text = Point(line.StartPoint) + "|" + Point(line.EndPoint) + "|" + Point(line.Normal);
                transaction.Commit();
                using (var algorithm = SHA256.Create())
                    return string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string Point(Point3d value) => value.X.ToString("R", CultureInfo.InvariantCulture) + "," + value.Y.ToString("R", CultureInfo.InvariantCulture) + "," + value.Z.ToString("R", CultureInfo.InvariantCulture);
        private static string Point(Vector3d value) => value.X.ToString("R", CultureInfo.InvariantCulture) + "," + value.Y.ToString("R", CultureInfo.InvariantCulture) + "," + value.Z.ToString("R", CultureInfo.InvariantCulture);

        private static ProjectElement RequiredOwner(ProjectState project, string id)
        {
            var owner = project.FindElement(id) ?? throw new InvalidOperationException("P09 GlassWall owner is missing.");
            if (owner.Category != ElementCategory.GlassWall) throw new InvalidOperationException("P09 owner category changed.");
            return owner;
        }

        private static ObjectId ResolveSingleSource(Document document, ProjectElement owner)
        {
            var handles = CanonicalHandles(owner.SourceHandles, "P09 source");
            var ids = CadHandleService.Resolve(document, handles);
            if (handles.Count != 1 || ids.Count != 1) throw new InvalidOperationException("P09 owner requires one live source.");
            return ids[0];
        }

        private static void SelectSource(Document document, ProjectElement owner) => document.Editor.SetImpliedSelection(new[] { ResolveSingleSource(document, owner) });
        private static OwnerOutput RequiredOutput(OwnerOutput? value) => value ?? throw new InvalidOperationException("P09 output snapshot is missing.");
        private static bool HasProperty(ProjectElement owner, string key) => owner.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);
        private static IReadOnlyList<string> SplitProperty(ProjectElement owner, string key) => RequiredProperty(owner, key).Split(new[] { ';' }, StringSplitOptions.None);
        private static string RequiredProperty(ProjectElement owner, string key) => owner.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException("P09 required metadata is missing.");
        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles, string label) => handles.Select(x => CanonicalHandle(x, label)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        private static string CanonicalHandle(string? handle, string label) => CadHandleService.NormalizeHexHandle(handle) ?? throw new InvalidOperationException(label + " is invalid.");
        private static void RequireLegacyNoLevel(ProjectElement owner) { if (CadVerticalPlacementResolver.HasConfiguredLevel(owner)) throw new InvalidOperationException("P09 requires legacy/no-Level placement."); }
        private static bool Near(double left, double right) => Math.Abs(left - right) <= 1e-6d;

        private static void RequireAutomation(string? requestedPath, string nonce)
        {
            if (string.IsNullOrWhiteSpace(requestedPath) || !Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("P09 runtime commands are automation-only.");
            RequiredResultPath(requestedPath!);
        }

        private static string RequiredResultPath(string value)
        {
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("P09 result filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("P09 result directory must already exist.");
            return fullPath;
        }

        private static string FailureCode(Exception error)
        {
            if (error is InvalidDataException) return "DATA_REJECTED";
            if (error is OverflowException) return "OVERFLOW_REJECTED";
            if (error is IOException) return "IO_REJECTED";
            if (error is InvalidOperationException) return "STATE_REJECTED";
            return "UNEXPECTED_REJECTED";
        }

        private static void TryWriteFailure(string? requestedPath, string nonce, string phase, string failureCode)
        {
            try
            {
                var normalized = (requestedPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized) && Guid.TryParseExact(nonce, "N", out _) && FailurePhases.Contains(phase) && FailureCodes.Contains(failureCode))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL", "command=QS3DCURTAINP09PROBE", "nonce=" + nonce,
                        "schema=QS3D_CURTAIN_PANEL_POSTCOMMIT_RUNTIME_V1", "qualification_boundary=LOCAL_002_P09_ONLY",
                        "production_local002_qualified=false", "error_code=CURTAIN_PANEL_POSTCOMMIT_RUNTIME_FAILED",
                        "failure_phase=" + phase, "failure_code=" + failureCode
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredResultPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("P09 result already exists.");
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

        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
