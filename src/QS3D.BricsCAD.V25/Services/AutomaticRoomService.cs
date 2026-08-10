using System;
using System.Collections.Generic;
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

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class AutomaticRoomResult
    {
        public int Boundaries { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int RemovedStale { get; set; }
        public int RetainedStale { get; set; }
        public int UnsupportedEntities { get; set; }
    }

    internal static class AutomaticRoomService
    {
        private static readonly HashSet<ElementCategory> GeneratedFinishCategories = new HashSet<ElementCategory>
        {
            ElementCategory.FloorFinish,
            ElementCategory.Waterproofing,
            ElementCategory.Skirting,
            ElementCategory.WallFinish,
            ElementCategory.CeilingFinish
        };

        public static AutomaticRoomResult Generate(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var selection = CadBoundaryReader.ReadCurrentSelection(document);
            var result = new AutomaticRoomResult { UnsupportedEntities = selection.UnsupportedEntities };
            if (selection.Segments.Count < 3) return result;

            var project = ProjectContextCoordinator.GetOrCreate(document);
            var snapMm = PositiveMetadata(project, "QS3D.RoomBoundarySnapMm", 1d);
            var minimumArea = PositiveMetadata(project, "QS3D.RoomBoundaryMinAreaM2", 0.05d);
            var boundaries = RoomBoundaryFinder.Find(selection.Segments, snapMm / 1000d, minimumArea);
            result.Boundaries = boundaries.Count;
            if (boundaries.Count == 0) return result;

            var family = ResolveRoomFamily(project);
            var signatures = boundaries.Select(SourceSignature).ToArray();
            var signatureCounts = signatures.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
            var currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var correlation = Guid.NewGuid().ToString("N");
            var audit = AuditTrail.ForProject(project);

            for (var i = 0; i < boundaries.Count; i++)
            {
                var boundary = boundaries[i];
                var signature = signatures[i];
                var identityMaterial = signatureCounts[signature] == 1 && !string.IsNullOrWhiteSpace(signature) ? signature : signature + "|" + boundary.Key;
                var id = "ROOM-AUTO-" + StableHash(identityMaterial, 20);
                currentIds.Add(id);
                var room = project.FindElement(id);
                var created = room == null;
                if (room == null)
                {
                    room = new ProjectElement(id, ElementCategory.Room, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                    project.Elements.Add(room);
                    result.Created++;
                }
                else
                {
                    if (room.Category != ElementCategory.Room) throw new InvalidOperationException("Automatic room id collides with a non-Room element: " + id);
                    result.Updated++;
                }

                room.Category = ElementCategory.Room;
                room.FamilyId = family.Id;
                room.FloorId = project.ActiveFloorId;
                room.ZoneId = project.ActiveZoneId;
                room.DrawingFingerprint = project.DrawingFingerprint;
                room.SourceHandles.Clear();
                foreach (var handle in boundary.SourceIds) room.SourceHandles.Add(handle);
                room.Properties["AutoBoundaryManaged"] = "true";
                room.Properties["AutoBoundaryMode"] = "PlanarSegmentNetwork";
                room.Properties["AutoBoundaryKey"] = boundary.Key;
                room.Properties["AutoBoundarySourceSignature"] = signature;
                room.Properties["BoundaryVertexCount"] = boundary.Vertices.Count.ToString(CultureInfo.InvariantCulture);
                room.Properties["BoundaryVerticesM"] = SerializeVertices(boundary.Vertices);
                room.Properties["CentroidXM"] = boundary.Centroid.X.ToString("R", CultureInfo.InvariantCulture);
                room.Properties["CentroidYM"] = boundary.Centroid.Y.ToString("R", CultureInfo.InvariantCulture);
                room.Properties["AreaM2"] = boundary.Area.ToString("R", CultureInfo.InvariantCulture);
                room.Properties["PerimeterM"] = boundary.Perimeter.ToString("R", CultureInfo.InvariantCulture);
                room.Properties.Remove("AutoBoundaryStale");
                if (!room.Properties.ContainsKey("HeightM") && family.Properties.TryGetValue("HeightM", out var familyHeight)) room.Properties["HeightM"] = familyHeight;
                room.MarkDirty(ElementDirtyFlags.All);
                new RoomRegenerator().Regenerate(project, room);
                room.MarkClean(ElementDirtyFlags.All);
                audit.Record(created ? "room.auto.create" : "room.auto.update", room.Id, boundary.Area.ToString("0.###", CultureInfo.InvariantCulture) + " m2 • " + boundary.SourceIds.Count + " source(s)", correlationId: correlation);
            }

            ReconcileStale(project, selection.SourceHandles, currentIds, audit, correlation, result);
            project.Metadata["QS3D.RoomBoundarySnapMm"] = snapMm.ToString("R", CultureInfo.InvariantCulture);
            project.Metadata["QS3D.RoomBoundaryMinAreaM2"] = minimumArea.ToString("R", CultureInfo.InvariantCulture);
            project.Metadata["QS3D.DrawingUnit"] = CadUnitService.Describe(document);
            project.Touch();
            return result;
        }

        private static void ReconcileStale(ProjectState project, IReadOnlyList<string> selectedHandles, ISet<string> currentIds, AuditTrail audit, string correlation, AutomaticRoomResult result)
        {
            var selected = new HashSet<string>(selectedHandles, StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0) return;
            var stale = project.Elements.Where(x => x.Category == ElementCategory.Room && IsAutoManaged(x) && !currentIds.Contains(x.Id) && x.SourceHandles.Any(selected.Contains)).ToList();
            foreach (var room in stale)
            {
                var dependents = project.Elements.Where(x => x.DependsOn.Any(id => string.Equals(id, room.Id, StringComparison.OrdinalIgnoreCase))).ToList();
                var protectedDependents = dependents.Where(x => !GeneratedFinishCategories.Contains(x.Category)).ToList();
                if (protectedDependents.Count > 0)
                {
                    room.Properties["AutoBoundaryStale"] = "true";
                    result.RetainedStale++;
                    audit.Record("room.auto.stale", room.Id, "Retained because " + protectedDependents.Count + " non-finish dependent(s) exist.", correlationId: correlation);
                    continue;
                }
                foreach (var dependent in dependents) project.Elements.Remove(dependent);
                project.Elements.Remove(room);
                result.RemovedStale++;
                audit.Record("room.auto.remove", room.Id, "Boundary no longer exists in selected network.", correlationId: correlation);
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
            var family = new ProjectFamily("auto-room", "Phòng", ElementCategory.Room);
            family.Properties["HeightM"] = "3.6";
            project.Families.Add(family);
            return family;
        }

        private static bool IsAutoManaged(ProjectElement element) => element.Properties.TryGetValue("AutoBoundaryManaged", out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        private static double PositiveMetadata(ProjectState project, string key, double fallback)
        {
            if (!project.Metadata.TryGetValue(key, out var text) || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) return fallback;
            return value;
        }
        private static string SourceSignature(RoomBoundary2 boundary) => string.Join("|", boundary.SourceIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal));
        private static string SerializeVertices(IReadOnlyList<Point2> vertices) => string.Join(";", vertices.Select(x => x.X.ToString("R", CultureInfo.InvariantCulture) + "," + x.Y.ToString("R", CultureInfo.InvariantCulture)));
        private static string StableHash(string value, int length)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
                return hex.Substring(0, Math.Min(length, hex.Length));
            }
        }
    }
}
