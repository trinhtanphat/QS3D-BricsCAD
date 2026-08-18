using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Adds the original QS3D red-X / green-V mark to the Workspace header. The mark is authored
    /// entirely from QS3D-owned vector geometry; BLT3D remains a clean-room workflow reference only.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Qs3dWorkspaceBrandRegistered = RegisterQs3dWorkspaceBrand();
        private const string Qs3dWorkspaceBrandName = "Qs3dWorkspaceBrandMark";

        private static bool RegisterQs3dWorkspaceBrand()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQs3dWorkspaceBrandLoaded),
                true);
            return true;
        }

        private static void OnQs3dWorkspaceBrandLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !Qs3dWorkspaceBrandRegistered)
                return;

            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(panel.EnsureQs3dWorkspaceBrandMark));
        }

        private void EnsureQs3dWorkspaceBrandMark()
        {
            if (FindName(Qs3dWorkspaceBrandName) != null || WorkspaceContentRoot == null)
                return;

            var header = WorkspaceContentRoot.Children
                .OfType<Border>()
                .FirstOrDefault(child => Grid.GetRow(child) == 0);
            var headerGrid = header?.Child as Grid;
            var left = headerGrid?.Children
                .OfType<StackPanel>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 0);
            if (left == null)
                return;

            var mark = new Grid
            {
                Name = Qs3dWorkspaceBrandName,
                Width = 32,
                Height = 20,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "QS3D • X đỏ / V xanh"
            };
            RegisterName(Qs3dWorkspaceBrandName, mark);

            var tile = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 24, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(67, 80, 98)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
            mark.Children.Add(tile);

            var canvas = new Canvas { Width = 30, Height = 18 };
            mark.Children.Add(canvas);

            canvas.Children.Add(new Path
            {
                Data = Geometry.Parse("M 5,4 L 13,13 M 13,4 L 5,13"),
                Stroke = new SolidColorBrush(Color.FromRgb(232, 74, 74)),
                StrokeThickness = 2.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
            canvas.Children.Add(new Path
            {
                Data = Geometry.Parse("M 17,8 L 21,13 L 27,4"),
                Stroke = new SolidColorBrush(Color.FromRgb(82, 190, 108)),
                StrokeThickness = 2.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            });

            left.Children.Insert(0, mark);
        }
    }
}
