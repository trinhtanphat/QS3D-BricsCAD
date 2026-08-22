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
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                StatusText.Text = "Không có drawing BricsCAD đang active.";
                return;
            }
            StatusText.Text = "Gửi lệnh: " + command;
            document.SendStringToExecute(command.Trim() + " ", true, false, false);
        }
    }
}
