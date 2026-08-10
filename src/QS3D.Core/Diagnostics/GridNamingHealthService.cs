using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GridNamingHealthService
    {
        private const int MaxLabelLength = 64;
        private const int MaxSequenceIndex = 999999;

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            var labelOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in project.Elements)
            {
                if (element.Category != ElementCategory.Grid) continue;

                var hasLabelProperty = element.Properties.TryGetValue(GridNamingService.GridLabelKey, out var rawLabel);
                var label = (rawLabel ?? string.Empty).Trim();
                var hasSequenceProperty = element.Properties.TryGetValue(GridNamingService.GridSequenceIndexKey, out var rawSequence);

                if (hasLabelProperty && label.Length == 0)
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_LABEL_EMPTY",
                        HealthSeverity.Warning,
                        "Grid có thuộc tính nhãn nhưng giá trị đang rỗng.",
                        element.Id));
                }
                else if (label.Length > 0)
                {
                    if (label.Length > MaxLabelLength)
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_LABEL_TOO_LONG",
                            HealthSeverity.Error,
                            "Nhãn Grid vượt giới hạn " + MaxLabelLength + " ký tự của semantic naming contract.",
                            element.Id));
                    }

                    if (labelOwners.TryGetValue(label, out var existingOwner) &&
                        !string.Equals(existingOwner, element.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new ModelHealthIssue(
                            "GRID_LABEL_DUPLICATE",
                            HealthSeverity.Error,
                            "Nhãn Grid bị trùng với Grid " + existingOwner + ": " + label,
                            element.Id));
                        issues.Add(new ModelHealthIssue(
                            "GRID_LABEL_DUPLICATE",
                            HealthSeverity.Error,
                            "Nhãn Grid bị trùng với Grid " + element.Id + ": " + label,
                            existingOwner));
                    }
                    else
                    {
                        labelOwners[label] = element.Id;
                    }
                }

                if (!hasSequenceProperty) continue;
                var sequenceText = (rawSequence ?? string.Empty).Trim();
                if (!int.TryParse(sequenceText, NumberStyles.None, CultureInfo.InvariantCulture, out var sequenceIndex) ||
                    sequenceIndex < 1 || sequenceIndex > MaxSequenceIndex)
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_SEQUENCE_INVALID",
                        HealthSeverity.Error,
                        "GridSequenceIndex phải là số nguyên từ 1 đến " + MaxSequenceIndex + ".",
                        element.Id));
                    continue;
                }

                if (label.Length == 0)
                {
                    issues.Add(new ModelHealthIssue(
                        "GRID_SEQUENCE_WITHOUT_LABEL",
                        HealthSeverity.Warning,
                        "Grid có sequence index nhưng chưa có nhãn semantic hợp lệ.",
                        element.Id));
                }
            }

            return issues.AsReadOnly();
        }
    }
}
