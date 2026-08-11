using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Model;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        internal void SetInspectionReadOnly(IReadOnlyList<EntitySnapshot> snapshots, ProjectState? project)
        {
            _inspection = snapshots ?? Array.Empty<EntitySnapshot>();
            InspectionList.ItemsSource = null;
            InspectionList.ItemsSource = _inspection;
            SelectionCount.Text = _inspection.Count + " chọn";

            if (project == null || _inspection.Count == 0)
            {
                RestoreMultiSelectionPresentationState();
                _viewModel.SetSelectedElement(null);
                return;
            }

            if (!TryResolveSemanticSelection(project, _inspection, out var selectedElements, out var selectionError))
            {
                RestoreMultiSelectionPresentationState();
                _viewModel.SetSelectedElement(null);
                if (!string.IsNullOrWhiteSpace(selectionError)) SetStatus(selectionError);
                return;
            }

            if (selectedElements.Count > 1)
            {
                PresentMultiSelection(project, selectedElements);
                return;
            }

            RestoreMultiSelectionPresentationState();
            var singleElement = selectedElements.Count == 1 ? selectedElements[0] : null;
            if (singleElement == null)
            {
                _viewModel.SetSelectedElement(null);
                return;
            }

            var family = string.IsNullOrWhiteSpace(singleElement.FamilyId)
                ? null
                : project.FindFamily(singleElement.FamilyId);
            _loadingContext = true;
            try
            {
                _categoryFilter = family?.Category ?? singleElement.Category;
                ApplyFamilyFilter();
                if (family != null)
                {
                    var visibleFamily = FamilyList.Items
                        .Cast<object>()
                        .OfType<ProjectFamily>()
                        .FirstOrDefault(item => string.Equals(item.Id, family.Id, StringComparison.OrdinalIgnoreCase));
                    if (visibleFamily != null)
                    {
                        FamilyList.SelectedItem = visibleFamily;
                        FamilyList.ScrollIntoView(visibleFamily);
                    }
                }
                _viewModel.SetSelectedElement(singleElement);
            }
            finally { _loadingContext = false; }
        }
    }
}
