using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ScheduleHubWindow : Window
    {
        private readonly Document _document;

        public ScheduleHubWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            Loaded += (_, __) => RefreshSnapshot();
            Activated += (_, __) => RefreshSnapshot();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshSnapshot();

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            try
            {
                EnsureActive("chạy " + command);
                SetStatus("Chạy " + command + "…");
                _document.SendStringToExecute(command + " ", true, false, false);
            }
            catch (Exception ex) { SetStatus("Schedule Hub: " + ex.Message); }
        }

        private void RefreshSnapshot()
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                ElementCountText.Text = project.Elements.Count.ToString(CultureInfo.InvariantCulture);
                DoorCountText.Text = project.Elements.Count(x => x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening).ToString(CultureInfo.InvariantCulture);
                CurtainCountText.Text = project.Elements.Count(x => x.Category == ElementCategory.GlassWall).ToString(CultureInfo.InvariantCulture);
                MaterialCountText.Text = ProjectMaterialCatalog.ReferencedMaterialNames(project).Count.ToString(CultureInfo.InvariantCulture);
                Title = "QS3D • Schedule Hub • " + DrawingLabel(_document);
                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)) SetStatus("Schedule snapshot đã đồng bộ.");
                else SetStatus("Kích hoạt lại “" + DrawingLabel(_document) + "” trước khi chạy schedule/export command.");
            }
            catch (Exception ex) { SetStatus("Đọc Schedule Hub lỗi: " + ex.Message); }
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Schedule Hub trước khi " + operation + ".");
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            PaletteCoordinator.SetStatus(StatusText.Text);
        }
    }
}
