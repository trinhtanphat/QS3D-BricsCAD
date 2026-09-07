using System;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
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
