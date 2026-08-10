using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class DomainHubWindow : Window
    {
        public DomainHubWindow()
        {
            InitializeComponent();
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            var command = (button.Tag as string ?? string.Empty).Trim();
            if (command.Length == 0) return;
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                StatusText.Text = "Chưa có bản vẽ BricsCAD đang active.";
                return;
            }
            document.SendStringToExecute(command + " ", true, false, false);
            StatusText.Text = "Đã gửi lệnh " + command + " sang " + System.IO.Path.GetFileName(document.Name) + ".";
        }
    }
}
