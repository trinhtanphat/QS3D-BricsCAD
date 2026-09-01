using System;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class Rebar3DHubWindow : Window
    {
        public Rebar3DHubWindow()
        {
            InitializeComponent();
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            var normalizedCommand = command.Trim();
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                StatusText.Text = "Không có drawing BricsCAD đang active.";
                return;
            }

            try
            {
                document.SendStringToExecute(normalizedCommand + " ", true, false, false);
                StatusText.Text = "Đã gửi lệnh " + normalizedCommand + " sang " + DrawingLabel(document) + ".";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Không thể gửi lệnh " + normalizedCommand + " sang BricsCAD.";
                try { document.Editor.WriteMessage("\n" + normalizedCommand + " dispatch failed (" + ex.GetType().Name + ")."); } catch { }
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
