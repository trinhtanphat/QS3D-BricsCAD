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
        private static readonly Dictionary<Document, string> UnsavedProjectKeys = new Dictionary<Document, string>();
        private static readonly QsdbProjectStore Store = new QsdbProjectStore();

        public static ProjectState GetOrCreate(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (Projects.TryGetValue(document, out var existing))
            {
                SyncDrawingIdentity(existing, document);
                return existing;
            }

            var path = GetProjectPath(document);
            ProjectState project;
            if (File.Exists(path) || File.Exists(path + ".bak"))
            {
                try { project = LoadProject(path); }
                catch (Exception ex)
                {
                    project = CreateDefault(document);
                    project.Metadata[RecoveryRequiredKey] = "true";
                    project.Metadata["QS3D.LoadWarning"] = ex.GetType().Name + ": " + ex.Message;
                    project.Metadata["QS3D.FailedProjectPath"] = path;
                }
            }
            else project = CreateDefault(document);

            SyncDrawingIdentity(project, document);
            Projects[document] = project;
            return project;
        }

        public static bool TryGetReadOnly(Document document, out ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (Projects.TryGetValue(document, out var existing))
            {
                ValidateDrawingIdentityReadOnly(existing, document);
                project = existing;
                return true;
            }

            // Match the standard Try-pattern contract: callers must only consume
            // the non-null out value when this method returns true.
            project = null!;
            if (!TryGetExistingProjectPath(document, out var path)) return false;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return false;

            project = LoadProject(path);
            ValidateDrawingIdentityReadOnly(project, document);
            return true;
        }

        public static string Save(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var project = GetOrCreate(document);
            SyncDrawingIdentity(project, document);
            var path = GetProjectPath(document);
            if ((File.Exists(path) || File.Exists(path + ".bak")) && project.Metadata.TryGetValue(RecoveryRequiredKey, out var blocked) && string.Equals(blocked, "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QS3D project load failed and the existing .qsdb will not be overwritten. Recover or move the damaged project file first.");

            var recoveryMetadata = CaptureRecoveryMetadata(project);
            ClearRecoveryMetadata(project);
            try
            {
                using (ProjectFileLock.Acquire(path)) Store.Save(project, path);
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
            if (!File.Exists(path) && !File.Exists(path + ".bak")) throw new FileNotFoundException("QS3D project file was not found.", path);
            var project = LoadProject(path);
            SyncDrawingIdentity(project, document);
            Projects[document] = project;
            return project;
        }

        public static void Forget(Document document)
        {
            if (document == null) return;
            Projects.Remove(document);
            UnsavedProjectKeys.Remove(document);
        }

        public static void ForgetByName(string? drawingName)
        {
            if (string.IsNullOrWhiteSpace(drawingName)) return;
            foreach (var document in Projects.Keys.Where(x => SameDrawingName(x.Name, drawingName)).ToArray())
            {
                Projects.Remove(document);
                UnsavedProjectKeys.Remove(document);
            }
        }

        public static string GetProjectPath(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var drawing = document.Name;
            if (string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing))
            {
                var stem = SafeFileStem(string.IsNullOrWhiteSpace(drawing) ? "Untitled" : Path.GetFileNameWithoutExtension(drawing));
                if (!UnsavedProjectKeys.TryGetValue(document, out var key))
                {
                    key = Guid.NewGuid().ToString("N");
                    UnsavedProjectKeys[document] = key;
                }
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Projects", stem + "-" + key + ".qsdb");
            }
            return Path.ChangeExtension(drawing, ".qsdb");
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
            project.Touch();
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
            project.DrawingPath = drawing;
            project.DrawingFingerprint = fingerprint;
            foreach (var element in project.Elements)
            {
                if (string.IsNullOrWhiteSpace(element.DrawingFingerprint) ||
                    string.Equals(element.DrawingFingerprint, previousFingerprint, StringComparison.OrdinalIgnoreCase))
                    element.DrawingFingerprint = fingerprint;
            }
            project.Touch();
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
