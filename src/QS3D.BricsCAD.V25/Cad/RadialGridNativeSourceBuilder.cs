using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class RadialGridNativeRequest
    {
        public string SystemKey { get; set; } = string.Empty;
        public Point3d CenterDrawing { get; set; }
        public Point3d FirstRayDirectionPointDrawing { get; set; }
        public int RayCount { get; set; }
        public double RayStepDegrees { get; set; }
        public double InnerRadiusM { get; set; }
        public double FirstRingRadiusM { get; set; }
        public int RingCount { get; set; }
        public double RingSpacingM { get; set; }
    }

    internal sealed class RadialGridNativeResult
    {
        public string SystemKey { get; set; } = string.Empty;
        public int Curves { get; set; }
        public int Replaced { get; set; }
    }

    internal static class RadialGridNativeSourceBuilder
    {
        internal const string SystemKeyProperty = "QS3D.GridSystem.Key";
        internal const string StationFamilyProperty = "QS3D.GridSystem.StationFamily";
        internal const string StationIndexProperty = "QS3D.GridSystem.StationIndex";
        internal const string StationCoordinateProperty = "QS3D.GridSystem.StationCoordinateM";
        internal const string SystemKindProperty = "QS3D.GridSystem.Kind";
        private const int MaxStationsPerFamily = 200;
        private const double DirectionTolerance = 1e-9d;
        private const double DegreesToRadians = Math.PI / 180d;
        private const double TwoPi = Math.PI * 2d;

        private sealed class ExistingSource
        {
            public ExistingSource(ProjectElement element, ObjectId objectId) { Element = element; ObjectId = objectId; }
            public ProjectElement Element { get; }
            public ObjectId ObjectId { get; }
        }

        public static RadialGridNativeResult Build(Document document, ProjectState project, RadialGridNativeRequest request)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Radial Grid authoring yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var systemKey = NormalizeSystemKey(request.SystemKey);
            ValidateCount(request.RayCount, "ray");
            ValidateCount(request.RingCount, "ring");
            RequireFinite(request.CenterDrawing, "Radial Grid center");
            RequireFinite(request.FirstRayDirectionPointDrawing, "Radial Grid first-ray direction point");
            ValidatePositive(request.RayStepDegrees, "Ray angular step");
            if (!Finite(request.InnerRadiusM) || request.InnerRadiusM < 0d)
                throw new ArgumentOutOfRangeException(nameof(request.InnerRadiusM), "Radial Grid inner radius must be finite and non-negative.");
            ValidatePositive(request.FirstRingRadiusM, "First ring radius");
            if (request.RingCount > 1) ValidatePositive(request.RingSpacingM, "Ring spacing");
            else if (!Finite(request.RingSpacingM) || request.RingSpacingM < 0d)
                throw new ArgumentOutOfRangeException(nameof(request.RingSpacingM), "Radial Grid single-ring spacing must be finite and non-negative.");
            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Radial Grid native authoring");

            var dx = request.FirstRayDirectionPointDrawing.X - request.CenterDrawing.X;
            var dy = request.FirstRayDirectionPointDrawing.Y - request.CenterDrawing.Y;
            var directionLength = Hypot(dx, dy);
            if (!(directionLength > DirectionTolerance) || !Finite(directionLength))
                throw new InvalidOperationException("Radial Grid first-ray direction point must differ from the center in WCS-XY.");
            var firstAngle = Math.Atan2(dy, dx);
            if (!Finite(firstAngle)) throw new InvalidOperationException("Radial Grid first-ray direction is not finite.");

            var centerM = new Point2(
                CadGeometryGuard.ToMeters(document, request.CenterDrawing.X, "Radial Grid center X"),
                CadGeometryGuard.ToMeters(document, request.CenterDrawing.Y, "Radial Grid center Y"));
            var outerRadiusM = CheckedOuterRadius(request.FirstRingRadiusM, request.RingCount, request.RingSpacingM);
            var input = new RadialGridSystemInput
            {
                CenterM = centerM,
                Rays = CreateRays(systemKey, request.RayCount, firstAngle, request.RayStepDegrees * DegreesToRadians),
                Rings = CreateRings(systemKey, request.RingCount, request.FirstRingRadiusM, request.RingSpacingM),
                InnerRadiusM = request.InnerRadiusM,
                OuterRadiusM = outerRadiusM
            };
            var planned = GridSystemPlanner.PlanRadial(input);
            var materialization = GridSystemMaterializationPlan.Create(planned);
            if (materialization.Count != request.RayCount + request.RingCount)
                throw new InvalidOperationException("Radial Grid planner returned an unexpected curve count.");

            var family = ResolveGridFamily(project);
            var desiredIds = new HashSet<string>(materialization.Select(x => x.ElementId), StringComparer.OrdinalIgnoreCase);
            var rollback = ProjectStateSnapshot.Capture(project);
            var replaced = 0;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var existing = ValidateExistingSources(document.Database, transaction, project, systemKey, desiredIds);
                    replaced = existing.Count;
                    var currentSpace = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForWrite, false) as BlockTableRecord;
                    if (currentSpace == null) throw new InvalidOperationException("Radial Grid current space is unavailable.");

                    foreach (var source in existing)
                    {
                        var entity = transaction.GetObject(source.ObjectId, OpenMode.ForWrite, false) as Entity;
                        if (entity == null || entity.IsErased)
                            throw new InvalidOperationException("Radial Grid source changed after preflight: " + source.Element.Id + ".");
                        entity.Erase();
                    }

                    foreach (var retired in project.Elements
                        .Where(x => x.Category == ElementCategory.Grid && IsOwnedRadial(x, systemKey) && !desiredIds.Contains(x.Id))
                        .ToList())
                        project.Elements.Remove(retired);

                    for (var index = 0; index < materialization.Count; index++)
                    {
                        var curve = materialization[index].Curve;
                        Entity? entity = null;
                        try
                        {
                            if (curve.Kind == GridReferenceCurveKind.Line)
                            {
                                entity = new Line(
                                    ToDrawingPoint(document, curve.Start, request.CenterDrawing.Z, curve.ElementId + "/start"),
                                    ToDrawingPoint(document, curve.End, request.CenterDrawing.Z, curve.ElementId + "/end"));
                            }
                            else if (curve.Kind == GridReferenceCurveKind.Arc)
                            {
                                var radiusDrawing = CadGeometryGuard.ToDrawingUnits(document, curve.Radius, curve.ElementId + "/radius");
                                entity = new Arc(
                                    ToDrawingPoint(document, curve.Center, request.CenterDrawing.Z, curve.ElementId + "/center"),
                                    Vector3d.ZAxis,
                                    radiusDrawing,
                                    curve.StartAngleRad,
                                    curve.StartAngleRad + curve.SweepAngleRad);
                            }
                            else
                                throw new InvalidOperationException("Radial Grid planner emitted an unsupported curve kind for " + curve.ElementId + ".");

                            entity.LayerId = document.Database.Clayer;
                            currentSpace.AppendEntity(entity);
                            transaction.AddNewlyCreatedDBObject(entity, true);
                            BindSemantic(project, family, systemKey, curve, entity.Handle.ToString(), request.RayCount, index);
                            entity = null;
                        }
                        finally { entity?.Dispose(); }
                    }

                    project.Touch();
                    transaction.Commit();
                }
            }
            catch (Exception operationError)
            {
                try { rollback.Restore(project); }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        "Radial Grid native authoring failed and semantic rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }

            try { document.Editor.Regen(); } catch { }
            return new RadialGridNativeResult { SystemKey = systemKey, Curves = materialization.Count, Replaced = replaced };
        }

        private static List<ExistingSource> ValidateExistingSources(Database database, Transaction transaction, ProjectState project, string systemKey, HashSet<string> desiredIds)
        {
            var result = new List<ExistingSource>();
            var seenHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var owned = project.Elements.Where(x => x.Category == ElementCategory.Grid && IsOwnedRadial(x, systemKey)).ToList();
            foreach (var element in owned)
            {
                if (element.SourceHandles.Count != 1)
                    throw new InvalidOperationException("Radial Grid " + element.Id + " must have exactly one authoritative native source before replacement.");
                var handle = CadHandleService.NormalizeHexHandle(element.SourceHandles[0]);
                if (handle == null || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Radial Grid " + element.Id + " has an invalid source handle.");
                if (!seenHandles.Add(handle))
                    throw new InvalidOperationException("Radial Grid system contains duplicate authoritative source handle " + handle + ".");
                ObjectId id;
                try { id = database.GetObjectId(false, new Handle(value), 0); }
                catch (Exception ex) { throw new InvalidOperationException("Cannot resolve Radial Grid source " + element.Id + "/" + handle + ".", ex); }
                if (id.IsNull || !id.IsValid)
                    throw new InvalidOperationException("Cannot resolve Radial Grid source " + element.Id + "/" + handle + ".");
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException("Radial Grid authoritative source is not live: " + element.Id + ".");
                if (entity.OwnerId != database.CurrentSpaceId)
                    throw new InvalidOperationException("Radial Grid system spans a different owner space and cannot be replaced atomically: " + element.Id + ".");
                var family = StationFamily(element);
                if ((family == "RAY" && !(entity is Line)) || (family == "RING" && !(entity is Arc)))
                    throw new InvalidOperationException("Radial Grid authoritative source type no longer matches semantic family: " + element.Id + ".");
                result.Add(new ExistingSource(element, id));
            }

            foreach (var desiredId in desiredIds)
            {
                var collision = project.Elements.FirstOrDefault(x => string.Equals(x.Id, desiredId, StringComparison.OrdinalIgnoreCase));
                if (collision != null && !IsOwnedRadial(collision, systemKey))
                    throw new InvalidOperationException("Radial Grid semantic id is already owned by another element: " + desiredId + ".");
            }
            return result;
        }

        private static void BindSemantic(ProjectState project, ProjectFamily family, string systemKey, GridReferenceCurve curve, string handle, int rayCount, int planIndex)
        {
            var element = project.Elements.FirstOrDefault(x => string.Equals(x.Id, curve.ElementId, StringComparison.OrdinalIgnoreCase));
            if (element == null)
            {
                element = new ProjectElement(curve.ElementId, ElementCategory.Grid, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                project.Elements.Add(element);
            }
            else if (!IsOwnedRadial(element, systemKey))
                throw new InvalidOperationException("Radial Grid semantic id collision: " + curve.ElementId + ".");

            var stationFamily = planIndex < rayCount ? "RAY" : "RING";
            var stationIndex = planIndex < rayCount ? planIndex : planIndex - rayCount;
            var coordinate = stationFamily == "RAY" ? NormalizeAngle(curve.StartAngleRad) : curve.Radius;
            element.Category = ElementCategory.Grid;
            element.FamilyId = family.Id;
            element.FloorId = project.ActiveFloorId;
            element.ZoneId = project.ActiveZoneId;
            element.DrawingFingerprint = project.DrawingFingerprint;
            element.SourceHandles.Clear();
            element.SourceHandles.Add(handle);
            element.Properties[SystemKeyProperty] = systemKey;
            element.Properties[SystemKindProperty] = "RADIAL";
            element.Properties[StationFamilyProperty] = stationFamily;
            element.Properties[StationIndexProperty] = (stationIndex + 1).ToString(CultureInfo.InvariantCulture);
            element.Properties[StationCoordinateProperty] = coordinate.ToString("R", CultureInfo.InvariantCulture);
            element.Properties["Layer"] = "CURRENT";
            element.MarkDirty(ElementDirtyFlags.All);
        }

        private static ProjectFamily ResolveGridFamily(ProjectState project)
        {
            var active = ProjectFamilyActivationService.GetActive(project);
            if (active != null && active.Category == ElementCategory.Grid && active.Name.IndexOf("Lưới Cong", StringComparison.OrdinalIgnoreCase) >= 0) return active;
            var existing = project.Families.FirstOrDefault(x => x.Category == ElementCategory.Grid && x.Name.IndexOf("Lưới Cong", StringComparison.OrdinalIgnoreCase) >= 0);
            if (existing != null) return existing;
            const string id = "FAM-GRID-RADIAL";
            if (project.FindFamily(id) != null)
                throw new InvalidOperationException("Family id " + id + " is already used by a non-Radial Grid family.");
            var family = new ProjectFamily(id, "Lưới Cong - Radial", ElementCategory.Grid);
            project.Families.Add(family);
            return family;
        }

        private static IReadOnlyList<GridAngularStation> CreateRays(string systemKey, int count, double firstAngleRad, double stepRad)
        {
            if (!Finite(stepRad) || !(stepRad > 0d)) throw new ArgumentOutOfRangeException(nameof(stepRad));
            var result = new List<GridAngularStation>(count);
            for (var index = 0; index < count; index++)
            {
                var angle = firstAngleRad + checked(index * stepRad);
                if (!Finite(angle)) throw new OverflowException("Radial Grid ray angle overflowed.");
                result.Add(new GridAngularStation(StationId(systemKey, "RAY", index + 1), angle));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<GridRadialStation> CreateRings(string systemKey, int count, double firstRadiusM, double spacingM)
        {
            var result = new List<GridRadialStation>(count);
            for (var index = 0; index < count; index++)
            {
                var radius = firstRadiusM + checked(index * spacingM);
                if (!Finite(radius)) throw new OverflowException("Radial Grid ring radius overflowed.");
                result.Add(new GridRadialStation(StationId(systemKey, "RING", index + 1), radius));
            }
            return result.AsReadOnly();
        }

        private static string StationId(string systemKey, string family, int oneBasedIndex) =>
            "GRIDRAD:" + systemKey + ":" + family + ":" + oneBasedIndex.ToString("D3", CultureInfo.InvariantCulture);

        private static double CheckedOuterRadius(double firstRadiusM, int count, double spacingM)
        {
            var outer = firstRadiusM + (count - 1) * spacingM;
            if (!Finite(outer) || !(outer > 0d)) throw new OverflowException("Radial Grid outer radius is invalid.");
            return outer;
        }

        private static Point3d ToDrawingPoint(Document document, Point2 pointM, double zDrawing, string label) =>
            new Point3d(
                CadGeometryGuard.ToDrawingUnits(document, pointM.X, label + " X"),
                CadGeometryGuard.ToDrawingUnits(document, pointM.Y, label + " Y"),
                zDrawing);

        private static bool IsOwnedRadial(ProjectElement element, string systemKey) =>
            element.Properties.TryGetValue(SystemKeyProperty, out var key) && string.Equals(key, systemKey, StringComparison.Ordinal) &&
            element.Properties.TryGetValue(SystemKindProperty, out var kind) && string.Equals(kind, "RADIAL", StringComparison.Ordinal) &&
            (StationFamily(element) == "RAY" || StationFamily(element) == "RING");

        private static string StationFamily(ProjectElement element) =>
            element.Properties.TryGetValue(StationFamilyProperty, out var value) ? (value ?? string.Empty).Trim().ToUpperInvariant() : string.Empty;

        private static string NormalizeSystemKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException("Radial Grid system key is required.", nameof(raw));
            var value = raw.Trim().ToLowerInvariant();
            if (!string.Equals(value, raw, StringComparison.Ordinal))
                throw new ArgumentException("Radial Grid system key must already be canonical lowercase without surrounding whitespace.", nameof(raw));
            if (value.Length > 48) throw new ArgumentException("Radial Grid system key exceeds 48 characters.", nameof(raw));
            foreach (var ch in value)
                if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.'))
                    throw new ArgumentException("Radial Grid system key may contain only letters, digits, '-', '_' or '.'.", nameof(raw));
            return value;
        }

        private static void ValidateCount(int count, string family)
        {
            if (count < 1 || count > MaxStationsPerFamily)
                throw new ArgumentOutOfRangeException(family + "Count", "Radial Grid " + family + " count must be in [1, " + MaxStationsPerFamily + "].");
        }

        private static void ValidatePositive(double value, string label)
        {
            if (!Finite(value) || !(value > 0d)) throw new ArgumentOutOfRangeException(label, label + " must be finite and positive.");
        }

        private static void RequireFinite(Point3d point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y) || !Finite(point.Z))
                throw new InvalidOperationException(label + " must be finite.");
        }

        private static double NormalizeAngle(double angle)
        {
            var result = angle % TwoPi;
            if (result < 0d) result += TwoPi;
            return result;
        }

        private static double Hypot(double x, double y)
        {
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var scale = Math.Max(ax, ay);
            if (scale == 0d) return 0d;
            var ratio = Math.Min(ax, ay) / scale;
            var result = scale * Math.Sqrt(1d + ratio * ratio);
            return Finite(result) ? result : double.PositiveInfinity;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
