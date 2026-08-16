using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owner-reference labels for the existing native Xref/layer manager. Only presentation is
    /// changed; every button keeps its current production event handler and BricsCAD mutation path.
    /// </summary>
    public partial class RightPanel
    {
        private static readonly bool Blt3dRightReferenceShellRegistered = RegisterBlt3dRightReferenceShell();
        private bool _blt3dRightReferenceShellApplied;

        private static bool RegisterBlt3dRightReferenceShell()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dRightReferenceShellLoaded),
                true);
            return true;
        }

        private static void OnBlt3dRightReferenceShellLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is RightPanel panel)) return;
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.ApplyBlt3dRightReferenceShell));
        }

        private void ApplyBlt3dRightReferenceShell()
        {
            if (!Blt3dRightReferenceShellRegistered || _blt3dRightReferenceShellApplied) return;

            RenameRightTitle("QUẢN LÝ BẢN VẼ", "Quản lý bản vẽ");
            RenameRightTitle("QUẢN LÝ LỚP", "Quản lý lớp");
            RenameRightButton("Nạp lại", "Nạp");
            RenameRightButton("Gỡ Xref", "Xóa");
            RenameRightButton("Đảo chọn", "Đảo");

            var clearDrawing = FindRightButton("Bỏ chọn");
            if (clearDrawing != null)
                clearDrawing.Visibility = Visibility.Collapsed;

            _blt3dRightReferenceShellApplied = true;
        }

        private void RenameRightTitle(string oldText, string newText)
        {
            var title = FindRightVisualChildren<TextBlock>(this)
                .FirstOrDefault(text => string.Equals(text.Text, oldText, StringComparison.OrdinalIgnoreCase));
            if (title != null) title.Text = newText;
        }

        private void RenameRightButton(string oldText, string newText)
        {
            foreach (var button in FindRightVisualChildren<Button>(this)
                .Where(button => string.Equals(button.Content as string, oldText, StringComparison.Ordinal)))
                button.Content = newText;
        }
    }
}
