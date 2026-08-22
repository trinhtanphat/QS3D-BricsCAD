using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class LayerItemViewModel : INotifyPropertyChanged
    {
        private bool _isVisible;
        private bool _isLocked;

        public string Name { get; set; } = string.Empty;
        public short ColorIndex { get; set; }
        public Brush ColorBrush { get; set; } = Brushes.Transparent;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked == value) return;
                _isLocked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLocked)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class DrawingItemViewModel : INotifyPropertyChanged
    {
        private string _scaleText = "—";

        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Kind { get; set; } = "DWG";
        public string LockState { get; set; } = "—";
        public string InstanceText { get; set; } = "—";
        public bool IsXref { get; set; }

        public string ScaleText
        {
            get => _scaleText;
            set
            {
                var normalized = string.IsNullOrWhiteSpace(value) ? "—" : value;
                if (string.Equals(_scaleText, normalized, StringComparison.Ordinal)) return;
                _scaleText = normalized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScaleText)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class RightPanelViewModel : INotifyPropertyChanged
    {
        private string _layerSearch = string.Empty;
        private string _status = "Sẵn sàng";
        private int _visibleLayerCount;
        private int _totalLayerCount;
        public ObservableCollection<LayerItemViewModel> Layers { get; } = new ObservableCollection<LayerItemViewModel>();
        public ObservableCollection<DrawingItemViewModel> Drawings { get; } = new ObservableCollection<DrawingItemViewModel>();
        public string LayerSearch { get => _layerSearch; set { if (_layerSearch == value) return; _layerSearch = value ?? string.Empty; OnChanged(); } }
        public string LayerCountText => _visibleLayerCount == _totalLayerCount
            ? _totalLayerCount + " lớp"
            : _visibleLayerCount + "/" + _totalLayerCount + " lớp";
        public string Status { get => _status; set { if (_status == value) return; _status = value ?? string.Empty; OnChanged(); } }

        public void SetLayerCounts(int visible, int total)
        {
            total = Math.Max(0, total);
            visible = Math.Max(0, Math.Min(visible, total));
            if (_visibleLayerCount == visible && _totalLayerCount == total) return;
            _visibleLayerCount = visible;
            _totalLayerCount = total;
            OnChanged(nameof(LayerCountText));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
