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
            menu.Items.Insert(0, quick);
            menu.Items.Insert(1, new Separator());
        }

        private void OnQuickDrawPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control || e.Key != Key.D) return;
            ExecuteWorkspaceQuickDraw();
            e.Handled = true;
        }

        private void OnFamilyQuickDrawDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            var item = FindContainer<ListBoxItem>(FamilyList, e.OriginalSource as DependencyObject);
            if (item == null) return;
            item.IsSelected = true;
            ExecuteWorkspaceQuickDraw();
            e.Handled = true;
        }

        private void OnQuickDrawClick(object sender, RoutedEventArgs e) => ExecuteWorkspaceQuickDraw();

        private void ExecuteWorkspaceQuickDraw()
        {
            try
            {
                if (!(FamilyList.SelectedItem is ProjectFamily family))
                {
                    SetStatus("Chọn một Family / Type trước khi Vẽ Nhanh.");
                    return;
                }

                // Reuse the same canonical active-Family write used by other Workspace authoring
                // actions. QS3DDRAWACTIVE itself remains read-only/non-creating and owns no geometry.
                _viewModel.SetActiveFamily(family);
                SetStatus("Vẽ Nhanh → " + family.Name + " • " + family.Category);
                Send("QS3DDRAWACTIVE");
            }
            catch (Exception ex)
            {
                SetStatus("Vẽ Nhanh lỗi: " + ex.Message);
            }
        }
    }
}
