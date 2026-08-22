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

        public const string FamilyState = "Family";
        public const string InstanceState = "Instance";
        public const string OverrideState = "Override";
        public const string CadState = "Cad";
        public const string SystemState = "System";
        public const string SelectionState = "Selection";
        public const string MultiState = "Multi";

        private string _value = string.Empty;
        private string _group = string.Empty;
        private string _name = string.Empty;
        private bool _canReset;
        private bool _isReadOnly;

        public string Group
        {
            get => _group;
            set
            {
                var next = value ?? string.Empty;
                if (_group == next) return;
                _group = next;
                OnChanged();
                OnStateChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                var next = value ?? string.Empty;
                if (_name == next) return;
                _name = next;
                OnChanged();
                OnStateChanged();
            }
        }

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
                OnStateChanged();
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
                OnStateChanged();
            }
        }

        public string StateKind
        {
            get
            {
                if (_canReset) return OverrideState;
                if (_isReadOnly)
                {
                    if (StartsWithGroup("NGUỒN CAD / ĐO ĐẠC") || StartsWithGroup("KHỐI LƯỢNG / ĐO ĐẠC"))
                        return CadState;
                    if (StartsWithGroup("SELECTION")) return SelectionState;
                    return SystemState;
                }

                if (StartsWithGroup("INSTANCE"))
                {
                    if (ContainsMultiSelectionMarker(_name)) return MultiState;
                    return InstanceState;
                }
                return FamilyState;
            }
        }

        public string StateLabel
        {
            get
            {
                switch (StateKind)
                {
                    case OverrideState: return "Override";
                    case CadState: return "CAD / đo";
                    case SystemState: return "Hệ thống";
                    case SelectionState: return "Selection";
                    case MultiState: return "Multi";
                    case InstanceState: return "Kế thừa";
                    default: return "Family";
                }
            }
        }

        public string StateSearchText
        {
            get
            {
                switch (StateKind)
                {
                    case OverrideState: return "Override ghi đè instance";
                    case CadState: return "CAD đo đạc nguồn source readonly read-only khóa";
                    case SystemState: return "Hệ thống system identity ownership readonly read-only khóa";
                    case SelectionState: return "Selection chọn metadata reference readonly read-only";
                    case MultiState: return "Multi selection common mixed nhiều chung instance";
                    case InstanceState: return "Instance kế thừa inherited family";
                    default: return "Family type";
                }
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

        private bool StartsWithGroup(string prefix) =>
            _group.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        private static bool ContainsMultiSelectionMarker(string name) =>
            name.IndexOf("• Chung", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
            name.IndexOf("• Nhiều giá trị", StringComparison.CurrentCultureIgnoreCase) >= 0;

        private static bool ParseBoolean(string value)
        {
            var text = (value ?? string.Empty).Trim();
            if (bool.TryParse(text, out var parsed)) return parsed;
            return text == "1" ||
                   text.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("bật", StringComparison.CurrentCultureIgnoreCase);
        }

        private void OnStateChanged()
        {
            OnChanged(nameof(StateKind));
            OnChanged(nameof(StateLabel));
            OnChanged(nameof(StateSearchText));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
