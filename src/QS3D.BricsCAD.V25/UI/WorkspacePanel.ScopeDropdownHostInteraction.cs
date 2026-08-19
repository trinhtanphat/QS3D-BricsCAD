using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// BricsCAD PaletteSet interaction fallback for the two Workspace scope ComboBoxes.
    ///
    /// The shared premium ComboBox template owns its dark chrome and uses a transparent
    /// template toggle. Some PaletteSet host/input paths can consume the mouse press before
    /// that template toggle changes IsDropDownOpen. Keep the normal ComboBox semantics, but
    /// explicitly open these two non-editable scope selectors on their first preview press.
    /// Item clicks are left untouched because the fallback only runs while the popup is closed.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool WorkspaceScopeDropdownHostInteractionRegistered =
            RegisterWorkspaceScopeDropdownHostInteraction();

        private bool _workspaceScopeDropdownHostInteractionWired;

        private static bool RegisterWorkspaceScopeDropdownHostInteraction()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspaceScopeDropdownHostInteractionLoaded),
                true);
            return true;
        }

        private static void OnWorkspaceScopeDropdownHostInteractionLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !WorkspaceScopeDropdownHostInteractionRegistered)
                return;

            panel.WireWorkspaceScopeDropdownHostInteraction();
        }

        private void WireWorkspaceScopeDropdownHostInteraction()
        {
            if (_workspaceScopeDropdownHostInteractionWired)
                return;

            _workspaceScopeDropdownHostInteractionWired = true;
            WireWorkspaceScopeCombo(ZoneCombo);
            WireWorkspaceScopeCombo(FloorCombo);
        }

        private static void WireWorkspaceScopeCombo(ComboBox combo)
        {
            combo.PreviewMouseLeftButtonDown += OnWorkspaceScopeComboPreviewMouseLeftButtonDown;
        }

        private static void OnWorkspaceScopeComboPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ComboBox combo) || !combo.IsEnabled || combo.IsDropDownOpen || !combo.HasItems)
                return;

            combo.Focus();
            combo.IsDropDownOpen = true;

            // Prevent the same press from reaching the custom template ToggleButton and
            // immediately toggling the popup closed again inside the host.
            e.Handled = true;
        }
    }
}
