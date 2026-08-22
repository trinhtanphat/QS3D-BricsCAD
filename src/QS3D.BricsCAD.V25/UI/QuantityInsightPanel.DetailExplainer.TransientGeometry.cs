using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsInterface;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private static readonly bool _quantityRegionPreviewRegistered = RegisterQuantityRegionPreview();
        private readonly List<Solid3d> _quantityRegionTransientSolids = new List<Solid3d>();
        private IntegerCollection? _quantityRegionTransientViewports;

        private static bool RegisterQuantityRegionPreview()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                Button.ClickEvent,
                new RoutedEventHandler(OnQuantityRegionPreviewButtonClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                TreeView.SelectedItemChangedEvent,
                new RoutedPropertyChangedEventHandler<object>(OnQuantityRegionPreviewTreeSelectionChanged),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnQuantityRegionPreviewUnloaded),
                true);
            return true;
        }

        private static void OnQuantityRegionPreviewButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is QuantityInsightPanel panel)) return;
            if (!(e.Source is Button button)) return;
            panel.HandleQuantityRegionPreviewButtonClick(button);
        }

        private static void OnQuantityRegionPreviewTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is QuantityInsightPanel panel) panel.ClearQuantityRegionPreview();
        }

        private static void OnQuantityRegionPreviewUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantityInsightPanel panel) panel.ClearQuantityRegionPreview();
        }

        private void HandleQuantityRegionPreviewButtonClick(Button button)
        {
            ClearQuantityRegionPreview();
            if (!(button.Tag is QuantityGeometryDeduction displayedDeduction)) return;

            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _viewModel.Status = "Không thể highlight vùng giao: DWG hiện hành đã thay đổi.";
                return;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                _viewModel.Status = "Không thể highlight vùng giao: QS3D project đã thay đổi; hãy bấm Làm mới.";
                return;
            }

            var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
            if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
            if (option == null)
            {
                _viewModel.Status = "Không còn cấu kiện detail hiện hành để highlight vùng giao.";
                return;
            }

            try
            {
                if (!TryRevalidateQuantityGeometry(document, project, option, out var freshGeometry, out var elementIds, out var error))
                {
                    _viewModel.Status = error;
                    return;
                }
                if (freshGeometry == null || elementIds.Length != 1)
                {
                    _viewModel.Status = "Provenance hình học đã thay đổi; bấm Làm mới trước khi highlight vùng giao.";
                    return;
                }

                var current = freshGeometry.VolumeDeductions
                    .Concat(freshGeometry.FormworkFaces.SelectMany(x => x.Deductions))
                    .Where(x => string.Equals(x.RegionKey, displayedDeduction.RegionKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (current.Count != 1)
                {
                    _viewModel.Status = "Dòng khấu trừ đã thay đổi hoặc không còn duy nhất; bấm Làm mới trước khi highlight vùng giao.";
                    return;
                }

                var geometryProject = PrepareQuantityGeometrySnapshot(document, project, elementIds, out var geometryError);
                if (geometryProject == null)
                {
                    _viewModel.Status = string.IsNullOrWhiteSpace(geometryError)
                        ? "Không thể tạo snapshot Solid3d live để highlight vùng giao."
                        : geometryError;
                    return;
                }

                var regions = QuantityGeometryRegionPreviewService.Build(document, geometryProject, elementIds[0], current[0]);
                if (regions.Count == 0)
                {
                    _viewModel.Status = "Đã định vị cấu kiện nhưng không dựng lại được vùng giao/contact BREP transient • " + current[0].RegionKey;
                    return;
                }

                if (!ShowQuantityRegionPreview(document, regions))
                {
                    _viewModel.Status = "Đã dựng vùng giao/contact BREP nhưng BricsCAD không đăng ký được transient highlight • " + current[0].RegionKey;
                    return;
                }

                _viewModel.Status = "Đã chọn cấu kiện đích + nguyên nhân, highlight vùng giao/contact BREP transient và zoom đúng vùng • " + current[0].RegionKey;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException))
            {
                ClearQuantityRegionPreview();
                _viewModel.Status = "Không thể highlight vùng giao/contact: " + ex.Message;
            }
        }

        private bool ShowQuantityRegionPreview(Document document, IReadOnlyList<Solid3d> regions)
        {
            if (regions == null || regions.Count == 0) return false;
            var manager = TransientManager.CurrentTransientManager;
            var viewports = new IntegerCollection(0);
            var subDrawingMode = 128;
            if (manager.GetFreeSubDrawingMode(TransientDrawingMode.Highlight, viewports, ref subDrawingMode) == 0)
            {
                foreach (var region in regions) region.Dispose();
                return false;
            }

            var extents = new List<Extents3d>();
            foreach (var region in regions)
            {
                var added = false;
                try
                {
                    var regionExtents = region.GeometricExtents;
                    if (!FiniteQuantityExtents(regionExtents))
                    {
                        region.Dispose();
                        continue;
                    }
                    if (!manager.AddTransient(region, TransientDrawingMode.Highlight, subDrawingMode, viewports))
                    {
                        region.Dispose();
                        continue;
                    }
                    added = true;
                    _quantityRegionTransientSolids.Add(region);
                    extents.Add(regionExtents);
                }
                catch
                {
                    if (!added) region.Dispose();
                }
            }

            if (_quantityRegionTransientSolids.Count == 0) return false;
            _quantityRegionTransientViewports = viewports;
            if (!TryZoomQuantityRegion(document, extents)) document.Editor.UpdateScreen();
            return true;
        }

        private void ClearQuantityRegionPreview()
        {
            if (_quantityRegionTransientSolids.Count == 0)
            {
                _quantityRegionTransientViewports = null;
                return;
            }

            var manager = TransientManager.CurrentTransientManager;
            var viewports = _quantityRegionTransientViewports ?? new IntegerCollection(0);
            foreach (var solid in _quantityRegionTransientSolids)
            {
                try { manager.EraseTransient(solid, viewports); }
                catch { }
                finally { solid.Dispose(); }
            }
            _quantityRegionTransientSolids.Clear();
            _quantityRegionTransientViewports = null;

            try
            {
                var document = BcadApplication.DocumentManager.MdiActiveDocument;
                document?.Editor.UpdateScreen();
            }
            catch { }
        }

        private static bool TryZoomQuantityRegion(Document document, IEnumerable<Extents3d> worldExtents)
        {
            if (document == null) return false;
            var extentsList = (worldExtents ?? Array.Empty<Extents3d>()).ToList();
            if (extentsList.Count == 0) return false;

            using (var view = document.Editor.GetCurrentView())
            {
                var worldToDisplay = QuantityWorldToDisplay(view);
                var hasExtents = false;
                var min = new Point3d();
                var max = new Point3d();

                foreach (var source in extentsList)
                {
                    var extents = source;
                    extents.TransformBy(worldToDisplay);
                    var extentMin = extents.MinPoint;
                    var extentMax = extents.MaxPoint;
                    if (!QuantityFinite(extentMin) || !QuantityFinite(extentMax)) continue;

                    if (!hasExtents)
                    {
                        min = extentMin;
                        max = extentMax;
                        hasExtents = true;
                    }
                    else
                    {
                        min = new Point3d(
                            Math.Min(min.X, extentMin.X),
                            Math.Min(min.Y, extentMin.Y),
                            Math.Min(min.Z, extentMin.Z));
                        max = new Point3d(
                            Math.Max(max.X, extentMax.X),
                            Math.Max(max.Y, extentMax.Y),
                            Math.Max(max.Z, extentMax.Z));
                    }
                }

                if (!hasExtents) return false;
                var minimumSpan = QuantityMinimumViewSpan(view);
                var width = Math.Max(max.X - min.X, minimumSpan);
                var height = Math.Max(max.Y - min.Y, minimumSpan);
                var ratio = QuantityFinitePositive(view.Height) && QuantityFinitePositive(view.Width)
                    ? view.Width / view.Height
                    : 1d;
                if (!QuantityFinitePositive(ratio)) ratio = 1d;
                if (width / height > ratio) height = width / ratio;
                else width = height * ratio;

                var centerX = (min.X + max.X) * 0.5d;
                var centerY = (min.Y + max.Y) * 0.5d;
                if (!QuantityFinite(centerX) || !QuantityFinite(centerY) || !QuantityFinitePositive(width) || !QuantityFinitePositive(height)) return false;

                view.CenterPoint = new Point2d(centerX, centerY);
                view.Width = width * 1.25d;
                view.Height = height * 1.25d;
                document.Editor.SetCurrentView(view);
            }

            document.Editor.UpdateScreen();
            return true;
        }

        private static Matrix3d QuantityWorldToDisplay(ViewTableRecord view)
        {
            var matrix = Matrix3d.PlaneToWorld(view.ViewDirection);
            matrix = Matrix3d.Displacement(view.Target - Point3d.Origin) * matrix;
            matrix = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * matrix;
            return matrix.Inverse();
        }

        private static double QuantityMinimumViewSpan(ViewTableRecord view)
        {
            var scale = Math.Min(Math.Abs(view.Width), Math.Abs(view.Height));
            if (!QuantityFinitePositive(scale)) scale = 1d;
            var minimum = scale * 1e-6d;
            return QuantityFinitePositive(minimum) ? minimum : 1e-6d;
        }

        private static bool FiniteQuantityExtents(Extents3d extents) => QuantityFinite(extents.MinPoint) && QuantityFinite(extents.MaxPoint);
        private static bool QuantityFinite(Point3d point) => QuantityFinite(point.X) && QuantityFinite(point.Y) && QuantityFinite(point.Z);
        private static bool QuantityFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool QuantityFinitePositive(double value) => QuantityFinite(value) && value > 0d;
    }
}
