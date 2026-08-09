using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class WorkspaceViewModel : INotifyPropertyChanged
    {
        private string _status = "Sẵn sàng";
        private string _selectedFamilyName = string.Empty;

        public ObservableCollection<string> Zones { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Floors { get; } = new ObservableCollection<string>();
        public ObservableCollection<ProjectFamily> Families { get; } = new ObservableCollection<ProjectFamily>();
        public ObservableCollection<PropertyRowViewModel> Properties { get; } = new ObservableCollection<PropertyRowViewModel>();
        public string Status { get => _status; set { if (_status == value) return; _status = value ?? string.Empty; OnChanged(); } }
        public string SelectedFamilyName { get => _selectedFamilyName; set { if (_selectedFamilyName == value) return; _selectedFamilyName = value ?? string.Empty; OnChanged(); } }

        public void Load(ProjectState project)
        {
            Zones.Clear(); foreach (var item in project.Zones) Zones.Add(item.Name);
            Floors.Clear(); foreach (var item in project.Floors.OrderBy(x => x.ElevationM)) Floors.Add(item.Name);
            Families.Clear(); foreach (var item in project.Families.OrderBy(x => x.Category).ThenBy(x => x.Name)) Families.Add(item);
            var selected = Families.FirstOrDefault();
            SelectedFamilyName = selected?.Name ?? string.Empty;
            LoadProperties(selected);
            Status = project.Elements.Count + " cấu kiện • " + project.Families.Count + " family";
        }

        public void LoadProperties(ProjectFamily? family)
        {
            Properties.Clear();
            if (family == null) return;
            Properties.Add(new PropertyRowViewModel { Group = "INFORMATION", Name = "Tên Family", Value = family.Name });
            Properties.Add(new PropertyRowViewModel { Group = "INFORMATION", Name = "Loại cấu kiện", Value = family.Category.ToString(), IsReadOnly = true });
            foreach (var pair in family.Properties.OrderBy(x => x.Key)) Properties.Add(new PropertyRowViewModel { Group = "THUỘC TÍNH", Name = pair.Key, Value = pair.Value });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
