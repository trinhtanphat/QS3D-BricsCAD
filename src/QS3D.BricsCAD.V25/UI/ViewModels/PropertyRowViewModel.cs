using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class PropertyRowViewModel : INotifyPropertyChanged
    {
        private string _value = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; }
        public Action<string>? Apply { private get; set; }
        public string Value
        {
            get => _value;
            set
            {
                var next = value ?? string.Empty;
                if (_value == next) return;
                _value = next;
                if (!IsReadOnly) Apply?.Invoke(next);
                OnChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
