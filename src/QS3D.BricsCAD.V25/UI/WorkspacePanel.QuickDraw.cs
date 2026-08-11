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

        static WorkspacePanel()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspaceQuickDrawLoaded));
        }

        private static void OnWorkspaceQuickDrawLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.AttachQuickDrawInteractions();
        }

        private void AttachQuickDrawInteractions()
        {
            if (_quickDrawInteractionsAttached) return;
            _quickDrawInteractionsAttached = true;

            PreviewKeyDown += OnQuickDrawPreviewKeyDown;
            FamilyList.MouseDoubleClick += OnFamilyQuickDrawDoubleClick;

            var menu = FamilyList.ContextMenu;
            if (menu == null) return;
            if (menu.Items.OfType<MenuItem>().Any(x =>
                string.Equals(x.Tag as string, "QS3DDRAWACTIVE", StringComparison.OrdinalIgnoreCase))) return;

            var quick = CreateMenuItem("Vẽ Nhanh (Ctrl+D)", OnQuickDrawClick);
            quick.Tag = "QS3DDRAWACTIVE";
            var advanced = CreateMenuItem("Vẽ tùy chỉnh (Ctrl+Shift+D)", OnAdvancedDrawClick);
            advanced.Tag = "QS3DDRAWACTIVEADV";
            menu.Items.Insert(0, quick);
            menu.Items.Insert(1, advanced);
            menu.Items.Insert(2, new Separator());
        }

        private void OnQuickDrawPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.D) return;
            var modifiers = Keyboard.Modifiers;
            if (modifiers == ModifierKeys.Control)
            {
                ExecuteWorkspaceDraw(advanced: false);
                e.Handled = true;
                return;
            }
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
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

        private void ExecuteWorkspaceDraw(bool advanced)
        {
            try
            {
                if (!(FamilyList.SelectedItem is ProjectFamily family))
                {
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
            catch (Exception ex)
            {
                SetStatus((advanced ? "Vẽ tùy chỉnh lỗi: " : "Vẽ Nhanh lỗi: ") + ex.Message);
            }
        }
    }
}
