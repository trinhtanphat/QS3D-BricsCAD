using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QS3D.BricsCAD.V25.UI.ViewModels
{
    public sealed class PropertyRowViewModel : INotifyPropertyChanged
    {
        public const string TextEditor = "Text";
        public const string BooleanEditor = "Boolean";
        public const string ChoiceEditor = "Choice";

        private string _value = string.Empty;
        private bool _canReset;
        private bool _isReadOnly;
        public string Group { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly == value) return;
                _isReadOnly = value;
                if (_isReadOnly && _canReset)
                {
                    _canReset = false;
                    OnChanged(nameof(CanReset));
                }
                OnChanged();
                OnChanged(nameof(IsEditable));
            }
        }
        public bool IsEditable => !IsReadOnly;
        public string EditorKind { get; set; } = TextEditor;
        public IReadOnlyList<string> Choices { get; set; } = Array.Empty<string>();
        public Func<string, string>? Apply { private get; set; }
        public Action? Reset { private get; set; }

        public bool CanReset
        {
            get => _canReset;
            set
            {
                var next = !_isReadOnly && value;
                if (_canReset == next) return;
                _canReset = next;
                OnChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                var requested = value ?? string.Empty;
                if ((IsReadOnly || Apply == null) && string.Equals(_value, requested, StringComparison.Ordinal)) return;
                var next = !IsReadOnly && Apply != null ? Apply(requested) ?? string.Empty : requested;
                if (_value == next)
                {
                    if (requested != next) OnChanged();
                    OnChanged(nameof(BooleanValue));
                    return;
                }
                _value = next;
                OnChanged();
                OnChanged(nameof(BooleanValue));
            }
        }

        public bool BooleanValue
        {
            get => ParseBoolean(_value);
            set => Value = value ? "true" : "false";
        }

        public void ResetValue()
        {
            if (!CanReset || IsReadOnly || Reset == null) return;
            Reset();
        }

        private static bool ParseBoolean(string value)
        {
            var text = (value ?? string.Empty).Trim();
            if (bool.TryParse(text, out var parsed)) return parsed;
            return text == "1" ||
                   text.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("bật", StringComparison.CurrentCultureIgnoreCase);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
