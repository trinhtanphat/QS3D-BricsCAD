using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>Automation-only LOCAL-001 probe. Never targets owner/customer drawings.</summary>
    public sealed class SidecarRevisionProbeCommands
    {
        private const string ResultVariable = "QS3D_SIDECAR_REVISION_RESULT";
        private const string NonceVariable = "QS3D_SIDECAR_REVISION_NONCE";
        private const string DrawingVariable = "QS3D_SIDECAR_REVISION_DWG";
        private const string ResultFileName = "sidecar-revision-result.txt";
        private const string ProbeMetadataKey = "QS3D.SidecarRevisionProbe";

        [CommandMethod("QS3DSIDECARREVISIONPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var rawResult = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(rawResult))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D sidecar revision probe skipped: automation scope is not armed.");
                return;
            }

            try
            {
                var nonce = RequiredNonce();
                var resultPath = RequiredResultPath(rawResult);
                var drawingPath = RequiredDrawingPath();
                EnsureScope(resultPath, drawingPath);
                if (File.Exists(resultPath)) throw new IOException("The sidecar revision result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available for the sidecar revision probe.");
                if (!SamePath(document.Name, drawingPath))
                    throw new InvalidOperationException("The active drawing does not match the scoped disposable copy.");

                var sidecar = ProjectContextCoordinator.GetProjectPath(document);
                if (File.Exists(sidecar) || File.Exists(sidecar + ".bak"))
                    throw new InvalidOperationException("The sidecar revision probe requires a drawing copy without a sidecar.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                project.Metadata[ProbeMetadataKey] = nonce;
                project.Touch();
                ProjectContextCoordinator.Save(document);
                if (ProjectContextCoordinator.HasPendingChanges(document) || !File.Exists(sidecar) || File.Exists(sidecar + ".bak"))
                    throw new InvalidOperationException("The probe could not establish one clean primary-only sidecar baseline.");

                var scratchRoot = Path.GetDirectoryName(resultPath) ?? throw new InvalidOperationException("The probe scratch root is unavailable.");
                var baseline = ProjectStateProbeStamp.Capture(project, scratchRoot, nonce);
                TestBackupAppearance(document, project, sidecar, baseline);
                TestPrimaryReplacement(document, project, sidecar, nonce, baseline);
                TestPrimaryRemoval(document, project, sidecar, nonce, baseline);
                EnsureRecovered(document, project, baseline);

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "schema=QS3D_SIDECAR_REVISION_V1",
                    "command=QS3DSIDECARREVISIONPROBE",
                    "nonce=" + nonce,
                    "backup_appearance_refused=true",
                    "primary_replacement_refused=true",
                    "primary_removal_refused=true",
                    "read_only_boundary_refused=true",
                    "canonical_bind_refused=true",
                    "existing_mutation_refused=true",
                    "interchange_confirmation_refused=true",
                    "sidecar_overwrite_refused=true",
                    "project_state_unchanged=true",
                    "restored_session_recovered=true",
                    "dwg_write_not_requested=true"
                });
                document.Editor.WriteMessage("\nQS3D sidecar revision probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(rawResult, "SIDECAR_REVISION_PROBE_FAILED");
                throw;
            }
        }

        private static void TestBackupAppearance(
            Document document,
            ProjectState project,
            string sidecar,
            ProjectStateProbeStamp baseline)
        {
            var backup = sidecar + ".bak";
            if (File.Exists(backup)) throw new IOException("Unexpected backup exists before the backup-appearance phase.");
            File.Copy(sidecar, backup, false);
            try { AssertAllBoundariesReject(document, project, baseline); }
            finally { if (File.Exists(backup)) File.Delete(backup); }
            EnsureRecovered(document, project, baseline);
        }

        private static void TestPrimaryReplacement(
            Document document,
            ProjectState project,
            string sidecar,
            string nonce,
            ProjectStateProbeStamp baseline)
        {
            var original = sidecar + "." + nonce + ".original";
            var replacement = sidecar + "." + nonce + ".replacement";
            try
            {
                File.Copy(sidecar, replacement, false);
                File.AppendAllText(replacement, Environment.NewLine, new UTF8Encoding(false));
                File.Move(sidecar, original);
                File.Move(replacement, sidecar);
                AssertAllBoundariesReject(document, project, baseline);
            }
            finally
            {
                if (File.Exists(original))
                {
                    if (File.Exists(sidecar)) File.Delete(sidecar);
                    File.Move(original, sidecar);
                }
                if (File.Exists(replacement)) File.Delete(replacement);
            }
            EnsureRecovered(document, project, baseline);
        }

        private static void TestPrimaryRemoval(
            Document document,
            ProjectState project,
            string sidecar,
            string nonce,
            ProjectStateProbeStamp baseline)
        {
            var quarantine = sidecar + "." + nonce + ".removed";
            File.Move(sidecar, quarantine);
            try { AssertAllBoundariesReject(document, project, baseline); }
            finally
            {
                if (File.Exists(quarantine))
                {
                    if (File.Exists(sidecar)) File.Delete(sidecar);
                    File.Move(quarantine, sidecar);
                }
            }
            EnsureRecovered(document, project, baseline);
        }

        private static void AssertAllBoundariesReject(
            Document document,
            ProjectState project,
            ProjectStateProbeStamp baseline)
        {
            var reviewedVersion = project.ChangeVersion;
            ExpectInvalid(() => ProjectContextCoordinator.TryGetReadOnly(document, out _));
            ExpectInvalid(() => ProjectContextCoordinator.GetOrCreate(document));
            ExpectInvalid(() => ExistingProjectMutationContext.Require(document, "Sidecar revision probe"));
            ExpectInvalid(() => InterchangeConfirmationGuard.RequireFresh(document, project, reviewedVersion, "Sidecar revision probe"));
            ExpectInvalid(() => ProjectContextCoordinator.Save(document));
            baseline.EnsureUnchanged(project);
        }

        private static void EnsureRecovered(Document document, ProjectState project, ProjectStateProbeStamp baseline)
        {
            var current = ProjectContextCoordinator.GetOrCreate(document);
            if (!ReferenceEquals(current, project))
                throw new InvalidOperationException("Restored backing store did not retain the canonical project instance.");
            if (!ReferenceEquals(ExistingProjectMutationContext.Require(document, "Sidecar revision recovery"), project))
                throw new InvalidOperationException("Restored backing store did not recover existing-project mutation binding.");
            if (!ReferenceEquals(InterchangeConfirmationGuard.RequireFresh(document, project, project.ChangeVersion, "Sidecar revision recovery"), project))
                throw new InvalidOperationException("Restored backing store did not recover Interchange confirmation.");
            if (ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("Sidecar freshness rejection left pending semantic state.");
            baseline.EnsureUnchanged(project);
        }

        private static void ExpectInvalid(Action action)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("A stale sidecar boundary unexpectedly accepted cached project authority.");
        }

        private static string RequiredNonce()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            if (!Guid.TryParseExact(nonce, "N", out _)) throw new InvalidOperationException("The sidecar revision nonce is invalid.");
            return nonce;
        }

        private static string RequiredResultPath(string rawResult)
        {
            var path = Path.GetFullPath(rawResult);
            if (!string.Equals(Path.GetFileName(path), ResultFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The sidecar revision result filename is invalid.");
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new InvalidOperationException("The sidecar revision result directory must already exist.");
            return path;
        }

        private static string RequiredDrawingPath()
        {
            var value = Environment.GetEnvironmentVariable(DrawingVariable);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("The sidecar revision drawing scope is missing.");
            var path = Path.GetFullPath(value);
            if (!path.EndsWith(".reference-copy.dwg", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("The sidecar revision probe requires an existing disposable reference copy.");
            return path;
        }

        private static void EnsureScope(string resultPath, string drawingPath)
        {
            var artifactRoot = Path.GetDirectoryName(resultPath) ?? string.Empty;
            var copyRoot = Path.Combine(artifactRoot, "fixture-copies");
            if (!string.Equals(Path.GetDirectoryName(drawingPath), copyRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The sidecar revision drawing must stay in the qualification fixture-copies directory.");
        }

        private static bool SamePath(string? left, string right)
        {
            if (string.IsNullOrWhiteSpace(left)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch (System.Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) { return false; }
        }

        private static void TryWriteFailure(string? rawResult, string errorCode)
        {
            try
            {
                var normalizedResult = (rawResult ?? string.Empty).Trim();
                if (normalizedResult.Length == 0) return;
                var path = RequiredResultPath(normalizedResult);
                if (File.Exists(path)) return;
                WriteMarkerAtomic(path, new[]
                {
                    "status=FAIL",
                    "schema=QS3D_SIDECAR_REVISION_V1",
                    "command=QS3DSIDECARREVISIONPROBE",
                    "nonce=" + (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim(),
                    "error_code=" + errorCode
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("The sidecar revision marker already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine((line ?? string.Empty).Replace('\r', ' ').Replace('\n', ' '));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch { }
            }
        }

        private sealed class ProjectStateProbeStamp
        {
            private static readonly QsdbProjectStore Store = new QsdbProjectStore();
            private readonly long _changeVersion;
            private readonly DateTime _updatedUtc;
            private readonly int _auditCount;
            private readonly int _elementCount;
            private readonly int _familyCount;
            private readonly int _metadataCount;
            private readonly string _scratchRoot;
            private readonly string _nonce;
            private readonly byte[] _digest;

            private ProjectStateProbeStamp(ProjectState project, string scratchRoot, string nonce)
            {
                _changeVersion = project.ChangeVersion;
                _updatedUtc = project.UpdatedUtc;
                _auditCount = project.AuditEvents.Count;
                _elementCount = project.Elements.Count;
                _familyCount = project.Families.Count;
                _metadataCount = project.Metadata.Count;
                _scratchRoot = scratchRoot;
                _nonce = nonce;
                _digest = ComputeDigest(project, scratchRoot, nonce);
            }

            public static ProjectStateProbeStamp Capture(ProjectState project, string scratchRoot, string nonce) =>
                new ProjectStateProbeStamp(project, scratchRoot, nonce);

            public void EnsureUnchanged(ProjectState project)
            {
                if (project.ChangeVersion != _changeVersion || project.UpdatedUtc != _updatedUtc ||
                    project.AuditEvents.Count != _auditCount || project.Elements.Count != _elementCount ||
                    project.Families.Count != _familyCount || project.Metadata.Count != _metadataCount)
                    throw new InvalidOperationException("A rejected sidecar freshness boundary mutated semantic project state.");
                var current = ComputeDigest(project, _scratchRoot, _nonce);
                if (current.Length != _digest.Length)
                    throw new InvalidOperationException("A rejected sidecar freshness boundary changed serialized project state.");
                var difference = 0;
                for (var index = 0; index < current.Length; index++) difference |= current[index] ^ _digest[index];
                if (difference != 0)
                    throw new InvalidOperationException("A rejected sidecar freshness boundary changed serialized project state.");
            }

            private static byte[] ComputeDigest(ProjectState project, string scratchRoot, string nonce)
            {
                var path = Path.Combine(scratchRoot, "sidecar-project-state-" + nonce + "-" + Guid.NewGuid().ToString("N") + ".qsdb");
                try
                {
                    var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
                    Store.Save(detached, path);
                    var document = XDocument.Load(path, LoadOptions.None);
                    var root = document.Root ?? throw new InvalidDataException("Probe QSDB digest has no root element.");
                    root.SetAttributeValue("updatedUtc", "<normalized-by-sidecar-probe>");
                    var payload = Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
                    using (var sha = SHA256.Create()) return sha.ComputeHash(payload);
                }
                finally
                {
                    foreach (var candidate in new[] { path, path + ".bak", path + ".lock" })
                    {
                        try { if (File.Exists(candidate)) File.Delete(candidate); }
                        catch { }
                    }
                }
            }
        }
    }
}
