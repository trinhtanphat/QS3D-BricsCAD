using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        static WorkspacePanel()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnWorkspaceSelectionChangedClass),
                true);
        }

        private static void OnWorkspaceSelectionChangedClass(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !ReferenceEquals(e.OriginalSource, panel.FamilyList))
                return;

            // Class handlers run before the XAML instance handler. Marking the FamilyList
            // event handled prevents the legacy void SetActiveFamily path from rendering
            // property rows when activation was rejected for a stale project generation.
            e.Handled = true;
            panel.OnFamilySelectionChangedWithAffinity();
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
