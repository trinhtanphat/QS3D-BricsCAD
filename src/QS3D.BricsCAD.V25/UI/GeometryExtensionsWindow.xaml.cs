using System;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class GeometryExtensionsWindow : Window
    {
        public GeometryExtensionsWindow()
        {
            InitializeComponent();
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var command = button?.Tag as string;
            if (string.IsNullOrWhiteSpace(command)) return;
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                StatusText.Text = "Không có BricsCAD document đang active.";
                return;
            }
            try
            {
                document.SendStringToExecute(command.Trim() + " ", true, false, false);
                StatusText.Text = "Đã gửi lệnh " + command.Trim() + " sang BricsCAD.";
            }
            catch (Exception ex)
            {
                StatusText.Text = command.Trim() + " lỗi: " + ex.Message;
            }
        }
    }
}
