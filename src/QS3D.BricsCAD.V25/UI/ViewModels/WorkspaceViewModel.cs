using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

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
            "Layer"
        };

        private string _status = "Sẵn sàng";
        private string _selectedFamilyName = string.Empty;
        private string _selectedPropertyScope = FamilyScope;
        private ProjectState? _project;
        private ProjectFamily? _selectedFamily;
        private ProjectElement? _selectedElement;

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
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _selectedElement = null;
            _selectedPropertyScope = FamilyScope;
            OnChanged(nameof(SelectedPropertyScope));
            Zones.Clear(); foreach (var item in project.Zones) Zones.Add(item.Name);
            Floors.Clear(); foreach (var item in project.Floors.OrderBy(x => x.ElevationM)) Floors.Add(item.Name);
            Families.Clear(); foreach (var item in project.Families.OrderBy(x => x.Category).ThenBy(x => x.Name)) Families.Add(item);
            var activeFamilyId = project.Metadata.TryGetValue("ActiveFamilyId", out var stored) ? stored : string.Empty;
            _selectedFamily = Families.FirstOrDefault(x => string.Equals(x.Id, activeFamilyId, StringComparison.OrdinalIgnoreCase)) ?? Families.FirstOrDefault();
            SelectedFamilyName = _selectedFamily?.Name ?? string.Empty;
            LoadCurrentProperties();
            Status = project.Elements.Count + " cấu kiện • " + project.Families.Count + " family";
        }

        public int ActiveZoneIndex()
        {
            if (_project == null) return 0;
            var zone = _project.Zones.FirstOrDefault(x => string.Equals(x.Id, _project.ActiveZoneId, StringComparison.OrdinalIgnoreCase));
            return zone == null ? 0 : Math.Max(0, Zones.IndexOf(zone.Name));
        }

        public int ActiveFloorIndex()
        {
            if (_project == null) return 0;
            var floor = _project.Floors.FirstOrDefault(x => string.Equals(x.Id, _project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
            return floor == null ? 0 : Math.Max(0, Floors.IndexOf(floor.Name));
        }

        public void SetActiveZone(string? name)
        {
            if (_project == null || string.IsNullOrWhiteSpace(name)) return;
            var zone = _project.Zones.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
            if (zone == null || string.Equals(_project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)) return;
            _project.ActiveZoneId = zone.Id; _project.Touch(); Status = "Zone làm việc: " + zone.Name;
        }

        public void SetActiveFloor(string? name)
        {
            if (_project == null || string.IsNullOrWhiteSpace(name)) return;
            var floor = _project.Floors.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
            if (floor == null || string.Equals(_project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) return;
            _project.ActiveFloorId = floor.Id; _project.Touch(); Status = "Tầng làm việc: " + floor.Name;
        }

        public void SetActiveFamily(ProjectFamily? family)
        {
            if (_project == null || family == null) return;
            _selectedFamily = family;
            if (_selectedElement != null && !string.Equals(_selectedElement.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase))
            {
                _selectedElement = null;
                _selectedPropertyScope = FamilyScope;
                OnChanged(nameof(SelectedPropertyScope));
            }
            if (!_project.Metadata.TryGetValue("ActiveFamilyId", out var activeId) || !string.Equals(activeId, family.Id, StringComparison.OrdinalIgnoreCase))
            {
                _project.Metadata["ActiveFamilyId"] = family.Id;
                _project.Touch();
            }
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
            var family = _project.FindFamily(element.FamilyId);
            if (family == null)
            {
                _selectedElement = null;
                Status = "Cấu kiện " + element.Id + " chưa có Family hợp lệ.";
                ShowFamilyProperties();
                return;
            }

            _selectedElement = element;
            _selectedFamily = family;
            SelectedFamilyName = family.Name;
            if (!_project.Metadata.TryGetValue("ActiveFamilyId", out var activeId) || !string.Equals(activeId, family.Id, StringComparison.OrdinalIgnoreCase))
            {
                _project.Metadata["ActiveFamilyId"] = family.Id;
                _project.Touch();
            }
            _selectedPropertyScope = InstanceScope;
            OnChanged(nameof(SelectedPropertyScope));
            LoadCurrentProperties();
            Status = "Instance: " + element.Id + " • " + family.Name;
        }

        private void LoadCurrentProperties()
        {
            if (_selectedPropertyScope == InstanceScope && _selectedElement != null && _selectedFamily != null)
                LoadInstanceProperties(_selectedElement, _selectedFamily);
            else
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
                row.Apply = value => ApplyFamilyProperty(family, key, unit, value);
                row.Value = pair.Value;
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
                var isSourceDerived = hasInstance && IsSourceDerivedInstanceKey(key);
                if (isSourceDerived)
                {
                    row.Group = "NGUỒN CAD / ĐO ĐẠC";
                    row.IsReadOnly = true;
                    row.CanReset = false;
                    row.Value = current;
                }
                else
                {
                    row.Group = "INSTANCE • " + GroupFor(key);
                    row.CanReset = hasInstance && !string.Equals(current, familyValue, StringComparison.Ordinal);
                    row.Value = current;
                    row.Apply = value => ApplyInstanceProperty(element, family, key, unit, row, value);
                    row.Reset = () => row.Value = familyValue;
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
                sourceRow.Value = sourceValue;
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
            var next = (value ?? string.Empty).Trim();
            if (next.Length == 0)
            {
                Status = "Tên Family không được để trống.";
                return family.Name;
            }
            if (string.Equals(family.Name, next, StringComparison.Ordinal)) return family.Name;
            if (_project != null && _project.Families.Any(x => !ReferenceEquals(x, family) && string.Equals(x.Name, next, StringComparison.CurrentCultureIgnoreCase)))
            {
                Status = "Tên Family đã tồn tại: " + next;
                return family.Name;
            }

            family.Name = next;
            _project?.Touch();
            SelectedFamilyName = family.Name;
            Status = "Đã đổi tên Family: " + family.Name;
            return family.Name;
        }

        private string ApplyFamilyProperty(ProjectFamily family, string key, string unit, string value)
        {
            var next = NormalizePropertyValue(key, unit, family.Properties.TryGetValue(key, out var previous) ? previous : string.Empty, value, out var valid);
            var previousFamilyValue = previous ?? string.Empty;
            if (!valid) return previousFamilyValue;
            if (string.Equals(previousFamilyValue, next, StringComparison.Ordinal)) return previousFamilyValue;
            family.Properties[key] = next;
            if (_project == null) return next;

            var inherited = 0;
            var overrides = 0;
            foreach (var element in _project.Elements.Where(x => string.Equals(x.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase)))
            {
                var hasInstanceValue = element.Properties.TryGetValue(key, out var instanceValue);
                var isInherited = !hasInstanceValue || string.Equals(instanceValue, previousFamilyValue, StringComparison.Ordinal);
                if (isInherited)
                {
                    element.SetProperty(key, next);
                    inherited++;
                }
                else
                {
                    element.MarkDirty(ElementDirtyFlags.All);
                    overrides++;
                }
            }

            _project.Touch();
            Status = "Đã cập nhật " + DisplayNameFor(key) + " • kế thừa " + inherited + " cấu kiện" + (overrides > 0 ? " • giữ " + overrides + " instance override" : string.Empty);
            return next;
        }

        private string ApplyInstanceProperty(ProjectElement element, ProjectFamily family, string key, string unit, PropertyRowViewModel row, string value)
        {
            var familyValue = family.Properties.TryGetValue(key, out var familyRaw) ? familyRaw ?? string.Empty : string.Empty;
            var current = element.Properties.TryGetValue(key, out var stored) ? stored ?? string.Empty : familyValue;
            var next = NormalizePropertyValue(key, unit, current, value, out var valid);
            if (!valid) return current;
            if (string.Equals(current, next, StringComparison.Ordinal))
            {
                row.CanReset = !string.Equals(next, familyValue, StringComparison.Ordinal);
                return current;
            }

            element.SetProperty(key, next);
            element.MarkDirty(ElementDirtyFlags.All);
            _project?.Touch();
            row.CanReset = !string.Equals(next, familyValue, StringComparison.Ordinal);
            Status = row.CanReset
                ? "Instance override: " + DisplayNameFor(key) + " = " + next
                : "Đã đưa " + DisplayNameFor(key) + " về giá trị Family.";
            return next;
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
            if (key.EndsWith("M", StringComparison.OrdinalIgnoreCase) || key.EndsWith("M2", StringComparison.OrdinalIgnoreCase) || key.EndsWith("M3", StringComparison.OrdinalIgnoreCase) ||
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
            if (key.EndsWith("M", StringComparison.OrdinalIgnoreCase)) return "m";
            if (key.EndsWith("Deg", StringComparison.OrdinalIgnoreCase)) return "°";
            if (key.EndsWith("Percent", StringComparison.OrdinalIgnoreCase)) return "%";
            return string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
