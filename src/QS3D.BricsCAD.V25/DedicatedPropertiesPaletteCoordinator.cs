using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Bricscad.Windows;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Owns the dedicated QS3D Properties plugin palette required by the BLT3D BIM workspace.
    /// This is intentionally separate from BricsCAD's native Properties palette and from the
    /// monolithic Workspace palette. Visibility is activated only by the coordinated BIM surface.
    /// </summary>
    internal static class DedicatedPropertiesPaletteCoordinator
    {
        private static readonly Guid PropertiesGuid = new Guid("43E4BCFA-1697-43D4-95EF-90B88C59D61A");
        private static readonly WpfSize DefaultSize = new WpfSize(320d, 620d);
        private static PaletteSet? _palette;
        private static DedicatedPropertiesPanel? _panel;
        private static DispatcherTimer? _visibilityWatchdog;

        internal static bool IsVisible => _palette != null && _palette.Visible;

        internal static void SyncVisibility()
        {
            if (!IsBimSurfaceVisible())
            {
                Hide();
                return;
            }

            EnsureCreated();
            EnsureDockFallback();
            if (_palette != null)
                _palette.Visible = true;
            StartVisibilityWatchdog();
        }

        internal static void SetInspection(IEnumerable? snapshots)
        {
            if (!IsBimSurfaceVisible())
            {
                Hide();
                return;
            }

            EnsureCreated();
            EnsureDockFallback();
            _panel?.SetInspection(snapshots);
        }

        internal static void Hide()
        {
            if (_palette != null)
                _palette.Visible = false;
            StopVisibilityWatchdog();
        }

        internal static void Dispose()
        {
            StopVisibilityWatchdog();
            var current = _palette;
            _palette = null;
            _panel = null;
            if (current == null) return;
            try { current.Dispose(); }
            catch
            {
                // Native palette teardown is best-effort and must not block host shutdown.
            }
        }

        private static bool IsBimSurfaceVisible()
        {
            return PaletteCoordinator.IsWorkspaceVisible &&
                   PaletteCoordinator.IsRightPanelVisible &&
                   PaletteCoordinator.IsQuantityInsightVisible;
        }

        private static void EnsureCreated()
        {
            if (_palette != null && _panel != null) return;
            Dispose();

            _panel = new DedicatedPropertiesPanel();
            _palette = new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)
            {
                DockEnabled = DockSides.Left | DockSides.Right,
                Dock = DockSides.Left,
                Visible = false,
                KeepFocus = false,
                MinimumSize = new DrawingSize(260, 320)
            };
            _palette.DeviceIndependentSize = DefaultSize;
            _palette.AddVisual("Thuộc tính QS3D", _panel, true);
        }

        private static void EnsureDockFallback()
        {
            if (_palette == null) return;
            if (_palette.Dock != DockSides.Left)
                _palette.Dock = DockSides.Left;

            var size = _palette.DeviceIndependentSize;
            if (!IsFinite(size.Width) || !IsFinite(size.Height) ||
                size.Width < 260d || size.Width > 760d || size.Height < 320d)
            {
                _palette.DeviceIndependentSize = DefaultSize;
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void StartVisibilityWatchdog()
        {
            if (_visibilityWatchdog == null)
            {
                _visibilityWatchdog = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
                {
                    Interval = TimeSpan.FromMilliseconds(250d)
                };
                _visibilityWatchdog.Tick += OnVisibilityWatchdogTick;
            }

            if (!_visibilityWatchdog.IsEnabled)
                _visibilityWatchdog.Start();
        }

        private static void StopVisibilityWatchdog()
        {
            if (_visibilityWatchdog != null && _visibilityWatchdog.IsEnabled)
                _visibilityWatchdog.Stop();
        }

        private static void OnVisibilityWatchdogTick(object? sender, EventArgs e)
        {
            // Never reopen a palette here: manual close must remain respected after activation.
            // The watchdog only retires this plugin surface when BIM coordination is no longer active.
            if (!IsBimSurfaceVisible())
                Hide();
        }
    }

    /// <summary>
    /// Lightweight, plugin-owned QS3D selection inspector. It deliberately renders in a QS3D
    /// PaletteSet so native BricsCAD Properties can never be mistaken for the required region.
    /// </summary>
    internal sealed class DedicatedPropertiesPanel : UserControl
    {
        private readonly TextBlock _selectionSummary;
        private readonly ListView _rows;

        internal DedicatedPropertiesPanel()
        {
            MinWidth = 240d;
            MinHeight = 300d;
            Background = new SolidColorBrush(Color.FromRgb(35, 38, 43));
            Foreground = Brushes.White;

            var root = new Grid { Margin = new Thickness(8d) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = "THUỘC TÍNH QS3D",
                FontSize = 13d,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0d, 0d, 0d, 3d)
            };
            root.Children.Add(title);

            _selectionSummary = new TextBlock
            {
                Text = "Chưa chọn đối tượng",
                Foreground = new SolidColorBrush(Color.FromRgb(170, 178, 188)),
                Margin = new Thickness(0d, 0d, 0d, 8d),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(_selectionSummary, 1);
            root.Children.Add(_selectionSummary);

            _rows = new ListView
            {
                BorderThickness = new Thickness(1d),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 75, 82)),
                Background = new SolidColorBrush(Color.FromRgb(28, 31, 35)),
                Foreground = Brushes.White
            };
            var gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Thuộc tính",
                Width = 132d,
                DisplayMemberBinding = new Binding(nameof(PropertyDisplayRow.Name))
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Giá trị",
                Width = 160d,
                DisplayMemberBinding = new Binding(nameof(PropertyDisplayRow.Value))
            });
            _rows.View = gridView;
            Grid.SetRow(_rows, 2);
            root.Children.Add(_rows);
            Content = root;

            SetInspection(null);
        }

        internal void SetInspection(IEnumerable? snapshots)
        {
            var selected = snapshots == null
                ? new List<object>()
                : snapshots.Cast<object>().Where(item => item != null).ToList();

            if (selected.Count == 0)
            {
                _selectionSummary.Text = "Chưa chọn đối tượng • QS3D plugin inspector";
                _rows.ItemsSource = new[]
                {
                    new PropertyDisplayRow("Nguồn", "QS3D"),
                    new PropertyDisplayRow("Trạng thái", "Chọn đối tượng trong modelspace để xem thuộc tính")
                };
                return;
            }

            _selectionSummary.Text = selected.Count == 1
                ? "1 đối tượng đang chọn • QS3D plugin inspector"
                : selected.Count.ToString(CultureInfo.InvariantCulture) + " đối tượng đang chọn • giá trị chung";
            _rows.ItemsSource = BuildRows(selected);
        }

        private static IReadOnlyList<PropertyDisplayRow> BuildRows(IReadOnlyList<object> selected)
        {
            var first = selected[0];
            var properties = first.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .Where(property => IsDisplayable(property.PropertyType))
                .OrderBy(property => PropertyRank(property.Name))
                .ThenBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
                .Take(32)
                .ToArray();

            var rows = new List<PropertyDisplayRow>(properties.Length + 1)
            {
                new PropertyDisplayRow("QS3D selection", selected.Count.ToString(CultureInfo.InvariantCulture))
            };

            foreach (var property in properties)
            {
                var firstValue = SafeRead(property, first);
                var common = true;
                for (var index = 1; index < selected.Count; index++)
                {
                    var candidate = selected[index];
                    var candidateProperty = candidate.GetType().GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public);
                    var candidateValue = candidateProperty == null ? null : SafeRead(candidateProperty, candidate);
                    if (!string.Equals(firstValue, candidateValue, StringComparison.Ordinal))
                    {
                        common = false;
                        break;
                    }
                }

                rows.Add(new PropertyDisplayRow(property.Name, common ? firstValue : "— nhiều giá trị —"));
            }

            return rows;
        }

        private static bool IsDisplayable(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) ||
                   underlying == typeof(decimal) || underlying == typeof(DateTime) || underlying == typeof(Guid);
        }

        private static int PropertyRank(string name)
        {
            if (string.Equals(name, "Handle", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(name, "Category", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(name, "Layer", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(name, "Name", StringComparison.OrdinalIgnoreCase)) return 3;
            return 10;
        }

        private static string SafeRead(PropertyInfo property, object target)
        {
            try
            {
                var value = property.GetValue(target, null);
                return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch
            {
                return "<không đọc được>";
            }
        }

        private sealed class PropertyDisplayRow
        {
            internal PropertyDisplayRow(string name, string value)
            {
                Name = name ?? string.Empty;
                Value = value ?? string.Empty;
            }

            public string Name { get; }
            public string Value { get; }
        }
    }
}
