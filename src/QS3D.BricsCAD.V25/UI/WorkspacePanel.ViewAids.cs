using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const int ObjectSnapSuppressedBit = 16384;
        private const int ObjectSnapModeMask = ObjectSnapSuppressedBit - 1;
        private static readonly bool ViewAidClassHandlerRegistered = RegisterViewportAidClassHandler();

        private bool _viewportAidsApplied;
        private bool _syncingViewportAids;
        private CheckBox? _orthoModeCheck;
        private CheckBox? _objectSnapCheck;

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
                    DockPanel.GetDock(stack) == Dock.Right &&
                    stack.Orientation == Orientation.Horizontal)
                {
                    viewportStatus = stack;
                    break;
                }
            }
            if (viewportStatus == null) return;

            _orthoModeCheck = CreateViewportAidCheckBox(
                "Vuông góc",
                "Bật/tắt ORTHOMODE native của BricsCAD (tương đương trạng thái Ortho/F8).",
                OnOrthoModeCheckClick);
            _objectSnapCheck = CreateViewportAidCheckBox(
                "Bắt điểm",
                "Bật/tắt Entity Snap native bằng OSMODE; QS3D giữ nguyên các kiểu snap bạn đã cấu hình.",
                OnObjectSnapCheckClick);

            viewportStatus.Children.Add(_orthoModeCheck);
            viewportStatus.Children.Add(_objectSnapCheck);
            viewportStatus.MouseEnter += OnViewportAidBarMouseEnter;
            _viewportAidsApplied = true;
        }

        private CheckBox CreateViewportAidCheckBox(string label, string toolTip, RoutedEventHandler handler)
        {
            var checkBox = new CheckBox
            {
                Content = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(7, 0, 1, 0),
                Padding = new Thickness(2, 0, 2, 0),
                ToolTip = toolTip,
                Focusable = false
            };
            checkBox.Click += handler;
            ToolTipService.SetShowDuration(checkBox, 10000);
            return checkBox;
        }

        private void OnViewportAidBarMouseEnter(object sender, MouseEventArgs e) => RefreshViewportAidState();

        private void OnOrthoModeCheckClick(object sender, RoutedEventArgs e)
        {
            if (_syncingViewportAids || _orthoModeCheck == null) return;
            var enabled = _orthoModeCheck.IsChecked == true;
            try
            {
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

        private void OnObjectSnapCheckClick(object sender, RoutedEventArgs e)
        {
            if (_syncingViewportAids || _objectSnapCheck == null) return;
            var enable = _objectSnapCheck.IsChecked == true;
            try
            {
                var current = ReadSystemVariableInt("OSMODE");
                var configuredModes = current & ObjectSnapModeMask;
                if (enable && configuredModes == 0)
                {
                    RefreshViewportAidState();
                    SetStatus("Bắt điểm chưa có kiểu snap nào được cấu hình. Hãy cấu hình Entity Snap trong BricsCAD rồi bật lại.");
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

        private void RefreshViewportAidState()
        {
            if (_orthoModeCheck == null || _objectSnapCheck == null) return;
            _syncingViewportAids = true;
            try
            {
                var orthoMode = ReadSystemVariableInt("ORTHOMODE");
                var osMode = ReadSystemVariableInt("OSMODE");
                var configuredModes = osMode & ObjectSnapModeMask;
                var snapsSuppressed = (osMode & ObjectSnapSuppressedBit) != 0;

                _orthoModeCheck.IsChecked = orthoMode != 0;
                _objectSnapCheck.IsChecked = configuredModes != 0 && !snapsSuppressed;
                _objectSnapCheck.ToolTip = configuredModes == 0
                    ? "Chưa có kiểu Entity Snap nào được cấu hình trong BricsCAD; QS3D sẽ không tự chọn preset thay bạn."
                    : "Bật/tắt Entity Snap native bằng OSMODE; các bit kiểu snap hiện tại được giữ nguyên.";
            }
            catch (Exception ex)
            {
                _orthoModeCheck.IsChecked = false;
                _objectSnapCheck.IsChecked = false;
                SetStatus("Không đọc được trạng thái Viewport Aid: " + ex.Message);
            }
            finally
            {
                _syncingViewportAids = false;
            }
        }

        private static int ReadSystemVariableInt(string name)
        {
            var value = BcadApplication.GetSystemVariable(name);
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
