using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const int ObjectSnapEndpointBit = 1;
        private const int ObjectSnapMidpointBit = 2;
        private const int ObjectSnapCenterBit = 4;
        private const int ObjectSnapNearestBit = 512;
        private const int ObjectSnapSuppressedBit = 16384;
        private const int ObjectSnapModeMask = ObjectSnapSuppressedBit - 1;

        private const string ViewportAidPanelTag = "QS3D_REFERENCE_VIEWPORT_AIDS";
        private const string LightBackgroundColor = "RGB:250,250,250";
        private const string ContrastBackgroundColor = "RGB:0,0,0";
        private const string DefaultDarkBackgroundColor = "RGB:24,25,28";

        private static readonly bool ViewAidClassHandlerRegistered = RegisterViewportAidClassHandler();

        private bool _viewportAidsApplied;
        private bool _syncingViewportAids;
        private Button? _lightBackgroundButton;
        private Button? _contrastBackgroundButton;
        private Button? _orthoModeButton;
        private Button? _objectSnapButton;
        private Button? _objectSnapMenuButton;
        private MenuItem? _endpointSnapItem;
        private MenuItem? _midpointSnapItem;
        private MenuItem? _centerSnapItem;
        private MenuItem? _nearestSnapItem;
        private string? _lightBackgroundRestoreColor;
        private string? _contrastBackgroundRestoreColor;

        private static bool RegisterViewportAidClassHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnViewportAidsLoaded),
                true);
            return true;
        }

        private static void OnViewportAidsLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.EnsureViewportAidControls();
            panel.RefreshViewportAidState();
        }

        private void EnsureViewportAidControls()
        {
            _ = ViewAidClassHandlerRegistered;
            if (_viewportAidsApplied) return;
            if (!(Content is Grid root)) return;

            Border? footer = null;
            foreach (var child in root.Children)
            {
                if (child is Border border && Grid.GetRow(border) == 2)
                {
                    footer = border;
                    break;
                }
            }
            if (!(footer?.Child is DockPanel footerDock)) return;

            StackPanel? viewportStatus = null;
            foreach (var child in footerDock.Children)
            {
                if (child is StackPanel stack &&
                    string.Equals(stack.Tag as string, ViewportAidPanelTag, StringComparison.Ordinal))
                {
                    viewportStatus = stack;
                    break;
                }
            }

            if (viewportStatus == null)
            {
                viewportStatus = new StackPanel
                {
                    Tag = ViewportAidPanelTag,
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(viewportStatus, Dock.Right);

                var contextIndex = _footerContextText == null
                    ? -1
                    : footerDock.Children.IndexOf(_footerContextText);
                if (contextIndex >= 0)
                    footerDock.Children.Insert(contextIndex, viewportStatus);
                else
                    footerDock.Children.Add(viewportStatus);
            }

            _lightBackgroundButton = CreateViewportAidButton(
                "Nền sáng",
                "Chuyển nền Model Space sang sáng bằng BKGCOLOR; bấm lại để khôi phục nền trước đó.",
                OnLightBackgroundClick);
            _contrastBackgroundButton = CreateViewportAidButton(
                "Tương phản",
                "Chuyển nền Model Space sang đen tương phản cao bằng BKGCOLOR; bấm lại để khôi phục nền trước đó.",
                OnContrastBackgroundClick);
            _orthoModeButton = CreateViewportAidButton(
                "Vuông góc",
                "Bật/tắt ORTHOMODE native của BricsCAD (tương đương Ortho/F8).",
                OnOrthoModeButtonClick);
            _objectSnapButton = CreateViewportAidButton(
                "Bắt điểm",
                "Bật/tắt Entity Snap native bằng OSMODE; giữ nguyên các kiểu snap đã cấu hình.",
                OnObjectSnapButtonClick);
            _objectSnapMenuButton = CreateViewportAidButton(
                "⌄",
                "Chọn nhanh các kiểu bắt điểm: Điểm cuối, Trung điểm, Tâm, Trên cạnh.",
                OnObjectSnapMenuButtonClick);
            _objectSnapMenuButton.MinWidth = 22;
            _objectSnapMenuButton.Padding = new Thickness(3, 1, 3, 1);
            _objectSnapMenuButton.Margin = new Thickness(0, 0, 1, 0);

            var snapMenu = new ContextMenu
            {
                Placement = PlacementMode.Top,
                PlacementTarget = _objectSnapMenuButton
            };
            _endpointSnapItem = CreateObjectSnapMenuItem("Điểm cuối", ObjectSnapEndpointBit);
            _midpointSnapItem = CreateObjectSnapMenuItem("Trung điểm", ObjectSnapMidpointBit);
            _centerSnapItem = CreateObjectSnapMenuItem("Tâm", ObjectSnapCenterBit);
            _nearestSnapItem = CreateObjectSnapMenuItem("Trên cạnh", ObjectSnapNearestBit);
            snapMenu.Items.Add(_endpointSnapItem);
            snapMenu.Items.Add(_midpointSnapItem);
            snapMenu.Items.Add(_centerSnapItem);
            snapMenu.Items.Add(_nearestSnapItem);
            snapMenu.Opened += OnObjectSnapMenuOpened;
            _objectSnapMenuButton.ContextMenu = snapMenu;

            viewportStatus.Children.Add(_lightBackgroundButton);
            viewportStatus.Children.Add(_contrastBackgroundButton);
            viewportStatus.Children.Add(_orthoModeButton);
            viewportStatus.Children.Add(_objectSnapButton);
            viewportStatus.Children.Add(_objectSnapMenuButton);
            viewportStatus.MouseEnter += OnViewportAidBarMouseEnter;
            _viewportAidsApplied = true;
        }

        private Button CreateViewportAidButton(string label, string toolTip, RoutedEventHandler handler)
        {
            var button = new Button
            {
                Content = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3, 0, 1, 0),
                Padding = new Thickness(6, 1, 6, 1),
                MinHeight = 22,
                ToolTip = toolTip,
                Focusable = false
            };
            if (TryFindResource("DenseButton") is Style denseButtonStyle)
                button.Style = denseButtonStyle;
            button.Click += handler;
            ToolTipService.SetShowDuration(button, 10000);
            return button;
        }

        private MenuItem CreateObjectSnapMenuItem(string label, int bit)
        {
            var item = new MenuItem
            {
                Header = label,
                Tag = bit,
                IsCheckable = true
            };
            item.Click += OnObjectSnapModeClick;
            return item;
        }

        private void OnViewportAidBarMouseEnter(object sender, MouseEventArgs e) => RefreshViewportAidState();

        private void OnLightBackgroundClick(object sender, RoutedEventArgs e)
        {
            ToggleViewportBackgroundPreset(
                LightBackgroundColor,
                ref _lightBackgroundRestoreColor,
                "Nền sáng");
        }

        private void OnContrastBackgroundClick(object sender, RoutedEventArgs e)
        {
            ToggleViewportBackgroundPreset(
                ContrastBackgroundColor,
                ref _contrastBackgroundRestoreColor,
                "Tương phản");
        }

        private void ToggleViewportBackgroundPreset(string preset, ref string? restoreColor, string label)
        {
            if (_syncingViewportAids) return;

            try
            {
                var current = ReadSystemVariableString("BKGCOLOR");
                string next;
                if (BackgroundColorsEqual(current, preset))
                {
                    next = string.IsNullOrWhiteSpace(restoreColor)
                        ? DefaultDarkBackgroundColor
                        : restoreColor;
                    restoreColor = null;
                }
                else
                {
                    restoreColor = current;
                    next = preset;
                }

                BcadApplication.SetSystemVariable("BKGCOLOR", next);
                RefreshViewportAidState();
                SetStatus(label + (BackgroundColorsEqual(next, preset) ? " đã bật." : " đã khôi phục."));
            }
            catch (Exception ex)
            {
                RefreshViewportAidState();
                SetStatus("Không thể đổi BKGCOLOR: " + ex.Message);
            }
        }

        private void OnOrthoModeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_syncingViewportAids) return;
            try
            {
                var enabled = ReadSystemVariableInt("ORTHOMODE") == 0;
                BcadApplication.SetSystemVariable("ORTHOMODE", (short)(enabled ? 1 : 0));
                RefreshViewportAidState();
                SetStatus(enabled ? "Vuông góc (ORTHO) đã bật." : "Vuông góc (ORTHO) đã tắt.");
            }
            catch (Exception ex)
            {
                RefreshViewportAidState();
                SetStatus("Không thể đổi ORTHOMODE: " + ex.Message);
            }
        }

        private void OnObjectSnapButtonClick(object sender, RoutedEventArgs e)
        {
            if (_syncingViewportAids) return;
            try
            {
                var current = ReadSystemVariableInt("OSMODE");
                var configuredModes = current & ObjectSnapModeMask;
                var currentlyEnabled = configuredModes != 0 && (current & ObjectSnapSuppressedBit) == 0;
                var enable = !currentlyEnabled;
                if (enable && configuredModes == 0)
                {
                    RefreshViewportAidState();
                    SetStatus("Bắt điểm chưa có kiểu snap nào được cấu hình. Mở menu cạnh Bắt điểm để chọn kiểu snap.");
                    return;
                }

                var next = enable
                    ? configuredModes
                    : configuredModes | ObjectSnapSuppressedBit;
                BcadApplication.SetSystemVariable("OSMODE", checked((short)next));
                RefreshViewportAidState();
                SetStatus(enable ? "Bắt điểm (Entity Snap) đã bật." : "Bắt điểm (Entity Snap) đã tắt; cấu hình snap được giữ nguyên.");
            }
            catch (Exception ex)
            {
                RefreshViewportAidState();
                SetStatus("Không thể đổi OSMODE: " + ex.Message);
            }
        }

        private void OnObjectSnapMenuButtonClick(object sender, RoutedEventArgs e)
        {
            if (_objectSnapMenuButton?.ContextMenu == null) return;
            RefreshObjectSnapMenuState();
            _objectSnapMenuButton.ContextMenu.PlacementTarget = _objectSnapMenuButton;
            _objectSnapMenuButton.ContextMenu.IsOpen = true;
        }

        private void OnObjectSnapMenuOpened(object sender, RoutedEventArgs e)
        {
            RefreshObjectSnapMenuState();
        }

        private void OnObjectSnapModeClick(object sender, RoutedEventArgs e)
        {
            if (_syncingViewportAids || !(sender is MenuItem item) || !(item.Tag is int bit))
                return;

            try
            {
                var current = ReadSystemVariableInt("OSMODE");
                var suppression = current & ObjectSnapSuppressedBit;
                var configuredModes = current & ObjectSnapModeMask;
                configuredModes = item.IsChecked
                    ? configuredModes | bit
                    : configuredModes & ~bit;

                var next = suppression | configuredModes;
                BcadApplication.SetSystemVariable("OSMODE", checked((short)next));
                RefreshViewportAidState();
                SetStatus((item.Header as string ?? "Bắt điểm") + (item.IsChecked ? " đã bật." : " đã tắt."));
            }
            catch (Exception ex)
            {
                RefreshViewportAidState();
                SetStatus("Không thể đổi kiểu OSMODE: " + ex.Message);
            }
        }

        private void RefreshViewportAidState()
        {
            if (_lightBackgroundButton == null ||
                _contrastBackgroundButton == null ||
                _orthoModeButton == null ||
                _objectSnapButton == null)
                return;

            _syncingViewportAids = true;
            try
            {
                var background = ReadSystemVariableString("BKGCOLOR");
                var orthoMode = ReadSystemVariableInt("ORTHOMODE");
                var osMode = ReadSystemVariableInt("OSMODE");
                var configuredModes = osMode & ObjectSnapModeMask;
                var snapsSuppressed = (osMode & ObjectSnapSuppressedBit) != 0;

                SetViewportAidButtonState(_lightBackgroundButton, BackgroundColorsEqual(background, LightBackgroundColor));
                SetViewportAidButtonState(_contrastBackgroundButton, BackgroundColorsEqual(background, ContrastBackgroundColor));
                SetViewportAidButtonState(_orthoModeButton, orthoMode != 0);
                SetViewportAidButtonState(_objectSnapButton, configuredModes != 0 && !snapsSuppressed);

                _objectSnapButton.ToolTip = configuredModes == 0
                    ? "Chưa có kiểu Entity Snap nào được cấu hình. Mở menu cạnh Bắt điểm để chọn kiểu snap."
                    : "Bật/tắt Entity Snap native bằng OSMODE; các bit kiểu snap hiện tại được giữ nguyên.";
                RefreshObjectSnapMenuState(osMode);
            }
            catch (Exception ex)
            {
                SetViewportAidButtonState(_lightBackgroundButton, false);
                SetViewportAidButtonState(_contrastBackgroundButton, false);
                SetViewportAidButtonState(_orthoModeButton, false);
                SetViewportAidButtonState(_objectSnapButton, false);
                SetObjectSnapMenuState(0);
                SetStatus("Không đọc được trạng thái Viewport Aid: " + ex.Message);
            }
            finally
            {
                _syncingViewportAids = false;
            }
        }

        private void RefreshObjectSnapMenuState()
        {
            try
            {
                RefreshObjectSnapMenuState(ReadSystemVariableInt("OSMODE"));
            }
            catch (Exception ex)
            {
                SetObjectSnapMenuState(0);
                SetStatus("Không đọc được OSMODE: " + ex.Message);
            }
        }

        private void RefreshObjectSnapMenuState(int osMode)
        {
            SetObjectSnapMenuState(osMode & ObjectSnapModeMask);
        }

        private void SetObjectSnapMenuState(int configuredModes)
        {
            if (_endpointSnapItem != null)
                _endpointSnapItem.IsChecked = (configuredModes & ObjectSnapEndpointBit) != 0;
            if (_midpointSnapItem != null)
                _midpointSnapItem.IsChecked = (configuredModes & ObjectSnapMidpointBit) != 0;
            if (_centerSnapItem != null)
                _centerSnapItem.IsChecked = (configuredModes & ObjectSnapCenterBit) != 0;
            if (_nearestSnapItem != null)
                _nearestSnapItem.IsChecked = (configuredModes & ObjectSnapNearestBit) != 0;
        }

        private void SetViewportAidButtonState(Button button, bool active)
        {
            if (TryFindResource(active ? "AccentSoftBrush" : "BgRaisedBrush") is Brush background)
                button.Background = background;
            if (TryFindResource(active ? "AccentBrush" : "BorderStrongBrush") is Brush border)
                button.BorderBrush = border;
        }

        private static bool BackgroundColorsEqual(string left, string right)
        {
            return string.Equals(
                NormalizeBackgroundColor(left),
                NormalizeBackgroundColor(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeBackgroundColor(string value)
        {
            return (value ?? string.Empty).Replace(" ", string.Empty).Trim();
        }

        private static int ReadSystemVariableInt(string name)
        {
            var value = BcadApplication.GetSystemVariable(name);
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static string ReadSystemVariableString(string name)
        {
            var value = BcadApplication.GetSystemVariable(name);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
