using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CurtainWallPanelLiveStateService
    {
        private const string HandlesKey = "GeneratedCurtainPanelHandles";
        private const string FingerprintKey = "GeneratedCurtainPanelLiveFingerprint";

        public static int StampSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var stamped = new List<Tuple<ProjectElement, string>>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline))) continue;
                    var handle = source.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.GlassWall &&
                                    HasPanelBuild(x) &&
                                    x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("GlassWall source " + handle + " has ambiguous curtain-panel ownership.");
                    var element = matches[0];
                    stamped.Add(Tuple.Create(element, CurtainWallFrameLiveFingerprint.Compute(document, transaction, project, element, source)));
                }
                transaction.Commit();
            }
            foreach (var item in stamped) item.Item1.Properties[FingerprintKey] = item.Item2;
            if (stamped.Count > 0) project.Touch();
            return stamped.Count;
        }

        public static int TryStampSelected(Document document, ProjectState project, out string warning)
        {
            warning = string.Empty;
            try { return StampSelected(document, project); }
            catch (Exception ex)
            {
                warning = "Cannot stamp live curtain-panel fingerprint: " + ex.Message;
                return 0;
            }
        }

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var element in project.Elements.Where(x => x.Category == ElementCategory.GlassWall && HasPanelBuild(x)))
                {
                    if (!element.Properties.TryGetValue(FingerprintKey, out var stored) || string.IsNullOrWhiteSpace(stored))
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_PANEL_LIVE_FINGERPRINT_MISSING", HealthSeverity.Warning, "Missing live CAD fingerprint for curtain panels; rebuild before release.", element.Id));
                        continue;
                    }
                    var ids = CadHandleService.Resolve(document, element.SourceHandles);
                    if (ids.Count != 1)
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_PANEL_LIVE_SOURCE_INVALID", HealthSeverity.Error, "Curtain panel live check requires exactly one live LINE/POLYLINE source.", element.Id));
                        continue;
                    }
                    var source = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased || (!(source is Line) && !(source is Polyline)))
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_PANEL_LIVE_SOURCE_INVALID", HealthSeverity.Error, "Curtain panel live source is no longer a valid LINE/POLYLINE.", element.Id));
                        continue;
                    }
                    try
                    {
                        var current = CurtainWallFrameLiveFingerprint.Compute(document, transaction, project, element, source);
                        if (!string.Equals(current, stored.Trim(), StringComparison.OrdinalIgnoreCase))
                            issues.Add(new ModelHealthIssue("CURTAIN_PANEL_LIVE_GEOMETRY_STALE", HealthSeverity.Warning, "GlassWall/opening CAD geometry changed after curtain panels were built.", element.Id));
                        if (element.Properties.TryGetValue("GeneratedCurtainPanelConfigFingerprint", out var storedConfig) && !string.IsNullOrWhiteSpace(storedConfig))
                        {
                            var currentConfig = ComputeConfigFingerprint(document, transaction, project, element, source);
                            if (!string.Equals(currentConfig, storedConfig.Trim(), StringComparison.OrdinalIgnoreCase))
                                issues.Add(new ModelHealthIssue("CURTAIN_PANEL_CONFIG_STALE", HealthSeverity.Warning, "Curtain panel layout, opening, thickness, or path tessellation configuration changed after the native panels were built.", element.Id));
                        }
                        if (source is Polyline)
                        {
                            var currentSagitta = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                            if (!element.Properties.TryGetValue("GeneratedCurtainPanelPathSagittaM", out var storedSagitta) ||
                                !double.TryParse(storedSagitta, NumberStyles.Float, CultureInfo.InvariantCulture, out var previousSagitta) ||
                                double.IsNaN(previousSagitta) || double.IsInfinity(previousSagitta) || previousSagitta != currentSagitta)
                                issues.Add(new ModelHealthIssue("CURTAIN_PANEL_CONFIG_STALE", HealthSeverity.Warning, "Curtain panel path sagitta configuration differs from the last native panel build.", element.Id));
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add(new ModelHealthIssue("CURTAIN_PANEL_LIVE_GEOMETRY_INVALID", HealthSeverity.Warning, "Cannot inspect live curtain-panel geometry: " + ex.Message, element.Id));
                    }
                }
                transaction.Commit();
            }
            return issues.AsReadOnly();
        }

        private static bool HasPanelBuild(ProjectElement element) =>
            (element.Properties.TryGetValue("GeneratedCurtainPanelBuildState", out var state) &&
             string.Equals((state ?? string.Empty).Trim(), "Complete", StringComparison.OrdinalIgnoreCase)) ||
            (element.Properties.TryGetValue(HandlesKey, out var handles) && !string.IsNullOrWhiteSpace(handles));

        private static string ComputeConfigFingerprint(Document document, Transaction transaction, ProjectState project, ProjectElement element, Entity source)
        {
            var family = project.FindFamily(element.FamilyId);
            double sourceBaseDrawing;
            if (source is Line sourceLine) sourceBaseDrawing = sourceLine.StartPoint.Z;
            else if (source is Polyline sourcePolyline) sourceBaseDrawing = sourcePolyline.Elevation;
            else throw new InvalidOperationException("Curtain panel config fingerprint supports only LINE/POLYLINE sources.");
            var verticalPlacement = CadElementVerticalPlacement.Resolve(
                document,
                project,
                element,
                family,
                sourceBaseDrawing,
                "HeightM",
                3.6d);
            var heightM = verticalPlacement.HeightM;
            var panelDepthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", 0.012d), element.Id + "/ThicknessM");
            double lengthM;
            string sourceKind;
            int pathSegmentCount;
            IReadOnlyList<CurtainWallPanelPiece> pieces;

            if (source is Line line)
            {
                var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, element.Id + "/panel dx");
                var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, element.Id + "/panel dy");
                var dz = CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z, element.Id + "/panel dz");
                var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/panel length");
                if (Math.Abs(CadGeometryGuard.ToMeters(document, dz, element.Id + "/panel dz")) > 1e-6d)
                    throw new InvalidOperationException("Curtain panel LINE must be horizontal.");
                lengthM = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, lengthDrawing, element.Id + "/LengthM"), element.Id + "/LengthM");
                var detail = CurtainWallDetailPlanner.Plan(LayoutInput(element, family, lengthM, heightM));
                var ux = dx / lengthDrawing;
                var uy = dy / lengthDrawing;
                var openings = CurtainWallPanelBuilderSupport.ReadLineOpenings(document, transaction, project, element, verticalPlacement, line, ux, uy, lengthM, heightM, panelDepthM);
                pieces = CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d).Pieces;
                sourceKind = "Line";
                pathSegmentCount = 0;
            }
            else if (source is Polyline polyline)
            {
                var centerline = CadPolylinePathReader.ReadOpenWcsXy(document, polyline, ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d), element.Id + "/curtain panel path");
                lengthM = CadGeometryGuard.Positive(CurtainPathFramePlanner.Length(centerline), element.Id + "/LengthM");
                var detail = CurtainWallDetailPlanner.Plan(LayoutInput(element, family, lengthM, heightM));
                var openings = CurtainWallPanelBuilderSupport.ReadPathOpenings(document, transaction, project, element, verticalPlacement, centerline, lengthM, heightM, panelDepthM);
                pieces = CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d).Pieces;
                pathSegmentCount = CurtainPathFramePlanner.Plan(centerline, pieces.Select(x => new CurtainWallRect(x.X_M, x.Z_M, x.WidthM, x.HeightM)).ToList()).PathSegmentCount;
                sourceKind = "OpenPolyline";
            }
            else
            {
                throw new InvalidOperationException("Curtain panel config fingerprint supports only LINE/POLYLINE sources.");
            }

            return CurtainWallPanelFingerprint.Compute(new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = lengthM,
                HeightM = heightM,
                BottomOffsetM = verticalPlacement.FingerprintBottomM,
                PanelDepthM = panelDepthM,
                SourceKind = sourceKind,
                PathSegmentCount = pathSegmentCount,
                Pieces = pieces
            });
        }

        private static CurtainWallLayoutInput LayoutInput(ProjectElement element, ProjectFamily? family, double lengthM, double heightM) => new CurtainWallLayoutInput
        {
            LengthM = lengthM,
            HeightM = heightM,
            MaxPanelWidthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelWidthM", 1.2d), element.Id + "/CurtainMaxPanelWidthM"),
            MaxPanelHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelHeightM", 1.5d), element.Id + "/CurtainMaxPanelHeightM"),
            PerimeterFrameWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d), element.Id + "/CurtainPerimeterFrameWidthM"),
            MullionWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainMullionWidthM", 0.05d), element.Id + "/CurtainMullionWidthM"),
            TransomWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainTransomWidthM", 0.05d), element.Id + "/CurtainTransomWidthM")
        };

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) return fallback;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException("Project metadata " + key + " is invalid: " + text);
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            value = CadGeometryGuard.Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " must be >= 0.");
            return value;
        }
    }
}
