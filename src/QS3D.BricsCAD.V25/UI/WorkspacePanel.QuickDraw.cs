using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private bool _quickDrawInteractionsAttached;

        private void AttachQuickDrawInteractions()
        {
            if (_quickDrawInteractionsAttached) return;
            _quickDrawInteractionsAttached = true;

            PreviewKeyDown += OnQuickDrawPreviewKeyDown;
            FamilyList.MouseDoubleClick += OnFamilyQuickDrawDoubleClick;
            EnsureSlabOpeningWorkspaceRoute();
            AttachFamilySubtypeInteractions();

            var menu = FamilyList.ContextMenu;
            if (menu == null) return;
            if (menu.Items.OfType<MenuItem>().Any(x =>
                string.Equals(x.Tag as string, "QS3DDRAWACTIVE", StringComparison.OrdinalIgnoreCase))) return;

            var quick = CreateMenuItem("Vẽ Nhanh (Ctrl+D)", OnQuickDrawClick);
            quick.Tag = "QS3DDRAWACTIVE";
            var advanced = CreateMenuItem("Vẽ tùy chỉnh (Ctrl+Shift+D)", OnAdvancedDrawClick);
            advanced.Tag = "QS3DDRAWACTIVEADV";

            var line = CreateMenuItem("Vẽ cơ bản • Đường (Ctrl+1)", OnBasicLineClick);
            line.Tag = "QS3DDRAWLINE";
            var rectangle = CreateMenuItem("Vẽ cơ bản • Chữ nhật (Ctrl+2)", OnBasicRectangleClick);
            rectangle.Tag = "QS3DDRAWRECT";
            var circle = CreateMenuItem("Vẽ cơ bản • Hình tròn (Ctrl+3)", OnBasicCircleClick);
            circle.Tag = "QS3DDRAWCIRCLE";

            menu.Items.Insert(0, quick);
            menu.Items.Insert(1, advanced);
            menu.Items.Insert(2, new Separator());
            menu.Items.Insert(3, line);
            menu.Items.Insert(4, rectangle);
            menu.Items.Insert(5, circle);
            menu.Items.Insert(6, new Separator());
        }

        private void OnQuickDrawPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            if (modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.D)
                {
                    ExecuteWorkspaceDraw(advanced: false);
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                {
                    ExecuteWorkspaceBasicDraw("QS3DDRAWLINE", "Đường");
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                {
                    ExecuteWorkspaceBasicDraw("QS3DDRAWRECT", "Chữ nhật");
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                {
                    ExecuteWorkspaceBasicDraw("QS3DDRAWCIRCLE", "Hình tròn");
                    e.Handled = true;
                    return;
                }
            }
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.D)
            {
                ExecuteWorkspaceDraw(advanced: true);
                e.Handled = true;
            }
        }

        private void OnFamilyQuickDrawDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            var item = FindContainer<ListBoxItem>(FamilyList, e.OriginalSource as DependencyObject);
            if (item == null) return;
            item.IsSelected = true;
            ExecuteWorkspaceDraw(advanced: false);
            e.Handled = true;
        }

        private void OnQuickDrawClick(object sender, RoutedEventArgs e) => ExecuteWorkspaceDraw(advanced: false);
        private void OnAdvancedDrawClick(object sender, RoutedEventArgs e) => ExecuteWorkspaceDraw(advanced: true);
        private void OnBasicLineClick(object sender, RoutedEventArgs e) => ExecuteWorkspaceBasicDraw("QS3DDRAWLINE", "Đường");
        private void OnBasicRectangleClick(object sender, RoutedEventArgs e) => ExecuteWorkspaceBasicDraw("QS3DDRAWRECT", "Chữ nhật");
        private void OnBasicCircleClick(object sender, RoutedEventArgs e) => ExecuteWorkspaceBasicDraw("QS3DDRAWCIRCLE", "Hình tròn");

        private void ExecuteWorkspaceDraw(bool advanced)
        {
            try
            {
                var family = ResolveWorkspaceDrawFamily();
                if (family == null)
                {
                    if (!IsSlabOpeningWorkspaceRouteSelected())
                        SetStatus("Chọn một Family / Type trước khi vẽ.");
                    return;
                }

                // Reuse the same canonical active-Family write used by other Workspace authoring
                // actions. Both active-family dispatchers remain read-only/non-creating and own no geometry.
                _viewModel.SetActiveFamily(family);
                var command = advanced ? "QS3DDRAWACTIVEADV" : "QS3DDRAWACTIVE";
                SetStatus((advanced ? "Vẽ tùy chỉnh → " : "Vẽ Nhanh → ") + family.Name + " • " + family.Category);
                Send(command);
            }
            catch
            {
                SetStatus(advanced
                    ? "Không thể bắt đầu Vẽ tùy chỉnh. Hãy thử lại hoặc chọn lại Family / Type."
                    : "Không thể bắt đầu Vẽ Nhanh. Hãy thử lại hoặc chọn lại Family / Type.");
            }
        }

        private void ExecuteWorkspaceBasicDraw(string command, string label)
        {
            try
            {
                if (IsSlabOpeningWorkspaceRouteSelected())
                {
                    SetStatus("Lỗ Mở Sàn chỉ dùng Vẽ Nhanh / Vẽ tùy chỉnh để giữ exact slabOpen, hướng -Z và Auto BoolSubtract.");
                    return;
                }

                if (!(FamilyList.SelectedItem is ProjectFamily family))
                {
                    SetStatus("Chọn một Family / Type trước khi dùng Vẽ cơ bản.");
                    return;
                }

                // Make the selected row the canonical ActiveFamily before dispatch. The command
                // captures and revalidates this context around point acquisition, so a later
                // Family/Zone/Floor/property change cannot be committed under stale context.
                _viewModel.SetActiveFamily(family);
                SetStatus("Vẽ cơ bản " + label + " → " + family.Name + " • " + family.Category);
                Send(command);
            }
            catch
            {
                SetStatus("Không thể bắt đầu Vẽ cơ bản " + label + ". Hãy thử lại hoặc chọn lại Family / Type.");
            }
        }
    }
}
