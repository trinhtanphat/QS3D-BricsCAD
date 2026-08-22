using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
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

                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    var family = ResolveRoomFamily(project);
                    var audit = AuditTrail.ForProject(project);
                    var activeRoomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var selectedSourceHandles = new HashSet<string>(segments.Where(x => !string.IsNullOrWhiteSpace(x.SourceId)).Select(x => x.SourceId.Trim()), StringComparer.OrdinalIgnoreCase);
                    var created = 0;
                    var updated = 0;
                    var refreshedFinishes = 0;

                    foreach (var boundary in boundaries)
                    {
                        var sourceSignature = AutoRoomLifecycle.NormalizeSourceHandles(boundary.SourceIds);
                        var expectedId = "ROOMAUTO-" + StableToken(boundary.Key);
                        var element = project.FindElement(expectedId) ?? AutoRoomLifecycle.FindBySourceSignature(project, sourceSignature, project.ActiveFloorId, project.ActiveZoneId);
                        var isNew = element == null;
                        if (element == null)
                        {
                            element = new ProjectElement(expectedId, ElementCategory.Room, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                            project.Elements.Add(element);
                            created++;
                        }
                        else
                        {
                            if (element.Category != ElementCategory.Room || (!AutoRoomLifecycle.IsAutoRoom(element) && !string.Equals(element.Id, expectedId, StringComparison.OrdinalIgnoreCase)))
                                throw new InvalidOperationException("Boundary id/provenance collision with non-auto Room element: " + element.Id);
                            updated++;
                        }

                        element.Category = ElementCategory.Room;
                        element.FamilyId = family.Id;
                        element.FloorId = project.ActiveFloorId;
                        element.ZoneId = project.ActiveZoneId;
                        element.DrawingFingerprint = project.DrawingFingerprint;
                        element.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
                        AutoRoomLifecycle.MarkActive(element, sourceSignature);
                        element.Properties["BoundaryKey"] = boundary.Key;
                        element.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = sourceSignature;
                        element.Properties["BoundaryVertexCount"] = boundary.Vertices.Count.ToString(CultureInfo.InvariantCulture);
                        element.Properties["BoundaryArcSagittaM"] = arcSagitta.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["AreaM2"] = boundary.Area.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["PerimeterM"] = boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture);
                        foreach (var property in family.Properties)
                            if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
                        element.MarkDirty(ElementDirtyFlags.All);
                        activeRoomIds.Add(element.Id);
                        refreshedFinishes += SemanticCaptureService.SyncExistingRoomFinishes(project, element);
                        audit.Record(isNew ? "RoomBoundaryCreate" : "RoomBoundaryUpdate", element.Id,
                            "area=" + boundary.Area.ToString("R", CultureInfo.InvariantCulture) +
                            ";perimeter=" + boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture) +
                            ";sources=" + boundary.SourceIds.Count.ToString(CultureInfo.InvariantCulture) +
                            ";arcSagitta=" + arcSagitta.ToString("R", CultureInfo.InvariantCulture));
                    }

                    var staleRooms = AutoRoomLifecycle.MarkStaleForSelection(project, activeRoomIds, selectedSourceHandles, project.ActiveFloorId, project.ActiveZoneId, DateTime.UtcNow);
                    foreach (var stale in staleRooms)
                        audit.Record("RoomBoundaryStale", stale.Id, "topology changed within the selected boundary source set");

                    var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                    project.Touch();
                    PaletteCoordinator.RefreshProject();
                    var message = "Room Auto: " + boundaries.Count + " face • mới " + created + " • cập nhật " + updated + " • stale " + staleRooms.Count + " • sync finish " + refreshedFinishes + " • regenerate " + regenerated + ".";
                    PaletteCoordinator.SetStatus(message);
                    document.Editor.WriteMessage("\nQS3D " + message);
                }
                catch (System.Exception operationError)
                {
                    try
                    {
                        rollback.Restore(project);
                        PaletteCoordinator.RefreshProject();
                    }
                    catch (System.Exception restoreError)
                    {
                        throw new InvalidOperationException("QS3DROOMAUTO failed and project rollback also failed.", new AggregateException(operationError, restoreError));
                    }
                    throw;
                }
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
