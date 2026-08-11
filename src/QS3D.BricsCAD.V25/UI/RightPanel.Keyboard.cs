using System.Windows;
using System.Windows.Input;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel
    {
        private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;

            if (modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                LayerSearchBox.Focus();
                LayerSearchBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (modifiers == ModifierKeys.None && e.Key == Key.F5)
            {
                OnRefreshClick(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (modifiers == ModifierKeys.None && e.Key == Key.Escape)
            {
                if (!string.IsNullOrWhiteSpace(LayerSearchBox.Text))
                {
                    LayerSearchBox.Clear();
                    LayerSearchBox.Focus();
                }
                else
                {
                    OnClearLayerSelectionClick(this, new RoutedEventArgs());
                    OnClearDrawingSelectionClick(this, new RoutedEventArgs());
                }

                e.Handled = true;
            }
        }
    }
}
