using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CurtainWallPathPanelSolidBuilder
    {
        private const string Mode = "PathPanelSolids";
        private const string OpeningAwareMode = "PathPanelSolids.OpeningAware";
        private const int MaxPanelsPerElement = 4096;
        private const int MaxPanelsPerBatch = 8192;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public int Columns { get; set; }
            public int Rows { get; set; }
            public int BasePanelCount { get; set; }
            public int OpeningCount { get; set; }
            public int PathSegmentCount { get; set; }
            public int MappedCount { get; set; }
            public double PathSagittaM { get; set; }
            public double PanelDepthM { get; set; }
            public double SourceLengthM { get; set; }
            public double HeightM { get; set; }
            public double AreaM2 { get; set; }
            public string ConfigFingerprint { get; set; } = string.Empty;
        }

        public static CurtainPanelBuildResult BuildSelectedOpenPolylines(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return new CurtainPanelBuildResult();
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return new CurtainPanelBuildResult();

            var ownership = GeneratedCurtainPanelOwnershipGuard.Build(project);
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new List<PendingUpdate>();
            var batchPanels = 0;
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;
            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    foreach (var id in ids)
                    {
                        var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                        if (polyline == null || polyline.IsErased) continue;
                        var element = CurtainWallPanelBuilderSupport.FindElement(project, polyline.Handle.ToString());
                        if (element == null) continue;
                        if (!processed.Add(element.Id)) throw new InvalidOperationException("GlassWall " + element.Id + " has multiple selected POLYLINE sources.");

                        var family = project.FindFamily(element.FamilyId);
                        var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                        var centerline = CadPolylinePathReader.ReadOpenWcsXy(document, polyline, sagittaM, element.Id + "/curtain panel path");
                        var lengthM = CadGeometryGuard.Positive(CurtainPathFramePlanner.Length(centerline), element.Id + "/LengthM");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                        var panelDepthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", 0.012d), element.Id + "/ThicknessM");
                        var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var input = LayoutInput(element, family, lengthM, heightM);
                        var detail = CurtainWallDetailPlanner.Plan(input);
                        if (detail.Panels.Count > MaxPanelsPerElement) throw new InvalidOperationException(element.Id + " base panel count exceeds " + MaxPanelsPerElement + ".");
                        var openings = CurtainWallPanelBuilderSupport.ReadPathOpenings(document, transaction, project, element, centerline, lengthM, heightM, panelDepthM);
                        var panelPlan = CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d);
                        var rectangles = panelPlan.Pieces.Select(x => new CurtainWallRect(x.X_M, x.Z_M, x.WidthM, x.HeightM)).ToList();
                        var pathPlan = CurtainPathFramePlanner.Plan(centerline, rectangles);
                        var panelCount = pathPlan.Pieces.Count;
                        if (panelCount > MaxPanelsPerElement || batchPanels > MaxPanelsPerBatch - panelCount)
                            throw new InvalidOperationException("Curtain path panel native output exceeds the bounded panel budget.");

                        var previous = CurtainWallPanelBuilderSupport.ValidatePrevious(document, transaction, project, element, ownership);
                        CurtainWallPanelBuilderSupport.ErasePrevious(transaction, project, element, previous);
                        var baseZ = CadGeometryGuard.Add(polyline.Elevation, CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM"), element.Id + "/path panel base Z");
                        var update = new PendingUpdate
                        {
                            Element = element,
                            Columns = detail.Layout.Columns,
                            Rows = detail.Layout.Rows,
                            BasePanelCount = detail.Panels.Count,
                            OpeningCount = openings.Count,
                            PathSegmentCount = pathPlan.PathSegmentCount,
                            MappedCount = rectangles.Count,
                            PathSagittaM = sagittaM,
                            PanelDepthM = panelDepthM,
                            SourceLengthM = lengthM,
                            HeightM = heightM,
                            AreaM2 = panelPlan.RemainingPanelAreaM2,
                            ConfigFingerprint = CurtainWallPanelFingerprint.Compute(new CurtainWallPanelFingerprintInput
                            {
                                SourceLengthM = lengthM,
                                HeightM = heightM,
                                BottomOffsetM = bottomOffsetM,
                                PanelDepthM = panelDepthM,
                                SourceKind = "OpenPolyline",
                                PathSegmentCount = pathPlan.PathSegmentCount,
                                Pieces = panelPlan.Pieces
                            })
                        };
                        foreach (var piece in pathPlan.Pieces)
                        {
                            Solid3d? solid = CurtainWallPanelBuilderSupport.CreatePathPanel(document, piece, panelDepthM, baseZ, element.Id);
                            try
                            {
                                solid.Layer = polyline.Layer;
                                modelSpace.AppendEntity(solid);
                                transaction.AddNewlyCreatedDBObject(solid, true);
                                GeneratedCurtainPanelNativeOwnershipService.MarkGenerated(document, transaction, solid, project, element);
                                update.Handles.Add(solid.Handle.ToString());
                                solid = null;
                            }
                            finally { solid?.Dispose(); }
                        }
                        pending.Add(update);
                        batchPanels = checked(batchPanels + update.Handles.Count);
                    }
                    foreach (var update in pending) Commit(project, update);
                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError) { throw new InvalidOperationException("Curtain path panel replacement and semantic rollback both failed.", new AggregateException(operationError, restoreError)); }
                }
                throw;
            }
            return new CurtainPanelBuildResult { Elements = pending.Count, Panels = pending.Sum(x => x.Handles.Count) };
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

        private static void Commit(ProjectState project, PendingUpdate update)
        {
            var p = update.Element.Properties;
            p.Remove("GeneratedCurtainPanelLiveFingerprint");
            p[CurtainWallPanelBuilderSupport.HandlesKey] = string.Join(";", update.Handles);
            p["GeneratedCurtainPanelBuildState"] = "Complete";
            p["GeneratedCurtainPanelCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelBaseCount"] = update.BasePanelCount.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelOpeningCount"] = update.OpeningCount.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelColumns"] = update.Columns.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelRows"] = update.Rows.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelDepthM"] = update.PanelDepthM.ToString("R", CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelSourceLengthM"] = update.SourceLengthM.ToString("R", CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelHeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelAreaM2"] = update.AreaM2.ToString("R", CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelConfigFingerprint"] = update.ConfigFingerprint;
            p["GeneratedCurtainPanelMode"] = update.OpeningCount > 0 ? OpeningAwareMode : Mode;
            p["GeneratedCurtainPanelSourceKind"] = "OpenPolyline";
            p["GeneratedCurtainPanelPathSegmentCount"] = update.PathSegmentCount.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelMappedCount"] = update.MappedCount.ToString(CultureInfo.InvariantCulture);
            p["GeneratedCurtainPanelPathSagittaM"] = update.PathSagittaM.ToString("R", CultureInfo.InvariantCulture);
            p["LengthM"] = update.SourceLengthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.ClearGeneratedCurtainPanelStale();
            AuditTrail.ForProject(project).Record("geometry.curtain.path.panels", update.Element.Id,
                update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " path panel solids; base=" + update.BasePanelCount.ToString(CultureInfo.InvariantCulture) + "; mapped=" + update.MappedCount.ToString(CultureInfo.InvariantCulture) + "; openings=" + update.OpeningCount.ToString(CultureInfo.InvariantCulture));
        }

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
