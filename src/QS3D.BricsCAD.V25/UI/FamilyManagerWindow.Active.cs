using System;
using System.Windows;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FamilyManagerWindow
    {
        private void OnActivateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("đặt Family active");
                var family = FamilyList.SelectedItem as ProjectFamily ?? throw new InvalidOperationException("Chọn Family trước khi đặt active.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var previous = ProjectFamilyActivationService.GetActive(project);
                ProjectFamilyActivationService.SetActive(project, family.Id);
                if (previous == null || !string.Equals(previous.Id, family.Id, StringComparison.OrdinalIgnoreCase))
                    AuditTrail.ForProject(project).Record("family.activate", string.Empty, (previous?.Id ?? "") + " -> " + family.Id + " • " + family.Name);
                ActiveFamilyText.Text = family.Name + " • " + family.Category;
                PaletteCoordinator.RefreshProject();
                SetStatus("Family active: “" + family.Name + "”. Semantic Capture sẽ ưu tiên Family này khi Category khớp.");
            }
            catch (Exception ex) { SetStatus("Đặt Family active lỗi: " + ex.Message); }
        }
    }
}
