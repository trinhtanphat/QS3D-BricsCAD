using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsInterface;
using BcadApplication = Bricscad.ApplicationServices.Application;
using BrepFace = Teigha.BoundaryRepresentation.Face;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private enum RaftQuantityHighlightKind
        {
            None = 0,
            GrossVolume = 1,
            NetVolume = 2,
            GrossFace = 3,
            NetFace = 4
        }

        private static readonly bool _raftQuantityHighlightHandlersRegistered = RegisterRaftQuantityHighlightHandlers();
        private readonly List<Solid3d> _raftQuantityIncludedTransients = new List<Solid3d>();
        private IntegerCollection? _raftQuantityIncludedViewports;
        private bool _raftQuantityHighlightDocumentEventsAttached;

        private static bool RegisterRaftQuantityHighlightHandlers()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                Button.ClickEvent,
                new RoutedEventHandler(OnRaftQuantityButtonClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                TextBlock.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnRaftQuantityTotalMouseUp),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                TextBlock.MouseEnterEvent,
                new MouseEventHandler(OnRaftQuantityTotalMouseEnter),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                TreeView.SelectedItemChangedEvent,
                new RoutedPropertyChangedEventHandler<object>(OnRaftQuantityTreeSelectionChanged),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRaftQuantityLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnRaftQuantityUnloaded),
                true);
            return true;
        }

        private static void OnRaftQuantityButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is QuantityInsightPanel panel) || !(e.Source is Button button)) return;
            if (!panel.TryResolveRaftQuantityHighlightButton(button, out var kind, out var faceId)) return;

            // Run after the legacy target/deduction and native-face handlers have completed.
            // This leaves the final visual state deterministic: yellow = included, red = deduction.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => panel.ApplyRaftQuantityHighlight(kind, faceId)));
        }

        private bool TryResolveRaftQuantityHighlightButton(Button button, out RaftQuantityHighlightKind kind, out string faceId)
        {
            kind = RaftQuantityHighlightKind.None;
            faceId = string.Empty;
            if (_quantityGeometryPanel == null || !_quantityGeometryPanel.Children.Contains(button)) return false;
            if (!(button.Content is string content)) return false;

            if (content.StartsWith("V gộp:", StringComparison.Ordinal))
            {
                kind = RaftQuantityHighlightKind.GrossVolume;
                return true;
            }
            if (content.StartsWith("V còn:", StringComparison.Ordinal))
            {
                kind = RaftQuantityHighlightKind.NetVolume;
                return true;
            }
            if (content.StartsWith("S gộp:", StringComparison.Ordinal) && TryResolveQuantityExactFaceButton(button, out faceId))
            {
                kind = RaftQuantityHighlightKind.GrossFace;
                return true;
            }
            if (content.StartsWith("S còn:", StringComparison.Ordinal) && TryResolveQuantityExactFaceButton(button, out faceId))
            {
                kind = RaftQuantityHighlightKind.NetFace;
                return true;
            }
            return false;
        }

        private void ApplyRaftQuantityHighlight(RaftQuantityHighlightKind kind, string faceId)
        {
            ClearRaftQuantityIncludedPreview();
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument)) return;
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project)) return;

            var option = CurrentRaftQuantityDetailOption();
            if (option == null) return;

            try
            {
                if (!TryRevalidateQuantityGeometry(document, project, option, out var geometry, out var elementIds, out var error) ||
                    geometry == null || elementIds.Length != 1)
                {
                    _viewModel.Status = string.IsNullOrWhiteSpace(error) ? "Không thể revalidate highlight Móng Bè." : error;
                    return;
                }

                var element = project.FindElement(geometry.ElementId);
                var family = element == null ? null : project.FindFamily(element.FamilyId);
                if (!RaftFoundationPropertySet.IsRaftElement(element, family)) return;

                ClearQuantityExactFaceHighlight();
                ClearQuantityRegionPreview();
                CadHandleService.ClearSelection(document);

                var included = new List<Solid3d>();
                IReadOnlyList<QuantityGeometryDeduction> redDeductions = Array.Empty<QuantityGeometryDeduction>();
                if (kind == RaftQuantityHighlightKind.GrossVolume || kind == RaftQuantityHighlightKind.NetVolume)
                {
                    included.AddRange(CloneRaftTargetSolids(document, geometry));
                    if (kind == RaftQuantityHighlightKind.NetVolume)
                        redDeductions = geometry.VolumeDeductions;
                }
                else
                {
                    var face = geometry.FormworkFaces.SingleOrDefault(x => string.Equals(x.FaceId, faceId, StringComparison.Ordinal));
                    if (face == null)
                    {
                        _viewModel.Status = "Mặt ván khuôn đã thay đổi; bấm Làm mới trước khi highlight.";
                        return;
                    }
                    var plate = BuildRaftFacePlate(document, geometry, face.FaceId);
                    if (plate != null) included.Add(plate);
                    if (kind == RaftQuantityHighlightKind.NetFace)
                        redDeductions = face.Deductions;
                }

                if (included.Count == 0 || !ShowRaftIncludedPreview(document, included))
                {
                    foreach (var solid in included) if (!_raftQuantityIncludedTransients.Contains(solid)) solid.Dispose();
                    _viewModel.Status = "Không dựng được highlight vàng cho dòng khối lượng Móng Bè hiện hành.";
                    return;
                }

                if (redDeductions.Count > 0)
                    ShowRaftRedDeductions(document, project, elementIds, geometry.ElementId, redDeductions);

                _viewModel.Status = redDeductions.Count > 0
                    ? "Highlight Móng Bè: vàng = phần được tính • đỏ = phần khấu trừ giao cắt."
                    : "Highlight Móng Bè: vàng = phần đang được tính.";
            }
            catch (Exception ex) when (RaftQuantityRecoverable(ex))
            {
                ClearRaftQuantityIncludedPreview();
                ClearQuantityRegionPreview();
                _viewModel.Status = "Không thể highlight khối lượng Móng Bè: " + ex.Message;
            }
        }

        private QuantityInsightDetailOption? CurrentRaftQuantityDetailOption()
        {
            var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
            if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
            return option;
        }

        private static IReadOnlyList<Solid3d> CloneRaftTargetSolids(Document document, QuantityGeometryExplanation geometry)
        {
            var result = new List<Solid3d>();
            var ids = CadHandleService.Resolve(document, geometry.SourceHandles);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var solid = transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased) continue;
                    result.Add((Solid3d)solid.Clone());
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private Solid3d? BuildRaftFacePlate(Document document, QuantityGeometryExplanation geometry, string faceId)
        {
            if (!TryParseQuantityExactFaceId(faceId, out var solidNumber, out var faceNumber)) return null;
            var ids = CadHandleService.Resolve(document, geometry.SourceHandles);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solids = ids
                    .Select(id => transaction.GetObject(id, OpenMode.ForRead, false) as Solid3d)
                    .Where(x => x != null && !x.IsErased)
                    .Cast<Solid3d>()
                    .ToList();
                if (solidNumber <= 0 || solidNumber > solids.Count) return null;
                var solid = solids[solidNumber - 1];
                var rootPath = new FullSubentityPath(new[] { solid.ObjectId }, SubentityId.Null);
                FullSubentityPath facePath = default(FullSubentityPath);
                PlanarEntity? plane = null;
                var local = 0;
                using (var brep = new Brep(rootPath))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        local++;
                        if (local != faceNumber) continue;
                        facePath = face.SubentityPath;
                        plane = RaftFacePlane(face);
                        break;
                    }
                }
                if (plane == null) return null;

                var normal = plane.Normal.GetNormal();
                if (Math.Abs(normal.Z) > 1e-6d) return null;
                var extents = solid.GetSubentityGeometricExtents(facePath);
                var dx = Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
                var dy = Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
                var dz = Math.Abs(extents.MaxPoint.Z - extents.MinPoint.Z);
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (!(length > 0d) || !(dz > 0d) || double.IsNaN(length) || double.IsInfinity(length)) return null;

                var thickness = CadGeometryGuard.ToDrawingUnits(document, 0.002d, "quantity raft face highlight thickness");
                if (!(thickness > 0d)) return null;
                var tangent = new Vector3d(-normal.Y, normal.X, 0d).GetNormal();
                var angle = Math.Atan2(tangent.Y, tangent.X);
                var center = new Point3d(
                    (extents.MinPoint.X + extents.MaxPoint.X) * 0.5d,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) * 0.5d,
                    (extents.MinPoint.Z + extents.MaxPoint.Z) * 0.5d);

                var plate = new Solid3d();
                try
                {
                    plate.SetDatabaseDefaults(document.Database);
                    plate.CreateBox(length, thickness, dz);
                    plate.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                    var plateExtents = plate.GeometricExtents;
                    var plateCenter = new Point3d(
                        (plateExtents.MinPoint.X + plateExtents.MaxPoint.X) * 0.5d,
                        (plateExtents.MinPoint.Y + plateExtents.MaxPoint.Y) * 0.5d,
                        (plateExtents.MinPoint.Z + plateExtents.MaxPoint.Z) * 0.5d);
                    plate.TransformBy(Matrix3d.Displacement(center - plateCenter));
                    transaction.Commit();
                    return plate;
                }
                catch
                {
                    plate.Dispose();
                    throw;
                }
            }
        }

        private static PlanarEntity? RaftFacePlane(BrepFace face)
        {
            var surface = face.Surface;
            if (surface is PlanarEntity planar) return new Plane(planar.PointOnPlane, planar.Normal);
            if (surface is ExternalBoundedSurface external && external.IsPlane && external.BaseSurface is PlanarEntity basePlane)
                return new Plane(basePlane.PointOnPlane, basePlane.Normal);
            return null;
        }

        private bool ShowRaftIncludedPreview(Document document, IReadOnlyList<Solid3d> solids)
        {
            if (solids == null || solids.Count == 0) return false;
            var manager = TransientManager.CurrentTransientManager;
            var viewports = new IntegerCollection(0);
            var subDrawingMode = 192;
            if (manager.GetFreeSubDrawingMode(TransientDrawingMode.DirectTopmost, viewports, ref subDrawingMode) == 0)
                return false;

            var extents = new List<Extents3d>();
            foreach (var solid in solids)
            {
                var added = false;
                try
                {
                    solid.ColorIndex = 2; // ACI yellow: included/countable quantity geometry.
                    var currentExtents = solid.GeometricExtents;
                    if (!FiniteQuantityExtents(currentExtents)) continue;
                    if (!manager.AddTransient(solid, TransientDrawingMode.DirectTopmost, subDrawingMode, viewports)) continue;
                    added = true;
                    _raftQuantityIncludedTransients.Add(solid);
                    extents.Add(currentExtents);
                }
                finally
                {
                    if (!added) solid.Dispose();
                }
            }

            if (_raftQuantityIncludedTransients.Count == 0) return false;
            _raftQuantityIncludedViewports = viewports;
            if (!TryZoomQuantityRegion(document, extents)) document.Editor.UpdateScreen();
            return true;
        }

        private void ShowRaftRedDeductions(
            Document document,
            ProjectState project,
            IEnumerable<string> elementIds,
            string targetElementId,
            IEnumerable<QuantityGeometryDeduction> deductions)
        {
            var geometryProject = PrepareQuantityGeometrySnapshot(document, project, elementIds, out var geometryError);
            if (geometryProject == null)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(geometryError)
                    ? "Không thể tạo snapshot cho vùng khấu trừ."
                    : geometryError);

            var regions = new List<Solid3d>();
            try
            {
                foreach (var deduction in deductions
                    .Where(x => x != null)
                    .GroupBy(x => x.RegionKey, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First()))
                {
                    regions.AddRange(QuantityGeometryRegionPreviewService.Build(document, geometryProject, targetElementId, deduction));
                }
                if (regions.Count == 0) return;
                var ownershipTransferred = ShowQuantityRegionPreview(document, regions);
                if (!ownershipTransferred)
                    throw new InvalidOperationException("BricsCAD không đăng ký được transient đỏ cho vùng khấu trừ.");
                regions.Clear();
            }
            finally
            {
                foreach (var region in regions) region.Dispose();
            }
        }

        private static void OnRaftQuantityTotalMouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsRaftFormworkTotal(textBlock)) return;
            textBlock.Cursor = Cursors.Hand;
            textBlock.ToolTip = "Click: tất cả mặt ván khuôn được tính sáng vàng; vùng bị trừ sáng đỏ.";
        }

        private static void OnRaftQuantityTotalMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsRaftFormworkTotal(textBlock)) return;
            e.Handled = true;
            panel.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(panel.ApplyRaftFormworkTotalHighlight));
        }

        private bool IsRaftFormworkTotal(TextBlock textBlock)
        {
            return _quantityGeometryPanel != null &&
                   _quantityGeometryPanel.Children.Contains(textBlock) &&
                   (textBlock.Text ?? string.Empty).StartsWith("Ván khuôn: S gộp", StringComparison.Ordinal);
        }

        private void ApplyRaftFormworkTotalHighlight()
        {
            ClearRaftQuantityIncludedPreview();
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument)) return;
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project)) return;
            var option = CurrentRaftQuantityDetailOption();
            if (option == null) return;

            try
            {
                if (!TryRevalidateQuantityGeometry(document, project, option, out var geometry, out var elementIds, out var error) ||
                    geometry == null || elementIds.Length != 1)
                {
                    _viewModel.Status = string.IsNullOrWhiteSpace(error) ? "Không thể revalidate tổng ván khuôn Móng Bè." : error;
                    return;
                }
                var element = project.FindElement(geometry.ElementId);
                var family = element == null ? null : project.FindFamily(element.FamilyId);
                if (!RaftFoundationPropertySet.IsRaftElement(element, family)) return;

                ClearQuantityExactFaceHighlight();
                ClearQuantityRegionPreview();
                CadHandleService.ClearSelection(document);
                var plates = new List<Solid3d>();
                foreach (var face in geometry.FormworkFaces.OrderBy(x => x.FaceId, StringComparer.Ordinal))
                {
                    var plate = BuildRaftFacePlate(document, geometry, face.FaceId);
                    if (plate != null) plates.Add(plate);
                }
                if (plates.Count == 0 || !ShowRaftIncludedPreview(document, plates))
                {
                    foreach (var plate in plates) if (!_raftQuantityIncludedTransients.Contains(plate)) plate.Dispose();
                    _viewModel.Status = "Không dựng được highlight vàng cho tổng ván khuôn Móng Bè.";
                    return;
                }

                var deductions = geometry.FormworkFaces.SelectMany(x => x.Deductions).ToArray();
                if (deductions.Length > 0)
                    ShowRaftRedDeductions(document, project, elementIds, geometry.ElementId, deductions);
                _viewModel.Status = deductions.Length > 0
                    ? "Tổng ván khuôn Móng Bè: vàng = mặt được tính • đỏ = vùng bị trừ."
                    : "Tổng ván khuôn Móng Bè: tất cả mặt đứng được tính đang sáng vàng.";
            }
            catch (Exception ex) when (RaftQuantityRecoverable(ex))
            {
                ClearRaftQuantityIncludedPreview();
                ClearQuantityRegionPreview();
                _viewModel.Status = "Không thể highlight tổng ván khuôn Móng Bè: " + ex.Message;
            }
        }

        private static void OnRaftQuantityTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is QuantityInsightPanel panel) panel.ClearRaftQuantityIncludedPreview();
        }

        private static void OnRaftQuantityLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantityInsightPanel panel) panel.AttachRaftQuantityDocumentEvents();
        }

        private static void OnRaftQuantityUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is QuantityInsightPanel panel)) return;
            panel.ClearRaftQuantityIncludedPreview();
            panel.DetachRaftQuantityDocumentEvents();
        }

        private void AttachRaftQuantityDocumentEvents()
        {
            if (_raftQuantityHighlightDocumentEventsAttached) return;
            var documents = BcadApplication.DocumentManager;
            documents.DocumentToBeDeactivated += OnRaftQuantityDocumentSwitch;
            documents.DocumentBecameCurrent += OnRaftQuantityDocumentSwitch;
            _raftQuantityHighlightDocumentEventsAttached = true;
        }

        private void DetachRaftQuantityDocumentEvents()
        {
            if (!_raftQuantityHighlightDocumentEventsAttached) return;
            var documents = BcadApplication.DocumentManager;
            documents.DocumentToBeDeactivated -= OnRaftQuantityDocumentSwitch;
            documents.DocumentBecameCurrent -= OnRaftQuantityDocumentSwitch;
            _raftQuantityHighlightDocumentEventsAttached = false;
        }

        private void OnRaftQuantityDocumentSwitch(object sender, DocumentCollectionEventArgs e)
        {
            ClearRaftQuantityIncludedPreview();
        }

        private void ClearRaftQuantityIncludedPreview()
        {
            if (_raftQuantityIncludedTransients.Count == 0)
            {
                _raftQuantityIncludedViewports = null;
                return;
            }

            var manager = TransientManager.CurrentTransientManager;
            var viewports = _raftQuantityIncludedViewports ?? new IntegerCollection(0);
            foreach (var solid in _raftQuantityIncludedTransients)
            {
                try { manager.EraseTransient(solid, viewports); }
                catch { }
                finally { solid.Dispose(); }
            }
            _raftQuantityIncludedTransients.Clear();
            _raftQuantityIncludedViewports = null;
            try { BcadApplication.DocumentManager.MdiActiveDocument?.Editor.UpdateScreen(); }
            catch { }
        }

        private static bool RaftQuantityRecoverable(Exception ex) =>
            !(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException);
    }
}
