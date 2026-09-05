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
    internal sealed class RectangularGridNativeRequest
    {
        public string SystemKey { get; set; } = string.Empty;
        public Point3d OriginDrawing { get; set; }
        public Point3d UDirectionPointDrawing { get; set; }
        public int UCount { get; set; }
        public int VCount { get; set; }
        public double USpacingM { get; set; }
        public double VSpacingM { get; set; }
    }

    internal sealed class RectangularGridNativeResult
    {
        public string SystemKey { get; set; } = string.Empty;
        public int Curves { get; set; }
        public int Replaced { get; set; }
    }

    internal static class RectangularGridNativeSourceBuilder
    {
        internal const string SystemKeyProperty = "QS3D.GridSystem.Key";
        internal const string StationFamilyProperty = "QS3D.GridSystem.StationFamily";
        internal const string StationIndexProperty = "QS3D.GridSystem.StationIndex";
        internal const string StationCoordinateProperty = "QS3D.GridSystem.StationCoordinateM";
        private const int MaxStationsPerFamily = 200;
        private const double DirectionTolerance = 1e-9d;

        private sealed class ExistingSource
        {
            public ExistingSource(ProjectElement element, ObjectId objectId, Line line)
            {
                Element = element;
                ObjectId = objectId;
                Line = line;
            }

            public ProjectElement Element { get; }
            public ObjectId ObjectId { get; }
            public Line Line { get; }
        }

        public static RectangularGridNativeResult Build(Document document, ProjectState project, RectangularGridNativeRequest request)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
                throw new InvalidOperationException("Rectangular Grid authoring yêu cầu DWG đích vẫn là MdiActiveDocument.");

            var systemKey = NormalizeSystemKey(request.SystemKey);
            ValidateCount(request.UCount, "U");
            ValidateCount(request.VCount, "V");
            ValidateSpacing(request.USpacingM, "U");
            ValidateSpacing(request.VSpacingM, "V");
            RequireFinite(request.OriginDrawing, "Grid origin");
            RequireFinite(request.UDirectionPointDrawing, "Grid U direction point");
            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Rectangular Grid native authoring");

            var dx = request.UDirectionPointDrawing.X - request.OriginDrawing.X;
            var dy = request.UDirectionPointDrawing.Y - request.OriginDrawing.Y;
            if (!Finite(dx) || !Finite(dy)) throw new InvalidOperationException("Rectangular Grid U direction is not finite.");
            var directionLength = Hypot(dx, dy);
            if (!(directionLength > DirectionTolerance) || !Finite(directionLength))
                throw new InvalidOperationException("Rectangular Grid U direction point must differ from the origin in WCS-XY.");
            var ux = dx / directionLength;
            var uy = dy / directionLength;
            var vx = -uy;
            var vy = ux;

            var originM = new Point2(
                CadGeometryGuard.ToMeters(document, request.OriginDrawing.X, "Rectangular Grid origin X"),
                CadGeometryGuard.ToMeters(document, request.OriginDrawing.Y, "Rectangular Grid origin Y"));
            var uExtent = CheckedExtent(request.UCount, request.USpacingM, "U");
            var vExtent = CheckedExtent(request.VCount, request.VSpacingM, "V");
            var input = new RectangularGridSystemInput
            {
                OriginM = originM,
                UAxis = new Point2(ux, uy),
                VAxis = new Point2(vx, vy),
                UStations = CreateStations(systemKey, "U", request.UCount, request.USpacingM),
                VStations = CreateStations(systemKey, "V", request.VCount, request.VSpacingM),
                UMinM = 0d,
                UMaxM = uExtent,
                VMinM = 0d,
                VMaxM = vExtent
            };
            var planned = GridSystemPlanner.PlanRectangular(input);
            if (planned.Count != request.UCount + request.VCount)
                throw new InvalidOperationException("Rectangular Grid planner returned an unexpected curve count.");

            var family = ResolveGridFamily(project);
            var desiredIds = new HashSet<string>(planned.Select(x => x.ElementId), StringComparer.OrdinalIgnoreCase);
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
                    if (currentSpace == null) throw new InvalidOperationException("Rectangular Grid current space is unavailable.");

                    // All old semantic/native ownership has been validated above. Destructive mutation starts only here.
                    foreach (var source in existing)
                    {
                        var entity = transaction.GetObject(source.ObjectId, OpenMode.ForWrite, false) as Entity;
                        if (entity == null || entity.IsErased)
                            throw new InvalidOperationException("Rectangular Grid source changed after preflight: " + source.Element.Id + ".");
                        entity.Erase();
                    }

                    foreach (var retired in project.Elements
                        .Where(x => x.Category == ElementCategory.Grid && HasSystemKey(x, systemKey) && !desiredIds.Contains(x.Id))
                        .ToList())
                        project.Elements.Remove(retired);

                    for (var index = 0; index < planned.Count; index++)
                    {
                        var plan = planned[index];
                        if (plan.Kind != GridReferenceCurveKind.Line)
                            throw new InvalidOperationException("Rectangular Grid planner emitted a non-LINE curve for " + plan.ElementId + ".");
                        var start = ToDrawingPoint(document, plan.Start, request.OriginDrawing.Z, plan.ElementId + "/start");
                        var end = ToDrawingPoint(document, plan.End, request.OriginDrawing.Z, plan.ElementId + "/end");
                        var line = new Line(start, end);
                        try
                        {
                            line.LayerId = document.Database.Clayer;
                            currentSpace.AppendEntity(line);
                            transaction.AddNewlyCreatedDBObject(line, true);
                            BindSemantic(project, family, systemKey, plan, line.Handle.ToString(), request.UCount, index);
                            line = null!;
                        }
                        finally { line?.Dispose(); }
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
                        "Rectangular Grid native authoring failed and semantic rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }

            try { document.Editor.Regen(); } catch { }
            return new RectangularGridNativeResult { SystemKey = systemKey, Curves = planned.Count, Replaced = replaced };
        }

        private static List<ExistingSource> ValidateExistingSources(
            Database database,
            Transaction transaction,
            ProjectState project,
            string systemKey,
            HashSet<string> desiredIds)
        {
            var result = new List<ExistingSource>();
            var seenHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var owned = project.Elements.Where(x => x.Category == ElementCategory.Grid && HasSystemKey(x, systemKey)).ToList();
            foreach (var element in owned)
            {
                if (element.SourceHandles.Count != 1)
                    throw new InvalidOperationException("Rectangular Grid " + element.Id + " must have exactly one authoritative native LINE source before replacement.");
                var handle = CadHandleService.NormalizeHexHandle(element.SourceHandles[0]);
                if (handle == null || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Rectangular Grid " + element.Id + " has an invalid source handle.");
                if (!seenHandles.Add(handle))
                    throw new InvalidOperationException("Rectangular Grid system contains duplicate authoritative source handle " + handle + ".");
                ObjectId id;
                try { id = database.GetObjectId(false, new Handle(value), 0); }
                catch (Exception ex) { throw new InvalidOperationException("Cannot resolve Rectangular Grid source " + element.Id + "/" + handle + ".", ex); }
                if (id.IsNull || !id.IsValid)
                    throw new InvalidOperationException("Cannot resolve Rectangular Grid source " + element.Id + "/" + handle + ".");
                var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                if (line == null || line.IsErased)
                    throw new InvalidOperationException("Rectangular Grid authoritative source is not a live LINE: " + element.Id + ".");
                if (line.OwnerId != database.CurrentSpaceId)
                    throw new InvalidOperationException("Rectangular Grid system spans a different owner space and cannot be replaced atomically: " + element.Id + ".");
                result.Add(new ExistingSource(element, id, line));
            }

            foreach (var desiredId in desiredIds)
            {
                var collision = project.Elements.FirstOrDefault(x => string.Equals(x.Id, desiredId, StringComparison.OrdinalIgnoreCase));
                if (collision != null && !HasSystemKey(collision, systemKey))
                    throw new InvalidOperationException("Rectangular Grid semantic id is already owned by another element: " + desiredId + ".");
            }
            return result;
        }

        private static void BindSemantic(
            ProjectState project,
            ProjectFamily family,
            string systemKey,
            GridReferenceCurve plan,
            string handle,
            int uCount,
            int planIndex)
        {
            var element = project.Elements.FirstOrDefault(x => string.Equals(x.Id, plan.ElementId, StringComparison.OrdinalIgnoreCase));
            if (element == null)
            {
                element = new ProjectElement(plan.ElementId, ElementCategory.Grid, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                project.Elements.Add(element);
            }
            else if (!HasSystemKey(element, systemKey))
                throw new InvalidOperationException("Rectangular Grid semantic id collision: " + plan.ElementId + ".");

            element.Category = ElementCategory.Grid;
            element.FamilyId = family.Id;
            element.FloorId = project.ActiveFloorId;
            element.ZoneId = project.ActiveZoneId;
            element.DrawingFingerprint = project.DrawingFingerprint;
            element.SourceHandles.Clear();
            element.SourceHandles.Add(handle);
            element.Properties[SystemKeyProperty] = systemKey;
            var stationFamily = planIndex < uCount ? "U" : "V";
            var stationIndex = planIndex < uCount ? planIndex : planIndex - uCount;
            var coordinate = stationFamily == "U" ? plan.Start : plan.End;
            element.Properties[StationFamilyProperty] = stationFamily;
            element.Properties[StationIndexProperty] = (stationIndex + 1).ToString(CultureInfo.InvariantCulture);
            element.Properties[StationCoordinateProperty] = (stationFamily == "U" ? coordinate.X : coordinate.Y).ToString("R", CultureInfo.InvariantCulture);
            element.Properties["Layer"] = "CURRENT";
            element.MarkDirty(ElementDirtyFlags.All);
        }

        private static ProjectFamily ResolveGridFamily(ProjectState project)
        {
            var active = ProjectFamilyActivationService.GetActive(project);
            if (active != null && active.Category == ElementCategory.Grid) return active;
            var existing = project.Families.FirstOrDefault(x => x.Category == ElementCategory.Grid);
            if (existing != null) return existing;
            var id = "FAM-GRID-RECTANGULAR";
            if (project.FindFamily(id) != null)
                throw new InvalidOperationException("Family id " + id + " is already used by a non-Grid family.");
            var family = new ProjectFamily(id, "Lưới Thẳng - Rectangular", ElementCategory.Grid);
            project.Families.Add(family);
            return family;
        }

        private static IReadOnlyList<GridLinearStation> CreateStations(string systemKey, string family, int count, double spacingM)
        {
            var result = new List<GridLinearStation>(count);
            for (var index = 0; index < count; index++)
            {
                var coordinate = checked(index * spacingM);
                if (!Finite(coordinate)) throw new OverflowException("Rectangular Grid " + family + " station coordinate overflowed.");
                result.Add(new GridLinearStation(StationId(systemKey, family, index + 1), coordinate));
            }
            return result.AsReadOnly();
        }

        private static string StationId(string systemKey, string family, int oneBasedIndex) =>
            "GRIDRECT:" + systemKey + ":" + family + ":" + oneBasedIndex.ToString("D3", CultureInfo.InvariantCulture);

        private static Point3d ToDrawingPoint(Document document, Point2 pointM, double zDrawing, string label) =>
            new Point3d(
                CadGeometryGuard.ToDrawingUnits(document, pointM.X, label + " X"),
                CadGeometryGuard.ToDrawingUnits(document, pointM.Y, label + " Y"),
                zDrawing);

        private static bool HasSystemKey(ProjectElement element, string systemKey) =>
            element.Properties.TryGetValue(SystemKeyProperty, out var value) &&
            string.Equals(value, systemKey, StringComparison.Ordinal);

        private static string NormalizeSystemKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException("Rectangular Grid system key is required.", nameof(raw));
            var value = raw.Trim().ToLowerInvariant();
            if (!string.Equals(value, raw, StringComparison.Ordinal))
                throw new ArgumentException("Rectangular Grid system key must already be canonical lowercase without surrounding whitespace.", nameof(raw));
            if (value.Length > 48) throw new ArgumentException("Rectangular Grid system key exceeds 48 characters.", nameof(raw));
            foreach (var ch in value)
                if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.'))
                    throw new ArgumentException("Rectangular Grid system key may contain only letters, digits, '-', '_' or '.'.", nameof(raw));
            return value;
        }

        private static void ValidateCount(int count, string family)
        {
            if (count < 2 || count > MaxStationsPerFamily)
                throw new ArgumentOutOfRangeException(family + "Count", "Rectangular Grid " + family + " count must be in [2, " + MaxStationsPerFamily + "].");
        }

        private static void ValidateSpacing(double spacing, string family)
        {
            if (!Finite(spacing) || !(spacing > 0d))
                throw new ArgumentOutOfRangeException(family + "SpacingM", "Rectangular Grid " + family + " spacing must be finite and positive.");
        }

        private static double CheckedExtent(int count, double spacing, string family)
        {
            var extent = (count - 1) * spacing;
            if (!Finite(extent) || !(extent > 0d)) throw new OverflowException("Rectangular Grid " + family + " extent is invalid.");
            return extent;
        }

        private static void RequireFinite(Point3d point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y) || !Finite(point.Z))
                throw new InvalidOperationException(label + " must be finite.");
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
