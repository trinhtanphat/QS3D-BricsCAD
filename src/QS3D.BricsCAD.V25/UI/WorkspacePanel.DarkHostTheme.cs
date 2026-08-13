using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Pins the narrow Workspace scope controls to QS3D-owned dark resources when the
    /// BricsCAD palette host supplies brighter system/default control resources.
    /// Presentation-only: no scope, selection or project semantics are changed here.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool DarkHostThemeGuardRegistered = RegisterDarkHostThemeGuard();
        private bool _darkHostThemeGuardApplied;

        private static bool RegisterDarkHostThemeGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnDarkHostThemeLoaded),
                true);
            return true;
        }

        private static void OnDarkHostThemeLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.ApplyDarkHostThemeGuard();
        }

        private void ApplyDarkHostThemeGuard()
        {
            if (_darkHostThemeGuardApplied)
                return;

            _darkHostThemeGuardApplied = true;

            // Theme.xaml already owns a host-independent ComboBox template. Resolving
            // that implicit style here and assigning it locally prevents a palette host
            // style from winning when Zone/Floor are focused or their selection changes.
            if (TryFindResource(typeof(ComboBox)) is Style comboStyle)
            {
                PinScopeComboStyle(ZoneCombo, comboStyle);
                PinScopeComboStyle(FloorCombo, comboStyle);
            }

            // The stock TreeViewItem template can still ask WPF for system selection
            // brushes even though our implicit TreeViewItem style sets Background.
            // Shadow both active and inactive keys at the ModelTree resource boundary so
            // nested containers keep the QS3D dark selection surface inside BricsCAD.
            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                ModelTree.Resources[SystemColors.HighlightBrushKey] = selectionBrush;
                ModelTree.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectionBrush;
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                ModelTree.Resources[SystemColors.HighlightTextBrushKey] = selectionTextBrush;
                ModelTree.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = selectionTextBrush;
            }
        }

        private static void PinScopeComboStyle(ComboBox combo, Style comboStyle)
        {
            combo.Style = comboStyle;
            combo.SetResourceReference(Control.BackgroundProperty, "BgInputBrush");
            combo.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            combo.SetResourceReference(Control.BorderBrushProperty, "BorderStrongBrush");
        }
    }
}
