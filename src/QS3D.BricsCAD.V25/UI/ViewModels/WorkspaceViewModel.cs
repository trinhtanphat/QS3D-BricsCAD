using System;
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
<<<<<<< origin/main
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
            nameRow.Value = family.Name;
            nameRow.Apply = value => ApplyFamilyName(family, value);
            Properties.Add(nameRow);
            var categoryRow = new PropertyRowViewModel { Group = "THÔNG TIN", Name = "Loại cấu kiện", IsReadOnly = true };
            categoryRow.Value = family.Category.ToString(); Properties.Add(categoryRow);
            foreach (var pair in family.Properties.OrderBy(x => x.Key))
            {
                var key = pair.Key;
                var unit = UnitFor(key);
                var row = new PropertyRowViewModel { Group = "THUỘC TÍNH", Name = key, Unit = unit };
                row.Value = pair.Value;
                row.Apply = value => ApplyFamilyProperty(family, key, unit, value);
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
            if (unit.Length > 0)
            {
                if (!TryFiniteNumber(next, out var number))
                {
                    Status = key + ": giá trị số không hợp lệ; đã giữ giá trị cũ.";
                    return family.Properties.TryGetValue(key, out var previous) ? previous : string.Empty;
                }
                next = number.ToString("R", CultureInfo.InvariantCulture);
            }

            if (family.Properties.TryGetValue(key, out var current) && string.Equals(current, next, StringComparison.Ordinal)) return current;
            family.Properties[key] = next;
            if (_project == null) return next;

            var affected = 0;
            foreach (var element in _project.Elements.Where(x => string.Equals(x.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase)))
            {
                element.SetProperty(key, next);
                affected++;
            }

            _project.Touch();
            Status = "Đã cập nhật " + key + " cho Family • " + affected + " cấu kiện cần tính lại";
            return next;
        }

        private static bool TryFiniteNumber(string value, out double number)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
                !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number)) return false;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

=======
        private string _status = "Sẵn sàng"; private string _selectedFamilyName = string.Empty; private ProjectState? _project;
        public ObservableCollection<string> Zones { get; } = new ObservableCollection<string>(); public ObservableCollection<string> Floors { get; } = new ObservableCollection<string>(); public ObservableCollection<ProjectFamily> Families { get; } = new ObservableCollection<ProjectFamily>(); public ObservableCollection<PropertyRowViewModel> Properties { get; } = new ObservableCollection<PropertyRowViewModel>();
        public string Status { get => _status; set { if (_status == value) return; _status = value ?? string.Empty; OnChanged(); } } public string SelectedFamilyName { get => _selectedFamilyName; set { if (_selectedFamilyName == value) return; _selectedFamilyName = value ?? string.Empty; OnChanged(); } }
        public void Load(ProjectState project) { _project = project ?? throw new ArgumentNullException(nameof(project)); Zones.Clear(); foreach (var item in project.Zones) Zones.Add(item.Name); Floors.Clear(); foreach (var item in project.Floors.OrderBy(x => x.ElevationM)) Floors.Add(item.Name); Families.Clear(); foreach (var item in project.Families.OrderBy(x => x.Category).ThenBy(x => x.Name)) Families.Add(item); var activeFamilyId = project.Metadata.TryGetValue("ActiveFamilyId", out var stored) ? stored : string.Empty; var selected = Families.FirstOrDefault(x => string.Equals(x.Id, activeFamilyId, StringComparison.OrdinalIgnoreCase)) ?? Families.FirstOrDefault(); SelectedFamilyName = selected?.Name ?? string.Empty; LoadProperties(selected); Status = project.Elements.Count + " cấu kiện • " + project.Families.Count + " family"; }
        public int ActiveZoneIndex() { if (_project == null) return 0; var zone = _project.Zones.FirstOrDefault(x => string.Equals(x.Id, _project.ActiveZoneId, StringComparison.OrdinalIgnoreCase)); return zone == null ? 0 : Math.Max(0, Zones.IndexOf(zone.Name)); }
        public int ActiveFloorIndex() { if (_project == null) return 0; var floor = _project.Floors.FirstOrDefault(x => string.Equals(x.Id, _project.ActiveFloorId, StringComparison.OrdinalIgnoreCase)); return floor == null ? 0 : Math.Max(0, Floors.IndexOf(floor.Name)); }
        public void SetActiveZone(string? name) { if (_project == null || string.IsNullOrWhiteSpace(name)) return; var zone = _project.Zones.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)); if (zone == null || string.Equals(_project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)) return; _project.ActiveZoneId = zone.Id; _project.Touch(); Status = "Zone làm việc: " + zone.Name; }
        public void SetActiveFloor(string? name) { if (_project == null || string.IsNullOrWhiteSpace(name)) return; var floor = _project.Floors.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)); if (floor == null || string.Equals(_project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)) return; _project.ActiveFloorId = floor.Id; _project.Touch(); Status = "Tầng làm việc: " + floor.Name; }
        public void SetActiveFamily(ProjectFamily? family) { if (_project == null || family == null) return; _project.Metadata["ActiveFamilyId"] = family.Id; _project.Touch(); SelectedFamilyName = family.Name; LoadProperties(family); }
        public void LoadProperties(ProjectFamily? family)
        {
            Properties.Clear(); if (family == null) return; var nameRow = new PropertyRowViewModel { Group = "INFORMATION", Name = "Tên Family" }; nameRow.Value = family.Name; nameRow.Apply = value => { var next = value.Trim(); if (next.Length == 0) return; family.Name = next; _project?.Touch(); SelectedFamilyName = next; }; Properties.Add(nameRow); var categoryRow = new PropertyRowViewModel { Group = "INFORMATION", Name = "Loại cấu kiện", IsReadOnly = true }; categoryRow.Value = family.Category.ToString(); Properties.Add(categoryRow);
            foreach (var pair in family.Properties.OrderBy(x => x.Key)) { var key = pair.Key; var row = new PropertyRowViewModel { Group = "THUỘC TÍNH", Name = key, Unit = UnitFor(key) }; row.Value = pair.Value; row.Apply = value => { family.Properties[key] = value; _project?.Touch(); }; Properties.Add(row); }
        }
>>>>>>> origin/agent/full-domain-20260810
        private static string UnitFor(string key)
        {
            if (key.EndsWith("Mm", StringComparison.OrdinalIgnoreCase)) return "mm"; if (key.EndsWith("M2", StringComparison.OrdinalIgnoreCase)) return "m²"; if (key.EndsWith("M3", StringComparison.OrdinalIgnoreCase)) return "m³"; if (key.EndsWith("Kg", StringComparison.OrdinalIgnoreCase)) return "kg"; if (key.EndsWith("M", StringComparison.OrdinalIgnoreCase)) return "m"; return string.Empty;
        }
        public event PropertyChangedEventHandler? PropertyChanged; private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
