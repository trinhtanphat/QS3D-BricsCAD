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

    public sealed class DrawingItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Kind { get; set; } = "DWG";
        public string LockState { get; set; } = "—";
        public string InstanceText { get; set; } = "—";
        public bool IsXref { get; set; }
    }

    public sealed class RightPanelViewModel : INotifyPropertyChanged
    {
        private string _layerSearch = string.Empty;
        private string _status = "Sẵn sàng";
        public ObservableCollection<LayerItemViewModel> Layers { get; } = new ObservableCollection<LayerItemViewModel>();
        public ObservableCollection<DrawingItemViewModel> Drawings { get; } = new ObservableCollection<DrawingItemViewModel>();
        public string LayerSearch { get => _layerSearch; set { if (_layerSearch == value) return; _layerSearch = value ?? string.Empty; OnChanged(); } }
        public string Status { get => _status; set { if (_status == value) return; _status = value ?? string.Empty; OnChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
