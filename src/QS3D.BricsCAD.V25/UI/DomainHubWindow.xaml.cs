using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class DomainHubWindow : Window
    {
        private bool _commercialQsCardInstalled;

        public DomainHubWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => InstallCommercialQsCard();
        }

        private void InstallCommercialQsCard()
        {
            if (_commercialQsCardInstalled) return;

            var root = Content as Grid;
            var scroll = root?.Children
                .OfType<ScrollViewer>()
                .FirstOrDefault(x => Grid.GetRow(x) == 1);
            var cardsGrid = scroll?.Content as Grid;
            var rightColumn = cardsGrid?.Children
                .OfType<StackPanel>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 2);
            if (rightColumn == null) return;

            var card = new Border
            {
                Style = TryFindResource("HubSectionCard") as Style
            };
            var content = new StackPanel();
            card.Child = content;

            content.Children.Add(new TextBlock
            {
                Text = "COMMERCIAL QS",
                Style = TryFindResource("PanelTitle") as Style
            });
            content.Children.Add(new TextBlock
            {
                Text = "Variation • IPC • Final Account — dữ liệu lưu trong QS3D project.",
                Style = TryFindResource("Caption") as Style,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });
            content.Children.Add(HubCommandButton("Variation Register", "QS3D_VARIATIONS", true));
            content.Children.Add(HubCommandButton("Interim Payment Certificate (IPC)", "QS3D_IPC", true));
            content.Children.Add(HubCommandButton("Final Account", "QS3D_FINAL_ACCOUNT", true));

            rightColumn.Children.Insert(0, card);
            _commercialQsCardInstalled = true;
        }

        private Button HubCommandButton(string content, string command, bool accent)
        {
            var button = new Button
            {
                Content = content,
                Tag = command,
                Style = TryFindResource(accent ? "HubAccentButton" : "HubCommandButton") as Style
            };
            button.Click += OnCommandClick;
            return button;
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
