using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI.ViewModels;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel : UserControl
    {
        private readonly RightPanelViewModel _viewModel = new RightPanelViewModel();

        public RightPanel()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += (_, __) => Refresh();
        }

        public void Refresh()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            try
            {
                var selectedDrawing = DrawingList?.SelectedItem as DrawingItemViewModel;
                _viewModel.Drawings.Clear();
                _viewModel.Drawings.Add(new DrawingItemViewModel
                {
                    Name = System.IO.Path.GetFileName(doc.Name),
                    Path = doc.Name,
                    Kind = "MODEL",
                    IsLocked = false,
                    IsXref = false
                });
                foreach (var item in DrawingCatalogReader.ReadReferences(doc))
                    _viewModel.Drawings.Add(new DrawingItemViewModel
                    {
                        Name = item.Name,
                        Path = item.Path,
                        Kind = "XREF",
                        IsLocked = false,
                        IsXref = true
                    });
                if (selectedDrawing != null)
                    DrawingList.SelectedItem = _viewModel.Drawings.FirstOrDefault(x => x.IsXref == selectedDrawing.IsXref && string.Equals(x.Name, selectedDrawing.Name, StringComparison.OrdinalIgnoreCase));
                RefreshLayers();
                _viewModel.Status = _viewModel.Drawings.Count + " bản vẽ • " + _viewModel.Layers.Count + " layer";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Lỗi đọc DWG: " + ex.Message;
            }
        }

        private void RefreshLayers()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var selectedNames = LayerList?.SelectedItems.Cast<LayerItemViewModel>().Select(x => x.Name) ?? Enumerable.Empty<string>();
            var selected = new System.Collections.Generic.HashSet<string>(selectedNames, StringComparer.OrdinalIgnoreCase);
            var search = LayerSearchBox?.Text?.Trim() ?? string.Empty;
            _viewModel.Layers.Clear();
            foreach (var item in DrawingCatalogReader.ReadLayers(doc).Where(x => search.Length == 0 || x.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0))
            {
                var vm = new LayerItemViewModel { Name = item.Name, IsVisible = item.IsVisible, ColorIndex = item.ColorIndex };
                _viewModel.Layers.Add(vm);
                if (selected.Contains(vm.Name)) LayerList?.SelectedItems.Add(vm);
            }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => Refresh();
        private void OnLayerSearchChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) RefreshLayers(); }
        private void OnShowLayersClick(object sender, RoutedEventArgs e) => SetSelectedLayers(true);
        private void OnHideLayersClick(object sender, RoutedEventArgs e) => SetSelectedLayers(false);
        private void OnInvertSelectionClick(object sender, RoutedEventArgs e)
        {
            var selected = LayerList.SelectedItems.Cast<LayerItemViewModel>().ToList();
            LayerList.UnselectAll();
            foreach (var item in _viewModel.Layers.Where(x => !selected.Contains(x))) LayerList.SelectedItems.Add(item);
        }
        private void OnClearLayerSelectionClick(object sender, RoutedEventArgs e) => LayerList.UnselectAll();

        private void OnClearDrawingSelectionClick(object sender, RoutedEventArgs e)
        {
            DrawingList.UnselectAll();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null) doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
            _viewModel.Status = "Đã bỏ chọn bản vẽ/Xref.";
        }

        private void OnDrawingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var item = DrawingList.SelectedItem as DrawingItemViewModel;
            if (doc == null || item == null || !item.IsXref) return;
            try
            {
                var count = XrefService.SelectInstances(doc, item.Name);
                _viewModel.Status = count > 0
                    ? "Xref " + item.Name + " • đã chọn " + count + " instance trong space hiện tại"
                    : "Xref " + item.Name + " chưa có instance trong space hiện tại.";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Lỗi chọn Xref: " + ex.Message;
            }
        }

        private void OnLayerChecked(object sender, RoutedEventArgs e) => SetLayerFromCheckBox(sender, true);
        private void OnLayerUnchecked(object sender, RoutedEventArgs e) => SetLayerFromCheckBox(sender, false);

        private void SetLayerFromCheckBox(object sender, bool visible)
        {
            if (!(sender is CheckBox box) || !(box.DataContext is LayerItemViewModel item)) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            try
            {
                LayerVisibilityService.SetVisible(doc, new[] { item.Name }, visible);
                _viewModel.Status = (visible ? "Hiện " : "Ẩn ") + item.Name;
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
            }
        }

        private void SetSelectedLayers(bool visible)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var names = LayerList.SelectedItems.Cast<LayerItemViewModel>().Select(x => x.Name).ToArray();
            if (names.Length == 0)
            {
                _viewModel.Status = "Chọn ít nhất một layer.";
                return;
            }
            try
            {
                var count = LayerVisibilityService.SetVisible(doc, names, visible);
                _viewModel.Status = (visible ? "Đã hiện " : "Đã ẩn ") + count + " layer";
                RefreshLayers();
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
            }
        }

        private void OnAttachXrefClick(object sender, RoutedEventArgs e) => Send("_XATTACH");

        private void OnReloadXrefClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var item = SelectedXref();
            if (doc == null || item == null) return;
            try
            {
                XrefService.Reload(doc, item.Name);
                _viewModel.Status = "Đã nạp lại Xref " + item.Name;
                Refresh();
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
            }
        }

        private void OnMoveDrawingClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var item = SelectedXref();
            if (doc == null || item == null) return;
            try
            {
                var count = XrefService.SelectInstances(doc, item.Name);
                if (count == 0)
                {
                    _viewModel.Status = "Không tìm thấy instance Xref trong space hiện tại.";
                    return;
                }
                _viewModel.Status = "Di chuyển " + count + " instance của " + item.Name + ".";
                Send("_MOVE");
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
            }
        }

        private void OnDeleteDrawingClick(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var item = SelectedXref();
            if (doc == null || item == null) return;
            if (MessageBox.Show("Gỡ Xref “" + item.Name + "” khỏi bản vẽ? File nguồn sẽ không bị xóa.", "QS3D", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                XrefService.Detach(doc, item.Name);
                _viewModel.Status = "Đã gỡ Xref " + item.Name;
                Refresh();
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
            }
        }

        private void OnZoomWindowClick(object sender, RoutedEventArgs e) => Send("_ZOOM _W");

        private DrawingItemViewModel? SelectedXref()
        {
            var item = DrawingList.SelectedItem as DrawingItemViewModel;
            if (item == null)
            {
                _viewModel.Status = "Chọn một Xref trong Quản lý bản vẽ.";
                return null;
            }
            if (!item.IsXref)
            {
                _viewModel.Status = "Thao tác này chỉ áp dụng cho Xref, không áp dụng DWG chính.";
                return null;
            }
            return item;
        }

        private static void Send(string command) => Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + " ", true, false, false);
    }
}
