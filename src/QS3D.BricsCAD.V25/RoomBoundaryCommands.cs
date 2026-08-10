using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
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
                    var created = 0;
                    var updated = 0;
                    var claimedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var signatures = boundaries.Select(x => AutomaticRoomLifecycleService.NormalizeSourceSignature(x.SourceIds)).ToArray();
                    var signatureCounts = signatures.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

                    for (var index = 0; index < boundaries.Count; index++)
                    {
                        var boundary = boundaries[index];
                        var signature = signatures[index];
                        var disambiguate = signature.Length == 0 || signatureCounts[signature] > 1;
                        var stableId = AutomaticRoomLifecycleService.BuildStableElementId(signature, boundary.Key, disambiguate);
                        var element = project.FindElement(stableId);
                        if (element == null && !disambiguate && signature.Length > 0)
                        {
                            element = project.Elements.FirstOrDefault(x =>
                                AutomaticRoomLifecycleService.IsManaged(x) &&
                                !claimedIds.Contains(x.Id) &&
                                string.Equals(AutomaticRoomLifecycleService.GetSourceSignature(x), signature, StringComparison.Ordinal));
                        }

                        var isNew = element == null;
                        if (element == null)
                        {
                            element = new ProjectElement(stableId, ElementCategory.Room, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                            project.Elements.Add(element);
                            created++;
                        }
                        else
                        {
                            if (element.Category != ElementCategory.Room) throw new InvalidOperationException("Boundary id collision with non-room semantic element: " + stableId);
                            updated++;
                        }
                        if (!claimedIds.Add(element.Id)) throw new InvalidOperationException("Automatic room identity collision: " + element.Id);
                        currentIds.Add(element.Id);

                        element.Category = ElementCategory.Room;
                        element.FamilyId = family.Id;
                        element.FloorId = project.ActiveFloorId;
                        element.ZoneId = project.ActiveZoneId;
                        element.DrawingFingerprint = project.DrawingFingerprint;
                        element.SourceHandles.Clear();
                        foreach (var sourceId in boundary.SourceIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                            element.SourceHandles.Add(sourceId.Trim());
                        element.Properties["BoundaryMode"] = "AutoNetwork";
                        element.Properties["AutoBoundaryManaged"] = "true";
                        element.Properties["BoundaryKey"] = boundary.Key;
                        element.Properties["BoundarySourceHandles"] = string.Join(";", element.SourceHandles);
                        element.Properties["BoundarySourceSignature"] = signature;
                        element.Properties["BoundaryVertexCount"] = boundary.Vertices.Count.ToString(CultureInfo.InvariantCulture);
                        element.Properties["BoundaryArcSagittaM"] = arcSagitta.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["AreaM2"] = boundary.Area.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["PerimeterM"] = boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties.Remove("AutoBoundaryStale");
                        foreach (var property in family.Properties)
                            if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
                        element.MarkDirty(ElementDirtyFlags.All);
                        audit.Record(isNew ? "RoomBoundaryCreate" : "RoomBoundaryUpdate", element.Id,
                            "area=" + boundary.Area.ToString("R", CultureInfo.InvariantCulture) +
                            ";perimeter=" + boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture) +
                            ";sources=" + boundary.SourceIds.Count.ToString(CultureInfo.InvariantCulture) +
                            ";arcSagitta=" + arcSagitta.ToString("R", CultureInfo.InvariantCulture));
                    }

                    var selectedSources = segments.Select(x => x.SourceId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    var lifecycle = AutomaticRoomLifecycleService.ReconcileStale(project, currentIds, selectedSources);
                    foreach (var removedId in lifecycle.RemovedRoomIds) audit.Record("RoomBoundaryRemove", removedId, "Boundary no longer exists in selected network.");
                    foreach (var staleId in lifecycle.RetainedStaleRoomIds) audit.Record("RoomBoundaryStale", staleId, "Retained because a non-generated dependent still references the room.");

                    var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                    project.Touch();
                    PaletteCoordinator.RefreshProject();
                    var message = "Room Auto: " + boundaries.Count + " face • mới " + created + " • cập nhật " + updated + " • xóa cũ " + lifecycle.RemovedRoomIds.Count + " • stale giữ " + lifecycle.RetainedStaleRoomIds.Count + " • regenerate " + regenerated + ".";
                    PaletteCoordinator.SetStatus(message);
                    document.Editor.WriteMessage("\nQS3D " + message);
                }
                catch (Exception operationError)
                {
                    try
                    {
                        rollback.Restore(project);
                        PaletteCoordinator.RefreshProject();
                    }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException("QS3DROOMAUTO failed and project rollback also failed.", new AggregateException(operationError, restoreError));
                    }
                    throw;
                }
            }
            catch (Exception ex)
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
    }
}
