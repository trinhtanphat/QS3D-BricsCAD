using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI.ViewModels;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel : UserControl
    {
        private const int MaxLayerSearchTokens = 8;
        private readonly RightPanelViewModel _viewModel = new RightPanelViewModel();
        private bool _refreshingLayers;
        private bool _refreshingDrawings;

        public RightPanel()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += OnInitialLoaded;
        }

        private void OnInitialLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnInitialLoaded;
            Refresh();
        }

        public void Refresh()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                _refreshingDrawings = true;
                try
                {
                    _viewModel.Drawings.Clear();
                    DrawingList?.UnselectAll();
                }
                finally
                {
                    _refreshingDrawings = false;
                }
                _refreshingLayers = true;
                try
                {
                    _viewModel.Layers.Clear();
                    LayerList?.UnselectAll();
                }
                finally
                {
                    _refreshingLayers = false;
                }
                ApplyLayerFilter();
                _viewModel.Status = "Không có bản vẽ BricsCAD đang active.";
                return;
            }
            try
            {
                RefreshDrawingsOnly();
                ReloadLayers();
                _viewModel.Status = _viewModel.Drawings.Count + " bản vẽ • " + _viewModel.Layers.Count + " layer";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Lỗi đọc DWG: " + ex.Message;
            }
        }

        private void ReloadLayers()
        {
            var selectedNames = LayerList?.SelectedItems.Cast<LayerItemViewModel>().Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            _refreshingLayers = true;
            try
            {
                _viewModel.Layers.Clear();
                if (doc != null)
                {
                    foreach (var item in DrawingCatalogReader.ReadLayers(doc))
                    {
                        var brush = new SolidColorBrush(Color.FromRgb(item.Red, item.Green, item.Blue));
                        brush.Freeze();
                        _viewModel.Layers.Add(new LayerItemViewModel
                        {
                            Name = item.Name,
                            IsVisible = item.IsVisible,
                            IsLocked = item.IsLocked,
                            ColorIndex = item.ColorIndex,
                            ColorBrush = brush
                        });
                    }
                }
            }
            finally
            {
                _refreshingLayers = false;
            }

            ApplyLayerFilter();
            RestoreLayerSelection(selectedNames);
        }

        private void ApplyLayerFilter()
        {
            var view = CollectionViewSource.GetDefaultView(_viewModel.Layers);
            if (view == null) return;

            var search = LayerSearchBox?.Text?.Trim() ?? string.Empty;
            var tokens = search
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Take(MaxLayerSearchTokens)
                .ToArray();

            view.Filter = tokens.Length == 0
                ? null
                : new Predicate<object>(item => item is LayerItemViewModel layer && tokens.All(token => MatchesLayerToken(layer, token)));
            view.Refresh();
            _viewModel.SetLayerCounts(view.Cast<object>().Count(), _viewModel.Layers.Count);
        }

        private void RestoreLayerSelection(IEnumerable<string> names)
        {
            if (LayerList == null) return;
            var selected = new HashSet<string>(names ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            LayerList.UnselectAll();
            if (selected.Count == 0) return;
            foreach (var item in LayerList.Items.Cast<LayerItemViewModel>())
                if (selected.Contains(item.Name)) LayerList.SelectedItems.Add(item);
        }

        private static bool MatchesLayerToken(LayerItemViewModel layer, string token)
        {
            if (layer.Name.IndexOf(token, StringComparison.CurrentCultureIgnoreCase) >= 0) return true;
            if (layer.ColorIndex.ToString(CultureInfo.InvariantCulture).IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (layer.IsVisible && AliasContains("hiện visible on", token)) return true;
            if (!layer.IsVisible && AliasContains("ẩn hidden off", token)) return true;
            if (layer.IsLocked && AliasContains("khóa locked lock", token)) return true;
            if (!layer.IsLocked && AliasContains("mở unlocked unlock", token)) return true;
            return false;
        }

        private static bool AliasContains(string aliases, string token) =>
            aliases.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(alias => string.Equals(alias, token, StringComparison.CurrentCultureIgnoreCase));

        private void RefreshDrawingsOnly()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var selectedDrawing = DrawingList?.SelectedItem as DrawingItemViewModel;
            _refreshingDrawings = true;
            try
            {
                _viewModel.Drawings.Clear();
                _viewModel.Drawings.Add(new DrawingItemViewModel
                {
                    Name = System.IO.Path.GetFileName(doc.Name),
                    Path = doc.Name,
                    Kind = "MODEL",
                    LockState = "—",
                    InstanceText = "—",
                    ScaleText = "—",
                    IsXref = false
                });
                foreach (var item in DrawingCatalogReader.ReadReferences(doc))
                    _viewModel.Drawings.Add(new DrawingItemViewModel
                    {
                        Name = item.Name,
                        Path = item.Path,
                        Kind = "XREF",
                        LockState = item.LockState,
                        InstanceText = item.InstanceCount.ToString(CultureInfo.InvariantCulture),
                        ScaleText = item.ScaleText,
                        IsXref = true
                    });
                if (selectedDrawing != null && DrawingList != null)
                {
                    var restored = _viewModel.Drawings.FirstOrDefault(x => x.IsXref == selectedDrawing.IsXref && string.Equals(x.Name, selectedDrawing.Name, StringComparison.OrdinalIgnoreCase));
                    DrawingList.SelectedItem = restored;
                    if (selectedDrawing.IsXref && restored == null)
                        doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                }
            }
            finally
            {
                _refreshingDrawings = false;
            }
        }

        private void RefreshAfterXrefMutation(string successStatus)
        {
            try
            {
                RefreshDrawingsOnly();
                ReloadLayers();
                _viewModel.Status = successStatus;
            }
            catch (Exception ex)
            {
                _viewModel.Status = successStatus + " • cảnh báo làm mới panel: " + ex.Message;
            }
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => Refresh();
        private void OnLayerSearchChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) ApplyLayerFilter(); }
        private void OnShowLayersClick(object sender, RoutedEventArgs e) => SetSelectedLayers(true);
        private void OnHideLayersClick(object sender, RoutedEventArgs e) => SetSelectedLayers(false);
        private void OnLockLayersClick(object sender, RoutedEventArgs e) => SetSelectedLayerLocks(true);
        private void OnUnlockLayersClick(object sender, RoutedEventArgs e) => SetSelectedLayerLocks(false);
        private void OnInvertSelectionClick(object sender, RoutedEventArgs e)
        {
            var selected = LayerList.SelectedItems.Cast<LayerItemViewModel>().ToList();
            var visible = LayerList.Items.Cast<LayerItemViewModel>().ToList();
            LayerList.UnselectAll();
            foreach (var item in visible.Where(x => !selected.Contains(x))) LayerList.SelectedItems.Add(item);
        }
        private void OnClearLayerSelectionClick(object sender, RoutedEventArgs e) => LayerList.UnselectAll();

        private void OnClearDrawingSelectionClick(object sender, RoutedEventArgs e)
        {
            _refreshingDrawings = true;
            try
            {
                DrawingList.UnselectAll();
            }
            finally
            {
                _refreshingDrawings = false;
            }
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                _viewModel.Status = "Không có bản vẽ BricsCAD đang active.";
                return;
            }
            try
            {
                doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                _viewModel.Status = "Đã bỏ chọn bản vẽ/Xref.";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể bỏ chọn CAD: " + ex.Message;
            }
        }

        private void OnDrawingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_refreshingDrawings) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var item = DrawingList.SelectedItem as DrawingItemViewModel;
            if (item == null || !item.IsXref)
            {
                try
                {
                    doc.Editor.SetImpliedSelection(Array.Empty<ObjectId>());
                    _viewModel.Status = item == null
                        ? "Đã bỏ chọn bản vẽ/Xref."
                        : "Bản vẽ chính " + item.Name + " • đã bỏ chọn Xref trong CAD.";
                }
                catch (Exception ex)
                {
                    _viewModel.Status = "Không thể bỏ chọn Xref trong CAD: " + ex.Message;
                }
                return;
            }
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
            if (_refreshingLayers) return;
            if (!(sender is CheckBox box) || !(box.DataContext is LayerItemViewModel item)) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var selected = LayerList.SelectedItems.Cast<LayerItemViewModel>().ToArray();
            var applyToSelection = selected.Length > 1 && selected.Any(candidate => ReferenceEquals(candidate, item));
            var names = applyToSelection
                ? selected.Select(candidate => candidate.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : new[] { item.Name };

            try
            {
                var count = LayerVisibilityService.SetVisible(doc, names, visible);
                _viewModel.Status = applyToSelection
                    ? (visible ? "Đã hiện " : "Đã ẩn ") + count + " layer trong cụm đang chọn."
                    : (visible ? "Hiện " : "Ẩn ") + item.Name;
                ReloadLayers();
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
                ReloadLayers();
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
                ReloadLayers();
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
                ReloadLayers();
            }
        }

        private void SetSelectedLayerLocks(bool locked)
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
                var count = LayerVisibilityService.SetLocked(doc, names, locked);
                _viewModel.Status = (locked ? "Đã khóa " : "Đã mở khóa ") + count + " layer";
                ReloadLayers();
                RefreshDrawingsOnly();
            }
            catch (Exception ex)
            {
                _viewModel.Status = ex.Message;
                ReloadLayers();
                RefreshDrawingsOnly();
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
                RefreshAfterXrefMutation("Đã nạp lại Xref " + item.Name);
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
                if (TrySend(doc, "_MOVE"))
                    _viewModel.Status = "Di chuyển " + count + " instance của " + item.Name + ".";
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
                RefreshAfterXrefMutation("Đã gỡ Xref " + item.Name);
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

        private void Send(string command)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                _viewModel.Status = "Không có bản vẽ BricsCAD đang active.";
                return;
            }
            if (TrySend(doc, command))
                _viewModel.Status = "Đã gửi lệnh " + command.Trim() + " sang " + DrawingLabel(doc) + ".";
        }

        private bool TrySend(Document document, string command)
        {
            var normalized = (command ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                _viewModel.Status = "Command rỗng; không gửi sang BricsCAD.";
                return false;
            }
            try
            {
                document.SendStringToExecute(normalized + " ", true, false, false);
                return true;
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể gửi lệnh " + normalized + ": " + ex.Message;
                return false;
            }
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }
    }
}
