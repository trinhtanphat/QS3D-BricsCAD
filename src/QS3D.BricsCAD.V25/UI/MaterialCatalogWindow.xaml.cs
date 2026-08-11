using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class MaterialCatalogWindow : Window
    {
        private readonly Document _document;
        private ProjectState _boundProject;
        private string _editingId = string.Empty;
        private bool _loading;

        public MaterialCatalogWindow(Document document)
            : this(document, RequireExistingProject(document))
        {
        }

        public MaterialCatalogWindow(Document document, ProjectState project)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _boundProject = project ?? throw new ArgumentNullException(nameof(project));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("xuất bảng vật liệu");
                SetStatus("Chuẩn bị Material Usage XLSX…");
                _document.SendStringToExecute("QS3DMATERIALXLSX ", true, false, false);
            }
            catch (Exception ex) { SetStatus("Xuất bảng vật liệu lỗi: " + ex.Message); }
        }

        private void OnMaterialSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (MaterialList.SelectedItem is ProjectMaterial material)
                LoadEditor(material, true);
        }

        private void OnNewClick(object sender, RoutedEventArgs e)
        {
            ClearEditor();
            MaterialList.SelectedItem = null;
            NameBox.Focus();
            SetStatus("Tạo custom material mới.");
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("lưu material");
                if (!ExistingProjectMutationContext.TryGet(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Material Catalog không tạo project thay thế; hãy nạp project rồi Refresh trước khi lưu.");
                RequireBoundProject(project, "thử lại lưu material");
                var editingExisting = !string.IsNullOrWhiteSpace(_editingId);
                if (editingExisting)
                {
                    var current = ProjectMaterialCatalog.GetCustom(project)
                        .FirstOrDefault(x => string.Equals(x.Id, _editingId, StringComparison.OrdinalIgnoreCase));
                    if (current == null)
                        throw new InvalidOperationException("Material đang chỉnh sửa không còn tồn tại trong project hiện tại. Hãy Refresh rồi chọn lại material; Save không tự tạo lại row stale.");
                }

                var id = editingExisting ? _editingId : "mat-" + Guid.NewGuid().ToString("N");
                var rollback = ProjectStateSnapshot.Capture(project);
                ProjectMaterial material;
                try
                {
                    material = ProjectMaterialCatalog.UpsertCustom(project, id, NameBox.Text, UnitBox.Text, DescriptionBox.Text);
                    AuditTrail.ForProject(project).Record("material.catalog.upsert", string.Empty, material.Id + " • " + material.Name);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Lưu Material Catalog");
                    throw;
                }

                _editingId = material.Id;
                RefreshAfterCommit(
                    () => RefreshAll(material.Id),
                    "Đã lưu custom material: " + material.Name + ".",
                    "Material Catalog save");
            }
            catch (Exception ex) { SetStatus("Lưu material lỗi: " + ex.Message); }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (!(MaterialList.SelectedItem is ProjectMaterial selectedMaterial)) return;
            try
            {
                EnsureActive("xóa material");
                if (!ExistingProjectMutationContext.TryGet(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Material Catalog không tạo project thay thế; hãy nạp project rồi Refresh trước khi xóa.");
                RequireBoundProject(project, "chọn lại material và thử xóa");
                var material = ProjectMaterialCatalog.GetAll(project)
                    .FirstOrDefault(x => string.Equals(x.Id, selectedMaterial.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Material đã thay đổi hoặc bị xóa khỏi project hiện tại. Hãy Refresh và chọn lại material.");
                if (material.IsBuiltIn) throw new InvalidOperationException("Built-in material không thể xóa.");

                var rollback = ProjectStateSnapshot.Capture(project);
                var deleted = false;
                try
                {
                    deleted = ProjectMaterialCatalog.DeleteCustom(project, material.Id);
                    if (deleted)
                        AuditTrail.ForProject(project).Record("material.catalog.delete", string.Empty, material.Id + " • " + material.Name);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Xóa Material Catalog");
                    throw;
                }

                if (!deleted)
                {
                    SetStatus("Material đã không còn tồn tại trong project hiện tại. Danh sách sẽ được làm mới.");
                    RefreshAll();
                    return;
                }

                _editingId = string.Empty;
                RefreshAfterCommit(
                    () => RefreshAll(),
                    "Đã xóa custom material: " + material.Name + ".",
                    "Material Catalog delete");
            }
            catch (Exception ex) { SetStatus("Xóa material lỗi: " + ex.Message); }
        }

        private void OnApplyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("áp dụng material cho selection");
                if (!(MaterialList.SelectedItem is ProjectMaterial selectedMaterial)) throw new InvalidOperationException("Chọn một material trước khi áp dụng.");
                var target = (TargetCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Material";
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var previewProject))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Material Catalog không tạo project thay thế; hãy nạp project rồi Refresh trước khi áp dụng.");
                RequireBoundProject(previewProject, "chọn lại material rồi thử Apply");
                var previewMaterial = ProjectMaterialCatalog.GetAll(previewProject)
                    .FirstOrDefault(x => string.Equals(x.Id, selectedMaterial.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Material đã thay đổi hoặc bị xóa khỏi project hiện tại. Hãy Refresh và chọn lại material.");
                var expectedProjectId = previewProject.ProjectId;
                var previewElements = SemanticSelectionResolver.ResolveImplied(_document, previewProject)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                if (previewElements.Count == 0) throw new InvalidOperationException("Selection hiện tại không resolve được QS3D semantic element.");
                var previewIds = previewElements.Select(x => x.Id)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (string.Equals(target, "CurtainFrameMaterial", StringComparison.OrdinalIgnoreCase))
                {
                    var invalid = previewElements.Where(x => x.Category != ElementCategory.GlassWall).Select(x => x.Id).ToList();
                    if (invalid.Count > 0) throw new InvalidOperationException("CurtainFrameMaterial chỉ áp dụng cho Vách Kính. Selection không hợp lệ: " + string.Join(", ", invalid.Take(5)) + (invalid.Count > 5 ? "…" : string.Empty));
                }

                if (!ExistingProjectMutationContext.TryGet(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Material Catalog không tạo project thay thế; hãy nạp project rồi Refresh trước khi áp dụng.");
                RequireBoundProject(project, "chọn lại material rồi thử Apply");
                if (!ReferenceEquals(project, previewProject))
                    throw new InvalidOperationException("QS3D project đã thay đổi trong lúc chuẩn bị Apply. Không có material assignment nào được áp dụng; hãy Refresh rồi thử lại.");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi sau khi đọc selection. Không có material assignment nào được áp dụng; hãy Refresh và thử lại.");
                var material = ProjectMaterialCatalog.GetAll(project)
                    .FirstOrDefault(x => string.Equals(x.Id, previewMaterial.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Material đã thay đổi hoặc bị xóa khỏi project hiện tại. Hãy Refresh và chọn lại material.");
                var elements = SemanticSelectionResolver.ResolveImplied(_document, project)
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
                var currentIds = elements.Select(x => x.Id)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!previewIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Selection hoặc semantic ownership đã thay đổi trước khi áp dụng material. Không có mutation nào được áp dụng; hãy chọn lại và thử lại.");
                if (string.Equals(target, "CurtainFrameMaterial", StringComparison.OrdinalIgnoreCase))
                {
                    var invalid = elements.Where(x => x.Category != ElementCategory.GlassWall).Select(x => x.Id).ToList();
                    if (invalid.Count > 0) throw new InvalidOperationException("CurtainFrameMaterial chỉ áp dụng cho Vách Kính. Selection không hợp lệ: " + string.Join(", ", invalid.Take(5)) + (invalid.Count > 5 ? "…" : string.Empty));
                }

                var rollback = ProjectStateSnapshot.Capture(project);
                var changed = 0;
                try
                {
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
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Áp dụng Material Catalog");
                    throw;
                }

                RefreshAfterCommit(
                    () => RefreshAll(material.Id),
                    "Đã áp dụng “" + material.Name + "” cho " + changed + "/" + elements.Count + " semantic element • " + target + ".",
                    "Material Catalog apply");
            }
            catch (Exception ex) { SetStatus("Áp dụng material lỗi: " + ex.Message); }
        }

        private void RefreshAll(string selectedId = "")
        {
            try
            {
                EnsureActive("làm mới Material Catalog");
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Material Catalog không tạo replacement project; hãy đóng và chạy lại QS3DMATERIALS.");
                var previous = string.IsNullOrWhiteSpace(selectedId) ? (MaterialList.SelectedItem as ProjectMaterial)?.Id : selectedId;
                var materials = ProjectMaterialCatalog.GetAll(project).ToList();
                var selectedMaterial = materials.FirstOrDefault(x => string.Equals(x.Id, previous, StringComparison.OrdinalIgnoreCase));
                _loading = true;
                try
                {
                    MaterialList.ItemsSource = materials;
                    MaterialList.SelectedItem = selectedMaterial;
                }
                finally { _loading = false; }

                if (selectedMaterial != null) LoadEditor(selectedMaterial, false);
                else ClearEditor();

                var referenced = ProjectMaterialCatalog.ReferencedMaterialNames(project);
                ReferencedText.Text = referenced.Count == 0 ? "—" : string.Join(" • ", referenced);
                Title = "QS3D • Vật liệu • " + DrawingLabel(_document);
                _boundProject = project;
            }
            catch (Exception ex) { SetStatus("Đọc Material Catalog lỗi: " + ex.Message); }
        }

        private void LoadEditor(ProjectMaterial material, bool announce)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            _editingId = material.IsBuiltIn ? string.Empty : material.Id;
            NameBox.Text = material.Name;
            UnitBox.Text = material.Unit;
            DescriptionBox.Text = material.Description;
            if (announce)
                SetStatus(material.IsBuiltIn ? "Built-in material: có thể áp dụng nhưng không sửa/xóa." : "Custom material: " + material.Name);
        }

        private void ClearEditor()
        {
            _editingId = string.Empty;
            NameBox.Text = string.Empty;
            UnitBox.Text = string.Empty;
            DescriptionBox.Text = string.Empty;
        }

        private void RefreshAfterCommit(Action refresh, string successMessage, string context)
        {
            SetStatus(successMessage);
            try
            {
                refresh();
                PaletteCoordinator.RefreshProject();
            }
            catch (Exception refreshError)
            {
                var warning = successMessage + " UI sync warning: " + refreshError.Message;
                try { StatusText.Text = warning; } catch { }
                try { PaletteCoordinator.SetStatus(warning); } catch { }
                try { _document.Editor.WriteMessage("\nQS3D " + context + " đã commit; UI sync warning: " + refreshError.Message); } catch { }
            }
        }

        private static ProjectState RequireExistingProject(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!ExistingProjectMutationContext.TryGet(document, out var project))
                throw new InvalidOperationException("Material Catalog cần QS3D project hiện hữu. Hãy chạy QS3DINIT hoặc mở/nạp project trước.");
            return project;
        }

        private void RequireBoundProject(ProjectState project, string operation)
        {
            if (ReferenceEquals(project, _boundProject)) return;
            throw new InvalidOperationException(
                "QS3D project đã được reload/thay đổi kể từ lần Material Catalog Refresh gần nhất. Hãy Refresh rồi " + operation + ".");
        }

        private static void RestoreOrThrow(ProjectState project, ProjectStateSnapshot rollback, Exception operationError, string operation)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    operation + " thất bại và rollback project cũng không hoàn tất.",
                    new AggregateException(operationError, restoreError));
            }
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Material Catalog trước khi " + operation + ".");
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
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}