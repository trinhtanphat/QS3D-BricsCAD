using System;
using System.Collections.Generic;
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25
{
    internal static class ProjectContextCoordinator
    {
        private const string RecoveryRequiredKey = "QS3D.ReadOnlyRecoveryRequired";
        private static readonly Dictionary<string, ProjectState> Projects = new Dictionary<string, ProjectState>(StringComparer.OrdinalIgnoreCase);
        private static readonly QsdbProjectStore Store = new QsdbProjectStore();

        public static ProjectState GetOrCreate(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var key = GetKey(document); if (Projects.TryGetValue(key, out var existing)) return existing;
            var path = GetProjectPath(document); ProjectState project;
            if (File.Exists(path))
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
            Projects[key] = project; return project;
        }

        public static string Save(Document document)
        {
            var project = GetOrCreate(document); var path = GetProjectPath(document);
            if (File.Exists(path) && project.Metadata.TryGetValue(RecoveryRequiredKey, out var blocked) && string.Equals(blocked, "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QS3D project load failed and the existing .qsdb will not be overwritten. Recover or move the damaged project file first.");
            using (ProjectFileLock.Acquire(path)) Store.Save(project, path);
            project.Metadata.Remove("QS3D.RecoveredFromBackup");
            project.Metadata.Remove("QS3D.RecoverySource");
            project.Metadata.Remove("QS3D.PrimaryLoadFailure");
            return path;
        }

        public static ProjectState Reload(Document document)
        {
            var path = GetProjectPath(document); if (!File.Exists(path)) throw new FileNotFoundException("QS3D project file was not found.", path);
            var project = LoadProject(path); Projects[GetKey(document)] = project; return project;
        }

        public static void Forget(Document document) { if (document != null) Projects.Remove(GetKey(document)); }
        public static void ForgetByName(string? drawingName)
        {
            if (string.IsNullOrWhiteSpace(drawingName)) return;
            Projects.Remove(drawingName);
            var full = Path.GetFullPath(drawingName);
            Projects.Remove(full);
        }

        public static string GetProjectPath(Document document)
        {
            var drawing = document.Name;
            if (string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing))
            {
                var safe = string.IsNullOrWhiteSpace(drawing) ? "Untitled" : Path.GetFileNameWithoutExtension(drawing);
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Projects", safe + ".qsdb");
            }
            return Path.ChangeExtension(drawing, ".qsdb");
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

        private static string GetKey(Document document) => string.IsNullOrWhiteSpace(document.Name) ? document.GetHashCode().ToString() : document.Name;
        private static ProjectState CreateDefault(Document document)
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), Path.GetFileNameWithoutExtension(document.Name)); project.DrawingPath = document.Name ?? string.Empty; project.DrawingFingerprint = document.Name ?? string.Empty;
            project.Zones.Add(new ZoneDefinition("zone-1", "Vùng-1")); project.Floors.Add(new FloorDefinition("floor-0", "Nền 0.00", 0d)); project.ActiveZoneId = "zone-1"; project.ActiveFloorId = "floor-0";
            var room = new ProjectFamily("room-default", "Phòng-1", ElementCategory.Room); room.Properties["HeightM"] = "3.6"; project.Families.Add(room);
            var wall = new ProjectFamily("wall-200", "Tường Gạch 200", ElementCategory.ArchitecturalWall); wall.Properties["ThicknessM"] = "0.2"; wall.Properties["HeightM"] = "3.6"; wall.Properties["Material"] = "Gạch"; project.Families.Add(wall);
            var opening = new ProjectFamily("opening-default", "Lỗ Mở Vách", ElementCategory.WallOpening); opening.Properties["HeightM"] = "2.2"; project.Families.Add(opening);
            var door = new ProjectFamily("door-default", "Cửa Đi", ElementCategory.Door); door.Properties["HeightM"] = "2.2"; project.Families.Add(door); return project;
        }
    }
}
