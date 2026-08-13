using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Pins Workspace controls to QS3D-owned dark resources when the BricsCAD palette
    /// host supplies brighter system/default selection or control chrome.
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

            // Theme.xaml owns the canonical host-independent ComboBox template. Publish
            // it directly at the Workspace boundary so every descendant ComboBox (static
            // scope/property controls and later DataTemplate instances) resolves the QS3D
            // style before any BricsCAD/application-level implicit style. The two screenshot-
            // visible scope controls are also pinned locally because they already exist when
            // the guard runs and must not retain a host style resolved earlier in loading.
            if (TryFindResource(typeof(ComboBox)) is Style comboStyle)
            {
                Resources[typeof(ComboBox)] = comboStyle;
                PinScopeComboStyle(ZoneCombo, comboStyle);
                PinScopeComboStyle(FloorCombo, comboStyle);
            }

            // TreeViewItem/ListBoxItem/ListViewItem styles in Theme.xaml deliberately keep
            // the stock WPF container templates. Those templates can resolve active/inactive
            // selection brushes through SystemColors. Shadow all four keys at the Workspace
            // resource boundary so every collection surface (both TreeViews, FamilyList,
            // PropertyList and InspectionList) inherits the QS3D dark selection palette.
            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinWorkspaceSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinWorkspaceSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinWorkspaceSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinWorkspaceSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }
        }

        private void PinWorkspaceSelectionResource(object resourceKey, Brush brush)
        {
            Resources[resourceKey] = brush;

            // Keep the originally reported ModelTree locally pinned as well. This makes
            // its already-created item containers update immediately while Workspace-level
            // resources cover every other current and future descendant collection control.
            ModelTree.Resources[resourceKey] = brush;
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
