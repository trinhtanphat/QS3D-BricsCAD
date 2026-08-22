using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ProjectToolsWindow : Window
    {
        public ProjectToolsWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshSnapshot();
            Activated += (_, __) => RefreshSnapshot();
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            SetStatus("Chạy " + command + "…");
            document.SendStringToExecute(command + " ", true, false, false);
        }

        private void RefreshSnapshot()
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                ProjectNameText.Text = string.IsNullOrWhiteSpace(project.Name) ? project.ProjectId : project.Name;
                var activeFloor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase));
                FloorText.Text = activeFloor == null
                    ? (string.IsNullOrWhiteSpace(project.ActiveFloorId) ? "—" : project.ActiveFloorId)
                    : activeFloor.Name + " • " + activeFloor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
                FamilyCountText.Text = project.Families.Count.ToString(CultureInfo.InvariantCulture);
                ElementCountText.Text = project.Elements.Count.ToString(CultureInfo.InvariantCulture);
                SetStatus("Project snapshot đã đồng bộ.");
            }
            catch (Exception ex) { SetStatus("Đọc Project Tools lỗi: " + ex.Message); }
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            PaletteCoordinator.SetStatus(StatusText.Text);
        }
    }
}
