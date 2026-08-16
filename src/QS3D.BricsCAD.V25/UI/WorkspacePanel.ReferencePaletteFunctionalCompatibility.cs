using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps production actions and edit-semantic controls reachable after the final reference
    /// presentation pass. Visual parity must not silently remove commands or change what a command
    /// claims to do.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool ReferencePaletteFunctionalCompatibilityRegistered =
            RegisterReferencePaletteFunctionalCompatibility();

        private static bool RegisterReferencePaletteFunctionalCompatibility()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnReferencePaletteFunctionalCompatibilityLoaded),
                true);
            return true;
        }

        private static void OnReferencePaletteFunctionalCompatibilityLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !ReferencePaletteFunctionalCompatibilityRegistered)
                return;

            // ReferencePaletteLayout runs at ApplicationIdle. SystemIdle deliberately runs after it
            // so visual-density work cannot hide functional controls again.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                new Action(panel.RestoreReferencePaletteFunctionalControls));
        }

        private void RestoreReferencePaletteFunctionalControls()
        {
            var scopeLabel = FindTextBlock("Phạm vi sửa");
            var scopeGrid = FindNearestAncestor<Grid>(scopeLabel);
            if (scopeGrid != null)
                scopeGrid.Visibility = Visibility.Visible;

            var propertySearchBorder = FindNearestAncestor<Border>(PropertySearch);
            if (propertySearchBorder != null)
                propertySearchBorder.Visibility = Visibility.Visible;

            RestoreReferencePaletteButton("Làm mới");
            RestoreReferencePaletteButton("Vẽ 3D");
            RestoreReferencePaletteButton("Kiểm tra");

            // This production action imports the current CAD selection. Do not advertise an
            // autonomous/background importer that does not exist.
            RenameBlt3dButton("⚡ Nhập tự động", "⚡ Nhập từ chọn");
            RenameBlt3dButton("Bóc chọn", "⚡ Nhập từ chọn");
            RestoreReferencePaletteButton("⚡ Nhập từ chọn");
        }

        private void RestoreReferencePaletteButton(string text)
        {
            var button = FindButton(text);
            if (button != null)
                button.Visibility = Visibility.Visible;
        }
    }
}
