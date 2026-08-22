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
<<<<<<< origin/main
        private static readonly Dictionary<Document, ProjectState> Projects = new Dictionary<Document, ProjectState>();
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
=======
        private static readonly Dictionary<string, ProjectState> Projects = new Dictionary<string, ProjectState>(StringComparer.OrdinalIgnoreCase); private static readonly QsdbProjectStore Store = new QsdbProjectStore();
        public static ProjectState GetOrCreate(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document)); var key = GetKey(document); if (Projects.TryGetValue(key, out var existing)) return existing; var path = GetProjectPath(document); ProjectState project;
            if (File.Exists(path)) { try { project = LoadProject(path); } catch (Exception ex) { project = CreateDefault(document); project.Metadata[RecoveryRequiredKey] = "true"; project.Metadata["QS3D.LoadWarning"] = ex.GetType().Name + ": " + ex.Message; project.Metadata["QS3D.FailedProjectPath"] = path; } } else project = CreateDefault(document); Projects[key] = project; return project;
>>>>>>> origin/agent/full-domain-20260810
        }
        public static string Save(Document document)
        {
<<<<<<< origin/main
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
            if (document != null) Projects.Remove(document);
        }

        public static void ForgetByName(string? drawingName)
        {
            if (string.IsNullOrWhiteSpace(drawingName)) return;
            foreach (var document in Projects.Keys.Where(x => SameDrawingName(x.Name, drawingName)).ToArray()) Projects.Remove(document);
        }

        public static string GetProjectPath(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var drawing = document.Name;
            if (string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing))
            {
                var stem = SafeFileStem(string.IsNullOrWhiteSpace(drawing) ? "Untitled" : Path.GetFileNameWithoutExtension(drawing));
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Projects", stem + ".qsdb");
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

        private static void SyncDrawingIdentity(ProjectState project, Document document)
        {
            var drawing = document.Name ?? string.Empty;
            if (string.Equals(project.DrawingPath, drawing, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(project.DrawingFingerprint, drawing, StringComparison.OrdinalIgnoreCase)) return;
            project.DrawingPath = drawing;
            project.DrawingFingerprint = drawing;
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
            var stem = string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim();
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
            project.DrawingFingerprint = document.Name ?? string.Empty;
            project.Zones.Add(new ZoneDefinition("zone-1", "Vùng-1"));
            project.Floors.Add(new FloorDefinition("floor-0", "Nền 0.00", 0d));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-0";
            var room = new ProjectFamily("room-default", "Phòng-1", ElementCategory.Room); room.Properties["HeightM"] = "3.6"; project.Families.Add(room);
            var wall = new ProjectFamily("wall-200", "Tường Gạch 200", ElementCategory.ArchitecturalWall); wall.Properties["ThicknessM"] = "0.2"; wall.Properties["HeightM"] = "3.6"; wall.Properties["Material"] = "Gạch"; project.Families.Add(wall);
            var opening = new ProjectFamily("opening-default", "Lỗ Mở Vách", ElementCategory.WallOpening); opening.Properties["HeightM"] = "2.2"; project.Families.Add(opening);
            var door = new ProjectFamily("door-default", "Cửa Đi", ElementCategory.Door); door.Properties["HeightM"] = "2.2"; project.Families.Add(door);
            return project;
=======
            var project = GetOrCreate(document); var path = GetProjectPath(document); if (File.Exists(path) && project.Metadata.TryGetValue(RecoveryRequiredKey, out var blocked) && string.Equals(blocked, "true", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("QS3D project load failed and the existing .qsdb will not be overwritten. Recover or move the damaged project file first.");
            var recoveryMetadata = CaptureRecoveryMetadata(project); ClearRecoveryMetadata(project); try { using (ProjectFileLock.Acquire(path)) Store.Save(project, path); return path; } catch { RestoreMetadata(project, recoveryMetadata); throw; }
        }
        public static ProjectState Reload(Document document) { var path = GetProjectPath(document); if (!File.Exists(path)) throw new FileNotFoundException("QS3D project file was not found.", path); var project = LoadProject(path); Projects[GetKey(document)] = project; return project; }
        public static void Forget(Document document) { if (document != null) Projects.Remove(GetKey(document)); }
        public static void ForgetByName(string? drawingName) { if (string.IsNullOrWhiteSpace(drawingName)) return; Projects.Remove(drawingName); Projects.Remove(Path.GetFullPath(drawingName)); }
        public static string GetProjectPath(Document document)
        {
            var drawing = document.Name; if (string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing)) { var safe = string.IsNullOrWhiteSpace(drawing) ? "Untitled" : Path.GetFileNameWithoutExtension(drawing); return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Projects", safe + ".qsdb"); } return Path.ChangeExtension(drawing, ".qsdb");
        }
        private static ProjectState LoadProject(string path) { var loaded = Store.LoadWithBackupFallback(path); var project = loaded.Project; if (loaded.RecoveredFromBackup) { project.Metadata["QS3D.RecoveredFromBackup"] = "true"; project.Metadata["QS3D.RecoverySource"] = loaded.SourcePath; project.Metadata["QS3D.PrimaryLoadFailure"] = loaded.PrimaryFailureMessage; } return project; }
        private static Dictionary<string, string> CaptureRecoveryMetadata(ProjectState project) { var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); foreach (var key in new[] { "QS3D.RecoveredFromBackup", "QS3D.RecoverySource", "QS3D.PrimaryLoadFailure" }) if (project.Metadata.TryGetValue(key, out var value)) result[key] = value; return result; }
        private static void ClearRecoveryMetadata(ProjectState project) { project.Metadata.Remove("QS3D.RecoveredFromBackup"); project.Metadata.Remove("QS3D.RecoverySource"); project.Metadata.Remove("QS3D.PrimaryLoadFailure"); }
        private static void RestoreMetadata(ProjectState project, IDictionary<string, string> metadata) { foreach (var item in metadata) project.Metadata[item.Key] = item.Value; }
        private static string GetKey(Document document) => string.IsNullOrWhiteSpace(document.Name) ? document.GetHashCode().ToString() : document.Name;
        private static ProjectState CreateDefault(Document document)
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), Path.GetFileNameWithoutExtension(document.Name)); project.DrawingPath = document.Name ?? string.Empty; project.DrawingFingerprint = document.Name ?? string.Empty; project.Zones.Add(new ZoneDefinition("zone-1", "Vùng-1")); project.Floors.Add(new FloorDefinition("floor-0", "Nền 0.00", 0d)); project.ActiveZoneId = "zone-1"; project.ActiveFloorId = "floor-0";
            AddFamily(project, "room-default", "Phòng-1", ElementCategory.Room, "HeightM", "3.6"); AddFamily(project, "wall-200", "Tường Gạch 200", ElementCategory.ArchitecturalWall, "ThicknessM", "0.2", "HeightM", "3.6", "Material", "Gạch"); AddFamily(project, "opening-default", "Lỗ Mở Vách", ElementCategory.WallOpening, "HeightM", "2.2"); AddFamily(project, "door-default", "Cửa Đi", ElementCategory.Door, "HeightM", "2.2");
            AddFamily(project, "beam-200x400", "Dầm 200x400", ElementCategory.Beam, "WidthM", "0.2", "HeightM", "0.4", "Material", "Bê tông"); AddFamily(project, "slab-120", "Sàn BTCT 120", ElementCategory.Slab, "ThicknessM", "0.12", "Material", "Bê tông"); AddFamily(project, "column-300", "Cột 300x300", ElementCategory.Column, "WidthM", "0.3", "DepthM", "0.3", "HeightM", "3.6", "Material", "Bê tông"); AddFamily(project, "struct-wall-200", "Vách BTCT 200", ElementCategory.StructuralWall, "ThicknessM", "0.2", "HeightM", "3.6", "Material", "Bê tông"); AddFamily(project, "foundation-default", "Móng BTCT", ElementCategory.Foundation, "HeightM", "0.5", "Material", "Bê tông"); AddFamily(project, "earthwork-default", "Đào đất", ElementCategory.Earthwork, "DepthM", "0.5", "SwellFactor", "0.15"); AddFamily(project, "rebar-default", "Cốt thép CB400-V", ElementCategory.Rebar, "Notation", "4D16", "Grade", "CB400-V", "Shape", "Straight"); return project;
>>>>>>> origin/agent/full-domain-20260810
        }
        private static void AddFamily(ProjectState project, string id, string name, ElementCategory category, params string[] pairs) { var family = new ProjectFamily(id, name, category); for (var i = 0; i + 1 < pairs.Length; i += 2) family.Properties[pairs[i]] = pairs[i + 1]; project.Families.Add(family); }
    }
}
