using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Selection;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private static readonly HashSet<string> MultiSelectionSourceDerivedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LengthM",
            "AreaM2",
            "VolumeM3",
            "PerimeterM",
            "Layer",
            "MeasuredSolidVolumeM3",
            "MeasuredSolidSurfaceAreaM2"
        };

        private bool TryResolveSemanticSelection(
            ProjectState project,
            IReadOnlyList<EntitySnapshot> snapshots,
            out IReadOnlyList<ProjectElement> elements,
            out string error)
        {
            elements = Array.Empty<ProjectElement>();
            error = string.Empty;
            if (project == null)
            {
                error = "QS3D project hiện hành không khả dụng.";
                return false;
            }

            var rawHandles = (snapshots ?? Array.Empty<EntitySnapshot>())
                .Select(snapshot => (snapshot?.Handle ?? string.Empty).Trim())
                .ToArray();
            if (rawHandles.Length == 0) return true;
            if (rawHandles.Any(handle => handle.Length == 0))
            {
                error = "Selection có CAD handle rỗng; Property Inspector đã fail-closed.";
                return false;
            }

            var requestedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in rawHandles)
            {
                if (requestedHandles.Add(handle)) continue;
                error = "Selection chứa CAD handle trùng; Property Inspector đã fail-closed.";
                return false;
            }

            var matchesByHandle = requestedHandles.ToDictionary(
                handle => handle,
                _ => new List<ProjectElement>(),
                StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var alias in SemanticReferenceHandles.GetSelectionAliases(element))
                {
                    var normalized = (alias ?? string.Empty).Trim();
                    if (normalized.Length == 0 || !matchesByHandle.TryGetValue(normalized, out var matches)) continue;
                    if (!matches.Any(existing => string.Equals(existing.Id, element.Id, StringComparison.OrdinalIgnoreCase)))
                        matches.Add(element);
                }
            }

            var selected = new List<ProjectElement>(rawHandles.Length);
            var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in rawHandles)
            {
                var matches = matchesByHandle[handle];
                if (matches.Count == 0)
                {
                    error = "Selection có đối tượng CAD chưa gắn semantic QS3D; Inspector không trộn semantic và non-semantic.";
                    return false;
                }
                if (matches.Count != 1)
                {
                    error = "CAD handle " + handle + " khớp nhiều cấu kiện semantic; Property Inspector đã fail-closed.";
                    return false;
                }
                if (!selectedIds.Add(matches[0].Id))
                {
                    error = "Selection chứa nhiều CAD reference của cùng một cấu kiện semantic; Property Inspector đã fail-closed.";
                    return false;
                }
                selected.Add(matches[0]);
            }

            elements = selected;
            return true;
        }

        private void RestoreMultiSelectionPresentationState()
        {
            FamilyList.IsEnabled = true;
            if (_viewModel.PropertyScopes.Count == 2 &&
                _viewModel.PropertyScopes.Contains(WorkspaceViewModel.FamilyScope) &&
                _viewModel.PropertyScopes.Contains(WorkspaceViewModel.InstanceScope)) return;

            _viewModel.PropertyScopes.Clear();
            _viewModel.PropertyScopes.Add(WorkspaceViewModel.FamilyScope);
            _viewModel.PropertyScopes.Add(WorkspaceViewModel.InstanceScope);
        }

        private void PresentMultiSelection(ProjectState project, IReadOnlyList<ProjectElement> elements)
        {
            var ids = elements
                .Select(element => (element.Id ?? string.Empty).Trim())
                .Where(id => id.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var inspection = SemanticSelectionInspector.Inspect(project, ids);
            if (inspection.Count != elements.Count || inspection.Count < 2)
                throw new InvalidOperationException("Multi-selection presentation requires an exact semantic selection of at least two elements.");

            var scopeAnchor = elements.FirstOrDefault(element =>
                !string.IsNullOrWhiteSpace(element.FamilyId) && project.FindFamily(element.FamilyId) != null);
            _viewModel.SetSelectedElement(scopeAnchor);
            _viewModel.PropertyScopes.Clear();
            if (scopeAnchor != null)
            {
                _viewModel.PropertyScopes.Add(WorkspaceViewModel.InstanceScope);
                _viewModel.SelectedPropertyScope = WorkspaceViewModel.InstanceScope;
            }
            else
            {
                _viewModel.PropertyScopes.Add(WorkspaceViewModel.FamilyScope);
                _viewModel.SelectedPropertyScope = WorkspaceViewModel.FamilyScope;
            }

            _loadingContext = true;
            try
            {
                FamilyList.IsEnabled = false;
                _categoryFilter = inspection.HasMixedCategories ? (ElementCategory?)null : inspection.Categories.Single();
                ApplyFamilyFilter();
                var commonFamilyId = !inspection.Family.IsMixed
                    ? (inspection.Family.Value ?? string.Empty).Trim()
                    : string.Empty;
                var commonFamily = commonFamilyId.Length > 0 ? project.FindFamily(commonFamilyId) : null;
                FamilyList.SelectedItem = commonFamily;
                if (commonFamily != null) FamilyList.ScrollIntoView(commonFamily);
            }
            finally
            {
                _loadingContext = false;
            }

            _viewModel.SelectedFamilyName = BuildMultiSelectionHeader(project, inspection);
            LoadMultiSelectionRows(project, inspection);
            _viewModel.Status = inspection.Count + " cấu kiện semantic • common/mixed Inspector • bulk edit có stale-selection guard.";
        }

        private void LoadMultiSelectionRows(ProjectState project, SemanticSelectionInspection inspection)
        {
            _viewModel.Properties.Clear();
            AddMultiReadOnlyRow("SELECTION", "Số cấu kiện", inspection.Count.ToString(CultureInfo.InvariantCulture));
            AddMultiReadOnlyRow(
                "SELECTION",
                "Loại cấu kiện",
                inspection.HasMixedCategories ? "Nhiều giá trị" : inspection.Categories.Single().ToString());
            AddMultiReferenceRow(project, inspection.Family, "Family", inspection.Count);
            AddMultiReferenceRow(project, inspection.Floor, "Tầng", inspection.Count);
            AddMultiReferenceRow(project, inspection.Zone, "Zone", inspection.Count);

            foreach (var summary in inspection.Properties
                         .OrderBy(item => MultiGroupFor(item.Name), StringComparer.CurrentCultureIgnoreCase)
                         .ThenBy(item => MultiDisplayNameFor(item.Name), StringComparer.CurrentCultureIgnoreCase))
            {
                var key = summary.Name;
                var storageValue = summary.IsMixed ? string.Empty : summary.Value ?? string.Empty;
                var unit = MultiUnitFor(key);
                var readOnly = IsMultiSelectionReadOnlyKey(key);
                var label = MultiDisplayNameFor(key);
                if (summary.IsMixed)
                    label += " • Nhiều giá trị (" + summary.PresentCount + "/" + inspection.Count + ")";
                else
                    label += " • Chung";

                var row = new PropertyRowViewModel
                {
                    Group = "INSTANCE • " + MultiGroupFor(key),
                    Name = label,
                    Unit = unit,
                    IsReadOnly = readOnly,
                    CanReset = false,
                    EditorKind = MultiIsBooleanProperty(key, storageValue) && !summary.IsMixed
                        ? PropertyRowViewModel.BooleanEditor
                        : PropertyRowViewModel.TextEditor,
                    Choices = MultiIsBooleanProperty(key, storageValue) && !summary.IsMixed
                        ? new[] { "true", "false" }
                        : Array.Empty<string>()
                };

                // Presentation value is assigned before Apply so a mixed/blank sentinel cannot mutate the project.
                row.Value = summary.IsMixed ? string.Empty : MultiToDisplayValue(key, storageValue);
                if (!readOnly)
                {
                    var capturedIds = inspection.ElementIds.ToArray();
                    row.Apply = value => ApplyMultiSelectionProperty(project, capturedIds, key, unit, row, value);
                }
                _viewModel.Properties.Add(row);
            }

            foreach (var quantity in inspection.Quantities.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var label = quantity.Name + (quantity.IsMixed
                    ? " • Nhiều giá trị (" + quantity.PresentCount + "/" + inspection.Count + ")"
                    : " • Chung");
                AddMultiReadOnlyRow(
                    "KHỐI LƯỢNG / ĐO ĐẠC",
                    label,
                    quantity.IsMixed || !quantity.Value.HasValue
                        ? string.Empty
                        : quantity.Value.Value.ToString("0.###############", CultureInfo.InvariantCulture));
            }
        }

        private string ApplyMultiSelectionProperty(
            ProjectState presentedProject,
            IReadOnlyList<string> presentedIds,
            string key,
            string unit,
            PropertyRowViewModel row,
            string requestedValue)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                SetStatus("Cập nhật multi-selection bị từ chối: không có bản vẽ BricsCAD đang active.");
                return CurrentMultiDisplayValue(presentedProject, presentedIds, key);
            }

            try
            {
                if (!global::QS3D.BricsCAD.V25.ExistingProjectMutationContext.TryGet(document, out var currentProject))
                {
                    SetStatus("Cập nhật multi-selection bị từ chối: QS3D project hiện hành không còn khả dụng.");
                    return CurrentMultiDisplayValue(presentedProject, presentedIds, key);
                }
                if (!ReferenceEquals(currentProject, presentedProject))
                {
                    SetStatus("Cập nhật multi-selection bị từ chối: Inspector đang giữ project stale sau reload/thay thế. Hãy Refresh Workspace.");
                    return CurrentMultiDisplayValue(presentedProject, presentedIds, key);
                }
                if (!TryResolveSemanticSelection(currentProject, _inspection, out var currentElements, out var selectionError))
                {
                    SetStatus("Cập nhật multi-selection bị từ chối: " + selectionError);
                    return CurrentMultiDisplayValue(currentProject, presentedIds, key);
                }

                var currentIds = currentElements.Select(element => element.Id).ToArray();
                if (!SameSemanticSelection(presentedIds, currentIds))
                {
                    SetStatus("Cập nhật multi-selection bị từ chối: CAD selection đã thay đổi sau khi Inspector được dựng.");
                    return CurrentMultiDisplayValue(currentProject, presentedIds, key);
                }

                var before = SemanticSelectionInspector.Inspect(currentProject, presentedIds);
                var beforeSummary = before.Properties.FirstOrDefault(item => string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase));
                var previousStorage = beforeSummary == null || beforeSummary.IsMixed ? string.Empty : beforeSummary.Value ?? string.Empty;
                var nextStorage = NormalizeMultiPropertyValue(key, unit, previousStorage, requestedValue, out var valid);
                if (!valid)
                    return beforeSummary == null || beforeSummary.IsMixed ? string.Empty : MultiToDisplayValue(key, previousStorage);

                var result = ExecuteAtomic(
                    currentProject,
                    () => new SemanticSelectionBulkEditService().SetProperty(currentProject, presentedIds, key, nextStorage),
                    "Cập nhật Property multi-selection từ Workspace");

                var after = SemanticSelectionInspector.Inspect(currentProject, presentedIds);
                var afterSummary = after.Properties.FirstOrDefault(item => string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase));
                row.CanReset = false;
                SetStatus(result.ChangedCount == 0
                    ? "Multi-selection: " + MultiDisplayNameFor(key) + " không thay đổi; stale/project guards đã được revalidate."
                    : "Multi-selection: đã cập nhật " + MultiDisplayNameFor(key) + " cho " + result.ChangedCount + "/" + result.SelectedCount + " cấu kiện.");
                return afterSummary == null || afterSummary.IsMixed
                    ? string.Empty
                    : MultiToDisplayValue(key, afterSummary.Value ?? nextStorage);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is FormatException || ex is OverflowException)
            {
                SetStatus("Không thể cập nhật multi-selection " + MultiDisplayNameFor(key) + ": " + ex.Message);
                return CurrentMultiDisplayValue(presentedProject, presentedIds, key);
            }
        }

        private static bool SameSemanticSelection(IEnumerable<string> expected, IEnumerable<string> current)
        {
            var left = expected.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ThenBy(id => id, StringComparer.Ordinal).ToArray();
            var right = current.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ThenBy(id => id, StringComparer.Ordinal).ToArray();
            return left.Length == right.Length && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
        }

        private static string CurrentMultiDisplayValue(ProjectState project, IReadOnlyList<string> elementIds, string key)
        {
            try
            {
                var inspection = SemanticSelectionInspector.Inspect(project, elementIds);
                var summary = inspection.Properties.FirstOrDefault(item => string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase));
                return summary == null || summary.IsMixed ? string.Empty : MultiToDisplayValue(key, summary.Value);
            }
            catch
            {
                return string.Empty;
            }
        }

        private void AddMultiReferenceRow(ProjectState project, SemanticSelectionTextValue summary, string label, int selectionCount)
        {
            var suffix = summary.IsMixed
                ? " • Nhiều giá trị (" + summary.PresentCount + "/" + selectionCount + ")"
                : " • Chung";
            var value = summary.IsMixed ? string.Empty : ResolveMultiReferenceDisplay(project, summary.Name, summary.Value);
            AddMultiReadOnlyRow("SELECTION", label + suffix, value);
        }

        private void AddMultiReadOnlyRow(string group, string name, string value)
        {
            var row = new PropertyRowViewModel
            {
                Group = group,
                Name = name,
                IsReadOnly = true,
                CanReset = false,
                EditorKind = PropertyRowViewModel.TextEditor
            };
            row.Value = value ?? string.Empty;
            _viewModel.Properties.Add(row);
        }

        private static string ResolveMultiReferenceDisplay(ProjectState project, string referenceName, string? value)
        {
            var id = (value ?? string.Empty).Trim();
            if (id.Length == 0) return "—";
            if (string.Equals(referenceName, "FamilyId", StringComparison.OrdinalIgnoreCase))
                return project.FindFamily(id)?.Name ?? id;
            if (string.Equals(referenceName, "FloorId", StringComparison.OrdinalIgnoreCase))
                return project.FindFloor(id)?.Name ?? id;
            if (string.Equals(referenceName, "ZoneId", StringComparison.OrdinalIgnoreCase))
                return project.FindZone(id)?.Name ?? id;
            return id;
        }

        private static string BuildMultiSelectionHeader(ProjectState project, SemanticSelectionInspection inspection)
        {
            var category = inspection.HasMixedCategories ? "nhiều loại" : inspection.Categories.Single().ToString();
            var family = inspection.Family.IsMixed
                ? "nhiều Family"
                : ResolveMultiReferenceDisplay(project, inspection.Family.Name, inspection.Family.Value);
            return inspection.Count + " cấu kiện • " + category + " • " + family;
        }

        private static bool IsMultiSelectionReadOnlyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return true;
            var normalized = key.Trim();
            if (MultiSelectionSourceDerivedKeys.Contains(normalized) || normalized.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("ElementId", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Category", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("FamilyId", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("FloorId", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("ZoneId", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("Ids", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("Ref", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("Refs", StringComparison.OrdinalIgnoreCase) ||
                normalized.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private string NormalizeMultiPropertyValue(string key, string unit, string previousValue, string value, out bool valid)
        {
            var next = (value ?? string.Empty).Trim();
            if (MultiIsBooleanProperty(key, previousValue))
            {
                if (!TryMultiBoolean(next, out var boolean))
                {
                    SetStatus(MultiDisplayNameFor(key) + ": chỉ nhận Bật/Tắt (true/false)." );
                    valid = false;
                    return previousValue;
                }
                valid = true;
                return boolean ? "true" : "false";
            }

            if (unit.Length > 0 || MultiIsNumericProperty(key))
            {
                if (!TryMultiFiniteNumber(next, out var number))
                {
                    SetStatus(MultiDisplayNameFor(key) + ": giá trị số không hợp lệ; đã giữ trạng thái selection hiện tại.");
                    valid = false;
                    return previousValue;
                }
                if (MultiUsesMillimeterPresentation(key)) number /= 1000d;
                if (MultiRequiresPositiveNumber(key) && !(number > 0d))
                {
                    SetStatus(MultiDisplayNameFor(key) + ": phải lớn hơn 0.");
                    valid = false;
                    return previousValue;
                }
                if (MultiRequiresNonNegativeNumber(key) && number < 0d)
                {
                    SetStatus(MultiDisplayNameFor(key) + ": không được âm.");
                    valid = false;
                    return previousValue;
                }
                valid = true;
                return number.ToString("R", CultureInfo.InvariantCulture);
            }

            valid = true;
            return next;
        }

        private static bool TryMultiFiniteNumber(string value, out double number)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
                !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number)) return false;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private static bool TryMultiBoolean(string value, out bool boolean)
        {
            var text = (value ?? string.Empty).Trim();
            if (bool.TryParse(text, out boolean)) return true;
            if (text == "1" || text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text.Equals("on", StringComparison.OrdinalIgnoreCase) || text.Equals("bật", StringComparison.CurrentCultureIgnoreCase))
            {
                boolean = true;
                return true;
            }
            if (text == "0" || text.Equals("no", StringComparison.OrdinalIgnoreCase) || text.Equals("off", StringComparison.OrdinalIgnoreCase) || text.Equals("tắt", StringComparison.CurrentCultureIgnoreCase))
            {
                boolean = false;
                return true;
            }
            boolean = false;
            return false;
        }

        private static bool MultiIsBooleanProperty(string key, string? current)
        {
            if (MultiIsNumericProperty(key)) return false;
            if (TryMultiBoolean(current ?? string.Empty, out _)) return true;
            if (string.IsNullOrWhiteSpace(key)) return false;
            return key.StartsWith("Is", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("Has", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("Enable", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("CloseProfile", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("FreeformProfile", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Visible", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Enabled", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Locked", StringComparison.OrdinalIgnoreCase);
        }

        private static bool MultiIsNumericProperty(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (SemanticPropertyUnitClassifier.IsLinearMeterProperty(key) ||
                key.EndsWith("M2", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("M3", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Mm", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Deg", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Count", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Ratio", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Factor", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Percent", StringComparison.OrdinalIgnoreCase)) return true;
            return key.IndexOf("Tolerance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Confidence", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool MultiRequiresPositiveNumber(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (key.Equals("SillHeightM", StringComparison.OrdinalIgnoreCase)) return false;
            return key.IndexOf("Thickness", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Width", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Depth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Diameter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Spacing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Radius", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.EndsWith("HeightM", StringComparison.OrdinalIgnoreCase);
        }

        private static bool MultiRequiresNonNegativeNumber(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return key.Equals("SillHeightM", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("BooleanClearanceM", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("CoverM", StringComparison.OrdinalIgnoreCase);
        }

        private static string MultiToDisplayValue(string key, string? storageValue)
        {
            var raw = (storageValue ?? string.Empty).Trim();
            if (!MultiUsesMillimeterPresentation(key) || !TryMultiFiniteNumber(raw, out var value)) return raw;
            return (value * 1000d).ToString("0.###############", CultureInfo.InvariantCulture);
        }

        private static bool MultiUsesMillimeterPresentation(string key) => SemanticPropertyUnitClassifier.IsLinearMeterProperty(key);

        private static string MultiUnitFor(string key)
        {
            if (key.EndsWith("Mm", StringComparison.OrdinalIgnoreCase)) return "mm";
            if (key.EndsWith("M2", StringComparison.OrdinalIgnoreCase)) return "m²";
            if (key.EndsWith("M3", StringComparison.OrdinalIgnoreCase)) return "m³";
            if (MultiUsesMillimeterPresentation(key)) return "mm";
            if (key.EndsWith("Deg", StringComparison.OrdinalIgnoreCase)) return "°";
            if (key.EndsWith("Percent", StringComparison.OrdinalIgnoreCase)) return "%";
            return string.Empty;
        }

        private static string MultiGroupFor(string key)
        {
            if (key.IndexOf("Rebar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Diameter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Spacing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Cover", StringComparison.OrdinalIgnoreCase) >= 0) return "CỐT THÉP";
            if (key.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Classification", StringComparison.OrdinalIgnoreCase) >= 0) return "VẬT LIỆU / PHÂN LOẠI";
            if (key.IndexOf("Offset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Elevation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Sill", StringComparison.OrdinalIgnoreCase) >= 0) return "VỊ TRÍ / CAO ĐỘ";
            if (key.IndexOf("Length", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Width", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Height", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Depth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Thickness", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Perimeter", StringComparison.OrdinalIgnoreCase) >= 0) return "HÌNH HỌC";
            return "THUỘC TÍNH";
        }

        private static string MultiDisplayNameFor(string key)
        {
            switch (key)
            {
                case "ThicknessM": return "Bề dày";
                case "WidthM": return "Bề rộng";
                case "DepthM": return "Chiều sâu";
                case "HeightM": return "Chiều cao";
                case "LengthM": return "Chiều dài";
                case "AreaM2": return "Diện tích";
                case "VolumeM3": return "Thể tích";
                case "PerimeterM": return "Chu vi";
                case "BottomOffsetM": return "Offset đáy (so với source)";
                case "TopOffsetM": return "Offset đỉnh (so với source)";
                case "SillHeightM": return "Cao độ bậu";
                case "Material": return "Vật liệu";
                case "ClassificationCode": return "Mã phân loại";
                case "RebarNotation": return "Ký hiệu cốt thép";
                case "CoverM": return "Lớp bê tông bảo vệ";
                case "RebarDiameterMm": return "Đường kính cốt thép";
                case "DiameterMm": return "Đường kính";
                case "SpacingMm": return "Khoảng cách";
                default: return key;
            }
        }
    }
}
