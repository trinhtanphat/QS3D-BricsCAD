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
            _inspection.Clear();
            if (snapshots != null) _inspection.AddRange(snapshots);
            InspectionList.ItemsSource = null;
            InspectionList.ItemsSource = _inspection;
            SelectionCount.Text = _inspection.Count + " selected";

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

            _viewModel.SetSelectedElement(singleElement);
            if (!string.IsNullOrWhiteSpace(singleElement.FamilyId))
            {
                var family = project.FindFamily(singleElement.FamilyId);
                if (family != null)
                {
                    FilterFamily(family.Category);
                    return;
                }
            }

            FilterFamily(singleElement.Category);
        }
    }
}
