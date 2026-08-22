using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class LayerItemViewModel : INotifyPropertyChanged
    {
        private bool _isVisible;
        public string Name { get; set; } = string.Empty;
        public short ColorIndex { get; set; }
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
        public event PropertyChangedEventHandler? PropertyChanged;
    }
<<<<<<< origin/main

=======
>>>>>>> origin/agent/review-hardening-20260810
    public sealed class DrawingItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
<<<<<<< origin/main
        public string Kind { get; set; } = "DWG";
        public bool IsLocked { get; set; }
        public bool IsXref { get; set; }
    }

=======
        public string Scale { get; set; } = "1:100";
        public bool IsLocked { get; set; }
        public bool IsXref { get; set; }
    }
>>>>>>> origin/agent/review-hardening-20260810
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
