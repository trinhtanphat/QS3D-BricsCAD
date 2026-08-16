using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private static readonly bool FamilySubtypeSelectionSyncRegistered =
            RegisterFamilySubtypeSelectionSync();

        private static bool RegisterFamilySubtypeSelectionSync()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnWorkspaceSelectionChangedForSubtypeSync),
                true);
            return true;
        }

        private static void OnWorkspaceSelectionChangedForSubtypeSync(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) ||
                !ReferenceEquals(e.OriginalSource, panel.FamilyList))
                return;

            panel.SyncFamilySubtypeToProgrammaticSelection();
        }

        private void SyncFamilySubtypeToProgrammaticSelection()
        {
            if (!FamilySubtypeSelectionSyncRegistered ||
                !_loadingContext ||
                _applyingFamilySubtypeFilter ||
                _inspection.Count == 0 ||
                !(FamilyList.SelectedItem is ProjectFamily family))
                return;

            var inferred = family.Category == ElementCategory.Foundation
                ? InferFoundationSubtype(family.Name)
                : string.Empty;
            if (string.Equals(_familySubtypeFilter, inferred, StringComparison.OrdinalIgnoreCase))
                return;

            _familySubtypeFilter = inferred;
            _categoryFilter = family.Category;
            if (inferred.Length == 0)
                ApplyFamilyFilter();
            else
                ApplyFamilySubtypeFilter();
        }
    }
}
