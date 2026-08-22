using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class MaterialCatalogWindow : Window
    {
        private readonly Document _document;
        private string _editingId = string.Empty;
        private bool _loading;

        public MaterialCatalogWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnMaterialSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || !(MaterialList.SelectedItem is ProjectMaterial material)) return;
            if (!material.IsBuiltIn)
            {
                _editingId = material.Id;
                NameBox.Text = material.Name;
                UnitBox.Text = material.Unit;
                DescriptionBox.Text = material.Description;
            }
            else
            {
                _editingId = string.Empty;
                NameBox.Text = material.Name;
                UnitBox.Text = material.Unit;
                DescriptionBox.Text = material.Description;
            }
            SetStatus(material.IsBuiltIn ? "Built-in material: có thể áp dụng nhưng không sửa/xóa." : "Custom material: " + material.Name);
        }

        private void OnNewClick(object sender, RoutedEventArgs e)
        {
            _editingId = string.Empty;
            NameBox.Text = string.Empty;
            UnitBox.Text = string.Empty;
            DescriptionBox.Text = string.Empty;
            MaterialList.SelectedItem = null;
            NameBox.Focus();
            SetStatus("Tạo custom material mới.");
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
=======
>>>>>>> origin/main
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var id = string.IsNullOrWhiteSpace(_editingId) ? "mat-" + Guid.NewGuid().ToString("N") : _editingId;
                var material = ProjectMaterialCatalog.UpsertCustom(project, id, NameBox.Text, UnitBox.Text, DescriptionBox.Text);
                AuditTrail.ForProject(project).Record("material.catalog.upsert", string.Empty, material.Id + " • " + material.Name);
                _editingId = material.Id;
                RefreshAll(material.Id);
                PaletteCoordinator.RefreshProject();
                SetStatus("Đã lưu custom material: " + material.Name + ".");
            }
            catch (Exception ex) { SetStatus("Lưu material lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null || !(MaterialList.SelectedItem is ProjectMaterial material)) return;
=======
            if (!(MaterialList.SelectedItem is ProjectMaterial material)) return;
>>>>>>> origin/main
            try
            {
                if (material.IsBuiltIn) throw new InvalidOperationException("Built-in material không thể xóa.");
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                if (!ProjectMaterialCatalog.DeleteCustom(project, material.Id)) return;
                AuditTrail.ForProject(project).Record("material.catalog.delete", string.Empty, material.Id + " • " + material.Name);
                _editingId = string.Empty;
                RefreshAll();
                PaletteCoordinator.RefreshProject();
                SetStatus("Đã xóa custom material: " + material.Name + ".");
            }
            catch (Exception ex) { SetStatus("Xóa material lỗi: " + ex.Message); }
        }

        private void OnApplyClick(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
=======
>>>>>>> origin/main
            try
            {
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                    throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Material Catalog trước khi áp dụng cho selection.");
                if (!(MaterialList.SelectedItem is ProjectMaterial material)) throw new InvalidOperationException("Chọn một material trước khi áp dụng.");
                var target = (TargetCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Material";
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project).ToList();
                if (elements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");
                if (string.Equals(target, "CurtainFrameMaterial", StringComparison.OrdinalIgnoreCase))
                {
                    var invalid = elements.Where(x => x.Category != ElementCategory.GlassWall).Select(x => x.Id).ToList();
                    if (invalid.Count > 0) throw new InvalidOperationException("CurtainFrameMaterial chỉ áp dụng cho Vách Kính. Selection không hợp lệ: " + string.Join(", ", invalid.Take(5)) + (invalid.Count > 5 ? "…" : string.Empty));
                }

                var changed = 0;
                foreach (var element in elements)
                {
                    var before = element.Properties.TryGetValue(target, out var value) ? value : string.Empty;
                    element.SetProperty(target, material.Name);
                    var after = element.Properties.TryGetValue(target, out var next) ? next : string.Empty;
                    if (string.Equals(before, after, StringComparison.Ordinal)) continue;
                    changed++;
                    AuditTrail.ForProject(project).Record("material.assign", element.Id, target + "=" + material.Name);
                }
                if (changed > 0) project.Touch();
                PaletteCoordinator.RefreshProject();
                RefreshAll(material.Id);
                SetStatus("Đã áp dụng “" + material.Name + "” cho " + changed + "/" + elements.Count + " semantic element • " + target + ".");
            }
            catch (Exception ex) { SetStatus("Áp dụng material lỗi: " + ex.Message); }
        }

        private void RefreshAll(string selectedId = "")
        {
<<<<<<< HEAD
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
=======
>>>>>>> origin/main
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var previous = string.IsNullOrWhiteSpace(selectedId) ? (MaterialList.SelectedItem as ProjectMaterial)?.Id : selectedId;
                var materials = ProjectMaterialCatalog.GetAll(project).ToList();
                _loading = true;
                try
                {
                    MaterialList.ItemsSource = materials;
                    MaterialList.SelectedItem = materials.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase));
                }
                finally { _loading = false; }
                var referenced = ProjectMaterialCatalog.ReferencedMaterialNames(project);
                ReferencedText.Text = referenced.Count == 0 ? "—" : string.Join(" • ", referenced);
                Title = "QS3D • Vật liệu • " + DrawingLabel(_document);
            }
            catch (Exception ex) { SetStatus("Đọc Material Catalog lỗi: " + ex.Message); }
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
