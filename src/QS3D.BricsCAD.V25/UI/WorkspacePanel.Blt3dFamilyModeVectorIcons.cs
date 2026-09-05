using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Bounded presentation patch for the BLT3D Family-mode chooser.
    ///
    /// The chooser itself currently lives in WorkspacePanel.Blt3dFamilyWorkspace.cs, which is
    /// reserved by #4586 for Add-routing ownership. This partial deliberately leaves that source
    /// untouched and replaces only its two font-dependent placeholder glyphs after the chooser
    /// has been constructed.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dFamilyModeVectorIconsBootstrapRegistered =
            RegisterBlt3dFamilyModeVectorIconsBootstrap();

        private bool _blt3dFamilyModeVectorIconsApplied;

        private static bool RegisterBlt3dFamilyModeVectorIconsBootstrap()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dFamilyModeVectorIconsLoaded),
                true);
            return true;
        }

        private static void OnBlt3dFamilyModeVectorIconsLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;

            // The legacy chooser bootstrap queues construction at DispatcherPriority.Loaded.
            // ContextIdle is intentionally later, so the bounded icon replacement never races
            // chooser creation and never needs to alter #4586's handler/routing source.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.ApplyBlt3dFamilyModeVectorIcons));
        }

        private void ApplyBlt3dFamilyModeVectorIcons()
        {
            if (!Blt3dFamilyModeVectorIconsBootstrapRegistered || _blt3dFamilyModeVectorIconsApplied)
                return;
            if (_blt3dFamilyModeChooser == null)
                return;

            var parameterButton = FindModeCardButton("Tham số");
            var solid3dButton = FindModeCardButton("Solid3D");
            if (parameterButton == null || solid3dButton == null)
                return;

            Blt3dVectorIcon.ApplyModeCard(parameterButton, Blt3dVectorIcon.Parameter, 30d);
            Blt3dVectorIcon.ApplyModeCard(solid3dButton, Blt3dVectorIcon.Solid3D, 30d);
            _blt3dFamilyModeVectorIconsApplied = true;
        }

        private Button? FindModeCardButton(string label)
        {
            if (_blt3dFamilyModeChooser == null) return null;

            return FindVisualChildren<Button>(_blt3dFamilyModeChooser)
                .FirstOrDefault(button =>
                {
                    var stack = button.Content as StackPanel;
                    return stack != null &&
                           stack.Children.OfType<TextBlock>()
                               .Any(text => string.Equals(text.Text, label, StringComparison.Ordinal));
                });
        }
    }
}
