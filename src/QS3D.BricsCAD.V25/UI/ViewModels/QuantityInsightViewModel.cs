using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class QuantityInsightItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelectionMatch;

        public QuantityInsightItemViewModel(
            string floor,
            string category,
            string familyName,
            string elementName,
            int count,
            string summary,
            IReadOnlyList<string> elementIds)
        {
            Floor = floor ?? string.Empty;
            Category = category ?? string.Empty;
            FamilyName = familyName ?? string.Empty;
            ElementName = elementName ?? string.Empty;
            Count = count;
            Summary = summary ?? string.Empty;
            ElementIds = elementIds ?? Array.Empty<string>();
        }

        public string Floor { get; }
        public string Category { get; }
        public string FamilyName { get; }
        public string ElementName { get; }
        public int Count { get; }
        public string Summary { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public string DisplayName => string.IsNullOrWhiteSpace(ElementName) ? FamilyName : ElementName;
        public string MetaText => string.IsNullOrWhiteSpace(Category) ? FamilyName : Category + " • " + FamilyName;

        public bool IsSelectionMatch
        {
            get => _isSelectionMatch;
            private set
            {
                if (_isSelectionMatch == value) return;
                _isSelectionMatch = value;
                OnPropertyChanged();
            }
        }

        public void SetSelectionMatch(bool value) => IsSelectionMatch = value;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class QuantityInsightFloorViewModel
    {
        public QuantityInsightFloorViewModel(string name, IEnumerable<QuantityInsightItemViewModel> items)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Chưa gán tầng" : name;
            Items = new ObservableCollection<QuantityInsightItemViewModel>(items ?? Array.Empty<QuantityInsightItemViewModel>());
        }

        public string Name { get; }
        public ObservableCollection<QuantityInsightItemViewModel> Items { get; }
        public int Count => Items.Sum(x => x.Count);
        public string Header => Name + " • " + Count.ToString("N0", CultureInfo.CurrentCulture) + " cấu kiện";
    }

    public sealed class QuantityInsightViewModel : INotifyPropertyChanged
    {
        private string _quantityCountText = "0 dòng";
        private string _grossConcreteText = "0 m³";
        private string _deductionText = "0 m³";
        private string _netConcreteText = "0 m³";
        private string _formworkText = "0 m²";
        private string _lengthText = "0 m";
        private string _status = "Sẵn sàng";

        public ObservableCollection<QuantityInsightFloorViewModel> Floors { get; } = new ObservableCollection<QuantityInsightFloorViewModel>();

        public string QuantityCountText { get => _quantityCountText; private set => SetField(ref _quantityCountText, value); }
        public string GrossConcreteText { get => _grossConcreteText; private set => SetField(ref _grossConcreteText, value); }
        public string DeductionText { get => _deductionText; private set => SetField(ref _deductionText, value); }
        public string NetConcreteText { get => _netConcreteText; private set => SetField(ref _netConcreteText, value); }
        public string FormworkText { get => _formworkText; private set => SetField(ref _formworkText, value); }
        public string LengthText { get => _lengthText; private set => SetField(ref _lengthText, value); }
        public string Status { get => _status; set => SetField(ref _status, value ?? string.Empty); }

        public void Replace(IReadOnlyList<QuantityInsightFloorViewModel> floors, QuantityReportTotals totals, int rowCount)
        {
            Floors.Clear();
            foreach (var floor in floors ?? Array.Empty<QuantityInsightFloorViewModel>()) Floors.Add(floor);

            QuantityCountText = rowCount.ToString("N0", CultureInfo.CurrentCulture) + " dòng • " +
                                totals.Count.ToString("N0", CultureInfo.CurrentCulture) + " cấu kiện";
            GrossConcreteText = Format(totals.GrossConcreteM3, "m³");
            DeductionText = Format(totals.DeductionM3, "m³");
            NetConcreteText = Format(totals.NetConcreteM3, "m³");
            FormworkText = Format(totals.FormworkM2, "m²");
            LengthText = Format(totals.LengthM, "m");
        }

        public void Clear(string status)
        {
            Floors.Clear();
            QuantityCountText = "0 dòng";
            GrossConcreteText = "0 m³";
            DeductionText = "0 m³";
            NetConcreteText = "0 m³";
            FormworkText = "0 m²";
            LengthText = "0 m";
            Status = status;
        }

        public IEnumerable<QuantityInsightItemViewModel> AllItems() => Floors.SelectMany(x => x.Items);

        private static string Format(double value, string unit) =>
            value.ToString("0.###", CultureInfo.CurrentCulture) + " " + unit;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField(ref string field, string value, [CallerMemberName] string? name = null)
        {
            if (string.Equals(field, value, StringComparison.Ordinal)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
