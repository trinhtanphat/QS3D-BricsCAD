using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        // The BLT3D mode button intentionally hides its chooser before the family-create path runs.
        // Observe the routed Click after the button handler has completed and enforce the UI invariant
        // for Parameter mode: either a live Family is selected with properties, or the chooser is
        // restored. This keeps a failed/cancelled create from stranding an empty subtype as a blank pane.
        private static bool Blt3dParameterModePostClickRecoveryRegistered { get; } =
            RegisterBlt3dParameterModePostClickRecovery();

        private static bool RegisterBlt3dParameterModePostClickRecovery()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                Button.ClickEvent,
                new RoutedEventHandler(OnBlt3dParameterModePostClick),
                true);
            return true;
        }

        private static void OnBlt3dParameterModePostClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            if (!(e.Source is Button button)) return;
            if (panel._blt3dFamilyModeChooser == null) return;
            if (!IsVisualDescendant(panel._blt3dFamilyModeChooser, button)) return;
            if (!IsBlt3dParameterModeButton(button)) return;

            // CreateFamilyFromWorkspaceSubtype uses RefreshAfterCommit, so inspect only after queued
            // refresh/selection work has had a chance to settle instead of racing the normal success path.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() => RecoverBlt3dParameterModeSurface(panel)));
        }

        private static bool IsBlt3dParameterModeButton(Button button)
        {
            if (!(button.Content is Panel content)) return false;
            return content.Children
                .OfType<TextBlock>()
                .Any(text => string.Equals(text.Text, "Tham số", StringComparison.Ordinal));
        }

        private static void RecoverBlt3dParameterModeSurface(WorkspacePanel panel)
        {
            if (panel._blt3dFamilyModeChooser == null) return;

            if (IsGridSubtype(panel._familySubtypeFilter))
                panel.ApplyGridFamilySubtypeFilter();
            else
                panel.ApplyFamilySubtypeFilter();

            var selected = panel.FamilyList.SelectedItem as ProjectFamily;
            if (selected == null)
            {
                selected = panel.FamilyList.Items
                    .Cast<object>()
                    .OfType<ProjectFamily>()
                    .FirstOrDefault();
            }

            if (selected != null)
            {
                panel._blt3dFamilyModeChooser.Visibility = Visibility.Collapsed;
                panel.FamilyList.Visibility = Visibility.Visible;
                panel.FamilyList.SelectedItem = selected;
                panel._viewModel.SetActiveFamily(selected);
                panel._viewModel.ShowFamilyProperties();
                panel.RefreshSelectedFamilyHighlight();
                return;
            }

            // Preserve the create-path status/error text. Calling ShowBlt3dFamilyModeChooser here
            // would replace the real diagnostic with the generic chooser prompt.
            panel.FamilyList.Visibility = Visibility.Collapsed;
            panel._blt3dFamilyModeChooser.Visibility = Visibility.Visible;
        }
    }
}
