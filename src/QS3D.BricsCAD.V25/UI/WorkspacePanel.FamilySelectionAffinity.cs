using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        static WorkspacePanel()
        {
            // The XAML SelectionChanged handler is attached to the ListBox source itself.
            // Register on ListBox (not WorkspacePanel) so this class handler runs before
            // that instance handler. The callback filters back to this panel's FamilyList.
            EventManager.RegisterClassHandler(
                typeof(ListBox),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnFamilyListSelectionChangedClass),
                true);
        }

        private static void OnFamilyListSelectionChangedClass(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListBox familyList) ||
                !string.Equals(familyList.Name, "FamilyList", StringComparison.Ordinal))
                return;

            var panel = FindOwningWorkspacePanel(familyList);
            if (panel == null || !ReferenceEquals(familyList, panel.FamilyList))
                return;

            // A ListBox class handler runs before the source ListBox's instance/XAML
            // handler. Suppress that legacy void SetActiveFamily path and route through
            // the affinity-safe path below instead.
            e.Handled = true;
            panel.OnFamilySelectionChangedWithAffinity();
        }

        private static WorkspacePanel? FindOwningWorkspacePanel(DependencyObject current)
        {
            DependencyObject? node = current;
            while (node != null)
            {
                if (node is WorkspacePanel panel) return panel;
                node = VisualTreeHelper.GetParent(node);
            }
            return null;
        }

        private void OnFamilySelectionChangedWithAffinity()
        {
            if (_loadingContext) return;

            try
            {
                var selectedFamily = FamilyList.SelectedItem as ProjectFamily;
                if (selectedFamily == null)
                {
                    // Re-resolve from the active document instead of retaining the previous
                    // ViewModel Family when filtering or collection reconciliation clears selection.
                    RefreshProject();
                    return;
                }

                if (!TryActivateFamilyForWorkspaceAction(selectedFamily, "Đổi Family active"))
                {
                    // The selected item belongs to an obsolete project generation (or the
                    // document/project changed). Reconcile before any old property rows render.
                    RefreshProject();
                    return;
                }

                _viewModel.ShowFamilyProperties();
            }
            catch (Exception)
            {
                ReportWorkspaceFailure("Đổi Family active");
            }
        }
    }
}
