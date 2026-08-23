using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class WallJunctionSelectedSegment
    {
        public WallAxisSegment Axis { get; set; } = null!;
        public ObjectId SourceObjectId { get; set; }
        public string SourceHandle { get; set; } = string.Empty;
        public ObjectId LayerId { get; set; }
        public ProjectElement Owner { get; set; } = null!;
        public WallJunctionOwnerContext OwnerContext { get; set; } = null!;
    }

    internal sealed class WallJunctionSelection
    {
        internal WallJunctionSelection(IReadOnlyList<WallJunctionSelectedSegment> segments)
        {
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
            Owners = segments
                .Select(x => x.Owner)
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<WallJunctionSelectedSegment> Segments { get; }
        public IReadOnlyList<ProjectElement> Owners { get; }
        public IReadOnlyList<WallAxisSegment> Axes => Segments.Select(x => x.Axis).ToList().AsReadOnly();
        public IReadOnlyList<WallJunctionOwnerContext> OwnerMappings => Segments.Select(x => x.OwnerContext).ToList().AsReadOnly();
    }

    internal static class WallJunctionSelectionReader
    {
        private const int MaxSelectedEntities = 10000;

        private sealed class ProjectSourcePlaneEntry
        {
            public ObjectId ObjectId { get; set; }
            public string Handle { get; set; } = string.Empty;
            public double ElevationM { get; set; }
        }

        private sealed class ProjectSourcePlaneScope
        {
            public double ReferenceElevationM { get; set; }
            public List<ObjectId> ObjectIds { get; } = new List<ObjectId>();
        }

        public static IReadOnlyList<IReadOnlyList<ObjectId>> ResolveProjectPlaneScopes(
            Document document,
            Transaction transaction,
            ProjectState project,
            double planarityToleranceM,
            bool rejectUnsupportedSources)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            planarityToleranceM = Positive(planarityToleranceM, nameof(planarityToleranceM));

            var ownersByHandle = BuildOwnerIndex(project);
            var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            var units = CadUnitService.GetPolicy(document);
            var entries = new List<ProjectSourcePlaneEntry>();
            foreach (ObjectId id in modelSpace)
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                var handle = entity.Handle.ToString();
                if (!ownersByHandle.ContainsKey(handle)) continue;

                if (GeneratedNativeSourceGuard.HasKnownOwnershipMarker(entity))
                {
                    if (rejectUnsupportedSources)
                        throw new InvalidOperationException("Generated QS3D output is registered as a semantic wall source: " + handle + ".");
                    continue;
                }

                double elevationM;
                if (entity is Line line)
                {
                    var startM = units.ToMeters(line.StartPoint.Z);
                    var endM = units.ToMeters(line.EndPoint.Z);
                    var deltaM = endM - startM;
                    if (!Finite(startM) || !Finite(endM) || !Finite(deltaM) || Math.Abs(deltaM) > planarityToleranceM)
                    {
                        if (rejectUnsupportedSources)
                            throw new InvalidOperationException("Semantic wall LINE is not plan-view coplanar: " + handle + ".");
                        continue;
                    }
                    elevationM = startM + deltaM / 2d;
                }
                else if (entity is Polyline polyline)
                {
                    var normal = polyline.Normal;
                    var supported = !polyline.Closed && polyline.NumberOfVertices >= 2 &&
                                    Math.Abs(normal.X) <= 1e-9d && Math.Abs(normal.Y) <= 1e-9d && normal.Z >= 1d - 1e-9d;
                    if (!supported)
                    {
                        if (rejectUnsupportedSources)
                            throw new InvalidOperationException("Semantic wall POLYLINE is not an eligible open plan-view source: " + handle + ".");
                        continue;
                    }
                    elevationM = units.ToMeters(polyline.Elevation);
                }
                else
                {
                    if (rejectUnsupportedSources)
                        throw new InvalidOperationException("Semantic wall source type is not supported for physical junction planning: " + entity.GetType().Name + " (" + handle + ").");
                    continue;
                }

                if (!Finite(elevationM))
                {
                    if (rejectUnsupportedSources)
                        throw new InvalidOperationException("Semantic wall source elevation is not finite: " + handle + ".");
                    continue;
                }
                if (entries.Count >= MaxSelectedEntities)
                    throw new InvalidOperationException("Wall Junction project plane-scope discovery supports at most " + MaxSelectedEntities.ToString(CultureInfo.InvariantCulture) + " live semantic wall sources.");
                entries.Add(new ProjectSourcePlaneEntry { ObjectId = id, Handle = handle, ElevationM = elevationM });
            }

            var scopes = new List<ProjectSourcePlaneScope>();
            foreach (var entry in entries.OrderBy(x => x.ElevationM).ThenBy(x => x.Handle, StringComparer.OrdinalIgnoreCase))
            {
                var scope = scopes.LastOrDefault();
                if (scope == null || Math.Abs(entry.ElevationM - scope.ReferenceElevationM) > planarityToleranceM)
                {
                    scope = new ProjectSourcePlaneScope { ReferenceElevationM = entry.ElevationM };
                    scopes.Add(scope);
                }
                scope.ObjectIds.Add(entry.ObjectId);
            }

            return scopes
                .Select(x => (IReadOnlyList<ObjectId>)x.ObjectIds.AsReadOnly())
                .ToList()
                .AsReadOnly();
        }

        public static WallJunctionSelection Read(
            Document document,
            Transaction transaction,
            ProjectState project,
            IEnumerable<ObjectId> selectedIds,
            double sagittaM,
            double planarityToleranceM)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));
            sagittaM = Positive(sagittaM, nameof(sagittaM));
            planarityToleranceM = Positive(planarityToleranceM, nameof(planarityToleranceM));
            if (string.IsNullOrWhiteSpace(project.ProjectId))
                throw new InvalidOperationException("Wall Junction 3D requires a non-empty project identity.");
            if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                throw new InvalidOperationException("Wall Junction 3D requires a bound drawing fingerprint.");

            var ids = selectedIds.Take(MaxSelectedEntities + 1).Distinct().ToList();
            if (ids.Count > MaxSelectedEntities)
                throw new InvalidOperationException("Wall Junction 3D supports at most " + MaxSelectedEntities.ToString(CultureInfo.InvariantCulture) + " selected entities per batch.");

            var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
            var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
            var ownersByHandle = BuildOwnerIndex(project);
            var units = CadUnitService.GetPolicy(document);
            var result = new List<WallJunctionSelectedSegment>();
            double? referenceElevationM = null;

            foreach (var id in ids)
            {
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException("Wall Junction 3D selection contains a non-live CAD entity.");
                if (!entity.OwnerId.Equals(modelSpaceId))
                    throw new InvalidOperationException("Wall Junction 3D source must be in Model Space: " + entity.Handle + ".");
                if (GeneratedNativeSourceGuard.HasKnownOwnershipMarker(entity))
                    throw new InvalidOperationException("Generated QS3D output cannot be reused as a Wall Junction 3D source: " + entity.Handle + ".");

                var handle = entity.Handle.ToString();
                var owner = ResolveOwner(ownersByHandle, handle);
                var family = project.FindFamily(owner.FamilyId);
                var thicknessM = CadGeometryGuard.Positive(
                    CadGeometryGuard.Number(owner, family, "ThicknessM", .2d),
                    owner.Id + "/ThicknessM");

                if (entity is Line line)
                {
                    var startElevationM = units.ToMeters(line.StartPoint.Z);
                    var endElevationM = units.ToMeters(line.EndPoint.Z);
                    EnsureElevation(ref referenceElevationM, startElevationM, planarityToleranceM, handle + "/start");
                    EnsureElevation(ref referenceElevationM, endElevationM, planarityToleranceM, handle + "/end");
                    var vertical = CadElementVerticalPlacement.Resolve(document, project, owner, family, line.StartPoint.Z, "HeightM", 3.6d);
                    Add(
                        result,
                        id,
                        entity.LayerId,
                        handle,
                        owner,
                        vertical,
                        thicknessM,
                        "L:" + handle,
                        new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                        new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y)),
                        project);
                    continue;
                }

                var polyline = entity as Polyline;
                if (polyline == null)
                    throw new InvalidOperationException("Wall Junction 3D supports only semantic LINE/open POLYLINE sources; received " + entity.GetType().Name + " (" + handle + ").");
                if (polyline.Closed)
                    throw new InvalidOperationException("Wall Junction 3D requires an open wall-centerline POLYLINE: " + handle + ".");
                if (polyline.NumberOfVertices < 2)
                    throw new InvalidOperationException("Wall Junction 3D open POLYLINE requires at least two vertices: " + handle + ".");
                var normal = polyline.Normal;
                if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                    throw new InvalidOperationException("Wall Junction 3D requires a plan-view +Z POLYLINE: " + handle + ".");
                EnsureElevation(ref referenceElevationM, units.ToMeters(polyline.Elevation), planarityToleranceM, handle);
                var polylineVertical = CadElementVerticalPlacement.Resolve(document, project, owner, family, polyline.Elevation, "HeightM", 3.6d);
                for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
                {
                    var a = polyline.GetPoint2dAt(index);
                    var b = polyline.GetPoint2dAt(index + 1);
                    var start = new Point2(units.ToMeters(a.X), units.ToMeters(a.Y));
                    var end = new Point2(units.ToMeters(b.X), units.ToMeters(b.Y));
                    var bulge = polyline.GetBulgeAt(index);
                    var points = Math.Abs(bulge) <= 1e-12d
                        ? (IReadOnlyList<Point2>)new[] { start, end }
                        : BulgeArcTessellator.Tessellate(start, end, bulge, sagittaM);
                    for (var part = 1; part < points.Count; part++)
                    {
                        Add(
                            result,
                            id,
                            entity.LayerId,
                            handle,
                            owner,
                            polylineVertical,
                            thicknessM,
                            "P:" + handle + ":" + index.ToString(CultureInfo.InvariantCulture) + ":" + part.ToString(CultureInfo.InvariantCulture),
                            points[part - 1],
                            points[part],
                            project);
                    }
                }
            }

            return new WallJunctionSelection(result
                .OrderBy(x => x.Axis.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly());
        }

        private static Dictionary<string, List<ProjectElement>> BuildOwnerIndex(ProjectState project)
        {
            var result = new Dictionary<string, List<ProjectElement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements.Where(x => IsWall(x.Category)))
            {
                foreach (var raw in element.SourceHandles)
                {
                    var handle = (raw ?? string.Empty).Trim();
                    if (handle.Length == 0) continue;
                    if (!result.TryGetValue(handle, out var owners))
                    {
                        owners = new List<ProjectElement>();
                        result[handle] = owners;
                    }
                    if (!owners.Any(x => string.Equals(x.Id, element.Id, StringComparison.OrdinalIgnoreCase))) owners.Add(element);
                }
            }
            return result;
        }

        private static ProjectElement ResolveOwner(IReadOnlyDictionary<string, List<ProjectElement>> ownersByHandle, string handle)
        {
            if (!ownersByHandle.TryGetValue(handle, out var owners) || owners.Count == 0)
                throw new InvalidOperationException("Wall Junction 3D source " + handle + " has no semantic wall owner.");
            if (owners.Count != 1)
                throw new InvalidOperationException("Wall Junction 3D source " + handle + " has " + owners.Count.ToString(CultureInfo.InvariantCulture) + " semantic wall owners; exactly one is required.");
            return owners[0];
        }

        private static void Add(
            ICollection<WallJunctionSelectedSegment> result,
            ObjectId sourceObjectId,
            ObjectId layerId,
            string sourceHandle,
            ProjectElement owner,
            CadElementVerticalPlacement vertical,
            double thicknessM,
            string segmentId,
            Point2 start,
            Point2 end,
            ProjectState project)
        {
            if (!(start.DistanceTo(end) > 1e-9d))
                throw new InvalidOperationException("Wall Junction 3D source segment is degenerate: " + segmentId + ".");
            var axis = new WallAxisSegment(segmentId, start, end);
            result.Add(new WallJunctionSelectedSegment
            {
                Axis = axis,
                SourceObjectId = sourceObjectId,
                SourceHandle = sourceHandle,
                LayerId = layerId,
                Owner = owner,
                OwnerContext = new WallJunctionOwnerContext(
                    segmentId,
                    owner.Id,
                    project.ProjectId,
                    project.DrawingFingerprint,
                    vertical.BottomElevationM,
                    vertical.TopElevationM,
                    thicknessM)
            });
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;

        private static double Positive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(label, "Value must be finite and > 0.");
            return value;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void EnsureElevation(ref double? referenceElevationM, double elevationM, double toleranceM, string label)
        {
            if (double.IsNaN(elevationM) || double.IsInfinity(elevationM))
                throw new InvalidOperationException("Wall Junction 3D source elevation is not finite: " + label + ".");
            if (!referenceElevationM.HasValue)
            {
                referenceElevationM = elevationM;
                return;
            }
            var delta = elevationM - referenceElevationM.Value;
            if (double.IsNaN(delta) || double.IsInfinity(delta) || Math.Abs(delta) > toleranceM)
                throw new InvalidOperationException("Wall Junction 3D sources must be coplanar in Z within " + toleranceM.ToString("R", CultureInfo.InvariantCulture) + " m: " + label + ".");
        }
    }
}
