using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class WorkspaceViewModel : INotifyPropertyChanged
    {
        public const string FamilyScope = "Family / Type";
        public const string InstanceScope = "Đối tượng / Instance";

        private static readonly string[] SourceDerivedInstanceKeys =
        {
            "LengthM",
            "AreaM2",
            "VolumeM3",
            "PerimeterM",
            "Layer",
            MeasuredSolidQuantityPolicy.VolumeProperty,
            MeasuredSolidQuantityPolicy.SurfaceAreaProperty
        };

        private string _status = "Sẵn sàng";
        private string _selectedFamilyName = string.Empty;
        private string _selectedPropertyScope = FamilyScope;
        private ProjectState? _project;
        private ProjectFamily? _selectedFamily;
        private ProjectElement? _selectedElement;
        private Func<ProjectFamily, bool>? _familyPropertyPresenter;

        public ObservableCollection<string> Zones { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Floors { get; } = new ObservableCollection<string>();
        public ObservableCollection<ProjectFamily> Families { get; } = new ObservableCollection<ProjectFamily>();
        public ObservableCollection<PropertyRowViewModel> Properties { get; } = new ObservableCollection<PropertyRowViewModel>();
        public ObservableCollection<string> PropertyScopes { get; } = new ObservableCollection<string> { FamilyScope, InstanceScope };
        public string Status { get => _status; set { if (_status == value) return; _status = value ?? string.Empty; OnChanged(); } }
        public string SelectedFamilyName { get => _selectedFamilyName; set { if (_selectedFamilyName == value) return; _selectedFamilyName = value ?? string.Empty; OnChanged(); } }
        public string SelectedPropertyScope
        {
            get => _selectedPropertyScope;
            set
            {
                var requested = string.Equals(value, InstanceScope, StringComparison.Ordinal) ? InstanceScope : FamilyScope;
                if (requested == InstanceScope && _selectedElement == null)
                {
                    Status = "Chọn một cấu kiện semantic trước khi chuyển sang thuộc tính Instance.";
                    if (_selectedPropertyScope != FamilyScope) _selectedPropertyScope = FamilyScope;
                    OnChanged(nameof(SelectedPropertyScope));
                    LoadCurrentProperties();
                    return;
                }
                if (_selectedPropertyScope == requested) return;
                _selectedPropertyScope = requested;
                OnChanged();
                LoadCurrentProperties();
            }
        }

        public void Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateWorkspaceCatalogs(project);

            _project = project;
            _selectedElement = null;
            _selectedPropertyScope = FamilyScope;
            OnChanged(nameof(SelectedPropertyScope));
            Zones.Clear(); foreach (var item in project.Zones) Zones.Add(item.Name);
            Floors.Clear(); foreach (var item in project.Floors.OrderBy(x => x.ElevationM)) Floors.Add(item.Name);
            Families.Clear(); foreach (var item in project.Families.OrderBy(x => x.Category).ThenBy(x => x.Name)) Families.Add(item);

            _selectedFamily = ProjectFamilyActivationService.GetActive(project) ?? Families.FirstOrDefault();
            SelectedFamilyName = _selectedFamily?.Name ?? string.Empty;
            LoadCurrentProperties();
            Status = project.Elements.Count + " cấu kiện • " + project.Families.Count + " family";
        }

        public int ActiveZoneIndex()
        {
            if (_project == null) return 0;
            var zone = _project.FindZone(_project.ActiveZoneId);
            return zone == null ? 0 : Math.Max(0, Zones.IndexOf(zone.Name));
        }

        public int ActiveFloorIndex()
        {
            if (_project == null) return 0;
            var floor = _project.FindFloor(_project.ActiveFloorId);
            return floor == null ? 0 : Math.Max(0, Floors.IndexOf(floor.Name));
        }

        public void SetActiveZone(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!TryGetCurrentProjectForMutation("Đổi Zone làm việc", out var project)) return;
            var matches = project.Zones.Where(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)).Take(2).ToList();
            if (matches.Count > 1)
            {
                Status = "Không thể chọn Zone vì tên bị trùng: " + name;
                return;
            }
            var zone = matches.Count == 1 ? matches[0] : null;
            if (zone == null || string.Equals(project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)) return;
            ProjectZoneService.SetActive(project, zone.Id);
            Status = "Zone làm việc: " + zone.Name;
        }

        public void SetActiveFloor(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!TryGetCurrentProjectForMutation("Đổi tầng làm việc", out var project)) return;
            var matches = project.Floors.Where(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)).Take(2).ToList();
            if (matches.Count > 1)
            {
                Status = "Không thể chọn tầng vì tên bị trùng: " + name;
                return;
            }
            var floor = matches.Count == 1 ? matches[0] : null;
            if (floor == null || string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) return;
            ProjectFloorService.SetActive(project, floor.Id);
            Status = "Tầng làm việc: " + floor.Name;
        }

        public void SetActiveFamily(ProjectFamily? family)
        {
            if (family == null) return;
            if (!TryGetCurrentProjectForMutation("Đổi Family active", out var project)) return;
            ProjectFamily? ownedFamily;
            try
            {
                ownedFamily = project.FindFamily(family.Id);
            }
            catch (InvalidOperationException)
            {
                ReportMutationFailure("Chọn Family");
                return;
            }
            if (ownedFamily == null || !ReferenceEquals(ownedFamily, family))
            {
                Status = "Family " + family.Id + " không thuộc project đang mở.";
                return;
            }

            family = ownedFamily;
            _selectedFamily = family;
            if (_selectedElement != null && !string.Equals(_selectedElement.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase))
            {
                _selectedElement = null;
                _selectedPropertyScope = FamilyScope;
                OnChanged(nameof(SelectedPropertyScope));
            }
            ProjectFamilyActivationService.SetActive(project, family.Id);
            SelectedFamilyName = family.Name;
            LoadCurrentProperties();
        }

        public void ShowFamilyProperties()
        {
            if (_selectedPropertyScope != FamilyScope)
            {
                _selectedPropertyScope = FamilyScope;
                OnChanged(nameof(SelectedPropertyScope));
            }
            LoadCurrentProperties();
        }

        public void SetSelectedElement(ProjectElement? element)
        {
            if (_project == null || element == null)
            {
                _selectedElement = null;
                ShowFamilyProperties();
                return;
            }

            ProjectElement? ownedElement;
            ProjectFamily? family;
            try
            {
                ownedElement = _project.FindElement(element.Id);
                if (ownedElement == null || !ReferenceEquals(ownedElement, element))
                {
                    _selectedElement = null;
                    Status = "Cấu kiện " + element.Id + " không thuộc project đang mở.";
                    ShowFamilyProperties();
                    return;
                }
                family = _project.FindFamily(ownedElement.FamilyId);
            }
            catch (InvalidOperationException)
            {
                _selectedElement = null;
                ReportMutationFailure("Chọn cấu kiện");
                ShowFamilyProperties();
                return;
            }
            if (family == null)
            {
                _selectedElement = null;
                Status = "Cấu kiện " + ownedElement.Id + " chưa có Family hợp lệ.";
                ShowFamilyProperties();
                return;
            }

            _selectedElement = ownedElement;
            _selectedFamily = family;
            SelectedFamilyName = family.Name;
            _selectedPropertyScope = InstanceScope;
            OnChanged(nameof(SelectedPropertyScope));
            LoadCurrentProperties();
            Status = "Instance: " + ownedElement.Id + " • " + family.Name;
        }

        internal void SetFamilyPropertyPresenter(Func<ProjectFamily, bool> presenter)
        {
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            if (_familyPropertyPresenter == presenter) return;
            _familyPropertyPresenter = presenter;
            LoadCurrentProperties();
        }

        private void LoadCurrentProperties()
        {
            if (_selectedPropertyScope == InstanceScope && _selectedElement != null && _selectedFamily != null)
                LoadInstanceProperties(_selectedElement, _selectedFamily);
            else if (_selectedFamily == null || _familyPropertyPresenter?.Invoke(_selectedFamily) != true)
                LoadFamilyProperties(_selectedFamily);
        }

        private void LoadFamilyProperties(ProjectFamily? family)
        {
            Properties.Clear();
            if (family == null) return;
            var nameRow = new PropertyRowViewModel { Group = "THÔNG TIN", Name = "Tên Family" };
            nameRow.Apply = value => ApplyFamilyName(family, value);
            nameRow.Value = family.Name;
            Properties.Add(nameRow);
            var categoryRow = new PropertyRowViewModel { Group = "THÔNG TIN", Name = "Loại cấu kiện", IsReadOnly = true };
            categoryRow.Value = family.Category.ToString(); Properties.Add(categoryRow);
            foreach (var pair in family.Properties.OrderBy(x => GroupFor(x.Key)).ThenBy(x => DisplayNameFor(x.Key)))
            {
                var key = pair.Key;
                var unit = UnitFor(key);
                var row = CreatePropertyRow(key, pair.Value, unit);
                row.Apply = value => ToDisplayValue(key, ApplyFamilyProperty(family, key, unit, value));
                row.Value = ToDisplayValue(key, pair.Value);
                Properties.Add(row);
            }
        }

        private void LoadInstanceProperties(ProjectElement element, ProjectFamily family)
        {
            Properties.Clear();
            var idRow = new PropertyRowViewModel { Group = "ĐỐI TƯỢNG", Name = "Element ID", IsReadOnly = true };
            idRow.Value = element.Id; Properties.Add(idRow);
            var categoryRow = new PropertyRowViewModel { Group = "ĐỐI TƯỢNG", Name = "Loại cấu kiện", IsReadOnly = true };
            categoryRow.Value = element.Category.ToString(); Properties.Add(categoryRow);
            foreach (var pair in family.Properties.OrderBy(x => GroupFor(x.Key)).ThenBy(x => DisplayNameFor(x.Key)))
            {
                var key = pair.Key;
                var familyValue = pair.Value ?? string.Empty;
                var hasInstance = element.Properties.TryGetValue(key, out var stored);
                var current = hasInstance ? stored ?? string.Empty : familyValue;
                var unit = UnitFor(key);
                var row = CreatePropertyRow(key, current, unit);
                var isReadOnlyInstanceProperty = !SemanticPropertyEditPolicy.IsEditablePropertyKey(key);
                if (isReadOnlyInstanceProperty)
                {
                    row.Group = IsSourceDerivedInstanceKey(key) ? "NGUỒN CAD / ĐO ĐẠC" : "HỆ THỐNG / CHỈ ĐỌC";
                    row.IsReadOnly = true;
                    row.CanReset = false;
                    row.Value = ToDisplayValue(key, current);
                }
                else
                {
                    row.Group = "INSTANCE • " + GroupFor(key);
                    row.CanReset = hasInstance && !string.Equals(current, familyValue, StringComparison.Ordinal);
                    row.Value = ToDisplayValue(key, current);
                    row.Apply = value => ToDisplayValue(key, ApplyInstanceProperty(element, family, key, unit, row, value));
                    row.Reset = () => ResetInstanceProperty(element, family, key, row);
                }
                Properties.Add(row);
            }

            foreach (var key in SourceDerivedInstanceKeys)
            {
                if (family.Properties.ContainsKey(key)) continue;
                if (!element.Properties.TryGetValue(key, out var sourceValue) || string.IsNullOrWhiteSpace(sourceValue)) continue;
                var sourceRow = CreatePropertyRow(key, sourceValue, UnitFor(key));
                sourceRow.Group = "NGUỒN CAD / ĐO ĐẠC";
                sourceRow.IsReadOnly = true;
                sourceRow.CanReset = false;
                sourceRow.Value = ToDisplayValue(key, sourceValue);
                Properties.Add(sourceRow);
            }
        }

        private PropertyRowViewModel CreatePropertyRow(string key, string current, string unit)
        {
            return new PropertyRowViewModel
            {
                Group = GroupFor(key),
                Name = DisplayNameFor(key),
                Unit = unit,
                EditorKind = EditorKindFor(key, current),
                Choices = ChoicesFor(key, current)
            };
        }

        private string ApplyFamilyName(ProjectFamily family, string value)
        {
            var previous = family.Name;
            var next = (value ?? string.Empty).Trim();
            if (next.Length == 0)
            {
                Status = "Tên Family không được để trống.";
                return previous;
            }
            if (string.Equals(previous, next, StringComparison.Ordinal)) return previous;
            if (!TryGetCurrentProjectForMutation("Đổi tên Family", out var project)) return previous;

            try
            {
                var owned = project.FindFamily(family.Id);
                if (owned == null || !ReferenceEquals(owned, family))
                {
                    Status = "Không thể đổi tên Family vì lựa chọn đã stale hoặc không thuộc project đang mở.";
                    return previous;
                }
                var renamed = ProjectFamilyService.Rename(project, owned.Id, next);
                SelectedFamilyName = renamed.Name;
                Status = "Đã đổi tên Family: " + renamed.Name;
                return renamed.Name;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                ReportMutationFailure("Đổi tên Family");
                return previous;
            }
        }

        private string ApplyFamilyProperty(ProjectFamily family, string key, string unit, string value)
        {
            var previousFamilyValue = family.Properties.TryGetValue(key, out var previous) ? previous ?? string.Empty : string.Empty;
            var next = NormalizePropertyValue(key, unit, previousFamilyValue, value, out var valid);
            if (!valid) return previousFamilyValue;
            if (string.Equals(previousFamilyValue, next, StringComparison.Ordinal)) return previousFamilyValue;
            if (!TryGetCurrentProjectForMutation("Cập nhật Family property", out var project)) return previousFamilyValue;

            try
            {
                var owned = project.FindFamily(family.Id);
                if (owned == null || !ReferenceEquals(owned, family))
                {
                    Status = "Không thể cập nhật Family vì lựa chọn đã stale hoặc không thuộc project đang mở.";
                    return previousFamilyValue;
                }

                var result = ProjectFamilyService.SetProperty(project, owned.Id, key, next);
                Status = "Đã cập nhật " + DisplayNameFor(key) + " • kế thừa " + result.InheritedInstancesUpdated + " cấu kiện" +
                         (result.OverridesPreserved > 0 ? " • giữ " + result.OverridesPreserved + " instance override" : string.Empty);
                return owned.Properties.TryGetValue(key, out var stored) ? stored ?? next : next;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                ReportMutationFailure("Cập nhật " + DisplayNameFor(key));
                return previousFamilyValue;
            }
        }

        private string ApplyInstanceProperty(ProjectElement element, ProjectFamily family, string key, string unit, PropertyRowViewModel row, string value)
        {
            var familyValue = family.Properties.TryGetValue(key, out var familyRaw) ? familyRaw ?? string.Empty : string.Empty;
            var current = element.Properties.TryGetValue(key, out var stored) ? stored ?? string.Empty : familyValue;
            if (!SemanticPropertyEditPolicy.IsEditablePropertyKey(key))
            {
                Status = "Không thể cập nhật " + DisplayNameFor(key) + ": đây là thuộc tính nguồn/identity/ownership chỉ đọc.";
                return current;
            }
            if (!TryGetCurrentProjectForMutation("Cập nhật Instance property", out var project)) return current;

            try
            {
                var ownedElement = project.FindElement(element.Id);
                var ownedFamily = project.FindFamily(family.Id);
                if (ownedElement == null || !ReferenceEquals(ownedElement, element) || ownedFamily == null || !ReferenceEquals(ownedFamily, family))
                {
                    Status = "Không thể cập nhật Instance vì lựa chọn đã stale hoặc không thuộc project đang mở.";
                    return current;
                }
            }
            catch (InvalidOperationException)
            {
                ReportMutationFailure("Cập nhật Instance");
                return current;
            }

            var next = NormalizePropertyValue(key, unit, current, value, out var valid);
            if (!valid) return current;
            if (string.Equals(current, next, StringComparison.Ordinal))
            {
                row.CanReset = !string.Equals(next, familyValue, StringComparison.Ordinal);
                return current;
            }

            try
            {
                ProjectSemanticMutationExecutor.Execute(
                    project,
                    "Workspace single-instance property edit",
                    () =>
                    {
                        element.SetProperty(key, next);
                        project.Touch();
                        return 0;
                    });
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is OverflowException)
            {
                ReportMutationFailure("Cập nhật " + DisplayNameFor(key));
                return current;
            }

            row.CanReset = !string.Equals(next, familyValue, StringComparison.Ordinal);
            var displayValue = ToDisplayValue(key, next);
            Status = row.CanReset
                ? "Instance override: " + DisplayNameFor(key) + " = " + displayValue + (unit.Length > 0 ? " " + unit : string.Empty)
                : "Đã đưa " + DisplayNameFor(key) + " về giá trị Family.";
            return next;
        }

        private void ResetInstanceProperty(ProjectElement element, ProjectFamily family, string key, PropertyRowViewModel row)
        {
            if (!SemanticPropertyEditPolicy.IsEditablePropertyKey(key))
            {
                Status = "Không thể đặt lại " + DisplayNameFor(key) + ": đây là thuộc tính nguồn/identity/ownership chỉ đọc.";
                return;
            }
            if (!TryGetCurrentProjectForMutation("Đặt lại Instance property", out var project)) return;

            try
            {
                var ownedElement = project.FindElement(element.Id);
                var ownedFamily = project.FindFamily(family.Id);
                if (ownedElement == null || !ReferenceEquals(ownedElement, element) || ownedFamily == null || !ReferenceEquals(ownedFamily, family))
                {
                    Status = "Không thể đặt lại Instance vì lựa chọn đã stale hoặc không thuộc project đang mở.";
                    return;
                }
                if (!ownedFamily.Properties.TryGetValue(key, out var liveFamilyRaw))
                {
                    Status = "Không thể đặt lại " + DisplayNameFor(key) + " vì property không còn tồn tại trên Family hiện hành. Hãy Refresh Workspace.";
                    return;
                }

                row.Value = ToDisplayValue(key, liveFamilyRaw ?? string.Empty);
            }
            catch (InvalidOperationException)
            {
                ReportMutationFailure("Đặt lại Instance");
            }
        }

        private bool TryGetCurrentProjectForMutation(string operation, out ProjectState project)
        {
            project = null!;
            if (_project == null)
            {
                Status = operation + ": project không còn được mở.";
                return false;
            }

            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                Status = operation + ": không có bản vẽ BricsCAD đang active.";
                return false;
            }

            try
            {
                if (!global::QS3D.BricsCAD.V25.ExistingProjectMutationContext.TryGet(document, out var current))
                {
                    Status = operation + ": QS3D project hiện hành không còn khả dụng; không tạo project thay thế.";
                    return false;
                }
                if (!ReferenceEquals(current, _project))
                {
                    Status = operation + ": Workspace đang giữ project stale sau reload/thay thế. Hãy Refresh Workspace rồi thử lại.";
                    return false;
                }

                project = current;
                return true;
            }
            catch (Exception)
            {
                ReportMutationFailure(operation);
                return false;
            }
        }

        private void ReportMutationFailure(string operation)
        {
            Status = operation + " không hoàn tất. Chi tiết nội bộ đã được ẩn. Hãy Refresh Workspace rồi thử lại.";
        }

        private string NormalizePropertyValue(string key, string unit, string previousValue, string value, out bool valid)
        {
            var next = (value ?? string.Empty).Trim();
            if (IsBooleanProperty(key, previousValue))
            {
                if (!TryBoolean(next, out var boolean))
                {
                    Status = DisplayNameFor(key) + ": chỉ nhận Bật/Tắt (true/false).";
                    valid = false;
                    return previousValue;
                }
                valid = true;
                return boolean ? "true" : "false";
            }
            if (unit.Length > 0 || IsNumericProperty(key))
            {
                if (!TryFiniteNumber(next, out var number))
                {
                    Status = DisplayNameFor(key) + ": giá trị số không hợp lệ; đã giữ giá trị cũ.";
                    valid = false;
                    return previousValue;
                }
                if (UsesMillimeterPresentation(key)) number /= 1000d;
                if (RequiresPositiveNumber(key) && !(number > 0d))
                {
                    Status = DisplayNameFor(key) + ": phải lớn hơn 0; đã giữ giá trị cũ.";
                    valid = false;
                    return previousValue;
                }
                if (RequiresNonNegativeNumber(key) && number < 0d)
                {
                    Status = DisplayNameFor(key) + ": không được âm; đã giữ giá trị cũ.";
                    valid = false;
                    return previousValue;
                }
                valid = true;
                return number.ToString("R", CultureInfo.InvariantCulture);
            }
            valid = true;
            return next;
        }

        private IReadOnlyList<string> ChoicesFor(string key, string current)
        {
            if (!IsChoiceProperty(key) || _project == null) return Array.Empty<string>();
            return _project.Families
                .SelectMany(x => x.Properties.Where(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)).Select(p => (p.Value ?? string.Empty).Trim()))
                .Concat(new[] { (current ?? string.Empty).Trim() })
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }

        private static string EditorKindFor(string key, string current)
        {
            if (IsBooleanProperty(key, current)) return PropertyRowViewModel.BooleanEditor;
            if (IsChoiceProperty(key)) return PropertyRowViewModel.ChoiceEditor;
            return PropertyRowViewModel.TextEditor;
        }

        private static bool TryFiniteNumber(string value, out double number)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
                !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number)) return false;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private static string ToDisplayValue(string key, string? storageValue)
        {
            var raw = (storageValue ?? string.Empty).Trim();
            if (!UsesMillimeterPresentation(key) || !TryFiniteNumber(raw, out var value)) return raw;
            return (value * 1000d).ToString("0.###############", CultureInfo.InvariantCulture);
        }

        private static bool UsesMillimeterPresentation(string key) =>
            SemanticPropertyUnitClassifier.IsLinearMeterProperty(key);

        private static bool TryBoolean(string value, out bool boolean)
        {
            var text = (value ?? string.Empty).Trim();
            if (bool.TryParse(text, out boolean)) return true;
            if (text == "1" || text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text.Equals("on", StringComparison.OrdinalIgnoreCase) || text.Equals("bật", StringComparison.CurrentCultureIgnoreCase)) { boolean = true; return true; }
            if (text == "0" || text.Equals("no", StringComparison.OrdinalIgnoreCase) || text.Equals("off", StringComparison.OrdinalIgnoreCase) || text.Equals("tắt", StringComparison.CurrentCultureIgnoreCase)) { boolean = false; return true; }
            boolean = false;
            return false;
        }

        private static bool IsBooleanProperty(string key, string? current)
        {
            if (IsNumericProperty(key)) return false;
            if (TryBoolean(current ?? string.Empty, out _)) return true;
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

        private static bool IsChoiceProperty(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return key.EndsWith("Mode", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Type", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("Material", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Material", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("ClassificationCode", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumericProperty(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            // The generic Family view is populated before the dedicated footing rows.
            // Its initial value normalization must never turn metre values 0/1 into booleans.
            if (SingleFootingContract.IsDimensionKey(key) || new[]
                {
                    "SingleFootingL1M", "SingleFootingW1M", "SingleFootingL2M",
                    "SingleFootingW2M", "SingleFootingH1M", "SingleFootingH2M"
                }.Any(candidate => string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase))) return true;
            if (SemanticPropertyUnitClassifier.IsLinearMeterProperty(key) || key.EndsWith("M2", StringComparison.OrdinalIgnoreCase) || key.EndsWith("M3", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Mm", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Deg", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Count", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("Ratio", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Factor", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Percent", StringComparison.OrdinalIgnoreCase)) return true;
            return key.IndexOf("BarsAlong", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("MiterLimit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Tolerance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Confidence", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSourceDerivedInstanceKey(string key) =>
            SourceDerivedInstanceKeys.Any(x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase));

        private static bool RequiresPositiveNumber(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (key.Equals("LengthM", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.Equals("SillHeightM", StringComparison.OrdinalIgnoreCase)) return false;
            return key.IndexOf("Thickness", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Width", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Depth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Diameter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Spacing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Radius", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.EndsWith("HeightM", StringComparison.OrdinalIgnoreCase) ||
                   key.IndexOf("Sagitta", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool RequiresNonNegativeNumber(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return key.Equals("SillHeightM", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("BooleanClearanceM", StringComparison.OrdinalIgnoreCase) ||
                   key.Equals("CoverM", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateWorkspaceCatalogs(ProjectState project)
        {
            var zoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var zoneNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var zone in project.Zones)
            {
                if (zone == null) throw new InvalidOperationException("Project contains a null Zone entry.");
                if (!zoneIds.Add(zone.Id)) throw new InvalidOperationException("Project contains duplicate Zone id: " + zone.Id);
                if (!zoneNames.Add(zone.Name)) throw new InvalidOperationException("Project contains duplicate Zone name: " + zone.Name);
            }

            var floorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var floorNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var floor in project.Floors)
            {
                if (floor == null) throw new InvalidOperationException("Project contains a null Floor entry.");
                if (double.IsNaN(floor.ElevationM) || double.IsInfinity(floor.ElevationM))
                    throw new InvalidOperationException("Project contains a Floor with non-finite elevation: " + floor.Id);
                if (!floorIds.Add(floor.Id)) throw new InvalidOperationException("Project contains duplicate Floor id: " + floor.Id);
                if (!floorNames.Add(floor.Name)) throw new InvalidOperationException("Project contains duplicate Floor name: " + floor.Name);
            }

            var familyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var familyNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null) throw new InvalidOperationException("Project contains a null Family entry.");
                if (!familyIds.Add(family.Id)) throw new InvalidOperationException("Project contains duplicate Family id: " + family.Id);
                var nameKey = family.Category + "\u001f" + family.Name;
                if (!familyNames.Add(nameKey))
                    throw new InvalidOperationException("Project contains duplicate " + family.Category + " Family name: " + family.Name);
            }
        }

        private static string GroupFor(string key)
        {
            if (key.IndexOf("Rebar", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Bar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Diameter", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Spacing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Cover", StringComparison.OrdinalIgnoreCase) >= 0) return "CỐT THÉP";
            if (key.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Classification", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.EndsWith("Code", StringComparison.OrdinalIgnoreCase)) return "VẬT LIỆU / PHÂN LOẠI";
            if (key.IndexOf("Offset", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Elevation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Sill", StringComparison.OrdinalIgnoreCase) >= 0) return "VỊ TRÍ / CAO ĐỘ";
            if (key.IndexOf("Display", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Color", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Layer", StringComparison.OrdinalIgnoreCase) >= 0) return "HIỂN THỊ";
            if (key.IndexOf("Length", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Width", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Height", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Depth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Thickness", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Perimeter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Profile", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("Axis", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf("Radius", StringComparison.OrdinalIgnoreCase) >= 0) return "HÌNH HỌC";
            return "THUỘC TÍNH";
        }

        private static string DisplayNameFor(string key)
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
                case "AxisLeftOffsetM": return "Lệch tim trái";
                case "AxisRightOffsetM": return "Lệch tim phải";
                case "ProfileMode": return "Biên dạng";
                case "CloseProfile": return "Đóng biên dạng";
                case "FreeformProfile": return "Biên dạng tự do";
                case "Material": return "Vật liệu";
                case "ClassificationCode": return "Mã phân loại";
                case "RebarNotation": return "Ký hiệu cốt thép";
                case "CoverM": return "Lớp bê tông bảo vệ";
                case "RebarDiameterMm": return "Đường kính cốt thép";
                case "DiameterMm": return "Đường kính";
                case "SpacingMm": return "Khoảng cách";
                case "BooleanClearanceM": return "Dung sai khoét";
                case "WallMiterLimit": return "Giới hạn nối góc";
                case "WallArcSagittaM": return "Sai số cung tường";
                default: return key;
            }
        }

        private static string UnitFor(string key)
        {
            if (key.EndsWith("Mm", StringComparison.OrdinalIgnoreCase)) return "mm";
            if (key.EndsWith("M2", StringComparison.OrdinalIgnoreCase)) return "m²";
            if (key.EndsWith("M3", StringComparison.OrdinalIgnoreCase)) return "m³";
            if (UsesMillimeterPresentation(key)) return "mm";
            if (key.EndsWith("Deg", StringComparison.OrdinalIgnoreCase)) return "°";
            if (key.EndsWith("Percent", StringComparison.OrdinalIgnoreCase)) return "%";
            return string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
