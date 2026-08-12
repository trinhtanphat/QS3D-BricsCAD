using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class CurtainPanelBuildResult
    {
        public int Elements { get; set; }
        public int Panels { get; set; }
    }

    internal static class CurtainWallPanelBuilderSupport
    {
        internal const string HandlesKey = "GeneratedCurtainPanelHandles";

        public static ProjectElement? FindElement(ProjectState project, string sourceHandle)
        {
            var matches = project.Elements
                .Where(x => x.Category == ElementCategory.GlassWall && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                .Take(2)
                .ToList();
            if (matches.Count == 0) return null;
            if (matches.Count > 1) throw new InvalidOperationException("GlassWall source " + sourceHandle + " belongs to multiple semantic elements.");
            return matches[0];
        }

        public static IReadOnlyList<CurtainWallOpeningRect> ReadLineOpenings(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            Line hostLine,
            double ux,
            double uy,
            double hostLengthM,
            double hostLegacyHeightM,
            double hostLegacyBottomOffsetM,
            double hostThicknessM)
        {
            var result = new List<CurtainWallOpeningRect>();
            var maximumOffsetDrawing = CadGeometryGuard.ToDrawingUnits(document, hostThicknessM / 2d + 0.25d, host.Id + "/panel opening proximity");
            foreach (var opening in LinkedOpenings(project, host))
            {
                var openingFamily = project.FindFamily(opening.FamilyId);
                var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "WidthM", 0d), opening.Id + "/WidthM");
                var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "HeightM", 0d), opening.Id + "/HeightM");
                var sillM = NonNegative(CadGeometryGuard.Number(opening, openingFamily, "SillHeightM", opening.Category == ElementCategory.Door ? 0d : 0.9d), opening.Id + "/SillHeightM");
                var clearanceM = NonNegative(CadGeometryGuard.Number(opening, openingFamily, "BooleanClearanceM", 0.01d), opening.Id + "/BooleanClearanceM");
                var hostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(
                    document,
                    project,
                    host,
                    opening,
                    hostLine.StartPoint.Z,
                    hostLegacyHeightM,
                    hostLegacyBottomOffsetM,
                    heightM,
                    sillM);
                var entity = RequireSingleLiveOpening(document, transaction, opening);
                var extents = Extents(entity, opening.Id);
                var centerX = CadGeometryGuard.Midpoint(extents.MinPoint.X, extents.MaxPoint.X, opening.Id + "/opening center X");
                var centerY = CadGeometryGuard.Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y, opening.Id + "/opening center Y");
                var fromStartX = CadGeometryGuard.Subtract(centerX, hostLine.StartPoint.X, opening.Id + "/from start X");
                var fromStartY = CadGeometryGuard.Subtract(centerY, hostLine.StartPoint.Y, opening.Id + "/from start Y");
                var alongDrawing = CadGeometryGuard.Add(CadGeometryGuard.Multiply(fromStartX, ux, opening.Id + "/along X"), CadGeometryGuard.Multiply(fromStartY, uy, opening.Id + "/along Y"), opening.Id + "/along host");
                var perpendicularDrawing = Math.Abs(CadGeometryGuard.Add(CadGeometryGuard.Multiply(fromStartX, -uy, opening.Id + "/perp X"), CadGeometryGuard.Multiply(fromStartY, ux, opening.Id + "/perp Y"), opening.Id + "/perpendicular distance"));
                if (perpendicularDrawing > maximumOffsetDrawing)
                    throw new InvalidOperationException("Linked opening " + opening.Id + " is too far from the GlassWall centerline for safe panel clipping.");
                var cut = OpeningCutPlanner.Plan(new OpeningCutInput
                {
                    HostLengthM = hostLengthM,
                    HostThicknessM = hostThicknessM,
                    HostHeightM = hostedPlacement.Host.HeightM,
                    OpeningWidthM = widthM,
                    OpeningHeightM = hostedPlacement.Opening.HeightM,
                    SillHeightM = hostedPlacement.RelativeSillM,
                    CenterAlongHostM = CadGeometryGuard.ToMeters(document, alongDrawing, opening.Id + "/center along host"),
                    ClearanceM = clearanceM
                });
                result.Add(new CurtainWallOpeningRect
                {
                    X_M = cut.CenterAlongHostM - cut.CutterWidthM / 2d,
                    Z_M = cut.BaseElevationM,
                    WidthM = cut.CutterWidthM,
                    HeightM = cut.CutterHeightM
                });
            }
            return result.AsReadOnly();
        }

        public static IReadOnlyList<CurtainWallOpeningRect> ReadPathOpenings(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            IReadOnlyList<Point2> centerline,
            double sourceBaseDrawing,
            double hostLengthM,
            double hostLegacyHeightM,
            double hostLegacyBottomOffsetM,
            double hostThicknessM)
        {
            var result = new List<CurtainWallOpeningRect>();
            var maximumOffsetM = hostThicknessM / 2d + 0.25d;
            foreach (var opening in LinkedOpenings(project, host))
            {
                var openingFamily = project.FindFamily(opening.FamilyId);
                var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "WidthM", 0d), opening.Id + "/WidthM");
                var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "HeightM", 0d), opening.Id + "/HeightM");
                var sillM = NonNegative(CadGeometryGuard.Number(opening, openingFamily, "SillHeightM", opening.Category == ElementCategory.Door ? 0d : 0.9d), opening.Id + "/SillHeightM");
                var clearanceM = NonNegative(CadGeometryGuard.Number(opening, openingFamily, "BooleanClearanceM", 0.01d), opening.Id + "/BooleanClearanceM");
                var hostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(
                    document,
                    project,
                    host,
                    opening,
                    sourceBaseDrawing,
                    hostLegacyHeightM,
                    hostLegacyBottomOffsetM,
                    heightM,
                    sillM);
                var entity = RequireSingleLiveOpening(document, transaction, opening);
                var extents = Extents(entity, opening.Id);
                var center = new Point2(
                    CadGeometryGuard.ToMeters(document, CadGeometryGuard.Midpoint(extents.MinPoint.X, extents.MaxPoint.X, opening.Id + "/center X"), opening.Id + "/center X meters"),
                    CadGeometryGuard.ToMeters(document, CadGeometryGuard.Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y, opening.Id + "/center Y"), opening.Id + "/center Y meters"));
                var projection = CurtainPathFramePlanner.ProjectPoint(centerline, center);
                if (projection.DistanceM > maximumOffsetM)
                    throw new InvalidOperationException("Linked opening " + opening.Id + " is too far from the GlassWall path for safe panel clipping.");
                var cut = OpeningCutPlanner.Plan(new OpeningCutInput
                {
                    HostLengthM = hostLengthM,
                    HostThicknessM = hostThicknessM,
                    HostHeightM = hostedPlacement.Host.HeightM,
                    OpeningWidthM = widthM,
                    OpeningHeightM = hostedPlacement.Opening.HeightM,
                    SillHeightM = hostedPlacement.RelativeSillM,
                    CenterAlongHostM = projection.StationM,
                    ClearanceM = clearanceM
                });
                result.Add(new CurtainWallOpeningRect
                {
                    X_M = cut.CenterAlongHostM - cut.CutterWidthM / 2d,
                    Z_M = cut.BaseElevationM,
                    WidthM = cut.CutterWidthM,
                    HeightM = cut.CutterHeightM
                });
            }
            return result.AsReadOnly();
        }

        public static Solid3d CreateLinePanel(Document document, Line line, CurtainWallPanelPiece panel, double depthM, double baseZ, double angle, double ux, double uy, string label)
        {
            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, panel.WidthM, label + "/panel width"), label + "/panel width drawing");
            var depth = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, depthM, label + "/panel depth"), label + "/panel depth drawing");
            var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, panel.HeightM, label + "/panel height"), label + "/panel height drawing");
            var centerStation = CadGeometryGuard.ToDrawingUnits(document, panel.X_M + panel.WidthM / 2d, label + "/panel station");
            var centerZ = CadGeometryGuard.Add(baseZ, CadGeometryGuard.ToDrawingUnits(document, panel.Z_M + panel.HeightM / 2d, label + "/panel Z"), label + "/panel center Z");
            var centerX = CadGeometryGuard.Add(line.StartPoint.X, CadGeometryGuard.Multiply(ux, centerStation, label + "/panel center dx"), label + "/panel center X");
            var centerY = CadGeometryGuard.Add(line.StartPoint.Y, CadGeometryGuard.Multiply(uy, centerStation, label + "/panel center dy"), label + "/panel center Y");
            return CreateBox(document, width, depth, height, centerX, centerY, centerZ, angle);
        }

        public static Solid3d CreatePathPanel(Document document, CurtainPathFramePiece piece, double depthM, double baseZ, string label)
        {
            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, piece.WidthM, label + "/path panel width"), label + "/path panel width drawing");
            var depth = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, depthM, label + "/path panel depth"), label + "/path panel depth drawing");
            var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, piece.HeightM, label + "/path panel height"), label + "/path panel height drawing");
            var centerX = CadGeometryGuard.ToDrawingUnits(document, piece.CenterX_M, label + "/path panel center X");
            var centerY = CadGeometryGuard.ToDrawingUnits(document, piece.CenterY_M, label + "/path panel center Y");
            var centerZ = CadGeometryGuard.Add(baseZ, CadGeometryGuard.ToDrawingUnits(document, piece.Z_M + piece.HeightM / 2d, label + "/path panel Z"), label + "/path panel center Z");
            return CreateBox(document, width, depth, height, centerX, centerY, centerZ, CadGeometryGuard.Finite(piece.AngleRadians, label + "/path panel angle"));
        }

        public static IReadOnlyList<KeyValuePair<string, ObjectId>> ValidatePrevious(Document document, Transaction transaction, ProjectState project, ProjectElement element, GeneratedCurtainPanelOwnershipGuard.OwnershipIndex ownership)
        {
            var hasBuildState = element.Properties.TryGetValue("GeneratedCurtainPanelBuildState", out var state);
            var hasHandles = element.Properties.TryGetValue(HandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw);
            if (!hasBuildState && !hasHandles) return Array.Empty<KeyValuePair<string, ObjectId>>();
            if (!hasBuildState || !string.Equals((state ?? string.Empty).Trim(), "Complete", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Generated curtain panel build state is missing or invalid. Refusing replacement before any panel is erased or appended.");
            if (!element.Properties.TryGetValue("GeneratedCurtainPanelCount", out var countRaw) ||
                !int.TryParse(countRaw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var recordedCount) ||
                recordedCount < 0)
                throw new InvalidOperationException("Generated curtain panel count is missing or invalid. Refusing replacement.");
            if (!hasHandles)
            {
                if (recordedCount == 0) return Array.Empty<KeyValuePair<string, ObjectId>>();
                throw new InvalidOperationException("Generated curtain panel metadata records " + recordedCount + " panels but has no handles. Refusing replacement to avoid orphaning native solids.");
            }
            var expected = new List<KeyValuePair<string, string>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var original = token.Trim();
                if (original.Length == 0) continue;
                ownership.EnsureOwned(original, element);
                var canonical = CadHandleService.NormalizeHexHandle(original);
                if (canonical == null) throw new InvalidOperationException("Generated curtain panel metadata contains an invalid handle: " + original + ".");
                if (!seen.Add(canonical)) throw new InvalidOperationException("Generated curtain panel metadata contains a duplicate handle: " + canonical + ".");
                expected.Add(new KeyValuePair<string, string>(canonical, original));
            }
            if (expected.Count == 0) throw new InvalidOperationException("Generated curtain panel metadata contains no valid handles.");
            if (recordedCount != expected.Count)
                throw new InvalidOperationException("Generated curtain panel count does not match the unique handle set. Refusing replacement.");
            var ids = CadHandleService.Resolve(document, expected.Select(x => x.Key));
            if (ids.Count != expected.Count) throw new InvalidOperationException("Generated curtain panel live-handle set is incomplete. Refusing destructive replacement.");
            var result = new List<KeyValuePair<string, ObjectId>>(expected.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                var entity = transaction.GetObject(ids[i], OpenMode.ForRead, false) as Entity;
                if (!(entity is Solid3d solid) || solid.IsErased) throw new InvalidOperationException("Generated curtain panel is missing, erased, or not a Solid3d: " + expected[i].Key + ".");
                GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "validate generated curtain panel " + expected[i].Key);
                result.Add(new KeyValuePair<string, ObjectId>(expected[i].Key, ids[i]));
            }
            return result;
        }

        public static void ErasePrevious(Transaction transaction, ProjectState project, ProjectElement element, IReadOnlyList<KeyValuePair<string, ObjectId>> previous)
        {
            foreach (var item in previous)
            {
                var entity = transaction.GetObject(item.Value, OpenMode.ForWrite, false) as Entity;
                if (!(entity is Solid3d solid) || solid.IsErased) throw new InvalidOperationException("Generated curtain panel changed after validation: " + item.Key + ".");
                GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "erase generated curtain panel " + item.Key);
                solid.Erase();
            }
        }

        private static IEnumerable<ProjectElement> LinkedOpenings(ProjectState project, ProjectElement host) => project.Elements
            .Where(x => (x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening) &&
                        x.Properties.TryGetValue("HostWallId", out var hostId) &&
                        string.Equals(hostId, host.Id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase);

        private static Entity RequireSingleLiveOpening(Document document, Transaction transaction, ProjectElement opening)
        {
            var sourceIds = CadHandleService.Resolve(document, opening.SourceHandles);
            if (sourceIds.Count != 1) throw new InvalidOperationException("Linked opening " + opening.Id + " requires exactly one live CAD source for panel clipping.");
            var entity = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Entity;
            return entity == null || entity.IsErased ? throw new InvalidOperationException("Linked opening source is not live: " + opening.Id) : entity;
        }

        private static Extents3d Extents(Entity entity, string label)
        {
            try { return entity.GeometricExtents; }
            catch (Exception ex) { throw new InvalidOperationException("Cannot read extents for linked opening " + label + ".", ex); }
        }

        private static Solid3d CreateBox(Document document, double width, double depth, double height, double centerX, double centerY, double centerZ, double angle)
        {
            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateBox(width, depth, height);
                solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, centerZ)));
                var complete = solid;
                solid = null!;
                return complete;
            }
            finally { solid?.Dispose(); }
        }

        private static double NonNegative(double value, string label)
        {
            value = CadGeometryGuard.Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " must be >= 0.");
            return value;
        }
    }
}
