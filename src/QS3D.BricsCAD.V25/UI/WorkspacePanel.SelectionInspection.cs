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
                _viewModel.SetSelectedElement(null);
                return;
            }

            var handles = new HashSet<string>(
                _inspection
                    .Select(x => (x.Handle ?? string.Empty).Trim())
                    .Where(x => x.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            if (handles.Count == 0)
            {
                _viewModel.SetSelectedElement(null);
                return;
            }

            var selectedElements = project.Elements
                .Where(element => SemanticReferenceHandles.GetSelectionAliases(element).Any(handles.Contains))
                .Take(2)
                .ToList();
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
