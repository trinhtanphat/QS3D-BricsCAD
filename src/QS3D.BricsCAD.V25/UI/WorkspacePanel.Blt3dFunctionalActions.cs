using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dFunctionalActionsBootstrapRegistered =
            RegisterBlt3dFunctionalActionsBootstrap();

        private static bool RegisterBlt3dFunctionalActionsBootstrap()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dFunctionalActionsLoaded),
                true);
            return true;
        }

        private static void OnBlt3dFunctionalActionsLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !Blt3dFunctionalActionsBootstrapRegistered) return;

            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.RestoreBlt3dFunctionalActions));
        }

        private void RestoreBlt3dFunctionalActions()
        {
            if (!_blt3dFamilyWorkspaceApplied) return;

            var refresh = FindButton("Làm mới");
            if (refresh != null)
            {
                if (refresh.Parent is DockPanel collapsedHeader &&
                    collapsedHeader.Parent is StackPanel host)
                {
                    var headerIndex = host.Children.IndexOf(collapsedHeader);
                    if (headerIndex >= 0)
                    {
                        collapsedHeader.Children.Remove(refresh);
                        refresh.ClearValue(DockPanel.DockProperty);
                        host.Children.Insert(Math.Min(headerIndex + 1, host.Children.Count), refresh);
                    }
                }

                refresh.Visibility = Visibility.Visible;
                refresh.HorizontalAlignment = HorizontalAlignment.Right;
                refresh.Margin = new Thickness(0, 0, 0, 5);
            }

            var native3d = FindButton("Vẽ 3D");
            if (native3d != null)
                native3d.Visibility = Visibility.Visible;
        }
    }
}
