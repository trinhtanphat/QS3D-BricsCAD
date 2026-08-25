using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Persistence;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only probe for the native SAVEAS project-sidecar path transition.
    /// It is shared by the V25 and V26 adapter projects. Evidence is deliberately
    /// boolean/digest-only so project ids and machine-local paths never leave the host.
    /// </summary>
    public sealed class SaveAsLifecycleProbeCommands
    {
        private const string ResultVariable = "QS3D_SAVEAS_RESULT";
        private const string StateVariable = "QS3D_SAVEAS_STATE";
        private const string NonceVariable = "QS3D_SAVEAS_NONCE";
        private const string OriginalVariable = "QS3D_SAVEAS_ORIGINAL_DWG";
        private const string TargetVariable = "QS3D_SAVEAS_TARGET_DWG";
        private const string ProbeMetadataKey = "QS3D.SaveAsLifecycleProbe";
        private const string PendingMetadataKey = "QS3D.SaveAsLifecyclePending";

        [CommandMethod("QS3DSAVEASLIFECYCLEPREP", CommandFlags.Modal)]
        public void Prepare()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(resultPath)) return;
            try
            {
                var nonce = RequiredToken(NonceVariable, "nonce");
                var statePath = RequiredPath(StateVariable, "state");
                var original = RequiredPath(OriginalVariable, "original drawing");
                var target = RequiredPath(TargetVariable, "target drawing");
                var result = RequiredPath(ResultVariable, "result");
                EnsureCommonDirectory(statePath, original, target, result);
                if (File.Exists(statePath) || File.Exists(result))
                    throw new InvalidOperationException("SAVEAS lifecycle evidence already exists.");
                if (File.Exists(target) || File.Exists(Path.ChangeExtension(target, ".qsdb")) || File.Exists(Path.ChangeExtension(target, ".qsdb") + ".bak"))
                    throw new InvalidOperationException("SAVEAS lifecycle target must not already exist.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available for SAVEAS lifecycle preparation.");
                if (!SamePath(document.Name, original))
                    throw new InvalidOperationException("SAVEAS lifecycle preparation is not running on the expected source drawing.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                project.Metadata[ProbeMetadataKey] = nonce;
                project.Metadata.Remove(PendingMetadataKey);
                project.Touch();
                var oldSidecar = ProjectContextCoordinator.Save(document);
                if (!SamePath(oldSidecar, Path.ChangeExtension(original, ".qsdb")) || ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("SAVEAS lifecycle baseline did not persist cleanly at the original drawing path.");

                var baselineHash = Sha256(oldSidecar);
                var projectDigest = Digest(project.ProjectId, nonce);
                project.Metadata[PendingMetadataKey] = nonce;
                project.Touch();
                if (!ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("SAVEAS lifecycle pending mutation was not detected before SAVEAS.");

                WriteStateAtomic(statePath, new[]
                {
                    "nonce=" + nonce,
                    "project_digest=" + projectDigest,
                    "old_sidecar_hash=" + baselineHash
                });
                document.Editor.WriteMessage("\nQS3D SAVEAS lifecycle preparation ready.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "SAVEAS_PREP_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DSAVEASLIFECYCLEVERIFY", CommandFlags.Modal)]
        public void Verify()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(resultPath)) return;
            try
            {
                var nonce = RequiredToken(NonceVariable, "nonce");
                var statePath = RequiredPath(StateVariable, "state");
                var original = RequiredPath(OriginalVariable, "original drawing");
                var target = RequiredPath(TargetVariable, "target drawing");
                var result = RequiredPath(ResultVariable, "result");
                EnsureCommonDirectory(statePath, original, target, result);
                if (File.Exists(result)) throw new InvalidOperationException("SAVEAS lifecycle result already exists.");

                var state = ReadState(statePath);
                RequireState(state, "nonce", nonce);
                var expectedDigest = RequireState(state, "project_digest");
                var oldHash = RequireState(state, "old_sidecar_hash");
                var oldSidecar = Path.ChangeExtension(original, ".qsdb");
                var newSidecar = Path.ChangeExtension(target, ".qsdb");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available after SAVEAS.");
                if (!SamePath(document.Name, target))
                    throw new InvalidOperationException("Native SAVEAS did not transition to the expected target drawing.");
                if (!File.Exists(newSidecar))
                    throw new InvalidOperationException("SAVEAS did not persist the QS3D sidecar at the target drawing path.");
                if (!File.Exists(oldSidecar) || !string.Equals(Sha256(oldSidecar), oldHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("SAVEAS unexpectedly changed the original drawing sidecar.");
                if (ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("QS3D project remains pending after SAVEAS SaveComplete persistence.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var current))
                    throw new InvalidOperationException("The SAVEAS target project is not readable.");
                EnsureProject(current, nonce, expectedDigest);

                ProjectContextCoordinator.Forget(document);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var reopened))
                    throw new InvalidOperationException("Cold-cache reload could not read the SAVEAS target sidecar.");
                EnsureProject(reopened, nonce, expectedDigest);
                if (ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("Cold-cache SAVEAS target unexpectedly reports pending changes.");

                WriteMarkerAtomic(result, new[]
                {
                    "status=PASS",
                    "schema=QS3D_SAVEAS_LIFECYCLE_V1",
                    "native_saveas_path_transition=true",
                    "canonical_project_identity_preserved=true",
                    "target_sidecar_persisted=true",
                    "original_sidecar_unchanged=true",
                    "pending_state_cleared=true",
                    "cold_cache_reload_matched=true"
                });
                document.Editor.WriteMessage("\nQS3D SAVEAS lifecycle probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "SAVEAS_VERIFY_FAILED");
                throw;
            }
        }

        private static void EnsureProject(QS3D.Core.Domain.ProjectState project, string nonce, string expectedDigest)
        {
            if (!project.Metadata.TryGetValue(ProbeMetadataKey, out var probe) || !string.Equals(probe, nonce, StringComparison.Ordinal))
                throw new InvalidOperationException("SAVEAS target lost baseline project metadata.");
            if (!project.Metadata.TryGetValue(PendingMetadataKey, out var pending) || !string.Equals(pending, nonce, StringComparison.Ordinal))
                throw new InvalidOperationException("SAVEAS target did not persist the pending semantic mutation.");
            if (!string.Equals(Digest(project.ProjectId, nonce), expectedDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("SAVEAS changed the canonical project identity.");
        }

        private static Dictionary<string, string> ReadState(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("SAVEAS lifecycle state is missing.", path);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(path))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0) throw new InvalidDataException("Malformed SAVEAS lifecycle state.");
                var key = line.Substring(0, separator);
                var value = line.Substring(separator + 1);
                if (!values.TryAdd(key, value)) throw new InvalidDataException("Duplicate SAVEAS lifecycle state key.");
            }
            return values;
        }

        private static string RequireState(Dictionary<string, string> state, string key)
        {
            if (!state.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("SAVEAS lifecycle state is incomplete.");
            return value;
        }

        private static void RequireState(Dictionary<string, string> state, string key, string expected)
        {
            if (!string.Equals(RequireState(state, key), expected, StringComparison.Ordinal))
                throw new InvalidDataException("SAVEAS lifecycle state identity mismatch.");
        }

        private static string RequiredToken(string variable, string label)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
                throw new InvalidOperationException("SAVEAS lifecycle " + label + " is missing or invalid.");
            return value;
        }

        private static string RequiredPath(string variable, string label)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("SAVEAS lifecycle " + label + " path is missing.");
            return Path.GetFullPath(value);
        }

        private static void EnsureCommonDirectory(params string[] paths)
        {
            var root = Path.GetDirectoryName(paths[0]) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("SAVEAS lifecycle artifact root is invalid.");
            foreach (var path in paths)
            {
                var directory = Path.GetDirectoryName(path) ?? string.Empty;
                if (!string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("SAVEAS lifecycle paths must share one isolated artifact directory.");
            }
        }

        private static bool SamePath(string left, string right)
        {
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        private static string Digest(string value, string nonce)
        {
            using (var sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(nonce + "\n" + value)));
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) builder.Append(b.ToString("X2"));
                return builder.ToString();
            }
        }

        private static void WriteStateAtomic(string path, IEnumerable<string> lines) => WriteAtomic(path, lines);
        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines) => WriteAtomic(path, lines);

        private static void WriteAtomic(string path, IEnumerable<string> lines)
        {
            var temp = path + ".tmp";
            if (File.Exists(temp)) throw new IOException("SAVEAS lifecycle temporary evidence already exists.");
            File.WriteAllLines(temp, lines, new UTF8Encoding(false));
            File.Move(temp, path);
        }

        private static void TryWriteFailure(string? path, string code)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var full = Path.GetFullPath(path);
                if (!File.Exists(full)) WriteMarkerAtomic(full, new[] { "status=FAIL", "error_code=" + code });
            }
            catch { }
        }
    }
}
