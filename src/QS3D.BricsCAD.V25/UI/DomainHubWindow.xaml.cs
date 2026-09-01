using System;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

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

            try
            {
                document.SendStringToExecute(command + " ", true, false, false);
                StatusText.Text = "Đã gửi lệnh " + command + " sang " + DrawingLabel(document) + ".";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Không thể gửi lệnh " + command + " sang BricsCAD.";
                try { document.Editor.WriteMessage("\n" + command + " dispatch failed (" + ex.GetType().Name + ")."); } catch { }
            }
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }
    }
}
