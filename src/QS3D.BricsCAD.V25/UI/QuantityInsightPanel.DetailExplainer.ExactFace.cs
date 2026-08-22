using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Services;
using Teigha.BoundaryRepresentation;
using Teigha.DatabaseServices;
using BcadApplication = Bricscad.ApplicationServices.Application;
using BrepFace = Teigha.BoundaryRepresentation.Face;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private static readonly bool _quantityExactFaceHandlersRegistered = RegisterQuantityExactFaceHandlers();
        private Document? _quantityExactFaceDocument;
        private ObjectId _quantityExactFaceObjectId = ObjectId.Null;
        private FullSubentityPath _quantityExactFacePath;
        private bool _quantityExactFaceHasHighlight;
        private bool _quantityExactFaceDocumentEventsAttached;

        private static bool RegisterQuantityExactFaceHandlers()
        {
            // Register at Button itself so an exact-face value action can mark the routed
            // Click handled before the legacy whole-Solid3d instance Click handler runs.
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnQuantityExactFaceButtonClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                TextBlock.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnQuantityExactFaceTitleClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                TextBlock.MouseEnterEvent,
                new MouseEventHandler(OnQuantityExactFaceTitleMouseEnter),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                TreeView.SelectedItemChangedEvent,
                new RoutedPropertyChangedEventHandler<object>(OnQuantityExactFaceTreeSelectionChanged),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnQuantityExactFaceDetailSelectionChanged),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantityExactFaceLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(QuantityInsightPanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnQuantityExactFaceUnloaded),
                true);
            return true;
        }

        private static void OnQuantityExactFaceButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            var panel = FindQuantityInsightPanel(button);
            if (panel == null) return;

            panel.ClearQuantityExactFaceHighlight();
            if (!panel.TryResolveQuantityExactFaceButton(button, out var faceId)) return;

            // Suppress the old OnQuantityGeometryTargetClick whole-solid action for the
            // two S-value buttons that belong to this exact face.
            e.Handled = true;
            panel.LocateQuantityExactFace(faceId);
        }

        private static void OnQuantityExactFaceTitleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsDirectQuantityGeometryChild(textBlock)) return;
            if (!TryQuantityExactFaceTitle(textBlock.Text, out var faceId)) return;

            panel.ClearQuantityExactFaceHighlight();
            e.Handled = true;
            panel.LocateQuantityExactFace(faceId);
        }

        private static void OnQuantityExactFaceTitleMouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is TextBlock textBlock)) return;
            var panel = FindQuantityInsightPanel(textBlock);
            if (panel == null || !panel.IsDirectQuantityGeometryChild(textBlock)) return;
            if (!TryQuantityExactFaceTitle(textBlock.Text, out _)) return;
            textBlock.Cursor = Cursors.Hand;
            textBlock.ToolTip = "Click để chỉ highlight đúng native BREP face này trong BricsCAD.";
        }

        private static void OnQuantityExactFaceTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is QuantityInsightPanel panel) panel.ClearQuantityExactFaceHighlight();
        }

        private static void OnQuantityExactFaceDetailSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is QuantityInsightPanel panel) panel.ClearQuantityExactFaceHighlight();
        }

        private static void OnQuantityExactFaceLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantityInsightPanel panel) panel.AttachQuantityExactFaceDocumentEvents();
        }

        private static void OnQuantityExactFaceUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is QuantityInsightPanel panel)) return;
            panel.ClearQuantityExactFaceHighlight();
            panel.DetachQuantityExactFaceDocumentEvents();
        }

        private void AttachQuantityExactFaceDocumentEvents()
        {
            if (_quantityExactFaceDocumentEventsAttached) return;
            var documents = BcadApplication.DocumentManager;
            documents.DocumentToBeDeactivated += OnQuantityExactFaceDocumentSwitch;
            documents.DocumentBecameCurrent += OnQuantityExactFaceDocumentSwitch;
            _quantityExactFaceDocumentEventsAttached = true;
        }

        private void DetachQuantityExactFaceDocumentEvents()
        {
            if (!_quantityExactFaceDocumentEventsAttached) return;
            var documents = BcadApplication.DocumentManager;
            documents.DocumentToBeDeactivated -= OnQuantityExactFaceDocumentSwitch;
            documents.DocumentBecameCurrent -= OnQuantityExactFaceDocumentSwitch;
            _quantityExactFaceDocumentEventsAttached = false;
        }

        private void OnQuantityExactFaceDocumentSwitch(object sender, DocumentCollectionEventArgs e)
        {
            ClearQuantityExactFaceHighlight();
        }

        private static QuantityInsightPanel? FindQuantityInsightPanel(FrameworkElement element)
        {
            FrameworkElement? current = element;
            while (current != null)
            {
                if (current is QuantityInsightPanel panel) return panel;
                current = current.Parent as FrameworkElement;
            }
            return null;
        }

        private bool IsDirectQuantityGeometryChild(UIElement element) =>
            _quantityGeometryPanel != null && _quantityGeometryPanel.Children.Contains(element);

        private bool TryResolveQuantityExactFaceButton(Button button, out string faceId)
        {
            faceId = string.Empty;
            if (_quantityGeometryPanel == null || !_quantityGeometryPanel.Children.Contains(button)) return false;
            if (!(button.Content is string content)) return false;
            if (!content.StartsWith("S gộp:", StringComparison.Ordinal) &&
                !content.StartsWith("S còn:", StringComparison.Ordinal)) return false;

            var buttonIndex = _quantityGeometryPanel.Children.IndexOf(button);
            for (var index = buttonIndex - 1; index >= 0; index--)
            {
                if (!(_quantityGeometryPanel.Children[index] is TextBlock candidate)) continue;
                if (TryQuantityExactFaceTitle(candidate.Text, out faceId)) return true;
                if ((candidate.Text ?? string.Empty).StartsWith("VÁN KHUÔN THEO MẶT", StringComparison.Ordinal)) break;
            }
            faceId = string.Empty;
            return false;
        }

        private static bool TryQuantityExactFaceTitle(string? text, out string faceId)
        {
            faceId = string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var value = text!;
            var separator = value.IndexOf(" • ", StringComparison.Ordinal);
            if (separator <= 0) return false;
            var candidate = value.Substring(0, separator);
            if (!TryParseQuantityExactFaceId(candidate, out _, out _)) return false;
            faceId = candidate;
            return true;
        }

        private static bool TryParseQuantityExactFaceId(string? faceId, out int solidNumber, out int faceNumber)
        {
            solidNumber = 0;
            faceNumber = 0;
            if (string.IsNullOrWhiteSpace(faceId)) return false;
            var value = faceId!;
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) return false;
            const string solidPrefix = "SOLID-";
            const string facePrefix = "FACE-";
            if (!value.StartsWith(solidPrefix, StringComparison.Ordinal) || value.IndexOf('/') < 0) return false;
            var parts = value.Split('/');
            if (parts.Length != 2 || !parts[1].StartsWith(facePrefix, StringComparison.Ordinal)) return false;
            var solidDigits = parts[0].Substring(solidPrefix.Length);
            var faceDigits = parts[1].Substring(facePrefix.Length);
            if (solidDigits.Length < 2 || faceDigits.Length < 2) return false;
            if (!int.TryParse(solidDigits, NumberStyles.None, CultureInfo.InvariantCulture, out solidNumber) || solidNumber <= 0) return false;
            if (!int.TryParse(faceDigits, NumberStyles.None, CultureInfo.InvariantCulture, out faceNumber) || faceNumber <= 0) return false;
            return true;
        }

        private void LocateQuantityExactFace(string displayedFaceId)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null || _boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _viewModel.Status = "Không thể highlight face: DWG hiện hành đã thay đổi.";
                return;
            }
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || !SameProjectIdentity(project))
            {
                _viewModel.Status = "Không thể highlight face: QS3D project đã thay đổi; hãy bấm Làm mới.";
                return;
            }
            if (!TryParseQuantityExactFaceId(displayedFaceId, out var solidNumber, out var faceNumber))
            {
                _viewModel.Status = "Face ID không hợp lệ; từ chối highlight native subentity.";
                return;
            }

            var option = _quantityDetailSelector?.SelectedItem as QuantityInsightDetailOption;
            if (option == null && _quantityDetailOptions.Count == 1) option = _quantityDetailOptions[0];
            if (option == null)
            {
                _viewModel.Status = "Không còn cấu kiện detail hiện hành để highlight face.";
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
                    _viewModel.Status = "Provenance hình học đã thay đổi; bấm Làm mới trước khi highlight face.";
                    return;
                }

                var freshMatches = freshGeometry.FormworkFaces
                    .Where(x => string.Equals(x.FaceId, displayedFaceId, StringComparison.Ordinal))
                    .ToList();
                var displayedMatches = (_quantityGeometryCurrent?.FormworkFaces ?? Array.Empty<QuantityFormworkFaceExplanation>())
                    .Where(x => string.Equals(x.FaceId, displayedFaceId, StringComparison.Ordinal))
                    .ToList();
                if (freshMatches.Count != 1 || displayedMatches.Count != 1 ||
                    !SameQuantityExactFace(displayedMatches[0], freshMatches[0]))
                {
                    _viewModel.Status = "BREP face đã thay đổi hoặc không còn duy nhất; bấm Làm mới trước khi highlight.";
                    return;
                }

                if (!TryHighlightQuantityExactFace(document, freshGeometry, solidNumber, faceNumber, out var extents, out var highlightError))
                {
                    _viewModel.Status = highlightError;
                    return;
                }

                if (!TryZoomQuantityRegion(document, new[] { extents })) document.Editor.UpdateScreen();
                _viewModel.Status = "Đã highlight đúng native BREP face " + displayedFaceId + "; whole Solid3d không được chọn.";
            }
            catch (Exception ex) when (QuantityExactFaceRecoverable(ex))
            {
                ClearQuantityExactFaceHighlight();
                _viewModel.Status = "Không thể highlight native BREP face: " + ex.Message;
            }
        }

        private bool TryHighlightQuantityExactFace(
            Document document,
            QuantityGeometryExplanation geometry,
            int solidNumber,
            int faceNumber,
            out Extents3d extents,
            out string error)
        {
            extents = new Extents3d();
            error = string.Empty;
            var objectIds = Cad.CadHandleService.Resolve(document, geometry.SourceHandles);
            if (objectIds.Count == 0)
            {
                error = "Không còn CAD handle live cho BREP face hiện hành.";
                return false;
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solids = new List<Solid3d>();
                foreach (var objectId in objectIds)
                {
                    var solid = transaction.GetObject(objectId, OpenMode.ForRead, false) as Solid3d;
                    if (solid != null && !solid.IsErased) solids.Add(solid);
                }

                var expectedSolidCount = QuantityExactFaceSolidCount(geometry.FormworkFaces);
                if (expectedSolidCount <= 0 || solids.Count != expectedSolidCount || solidNumber > solids.Count)
                {
                    error = "Thứ tự/số lượng Solid3d live không còn khớp BREP review; bấm Làm mới.";
                    return false;
                }

                var solid = solids[solidNumber - 1];
                var rootPath = new FullSubentityPath(new[] { solid.ObjectId }, SubentityId.Null);
                FullSubentityPath facePath = default(FullSubentityPath);
                var found = false;
                var localFaceNumber = 0;
                using (var brep = new Brep(rootPath))
                {
                    foreach (BrepFace face in brep.Faces)
                    {
                        localFaceNumber++;
                        if (localFaceNumber != faceNumber) continue;
                        facePath = face.SubentityPath;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    error = "BREP face index không còn tồn tại trên Solid3d live; bấm Làm mới.";
                    return false;
                }

                Cad.CadHandleService.ClearSelection(document);
                solid.Highlight(facePath, false);
                extents = solid.GeometricExtents;
                transaction.Commit();

                _quantityExactFaceDocument = document;
                _quantityExactFaceObjectId = solid.ObjectId;
                _quantityExactFacePath = facePath;
                _quantityExactFaceHasHighlight = true;
            }
            return true;
        }

        private static int QuantityExactFaceSolidCount(IEnumerable<QuantityFormworkFaceExplanation> faces)
        {
            var components = new HashSet<int>();
            foreach (var face in faces ?? Array.Empty<QuantityFormworkFaceExplanation>())
            {
                if (!TryParseQuantityExactFaceId(face.FaceId, out var solidNumber, out _)) return -1;
                components.Add(solidNumber);
            }
            if (components.Count == 0) return 0;
            var max = components.Max();
            if (components.Count != max) return -1;
            for (var value = 1; value <= max; value++) if (!components.Contains(value)) return -1;
            return max;
        }

        private static bool SameQuantityExactFace(QuantityFormworkFaceExplanation displayed, QuantityFormworkFaceExplanation fresh)
        {
            const double tolerance = 1e-9d;
            return string.Equals(displayed.FaceId, fresh.FaceId, StringComparison.Ordinal) &&
                   string.Equals(displayed.FaceType, fresh.FaceType, StringComparison.Ordinal) &&
                   Math.Abs(displayed.GrossArea - fresh.GrossArea) <= tolerance &&
                   Math.Abs(displayed.DeductionArea - fresh.DeductionArea) <= tolerance &&
                   Math.Abs(displayed.NetArea - fresh.NetArea) <= tolerance;
        }

        private void ClearQuantityExactFaceHighlight()
        {
            if (!_quantityExactFaceHasHighlight)
            {
                _quantityExactFaceDocument = null;
                _quantityExactFaceObjectId = ObjectId.Null;
                return;
            }

            var document = _quantityExactFaceDocument;
            var objectId = _quantityExactFaceObjectId;
            var path = _quantityExactFacePath;
            _quantityExactFaceHasHighlight = false;
            _quantityExactFaceDocument = null;
            _quantityExactFaceObjectId = ObjectId.Null;
            _quantityExactFacePath = default(FullSubentityPath);

            if (document == null || objectId.IsNull || !objectId.IsValid) return;
            try
            {
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var entity = transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity;
                    if (entity != null && !entity.IsErased) entity.Unhighlight(path, false);
                    transaction.Commit();
                }
                document.Editor.UpdateScreen();
            }
            catch (Exception ex) when (QuantityExactFaceRecoverable(ex))
            {
                // Cleanup is best effort after state has already been invalidated.
            }
        }

        private static bool QuantityExactFaceRecoverable(Exception ex) =>
            !(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException);
    }
}
