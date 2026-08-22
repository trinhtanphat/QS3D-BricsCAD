using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomBoundaryCommands
    {
        [CommandMethod("QS3DROOMAUTO", CommandFlags.UsePickSet)]
        public void DiscoverRooms()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var arcSagitta = MetadataNumber(project, "RoomBoundaryArcSagittaM", 0.002d, minimumExclusive: 0d);
                var segments = RoomBoundarySegmentReader.ReadCurrentSelection(document, arcSagitta);
                if (segments.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DROOMAUTO: chọn LINE hoặc POLYLINE tạo biên phòng.");
                    return;
                }

                var tolerance = MetadataNumber(project, "RoomBoundaryToleranceM", 0.005d, minimumExclusive: 0d);
                var minimumArea = MetadataNumber(project, "RoomBoundaryMinimumAreaM2", 0.5d, minimumExclusive: -1d);
                var boundaries = new RoomBoundaryEngine().Discover(segments, tolerance, minimumArea);
                if (boundaries.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DROOMAUTO: chưa phát hiện face kín hợp lệ trong selection.");
                    return;
                }

                var family = ResolveRoomFamily(project);
                var audit = AuditTrail.ForProject(project);
                var created = 0;
                var updated = 0;
                foreach (var boundary in boundaries)
                {
                    var id = "ROOMAUTO-" + StableToken(boundary.Key);
                    var element = project.FindElement(id);
                    var isNew = element == null;
                    if (element == null)
                    {
                        element = new ProjectElement(id, ElementCategory.Room, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                        project.Elements.Add(element);
                        created++;
                    }
                    else
                    {
                        if (element.Category != ElementCategory.Room) throw new InvalidOperationException("Boundary id collision with non-room semantic element: " + id);
                        updated++;
                    }

                    element.Category = ElementCategory.Room;
                    element.FamilyId = family.Id;
                    element.FloorId = project.ActiveFloorId;
                    element.ZoneId = project.ActiveZoneId;
                    element.DrawingFingerprint = project.DrawingFingerprint;
                    element.Properties["BoundaryMode"] = "AutoNetwork";
                    element.Properties["BoundaryKey"] = boundary.Key;
                    element.Properties["BoundarySourceHandles"] = string.Join(";", boundary.SourceIds);
                    element.Properties["BoundaryVertexCount"] = boundary.Vertices.Count.ToString(CultureInfo.InvariantCulture);
                    element.Properties["BoundaryArcSagittaM"] = arcSagitta.ToString("R", CultureInfo.InvariantCulture);
                    element.Properties["AreaM2"] = boundary.Area.ToString("R", CultureInfo.InvariantCulture);
                    element.Properties["PerimeterM"] = boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture);
                    foreach (var property in family.Properties)
                        if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
                    element.MarkDirty(ElementDirtyFlags.All);
                    audit.Record(isNew ? "RoomBoundaryCreate" : "RoomBoundaryUpdate", element.Id,
                        "area=" + boundary.Area.ToString("R", CultureInfo.InvariantCulture) +
                        ";perimeter=" + boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture) +
                        ";sources=" + boundary.SourceIds.Count.ToString(CultureInfo.InvariantCulture) +
                        ";arcSagitta=" + arcSagitta.ToString("R", CultureInfo.InvariantCulture));
                }

                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                project.Touch();
                PaletteCoordinator.RefreshProject();
                var message = "Room Auto: " + boundaries.Count + " face • mới " + created + " • cập nhật " + updated + " • regenerate " + regenerated + ".";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DROOMAUTO error: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DROOMAUTO lỗi: " + ex.Message);
            }
        }

        private static ProjectFamily ResolveRoomFamily(ProjectState project)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == ElementCategory.Room) return active;
            }
            var existing = project.Families.FirstOrDefault(x => x.Category == ElementCategory.Room);
            if (existing != null) return existing;
            var created = new ProjectFamily("room-auto-boundary", "Phòng Auto Boundary", ElementCategory.Room);
            created.Properties["HeightM"] = "3.6";
            project.Families.Add(created);
            return created;
        }

        private static double MetadataNumber(ProjectState project, string key, double fallback, double minimumExclusive)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= minimumExclusive)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static string StableToken(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty);
            }
        }
    }
}
