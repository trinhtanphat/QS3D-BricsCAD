using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GridIntersectionMarkerService
    {
        internal const string RegAppName = "QS3D_GRID_INTERSECTION";
        internal const string OwnershipVersion = "1";
        private const int MaxGridSources = 2000;
        private const int MaxMarkers = 100000;
        private const double GeometryTolerance = 1e-8d;
        private const double MarkerRadiusM = 0.10d;

        private sealed class NativeGrid
        {
            public NativeGrid(ProjectElement element, GridReferenceCurve curve, ObjectId ownerId, ObjectId sourceId, string layer, double elevation)
            {
                Element = element;
                Curve = curve;
                OwnerId = ownerId;
                SourceId = sourceId;
                Layer = layer;
                Elevation = elevation;
            }

            public ProjectElement Element { get; }
            public GridReferenceCurve Curve { get; }
            public ObjectId OwnerId { get; }
            public ObjectId SourceId { get; }
            public string Layer { get; }
            public double Elevation { get; }
        }

        private sealed class MarkerRecord
        {
            public MarkerRecord(ObjectId id, string handle, string projectId, string ownerToken, string pairToken, string firstGridId, string secondGridId, int occurrence, Point3d point)
            {
                Id = id;
                Handle = handle;
                ProjectId = projectId;
                OwnerToken = ownerToken;
                PairToken = pairToken;
                FirstGridId = firstGridId;
                SecondGridId = secondGridId;
                Occurrence = occurrence;
                Point = point;
            }

            public ObjectId Id { get; }
            public string Handle { get; }
            public string ProjectId { get; }
            public string OwnerToken { get; }
            public string PairToken { get; }
            public string FirstGridId { get; }
            public string SecondGridId { get; }
            public int Occurrence { get; }
            public Point3d Point { get; }
        }

        public static int Refresh(Document document, ProjectState project, IReadOnlyCollection<string>? targetGridIds = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Grid intersection refresh yêu cầu DWG đích vẫn là MdiActiveDocument.");
            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Grid intersection marker refresh");

            var targetIds = NormalizeTargets(targetGridIds);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var grids = ReadNativeGrids(document.Database, transaction, project);
                if (targetIds != null)
                {
                    foreach (var target in targetIds)
                        if (!grids.ContainsKey(target))
                            throw new InvalidOperationException("Grid intersection refresh target is not a live canonical Grid: " + target + ".");
                }

                var desired = PlanMarkers(grids, targetIds);
                var existing = ReadExistingMarkers(document.Database, transaction, project.ProjectId, targetIds);
                ValidateExistingAgainstDesired(existing, desired);

                foreach (var marker in existing.Values)
                {
                    var entity = transaction.GetObject(marker.Id, OpenMode.ForWrite, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Grid intersection marker changed after preflight: " + marker.Handle + ".");
                    RequireMatchingMarker(entity, marker, project.ProjectId);
                    entity.Erase();
                }

                EnsureRegApp(document.Database, transaction);
                var radius = CadGeometryGuard.ToDrawingUnits(document, MarkerRadiusM, "Grid intersection marker radius");
                foreach (var plan in desired)
                    AddMarker(document, transaction, project, grids, plan, radius);

                transaction.Commit();
                try { document.Editor.Regen(); } catch { }
                return desired.Count;
            }
        }

        public static IReadOnlyList<string> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<string>();
            try
            {
                ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Grid intersection marker health");
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var grids = ReadNativeGrids(document.Database, transaction, project);
                    var desired = PlanMarkers(grids, null);
                    Dictionary<string, MarkerRecord> existing;
                    try { existing = ReadExistingMarkers(document.Database, transaction, project.ProjectId, null); }
                    catch (Exception ex)
                    {
                        issues.Add("MARKER_OWNERSHIP_INVALID: " + ex.Message);
                        transaction.Commit();
                        return issues.AsReadOnly();
                    }

                    var desiredByOwner = desired.ToDictionary(x => x.OwnerToken, StringComparer.Ordinal);
                    foreach (var plan in desired)
                    {
                        if (!existing.TryGetValue(plan.OwnerToken, out var marker))
                        {
                            issues.Add("MARKER_MISSING: " + plan.OwnerToken);
                            continue;
                        }
                        var elevation = PairElevation(grids, plan);
                        if (Distance2d(marker.Point, plan.Point) > GeometryTolerance || Math.Abs(marker.Point.Z - elevation) > GeometryTolerance)
                            issues.Add("MARKER_STALE_GEOMETRY: " + plan.OwnerToken);
                    }
                    foreach (var marker in existing.Values)
                        if (!desiredByOwner.ContainsKey(marker.OwnerToken))
                            issues.Add("MARKER_STALE_OWNER: " + marker.OwnerToken);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                issues.Add("MARKER_HEALTH_BLOCKED: " + ex.Message);
            }
            return issues.AsReadOnly();
        }

        private static HashSet<string>? NormalizeTargets(IReadOnlyCollection<string>? targets)
        {
            if (targets == null) return null;
            if (targets.Count == 0) throw new InvalidOperationException("Selected Grid intersection refresh requires at least one Grid.");
            if (targets.Count > MaxGridSources) throw new InvalidOperationException("Selected Grid intersection refresh exceeds " + MaxGridSources + " targets.");
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in targets)
            {
                if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Grid intersection target id is blank.");
                var id = raw.Trim();
                if (!string.Equals(id, raw, StringComparison.Ordinal)) throw new InvalidOperationException("Grid intersection target id must be canonical: " + raw + ".");
                if (!result.Add(id)) throw new InvalidOperationException("Grid intersection target list contains duplicate id: " + id + ".");
            }
            return result;
        }

        private static Dictionary<string, NativeGrid> ReadNativeGrids(Database database, Transaction transaction, ProjectState project)
        {
            var semantic = project.Elements.Where(x => x != null && x.Category == ElementCategory.Grid).ToList();
            if (semantic.Count > MaxGridSources) throw new InvalidOperationException("Grid intersection source count exceeds " + MaxGridSources + ".");
            var result = new Dictionary<string, NativeGrid>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in semantic)
            {
                if (!result.TryAdd(element.Id, ReadNativeGrid(database, transaction, element)))
                    throw new InvalidOperationException("Project contains duplicate semantic Grid id: " + element.Id + ".");
            }
            return result;
        }

        private static NativeGrid ReadNativeGrid(Database database, Transaction transaction, ProjectElement element)
        {
            var sources = element.SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (sources.Length != 1) throw new InvalidOperationException("Grid " + element.Id + " must have exactly one authoritative native source handle.");
            var handle = CadHandleService.NormalizeHexHandle(sources[0]);
            if (handle == null || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException("Grid " + element.Id + " has invalid native source handle: " + sources[0] + ".");
            ObjectId id;
            try { id = database.GetObjectId(false, new Handle(value), 0); }
            catch (Exception ex) { throw new InvalidOperationException("Cannot resolve Grid source " + element.Id + "/" + handle + ".", ex); }
            if (id.IsNull || !id.IsValid) throw new InvalidOperationException("Cannot resolve Grid source " + element.Id + "/" + handle + ".");
            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null || entity.IsErased) throw new InvalidOperationException("Grid source is not live: " + element.Id + "/" + handle + ".");
            if (GeneratedNativeSourceGuard.HasKnownOwnershipMarker(entity))
                throw new InvalidOperationException("Grid source cannot be QS3D generated output: " + element.Id + "/" + handle + ".");

            if (entity is Line line)
            {
                RequireFinite(line.StartPoint, element.Id + "/start");
                RequireFinite(line.EndPoint, element.Id + "/end");
                if (Math.Abs(line.StartPoint.Z - line.EndPoint.Z) > GeometryTolerance)
                    throw new InvalidOperationException("Grid LINE must lie on one WCS-XY elevation: " + element.Id + ".");
                var curve = GridReferenceCurve.Line(element.Id, new Point2(line.StartPoint.X, line.StartPoint.Y), new Point2(line.EndPoint.X, line.EndPoint.Y));
                return new NativeGrid(element, curve, entity.OwnerId, id, entity.Layer ?? string.Empty, line.StartPoint.Z);
            }

            if (entity is Arc arc)
            {
                RequireFinite(arc.Center, element.Id + "/center");
                if (double.IsNaN(arc.Radius) || double.IsInfinity(arc.Radius) || !(arc.Radius > GeometryTolerance))
                    throw new InvalidOperationException("Grid ARC radius is invalid: " + element.Id + ".");
                if (Math.Abs(arc.Normal.X) > GeometryTolerance || Math.Abs(arc.Normal.Y) > GeometryTolerance || Math.Abs(Math.Abs(arc.Normal.Z) - 1d) > GeometryTolerance)
                    throw new InvalidOperationException("Grid ARC must be parallel to WCS-XY for deterministic pair markers: " + element.Id + ".");
                var sweep = arc.EndAngle - arc.StartAngle;
                while (sweep <= 0d) sweep += Math.PI * 2d;
                var curve = GridReferenceCurve.Arc(element.Id, new Point2(arc.Center.X, arc.Center.Y), arc.Radius, arc.StartAngle, sweep);
                return new NativeGrid(element, curve, entity.OwnerId, id, entity.Layer ?? string.Empty, arc.Center.Z);
            }

            throw new InvalidOperationException("Grid intersection markers support LINE/ARC native sources only; got " + entity.GetType().Name + " for " + element.Id + ".");
        }

        private static IReadOnlyList<GridIntersectionMarkerPlan> PlanMarkers(
            IReadOnlyDictionary<string, NativeGrid> grids,
            HashSet<string>? targetIds)
        {
            var intersections = GridIntersectionPlanner.FindIntersections(grids.Values.Select(x => x.Curve));
            var all = GridIntersectionMarkerPlanner.Plan(intersections);
            var result = targetIds == null
                ? all.ToList()
                : all.Where(x => targetIds.Contains(x.FirstElementId) || targetIds.Contains(x.SecondElementId)).ToList();
            if (result.Count > MaxMarkers) throw new InvalidOperationException("Grid intersection marker count exceeds " + MaxMarkers + ".");
            foreach (var plan in result)
            {
                var first = grids[plan.FirstElementId];
                var second = grids[plan.SecondElementId];
                if (first.OwnerId != second.OwnerId)
                    throw new InvalidOperationException("Grid pair crosses native owner spaces and cannot own one marker: " + plan.PairToken + ".");
                if (Math.Abs(first.Elevation - second.Elevation) > GeometryTolerance)
                    throw new InvalidOperationException("Grid pair lies on different elevations and cannot own one marker: " + plan.PairToken + ".");
            }
            return result.AsReadOnly();
        }

        private static Dictionary<string, MarkerRecord> ReadExistingMarkers(
            Database database,
            Transaction transaction,
            string projectId,
            HashSet<string>? targetIds)
        {
            var result = new Dictionary<string, MarkerRecord>(StringComparer.Ordinal);
            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId blockId in blockTable)
            {
                var space = transaction.GetObject(blockId, OpenMode.ForRead, false) as BlockTableRecord;
                if (space == null || space.IsLayout == false && space.Name.StartsWith("*", StringComparison.Ordinal)) continue;
                var scanned = 0;
                foreach (ObjectId id in space)
                {
                    if (scanned++ > 250000) throw new InvalidOperationException("Grid intersection ownership scan exceeds 250000 entities in one space.");
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    using (var xdata = entity.GetXDataForApplication(RegAppName))
                    {
                        if (xdata == null) continue;
                        var record = ParseMarker(entity, xdata);
                        if (!string.Equals(record.ProjectId, projectId, StringComparison.Ordinal))
                            throw new InvalidOperationException("Foreign Grid intersection marker project ownership detected at handle " + record.Handle + ".");
                        if (targetIds != null && !targetIds.Contains(record.FirstGridId) && !targetIds.Contains(record.SecondGridId)) continue;
                        if (!result.TryAdd(record.OwnerToken, record))
                            throw new InvalidOperationException("Duplicate live Grid intersection owner token: " + record.OwnerToken + ".");
                    }
                }
            }
            return result;
        }

        private static MarkerRecord ParseMarker(Entity entity, ResultBuffer xdata)
        {
            var values = xdata.AsArray();
            if (values.Length != 11 ||
                !string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.Ordinal) ||
                !string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), OwnershipVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("Malformed Grid intersection marker XData at handle " + entity.Handle + ".");
            var projectId = Canonical(values[2].Value, "project id", entity.Handle.ToString());
            var ownerToken = Canonical(values[3].Value, "owner token", entity.Handle.ToString());
            var pairToken = Canonical(values[4].Value, "pair token", entity.Handle.ToString());
            var first = Canonical(values[5].Value, "first Grid id", entity.Handle.ToString());
            var second = Canonical(values[6].Value, "second Grid id", entity.Handle.ToString());
            if (!int.TryParse(Convert.ToString(values[7].Value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var occurrence) || occurrence < 0)
                throw new InvalidOperationException("Malformed Grid intersection occurrence at handle " + entity.Handle + ".");
            var expectedPair = GridIntersectionIdentityPlanner.BuildPairToken(first, second);
            var expectedOwner = GridIntersectionIdentityPlanner.BuildIntersectionOwner(first, second, occurrence);
            if (!string.Equals(expectedPair, pairToken, StringComparison.Ordinal) || !string.Equals(expectedOwner, ownerToken, StringComparison.Ordinal))
                throw new InvalidOperationException("Grid intersection pair ownership does not match canonical GIP1/GIX1 identity at handle " + entity.Handle + ".");
            if (!double.TryParse(Convert.ToString(values[8].Value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !double.TryParse(Convert.ToString(values[9].Value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !double.TryParse(Convert.ToString(values[10].Value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
                !Finite(x) || !Finite(y) || !Finite(z))
                throw new InvalidOperationException("Malformed Grid intersection marker position at handle " + entity.Handle + ".");
            if (!(entity is Circle)) throw new InvalidOperationException("Grid intersection marker handle " + entity.Handle + " is not a Circle.");
            return new MarkerRecord(entity.ObjectId, entity.Handle.ToString(), projectId, ownerToken, pairToken, first, second, occurrence, new Point3d(x, y, z));
        }

        private static void ValidateExistingAgainstDesired(
            IReadOnlyDictionary<string, MarkerRecord> existing,
            IReadOnlyList<GridIntersectionMarkerPlan> desired)
        {
            var desiredOwners = new HashSet<string>(StringComparer.Ordinal);
            foreach (var plan in desired)
                if (!desiredOwners.Add(plan.OwnerToken))
                    throw new InvalidOperationException("Duplicate desired Grid intersection owner token: " + plan.OwnerToken + ".");
            foreach (var marker in existing.Values)
            {
                if (!GridIntersectionIdentityPlanner.IsOwnerForPair(marker.OwnerToken, marker.FirstGridId, marker.SecondGridId))
                    throw new InvalidOperationException("Existing Grid intersection marker is not pair-owned: " + marker.Handle + ".");
            }
        }

        private static void AddMarker(
            Document document,
            Transaction transaction,
            ProjectState project,
            IReadOnlyDictionary<string, NativeGrid> grids,
            GridIntersectionMarkerPlan plan,
            double radius)
        {
            var first = grids[plan.FirstElementId];
            var second = grids[plan.SecondElementId];
            var elevation = PairElevation(grids, plan);
            var point = new Point3d(plan.Point.X, plan.Point.Y, elevation);
            RequireFinite(point, plan.OwnerToken);
            var owner = transaction.GetObject(first.OwnerId, OpenMode.ForWrite, false) as BlockTableRecord;
            if (owner == null) throw new InvalidOperationException("Cannot open native owner space for Grid pair " + plan.PairToken + ".");

            var circle = new Circle(point, Vector3d.ZAxis, radius);
            circle.SetDatabaseDefaults(document.Database);
            try
            {
                var source = transaction.GetObject(first.SourceId, OpenMode.ForRead, false) as Entity;
                if (source != null) circle.LayerId = source.LayerId;
            }
            catch { }
            owner.AppendEntity(circle);
            transaction.AddNewlyCreatedDBObject(circle, true);
            Mark(circle, project.ProjectId, plan, point);
        }

        private static double PairElevation(IReadOnlyDictionary<string, NativeGrid> grids, GridIntersectionMarkerPlan plan)
        {
            var first = grids[plan.FirstElementId].Elevation;
            var second = grids[plan.SecondElementId].Elevation;
            if (Math.Abs(first - second) > GeometryTolerance)
                throw new InvalidOperationException("Grid pair elevations changed during marker planning: " + plan.PairToken + ".");
            return (first + second) * 0.5d;
        }

        private static void Mark(Entity entity, string projectId, GridIntersectionMarkerPlan plan, Point3d point)
        {
            using (var xdata = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, OwnershipVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, projectId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, plan.OwnerToken),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, plan.PairToken),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, plan.FirstElementId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, plan.SecondElementId),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, plan.Occurrence.ToString(CultureInfo.InvariantCulture)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, point.X.ToString("R", CultureInfo.InvariantCulture)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, point.Y.ToString("R", CultureInfo.InvariantCulture)),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, point.Z.ToString("R", CultureInfo.InvariantCulture))))
                entity.XData = xdata;
        }

        private static void RequireMatchingMarker(Entity entity, MarkerRecord expected, string projectId)
        {
            using (var xdata = entity.GetXDataForApplication(RegAppName))
            {
                if (xdata == null) throw new InvalidOperationException("Grid intersection marker lost ownership XData: " + expected.Handle + ".");
                var actual = ParseMarker(entity, xdata);
                if (!string.Equals(actual.ProjectId, projectId, StringComparison.Ordinal) ||
                    !string.Equals(actual.OwnerToken, expected.OwnerToken, StringComparison.Ordinal))
                    throw new InvalidOperationException("Grid intersection marker ownership changed after preflight: " + expected.Handle + ".");
            }
        }

        private static void EnsureRegApp(Database database, Transaction transaction)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        private static string Canonical(object value, string label, string handle)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text) || !string.Equals(text, text.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Malformed Grid intersection " + label + " at handle " + handle + ".");
            foreach (var ch in text)
                if (char.IsControl(ch)) throw new InvalidOperationException("Control character in Grid intersection " + label + " at handle " + handle + ".");
            return text;
        }

        private static void RequireFinite(Point3d point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y) || !Finite(point.Z))
                throw new InvalidOperationException("Grid intersection coordinate is not finite: " + label + ".");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static double Distance2d(Point3d point, Point2 expected)
        {
            var dx = point.X - expected.X;
            var dy = point.Y - expected.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
