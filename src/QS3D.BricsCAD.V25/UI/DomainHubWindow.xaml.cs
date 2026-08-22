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
            var command = button.Tag as string;
            if (string.IsNullOrWhiteSpace(command)) return;
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            document.SendStringToExecute(command.Trim() + " ", true, false, false);
        }
    }
}
