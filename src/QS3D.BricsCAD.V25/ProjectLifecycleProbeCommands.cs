using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only, synthetic-fixture qualification for save/reopen, cold-cache
    /// canonical binding and multi-DWG project isolation. Persisted evidence contains
    /// booleans/counts only; drawing paths and project ids never enter the final marker.
    /// </summary>
    public sealed class ProjectLifecycleProbeCommands
    {
        private const string ResultVariable = "QS3D_LIFECYCLE_RESULT";
        private const string StateVariable = "QS3D_LIFECYCLE_STATE";
        private const string NonceVariable = "QS3D_LIFECYCLE_NONCE";
        private const string RoleVariable = "QS3D_LIFECYCLE_ROLE";
        private const string DrawingAVariable = "QS3D_LIFECYCLE_DWG_A";
        private const string DrawingBVariable = "QS3D_LIFECYCLE_DWG_B";
        private const string DrawingCVariable = "QS3D_LIFECYCLE_DWG_C";
        private const string DrawingDVariable = "QS3D_LIFECYCLE_DWG_D";
        private const string StateFileName = "project-lifecycle-state.txt";
        private const string FinalResultFileName = "project-lifecycle-result.txt";
        private const string RoleMetadataKey = "QS3D.LifecycleProbe.Role";
        private const string MutationMetadataKey = "QS3D.LifecycleProbe.MultiDwgMutation";

        [CommandMethod("QS3DLIFECYCLESEED", CommandFlags.Modal)]
        public void Seed()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var role = RequiredRole();
                var statePath = RequiredStatePath(nonce);
                var expectedDrawing = RequiredDrawingPath(role == "A" ? DrawingAVariable : DrawingBVariable);
                EnsureProbeScope(statePath, resultPath!, expectedDrawing);
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available for the lifecycle seed.");
                if (!SamePath(document.Name, expectedDrawing))
                    throw new InvalidOperationException("The lifecycle seed active drawing does not match its assigned role.");
                var projectPath = ProjectContextCoordinator.GetProjectPath(document);
                if (File.Exists(projectPath) || File.Exists(projectPath + ".bak"))
                    throw new InvalidOperationException("The lifecycle seed requires a drawing copy without a QS3D sidecar.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                project.Metadata[RoleMetadataKey] = role;
                project.Metadata.Remove(MutationMetadataKey);
                project.Touch();
                if (!ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("The lifecycle seed mutation was not marked pending.");

                document.Editor.WriteMessage("\nQS3D lifecycle seed prepared for automatic DWG-save persistence.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "SEED_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DLIFECYCLEAFTERSAVE", CommandFlags.Modal)]
        public void VerifyAfterSave()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var role = RequiredRole();
                var statePath = RequiredStatePath(nonce);
                var expectedResult = "project-lifecycle-seed-" + role.ToLowerInvariant() + ".txt";
                var validatedResult = RequiredOutputPath(resultPath, expectedResult, "seed result");
                var expectedDrawing = RequiredDrawingPath(role == "A" ? DrawingAVariable : DrawingBVariable);
                EnsureProbeScope(statePath, validatedResult, expectedDrawing);
                if (File.Exists(validatedResult)) throw new IOException("The lifecycle seed result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available after the lifecycle save.");
                if (!SamePath(document.Name, expectedDrawing))
                    throw new InvalidOperationException("The lifecycle after-save drawing does not match its assigned role.");
                var projectPath = ProjectContextCoordinator.GetProjectPath(document);
                if (!File.Exists(projectPath))
                    throw new InvalidOperationException("DWG SaveComplete did not persist the matching QS3D sidecar.");
                if (ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("The QS3D project remains pending after DWG SaveComplete.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("The saved QS3D project is not readable.");
                if (!project.Metadata.TryGetValue(RoleMetadataKey, out var storedRole) ||
                    !string.Equals(storedRole, role, StringComparison.Ordinal))
                    throw new InvalidOperationException("The saved lifecycle role did not round-trip.");

                WriteStateRole(statePath, nonce, role, ProjectDigest(project.ProjectId, nonce));
                WriteMarkerAtomic(validatedResult, new[]
                {
                    "status=PASS",
                    "command=QS3DLIFECYCLEAFTERSAVE",
                    "schema=QS3D_PROJECT_LIFECYCLE_SEED_V1",
                    "nonce=" + nonce,
                    "role=" + role,
                    "dwg_savecomplete_sidecar=true",
                    "pending_changes_cleared=true",
                    "saved_project_readable=true"
                });
                document.Editor.WriteMessage("\nQS3D lifecycle seed/save check PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "AFTER_SAVE_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DLIFECYCLEPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var state = ReadState(RequiredStatePath(nonce), nonce);
                var expectedA = RequiredDigest(state, "a");
                var expectedB = RequiredDigest(state, "b");
                var result = RequiredOutputPath(resultPath, FinalResultFileName, "result");
                if (File.Exists(result)) throw new IOException("The lifecycle result already exists.");

                var drawingA = RequiredDrawingPath(DrawingAVariable);
                var drawingB = RequiredDrawingPath(DrawingBVariable);
                var drawingC = RequiredDrawingPath(DrawingCVariable);
                var drawingD = RequiredDrawingPath(DrawingDVariable);
                EnsureProbeScope(RequiredStatePath(nonce), result, drawingA, drawingB, drawingC, drawingD);
                var documentA = FindDocument(drawingA);
                var documentB = FindDocument(drawingB);
                var documentC = FindDocument(drawingC);
                var documentD = FindDocument(drawingD);

                // Simulate a cold cache after reopen. A and B must reload their existing
                // sidecars; C deliberately has none and must remain unavailable.
                ProjectContextCoordinator.Forget(documentA);
                ProjectContextCoordinator.Forget(documentB);
                if (ProjectContextCoordinator.TryGetReadOnly(documentC, out _))
                    throw new InvalidOperationException("Activating a drawing without a sidecar created or cached a replacement project.");
                if (!ProjectContextCoordinator.TryGetReadOnly(documentA, out var observedA) ||
                    !ProjectContextCoordinator.TryGetReadOnly(documentB, out var observedB))
                    throw new InvalidOperationException("Cold-cache reopen could not read both saved sidecars.");
                EnsureProject(observedA, "A", expectedA, nonce);
                EnsureProject(observedB, "B", expectedB, nonce);
                if (string.Equals(observedA.ProjectId, observedB.ProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The two reopened drawings share one project identity.");
                var detachedA = DetachedProjectStamp.Capture(observedA);
                var detachedB = DetachedProjectStamp.Capture(observedB);

                if (!ExistingProjectMutationContext.TryGet(documentA, out var canonicalA) ||
                    !ExistingProjectMutationContext.TryGet(documentB, out var canonicalB))
                    throw new InvalidOperationException("Existing-project mutation binding failed after cold-cache reopen.");
                if (ReferenceEquals(observedA, canonicalA) || ReferenceEquals(observedB, canonicalB))
                    throw new InvalidOperationException("A detached read-only snapshot leaked into the canonical mutation context.");
                EnsureProject(canonicalA, "A", expectedA, nonce);
                EnsureProject(canonicalB, "B", expectedB, nonce);

                canonicalA.Metadata[MutationMetadataKey] = "A";
                canonicalA.Touch();
                ProjectContextCoordinator.Save(documentA);
                canonicalB.Metadata[MutationMetadataKey] = "B";
                canonicalB.Touch();
                ProjectContextCoordinator.Save(documentB);
                detachedA.EnsureUnchanged(observedA);
                detachedB.EnsureUnchanged(observedB);

                ProjectContextCoordinator.Forget(documentA);
                ProjectContextCoordinator.Forget(documentB);
                if (!ProjectContextCoordinator.TryGetReadOnly(documentA, out var reopenedA) ||
                    !ProjectContextCoordinator.TryGetReadOnly(documentB, out var reopenedB))
                    throw new InvalidOperationException("The multi-DWG mutations did not survive a second cold reload.");
                EnsureProject(reopenedA, "A", expectedA, nonce);
                EnsureProject(reopenedB, "B", expectedB, nonce);
                EnsureMutation(reopenedA, "A");
                EnsureMutation(reopenedB, "B");
                if (ProjectContextCoordinator.TryGetReadOnly(documentC, out _))
                    throw new InvalidOperationException("Another drawing mutation populated the absent-sidecar document context.");
                EnsureCorruptSidecarFailsClosed(documentD);

                WriteMarkerAtomic(result, new[]
                {
                    "status=PASS",
                    "command=QS3DLIFECYCLEPROBE",
                    "schema=QS3D_PROJECT_LIFECYCLE_V1",
                    "nonce=" + nonce,
                    "document_count=" + Application.DocumentManager.Count.ToString(CultureInfo.InvariantCulture),
                    "dwg_savecomplete_sidecar=true",
                    "cold_reopen_project_identity_matched=true",
                    "canonical_bind_matched=true",
                    "detached_snapshot_not_mutated=true",
                    "distinct_project_identity=true",
                    "multi_dwg_mutation_isolated=true",
                    "second_cold_reload_persisted=true",
                    "absent_sidecar_noncreating=true",
                    "corrupt_sidecar_fail_closed=true"
                });
                documentD.Editor.WriteMessage("\nQS3D save/reopen/multi-DWG lifecycle probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "LIFECYCLE_FAILED");
                throw;
            }
        }

        private static void EnsureProject(QS3D.Core.Domain.ProjectState project, string role, string digest, string nonce)
        {
            if (!string.Equals(ProjectDigest(project.ProjectId, nonce), digest, StringComparison.Ordinal))
                throw new InvalidOperationException("A reopened project identity did not match its saved seed.");
            if (!project.Metadata.TryGetValue(RoleMetadataKey, out var storedRole) ||
                !string.Equals(storedRole, role, StringComparison.Ordinal))
                throw new InvalidOperationException("A reopened project role did not match its drawing.");
        }

        private static void EnsureMutation(QS3D.Core.Domain.ProjectState project, string expected)
        {
            if (!project.Metadata.TryGetValue(MutationMetadataKey, out var actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("A multi-DWG project mutation was lost or crossed into another drawing.");
        }

        private static void EnsureCorruptSidecarFailsClosed(Document document)
        {
            ProjectContextCoordinator.Forget(document);
            var readFailed = false;
            try { ProjectContextCoordinator.TryGetReadOnly(document, out _); }
            catch (InvalidDataException) { readFailed = true; }
            if (!readFailed)
                throw new InvalidOperationException("A corrupt sidecar did not fail the read-only load boundary.");

            var bindFailed = false;
            try { ProjectContextCoordinator.GetOrCreate(document); }
            catch (InvalidDataException) { bindFailed = true; }
            if (!bindFailed || ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("A corrupt sidecar created or cached mutable replacement state.");
        }

        private static Document FindDocument(string path)
        {
            foreach (Document document in Application.DocumentManager)
                if (SamePath(document.Name, path)) return document;
            throw new InvalidOperationException("A required lifecycle drawing is not open.");
        }

        private static string RequiredDrawingPath(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(variable + " is required.");
            var path = Path.GetFullPath(value);
            if (!path.EndsWith(".reference-copy.dwg", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Lifecycle drawings must be existing disposable '*.reference-copy.dwg' files.");
            return path;
        }

        private static void EnsureProbeScope(string statePath, string resultPath, params string[] drawings)
        {
            var artifactRoot = Path.GetDirectoryName(Path.GetFullPath(statePath));
            if (string.IsNullOrWhiteSpace(artifactRoot) ||
                !string.Equals(Path.GetDirectoryName(Path.GetFullPath(resultPath)), artifactRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Lifecycle state and results must share one qualification directory.");
            var copyRoot = Path.Combine(artifactRoot, "fixture-copies");
            foreach (var drawing in drawings)
                if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(drawing)), copyRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Lifecycle drawing copies must stay in the qualification fixture-copies directory.");
        }

        private static bool SamePath(string? left, string right)
        {
            if (string.IsNullOrWhiteSpace(left)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) { return false; }
        }

        private static string RequiredRole()
        {
            var role = (Environment.GetEnvironmentVariable(RoleVariable) ?? string.Empty).Trim().ToUpperInvariant();
            if (role != "A" && role != "B") throw new InvalidOperationException("The lifecycle role must be A or B.");
            return role;
        }

        private static string RequiredNonce()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            if (!Guid.TryParseExact(nonce, "N", out _)) throw new InvalidOperationException("The lifecycle nonce is invalid.");
            return nonce;
        }

        private static string RequiredStatePath(string nonce)
        {
            var path = RequiredOutputPath(Environment.GetEnvironmentVariable(StateVariable), StateFileName, "state");
            if (!File.Exists(path)) throw new FileNotFoundException("The lifecycle state file is missing.");
            var state = ReadState(path, nonce);
            if (!state.TryGetValue("nonce", out var storedNonce) || !string.Equals(storedNonce, nonce, StringComparison.Ordinal))
                throw new InvalidDataException("The lifecycle state nonce does not match.");
            return path;
        }

        private static Dictionary<string, string> ReadState(string path, string nonce)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0) throw new InvalidDataException("The lifecycle state is malformed.");
                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                if (key.Length == 0 || value.Length == 0 || result.ContainsKey(key))
                    throw new InvalidDataException("The lifecycle state contains an invalid or duplicate field.");
                result.Add(key, value);
            }
            if (!result.TryGetValue("nonce", out var storedNonce) || !string.Equals(storedNonce, nonce, StringComparison.Ordinal))
                throw new InvalidDataException("The lifecycle state nonce does not match.");
            return result;
        }

        private static void WriteStateRole(string path, string nonce, string role, string digest)
        {
            var state = ReadState(path, nonce);
            var key = role.ToLowerInvariant();
            if (state.ContainsKey(key)) throw new InvalidDataException("The lifecycle state already contains this role.");
            state[key] = digest;
            var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var backupPath = path + "." + Guid.NewGuid().ToString("N") + ".bak";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine("nonce=" + nonce);
                    foreach (var item in state.Where(x => !string.Equals(x.Key, "nonce", StringComparison.OrdinalIgnoreCase))
                                              .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        writer.WriteLine(item.Key.ToLowerInvariant() + "=" + item.Value);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Replace(tempPath, path, backupPath, true);
            }
            finally
            {
                TryDelete(tempPath);
                TryDelete(backupPath);
            }
        }

        private static string RequiredDigest(IDictionary<string, string> state, string key)
        {
            if (!state.TryGetValue(key, out var value) || value.Length != 64 || value.Any(x => !Uri.IsHexDigit(x)))
                throw new InvalidDataException("The lifecycle state is missing a canonical project digest.");
            return value.ToUpperInvariant();
        }

        private static string ProjectDigest(string projectId, string nonce)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(nonce + "\0" + (projectId ?? string.Empty)));
                return BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
        }

        private static string RequiredOutputPath(string? value, string expectedFileName, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Lifecycle " + label + " path is required.", label);
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), expectedFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The lifecycle " + label + " filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("The lifecycle output directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? resultPath, string errorCode)
        {
            try
            {
                var normalized = (resultPath ?? string.Empty).Trim();
                if (normalized.Length == 0 || File.Exists(normalized)) return;
                var fileName = Path.GetFileName(normalized);
                if (!string.Equals(fileName, FinalResultFileName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "project-lifecycle-seed-a.txt", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "project-lifecycle-seed-b.txt", StringComparison.OrdinalIgnoreCase)) return;
                WriteMarkerAtomic(normalized, new[]
                {
                    "status=FAIL",
                    "command=QS3DLIFECYCLEPROBE",
                    "nonce=" + SafeNonce(),
                    "error_code=" + errorCode
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = Path.GetFullPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("The lifecycle marker already exists.");
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

        private static bool SkipOutsideAutomation(string? resultPath)
        {
            if (!string.IsNullOrWhiteSpace(resultPath)) return false;
            Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                "\nQS3D project lifecycle probe skipped: " + ResultVariable + " is not set.");
            return true;
        }

        private static string SafeNonce()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            return Guid.TryParseExact(nonce, "N", out _) ? nonce : "invalid";
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private sealed class DetachedProjectStamp
        {
            private readonly long _changeVersion;
            private readonly DateTime _updatedUtc;
            private readonly int _auditCount;
            private readonly int _elementCount;
            private readonly int _familyCount;
            private readonly Dictionary<string, string> _metadata;

            private DetachedProjectStamp(QS3D.Core.Domain.ProjectState project)
            {
                _changeVersion = project.ChangeVersion;
                _updatedUtc = project.UpdatedUtc;
                _auditCount = AuditTrail.ForProject(project).Events.Count;
                _elementCount = project.Elements.Count;
                _familyCount = project.Families.Count;
                _metadata = new Dictionary<string, string>(project.Metadata, StringComparer.OrdinalIgnoreCase);
            }

            public static DetachedProjectStamp Capture(QS3D.Core.Domain.ProjectState project) =>
                new DetachedProjectStamp(project ?? throw new ArgumentNullException(nameof(project)));

            public void EnsureUnchanged(QS3D.Core.Domain.ProjectState project)
            {
                if (project.ChangeVersion != _changeVersion || project.UpdatedUtc != _updatedUtc ||
                    AuditTrail.ForProject(project).Events.Count != _auditCount ||
                    project.Elements.Count != _elementCount || project.Families.Count != _familyCount ||
                    project.Metadata.Count != _metadata.Count ||
                    _metadata.Any(x => !project.Metadata.TryGetValue(x.Key, out var value) || !string.Equals(value, x.Value, StringComparison.Ordinal)))
                    throw new InvalidOperationException("A detached read-only project snapshot was mutated by canonical multi-DWG work.");
            }
        }
    }
}
