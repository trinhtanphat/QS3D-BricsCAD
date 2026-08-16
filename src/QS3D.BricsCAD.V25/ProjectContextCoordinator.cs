using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25
{
    internal static class ProjectContextCoordinator
    {
        private const string RecoveryRequiredKey = "QS3D.ReadOnlyRecoveryRequired";
        private static readonly Dictionary<Document, ProjectState> Projects = new Dictionary<Document, ProjectState>();
        private static readonly Dictionary<Document, ProjectPersistenceStamp> PersistenceStamps = new Dictionary<Document, ProjectPersistenceStamp>();
        private static readonly Dictionary<Document, ProjectSidecarRevisionStamp> SidecarRevisionStamps = new Dictionary<Document, ProjectSidecarRevisionStamp>();
        private static readonly Dictionary<Document, string> UnsavedProjectKeys = new Dictionary<Document, string>();
        private static readonly Dictionary<Document, string> UnsavedProjectPaths = new Dictionary<Document, string>();
        private static readonly QsdbProjectStore Store = new QsdbProjectStore();

        public static ProjectState GetOrCreate(Document document)
        {
            return GetOrCreate(document, false);
        }

        private static ProjectState GetOrCreate(Document document, bool allowPathTransition)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (Projects.TryGetValue(document, out var existing))
            {
                EnsureUsable(existing);
                EnsureBackingStoreUnchanged(document, existing, allowPathTransition, "QS3D project bind");
                SyncDrawingIdentity(existing, document);
                return existing;
            }

            var path = GetProjectPath(document);
            var before = ProjectSidecarRevisionStamp.Capture(path);
            ProjectState project;
            if (before.HasAnyFile)
            {
                project = LoadExistingProjectOrThrow(path);
            }
            else project = CreateDefault(document);

            SyncDrawingIdentity(project, document);
            var persistenceStamp = new ProjectPersistenceStamp(project);
            var after = ProjectSidecarRevisionStamp.Capture(path);
            EnsureStableCapture(before, after, "QS3D project backing store changed while it was being bound. Retry the operation.");
            Projects[document] = project;
            PersistenceStamps[document] = persistenceStamp;
            SidecarRevisionStamps[document] = after;
            return project;
        }

        public static bool TryGetReadOnly(Document document, out ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (Projects.TryGetValue(document, out var existing))
            {
                EnsureUsable(existing);
                EnsureBackingStoreUnchanged(document, existing, false, "QS3D read-only project access");
                ValidateDrawingIdentityReadOnly(existing, document);
                project = existing;
                return true;
            }

            // Match the standard Try-pattern contract: callers must only consume
            // the non-null out value when this method returns true.
            project = null!;
            if (!TryGetExistingProjectPath(document, out var path)) return false;
            var before = ProjectSidecarRevisionStamp.Capture(path);
            if (!before.HasAnyFile) return false;

            project = LoadExistingProjectOrThrow(path);
            ValidateDrawingIdentityReadOnly(project, document);
            var after = ProjectSidecarRevisionStamp.Capture(path);
            EnsureStableCapture(before, after, "QS3D project backing store changed while it was being read. Retry the operation.");
            return true;
        }

        public static string Save(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var project = ExistingProjectMutationContext.Require(document, "Save Project");
            var path = GetProjectPath(document);
            if ((File.Exists(path) || File.Exists(path + ".bak")) && project.Metadata.TryGetValue(RecoveryRequiredKey, out var blocked) && string.Equals(blocked, "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QS3D project load failed and the existing .qsdb will not be overwritten. Recover or move the damaged project file first.");

            var recoveryMetadata = CaptureRecoveryMetadata(project);
            var recoveredFromBackup = recoveryMetadata.TryGetValue("QS3D.RecoveredFromBackup", out var recovered) &&
                                      string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase);
            ClearRecoveryMetadata(project);
            try
            {
                using (ProjectFileLock.Acquire(path))
                {
                    // Freshness and the conditional commit must share the same
                    // project lock. Otherwise two sessions can both validate an
                    // old baseline and the later writer can silently overwrite
                    // the earlier writer after waiting for the lock.
                    EnsureBackingStoreUnchanged(document, project, true, "QS3D save");
                    if (!SidecarRevisionStamps.TryGetValue(document, out var baseline))
                        throw new InvalidOperationException("QS3D save cannot verify its sidecar baseline. Reload and retry.");

                    var pathTransition = !baseline.IsForPath(path);
                    SyncDrawingIdentity(project, document);
                    if (pathTransition)
                        Store.SaveNew(project, path);
                    else if (recoveredFromBackup)
                        Store.SavePreservingValidatedBackup(project, path);
                    else
                        Store.Save(project, path);

                    // Record exactly the generation this session committed while
                    // the same lock still excludes another QS3D writer.
                    SidecarRevisionStamps[document] = ProjectSidecarRevisionStamp.Capture(path);
                    GetPersistenceStamp(document, project).MarkSaved(project);
                }
                CleanupObsoleteUnsavedProject(document, path);
                return path;
            }
            catch
            {
                RestoreMetadata(project, recoveryMetadata);
                throw;
            }
        }

        public static ProjectState Reload(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var path = GetProjectPath(document);
            var before = ProjectSidecarRevisionStamp.Capture(path);
            if (!before.HasAnyFile) throw new FileNotFoundException("QS3D project file was not found.", path);
            var project = LoadExistingProjectOrThrow(path);
            SyncDrawingIdentity(project, document);
            var persistenceStamp = new ProjectPersistenceStamp(project);
            var after = ProjectSidecarRevisionStamp.Capture(path);
            EnsureStableCapture(before, after, "QS3D project backing store changed while it was being reloaded. Retry the operation.");
            SourceReconcileUndoCoordinator.Forget(document);
            CurtainWallUndoCoordinator.Forget(document);
            Projects[document] = project;
            PersistenceStamps[document] = persistenceStamp;
            SidecarRevisionStamps[document] = after;
            return project;
        }

        public static bool HasPendingChanges(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!Projects.TryGetValue(document, out var project)) return false;
            EnsureBackingStoreUnchanged(document, project, true, "QS3D pending-state inspection");
            ValidateDrawingIdentityReadOnly(project, document);
            if (!SameDrawingName(project.DrawingPath, document.Name)) return true;
            return GetPersistenceStamp(document, project).RequiresSave(project);
        }

        public static bool TrySavePending(Document document, out string path)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            path = string.Empty;
            if (!Projects.TryGetValue(document, out var project)) return false;
            EnsureBackingStoreUnchanged(document, project, true, "QS3D pending save");
            SyncDrawingIdentity(project, document);
            if (!GetPersistenceStamp(document, project).RequiresSave(project)) return false;
            path = Save(document);
            return true;
        }

        public static string SaveRecoveryCopy(Document document, Exception saveFailure)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (saveFailure == null) throw new ArgumentNullException(nameof(saveFailure));
            if (!Projects.TryGetValue(document, out var project))
                throw new InvalidOperationException("No in-memory QS3D project is available for recovery.");

            var recovery = ProjectStateSnapshot.CreateDetachedCopy(project);
            recovery.Metadata["QS3D.RecoveryReason"] = SafeRecoveryText(saveFailure.GetType().Name + ": " + saveFailure.Message, 2048);
            recovery.Metadata["QS3D.RecoveryCanonicalPath"] = GetProjectPath(document);
            recovery.Metadata["QS3D.RecoveryCreatedUtc"] = DateTime.UtcNow.ToString("O");
            recovery.Touch();

            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Recovery");
            var drawingStem = LimitFileStem(SafeFileStem(Path.GetFileNameWithoutExtension(document.Name)), 80);
            var projectStem = LimitFileStem(SafeFileStem(project.ProjectId), 80);
            var recoveryPath = Path.Combine(directory, drawingStem + "-" + projectStem + ".recovery.qsdb");
            using (ProjectFileLock.Acquire(recoveryPath)) Store.Save(recovery, recoveryPath);
            return recoveryPath;
        }

        public static bool TryGetCached(Document document, out ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (Projects.TryGetValue(document, out project))
            {
                EnsureUsable(project);
                return true;
            }

            project = null!;
            return false;
        }

        public static void RequireBackingStoreUnchanged(Document document, ProjectState project, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(operation)) operation = "QS3D mutation";
            if (!Projects.TryGetValue(document, out var cached) || !ReferenceEquals(cached, project))
                throw new InvalidOperationException(operation + " requires the canonical cached QS3D project.");
            EnsureBackingStoreUnchanged(document, project, false, operation);
        }

        public static void Forget(Document document)
        {
            if (document == null) return;
            SourceReconcileUndoCoordinator.Forget(document);
            CurtainWallUndoCoordinator.Forget(document);
            Projects.Remove(document);
            PersistenceStamps.Remove(document);
            SidecarRevisionStamps.Remove(document);
            UnsavedProjectKeys.Remove(document);
            UnsavedProjectPaths.Remove(document);
        }

        public static void ForgetByName(string? drawingName)
        {
            if (string.IsNullOrWhiteSpace(drawingName)) return;
            foreach (var document in Projects.Keys.Where(x => SameDrawingName(x.Name, drawingName)).ToArray())
            {
                SourceReconcileUndoCoordinator.Forget(document);
                CurtainWallUndoCoordinator.Forget(document);
                Projects.Remove(document);
                PersistenceStamps.Remove(document);
                SidecarRevisionStamps.Remove(document);
                UnsavedProjectKeys.Remove(document);
                UnsavedProjectPaths.Remove(document);
            }
        }

        public static string GetProjectPath(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var drawing = document.Name;
            if (string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing))
            {
                if (UnsavedProjectPaths.TryGetValue(document, out var existingPath)) return existingPath;

                var stem = SafeFileStem(string.IsNullOrWhiteSpace(drawing) ? "Untitled" : Path.GetFileNameWithoutExtension(drawing));
                if (!UnsavedProjectKeys.TryGetValue(document, out var key))
                {
                    key = Guid.NewGuid().ToString("N");
                    UnsavedProjectKeys[document] = key;
                }
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Projects", stem + "-" + key + ".qsdb");
                UnsavedProjectPaths[document] = path;
                return path;
            }
            return Path.ChangeExtension(drawing, ".qsdb");
        }

        private static void CleanupObsoleteUnsavedProject(Document document, string currentPath)
        {
            if (!UnsavedProjectPaths.TryGetValue(document, out var obsoletePath) || string.IsNullOrWhiteSpace(obsoletePath)) return;

            try
            {
                if (SameDrawingName(obsoletePath, currentPath)) return;
                if (File.Exists(obsoletePath)) File.Delete(obsoletePath);
                if (File.Exists(obsoletePath + ".bak")) File.Delete(obsoletePath + ".bak");
                if (!File.Exists(obsoletePath) && !File.Exists(obsoletePath + ".bak"))
                {
                    UnsavedProjectPaths.Remove(document);
                    UnsavedProjectKeys.Remove(document);
                }
            }
            catch (Exception)
            {
                // The named sidecar has already committed. Keep the recovery path so a later explicit save can retry cleanup.
            }
        }

        private static bool TryGetExistingProjectPath(Document document, out string path)
        {
            path = string.Empty;
            var drawing = document.Name;
            if (string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing)) return false;
            try
            {
                path = Path.ChangeExtension(drawing, ".qsdb");
                return !string.IsNullOrWhiteSpace(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                path = string.Empty;
                return false;
            }
        }

        private static ProjectState LoadProject(string path)
        {
            var loaded = Store.LoadWithBackupFallback(path);
            var project = loaded.Project;
            if (loaded.RecoveredFromBackup)
            {
                project.Metadata["QS3D.RecoveredFromBackup"] = "true";
                project.Metadata["QS3D.RecoverySource"] = loaded.SourcePath;
                project.Metadata["QS3D.PrimaryLoadFailure"] = loaded.PrimaryFailureMessage;
            }
            return project;
        }

        private static ProjectState LoadExistingProjectOrThrow(string path)
        {
            try
            {
                var project = LoadProject(path);
                EnsureUsable(project);
                return project;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    "The existing QS3D sidecar could not be loaded. No replacement project was created and the sidecar was left unchanged.",
                    ex);
            }
        }

        private static void EnsureUsable(ProjectState project)
        {
            if (project.Metadata.TryGetValue(RecoveryRequiredKey, out var blocked) &&
                string.Equals(blocked, "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QS3D project is in read-only recovery mode.");
        }

        private static void EnsureBackingStoreUnchanged(
            Document document,
            ProjectState project,
            bool allowPathTransition,
            string operation)
        {
            if (!Projects.TryGetValue(document, out var cached) || !ReferenceEquals(cached, project) ||
                !SidecarRevisionStamps.TryGetValue(document, out var baseline))
                throw new InvalidOperationException(operation + " cannot verify the canonical QS3D backing store. Reload and retry.");

            bool unchanged;
            try { unchanged = baseline.MatchesCurrent(); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                throw new InvalidOperationException(operation + " cannot read a stable QS3D .qsdb/.bak backing store. Reload and retry.", ex);
            }
            if (!unchanged)
                throw new InvalidOperationException(operation + " stopped because the QS3D .qsdb/.bak backing store changed outside this session. Reload and review again.");

            var currentPath = GetProjectPath(document);
            if (baseline.IsForPath(currentPath)) return;
            if (!allowPathTransition)
                throw new InvalidOperationException(operation + " stopped because the DWG sidecar path changed. Save/reload the drawing and retry.");

            ProjectSidecarRevisionStamp target;
            try { target = ProjectSidecarRevisionStamp.Capture(currentPath); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                throw new InvalidOperationException(operation + " cannot verify the destination QS3D backing store. Reload and retry.", ex);
            }
            if (target.HasAnyFile)
                throw new InvalidOperationException(operation + " refused to overwrite an existing QS3D sidecar at the new DWG path. Reconcile it explicitly first.");
        }

        private static void EnsureStableCapture(
            ProjectSidecarRevisionStamp before,
            ProjectSidecarRevisionStamp after,
            string message)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            if (!before.Equals(after)) throw new InvalidOperationException(message);
        }

        private static void SyncDrawingIdentity(ProjectState project, Document document)
        {
            var drawing = document.Name ?? string.Empty;
            var fingerprint = GetDrawingFingerprint(document, drawing);
            var storedPath = project.DrawingPath ?? string.Empty;
            var storedFingerprint = project.DrawingFingerprint ?? string.Empty;

            if (string.IsNullOrWhiteSpace(storedFingerprint) || IsLegacyPathFingerprint(storedPath, storedFingerprint, drawing))
            {
                AdoptDrawingIdentity(project, drawing, fingerprint, storedFingerprint);
                return;
            }

            if (!string.Equals(storedFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                ThrowDrawingIdentityMismatch(storedFingerprint, fingerprint);

            if (SameDrawingName(storedPath, drawing)) return;
            project.DrawingPath = drawing;
        }

        private static void ValidateDrawingIdentityReadOnly(ProjectState project, Document document)
        {
            var drawing = document.Name ?? string.Empty;
            var fingerprint = GetDrawingFingerprint(document, drawing);
            var storedPath = project.DrawingPath ?? string.Empty;
            var storedFingerprint = project.DrawingFingerprint ?? string.Empty;

            if (string.IsNullOrWhiteSpace(storedFingerprint))
            {
                if (!string.IsNullOrWhiteSpace(storedPath) && !SameDrawingName(storedPath, drawing))
                    ThrowDrawingIdentityMismatch("<legacy-empty>", fingerprint);
                return;
            }

            if (string.Equals(storedPath, storedFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                if (SameDrawingName(storedPath, drawing)) return;
                ThrowDrawingIdentityMismatch(storedFingerprint, fingerprint);
            }

            if (!string.Equals(storedFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                ThrowDrawingIdentityMismatch(storedFingerprint, fingerprint);
        }

        private static void ThrowDrawingIdentityMismatch(string storedFingerprint, string currentFingerprint)
        {
            throw new InvalidOperationException(
                "QS3D drawing identity mismatch. The .qsdb belongs to a different DWG fingerprint. " +
                "Move/recover the matching sidecar or explicitly reconcile the project before using CAD Handles. " +
                "Stored=" + storedFingerprint + ", current=" + currentFingerprint + ".");
        }

        private static string GetDrawingFingerprint(Document document, string drawing)
        {
            try
            {
                // Avoid assuming a specific managed wrapper type for FINGERPRINTGUID across TD_Mgd builds.
                var fingerprint = Convert.ToString(document.Database.FingerprintGuid)?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(fingerprint)) return fingerprint;
            }
            catch (Exception)
            {
                // Some host/database states may not expose a fingerprint yet. The normalized path fallback
                // remains deterministic and, unlike the old raw-name assignment, detects a copied sidecar.
            }

            try { return "path:" + Path.GetFullPath(drawing).Trim().ToUpperInvariant(); }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return "path:" + (drawing ?? string.Empty).Trim().ToUpperInvariant();
            }
        }

        private static bool IsLegacyPathFingerprint(string storedPath, string storedFingerprint, string drawing)
        {
            return string.Equals(storedPath, storedFingerprint, StringComparison.OrdinalIgnoreCase) &&
                   SameDrawingName(storedPath, drawing);
        }

        private static void AdoptDrawingIdentity(ProjectState project, string drawing, string fingerprint, string previousFingerprint)
        {
            var elements = project.Elements.ToList();
            if (elements.Any(x => x == null))
                throw new InvalidOperationException("Project contains a null element entry.");

            var pathChanged = !string.Equals(project.DrawingPath, drawing, StringComparison.Ordinal);
            var fingerprintChanged = !string.Equals(project.DrawingFingerprint, fingerprint, StringComparison.Ordinal);
            var scalarChanges = (pathChanged ? 1L : 0L) + (fingerprintChanged ? 1L : 0L);
            _ = checked(project.ChangeVersion + scalarChanges);

            project.DrawingPath = drawing;
            project.DrawingFingerprint = fingerprint;
            foreach (var element in elements)
            {
                if (string.IsNullOrWhiteSpace(element.DrawingFingerprint) ||
                    string.Equals(element.DrawingFingerprint, previousFingerprint, StringComparison.OrdinalIgnoreCase))
                    element.DrawingFingerprint = fingerprint;
            }
        }

        private static bool SameDrawingName(string? left, string? right)
        {
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) { return false; }
        }

        private static string SafeFileStem(string? value)
        {
            var stem = string.IsNullOrWhiteSpace(value) ? "Untitled" : value!.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars()) stem = stem.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(stem) ? "Untitled" : stem;
        }

        private static string LimitFileStem(string value, int maxLength) =>
            value.Length <= maxLength ? value : value.Substring(0, maxLength);

        private static string SafeRecoveryText(string? value, int maxLength)
        {
            var source = value ?? string.Empty;
            var limit = Math.Min(source.Length, maxLength);
            var chars = new char[limit];
            var count = 0;
            for (var index = 0; index < limit; index++)
            {
                var current = source[index];
                if (char.IsHighSurrogate(current) && index + 1 < limit && char.IsLowSurrogate(source[index + 1]))
                {
                    chars[count++] = current;
                    chars[count++] = source[++index];
                    continue;
                }
                if (char.IsSurrogate(current) || (current < ' ' && current != '\t' && current != '\n' && current != '\r'))
                    current = '\uFFFD';
                chars[count++] = current;
            }
            return new string(chars, 0, count);
        }

        private static Dictionary<string, string> CaptureRecoveryMetadata(ProjectState project)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in new[] { "QS3D.RecoveredFromBackup", "QS3D.RecoverySource", "QS3D.PrimaryLoadFailure" })
                if (project.Metadata.TryGetValue(key, out var value)) result[key] = value;
            return result;
        }

        private static void ClearRecoveryMetadata(ProjectState project)
        {
            project.Metadata.Remove("QS3D.RecoveredFromBackup");
            project.Metadata.Remove("QS3D.RecoverySource");
            project.Metadata.Remove("QS3D.PrimaryLoadFailure");
        }

        private static void RestoreMetadata(ProjectState project, IDictionary<string, string> metadata)
        {
            foreach (var item in metadata) project.Metadata[item.Key] = item.Value;
        }

        private static ProjectPersistenceStamp GetPersistenceStamp(Document document, ProjectState project)
        {
            if (PersistenceStamps.TryGetValue(document, out var stamp)) return stamp;
            stamp = new ProjectPersistenceStamp(project);
            PersistenceStamps[document] = stamp;
            return stamp;
        }

        private static ProjectState CreateDefault(Document document)
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), Path.GetFileNameWithoutExtension(document.Name));
            project.DrawingPath = document.Name ?? string.Empty;
            project.DrawingFingerprint = GetDrawingFingerprint(document, project.DrawingPath);
            project.Zones.Add(new ZoneDefinition("zone-1", "Vùng-1"));
            project.Floors.Add(new FloorDefinition("floor-0", "Nền 0.00", 0d));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-0";
            var room = new ProjectFamily("room-default", "Phòng-1", ElementCategory.Room); room.Properties["HeightM"] = "3.6"; project.Families.Add(room);
            var wall = new ProjectFamily("wall-200", "Tường Gạch 200", ElementCategory.ArchitecturalWall); wall.Properties["ThicknessM"] = "0.2"; wall.Properties["HeightM"] = "3.6"; wall.Properties["Material"] = "Gạch"; project.Families.Add(wall);
            var opening = new ProjectFamily("opening-default", "Lỗ Mở Vách", ElementCategory.WallOpening); opening.Properties["HeightM"] = "2.2"; project.Families.Add(opening);
            var door = new ProjectFamily("door-default", "Cửa Đi", ElementCategory.Door); door.Properties["HeightM"] = "2.2"; project.Families.Add(door);
            return project;
        }
    }
}
