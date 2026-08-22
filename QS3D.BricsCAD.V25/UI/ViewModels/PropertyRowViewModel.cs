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
        public Func<string, string>? Apply { private get; set; }
        public string Value
        {
            get => _value;
            set
            {
                var requested = value ?? string.Empty;
                var next = !IsReadOnly && Apply != null ? Apply(requested) ?? string.Empty : requested;
                if (_value == next)
                {
                    if (requested != next) OnChanged();
                    return;
                }
                _value = next;
                OnChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
