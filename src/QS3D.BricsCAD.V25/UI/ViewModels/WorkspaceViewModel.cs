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
        private string _status = "Sẵn sàng";
        private string _selectedFamilyName = string.Empty;
        private ProjectState? _project;

        public ObservableCollection<string> Zones { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Floors { get; } = new ObservableCollection<string>();
        public ObservableCollection<ProjectFamily> Families { get; } = new ObservableCollection<ProjectFamily>();
        public ObservableCollection<PropertyRowViewModel> Properties { get; } = new ObservableCollection<PropertyRowViewModel>();
        public string Status { get => _status; set { if (_status == value) return; _status = value ?? string.Empty; OnChanged(); } }
        public string SelectedFamilyName { get => _selectedFamilyName; set { if (_selectedFamilyName == value) return; _selectedFamilyName = value ?? string.Empty; OnChanged(); } }

        public void Load(ProjectState project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            Zones.Clear(); foreach (var item in project.Zones) Zones.Add(item.Name);
            Floors.Clear(); foreach (var item in project.Floors.OrderBy(x => x.ElevationM)) Floors.Add(item.Name);
            Families.Clear(); foreach (var item in project.Families.OrderBy(x => x.Category).ThenBy(x => x.Name)) Families.Add(item);
            var activeFamilyId = project.Metadata.TryGetValue("ActiveFamilyId", out var stored) ? stored : string.Empty;
            var selected = Families.FirstOrDefault(x => string.Equals(x.Id, activeFamilyId, StringComparison.OrdinalIgnoreCase)) ?? Families.FirstOrDefault();
            SelectedFamilyName = selected?.Name ?? string.Empty;
            LoadProperties(selected);
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
            if (!_project.Metadata.TryGetValue("ActiveFamilyId", out var activeId) || !string.Equals(activeId, family.Id, StringComparison.OrdinalIgnoreCase))
            {
                _project.Metadata["ActiveFamilyId"] = family.Id;
                _project.Touch();
            }
            SelectedFamilyName = family.Name;
            LoadProperties(family);
        }

        public void LoadProperties(ProjectFamily? family)
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
                var row = new PropertyRowViewModel
                {
                    Group = GroupFor(key),
                    Name = DisplayNameFor(key),
                    Unit = unit,
                    EditorKind = EditorKindFor(key, pair.Value),
                    Choices = ChoicesFor(key, pair.Value)
                };
                row.Apply = value => ApplyFamilyProperty(family, key, unit, value);
                row.Value = pair.Value;
                Properties.Add(row);
            }
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
            var next = (value ?? string.Empty).Trim();
            family.Properties.TryGetValue(key, out var previousFamilyValue);
            if (IsBooleanProperty(key, previousFamilyValue))
            {
                if (!TryBoolean(next, out var boolean))
                {
                    Status = DisplayNameFor(key) + ": chỉ nhận Bật/Tắt (true/false).";
                    return previousFamilyValue ?? string.Empty;
                }
                next = boolean ? "true" : "false";
            }
            else if (unit.Length > 0 || IsNumericProperty(key))
            {
                if (!TryFiniteNumber(next, out var number))
                {
                    Status = DisplayNameFor(key) + ": giá trị số không hợp lệ; đã giữ giá trị cũ.";
                    return previousFamilyValue ?? string.Empty;
                }
                next = number.ToString("R", CultureInfo.InvariantCulture);
            }

            if (string.Equals(previousFamilyValue, next, StringComparison.Ordinal)) return previousFamilyValue ?? string.Empty;
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
                case "BottomOffsetM": return "Cao độ đáy";
                case "TopOffsetM": return "Cao độ đỉnh";
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
