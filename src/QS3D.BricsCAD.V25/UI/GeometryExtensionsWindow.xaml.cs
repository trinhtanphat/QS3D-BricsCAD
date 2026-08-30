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
            var normalizedCommand = command!.Trim();
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                StatusText.Text = "Không có BricsCAD document đang active.";
                return;
            }
            try
            {
                document.SendStringToExecute(normalizedCommand + " ", true, false, false);
                StatusText.Text = "Đã gửi lệnh " + normalizedCommand + " sang BricsCAD.";
            }
            catch (System.Exception ex)
            {
                StatusText.Text = normalizedCommand + " không thể gửi sang BricsCAD.";
                try { document.Editor.WriteMessage("\n" + normalizedCommand + " dispatch failed (" + ex.GetType().Name + ")."); } catch { }
            }
        }
    }
}
