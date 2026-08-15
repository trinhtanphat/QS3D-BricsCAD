using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FamilyManagerWindow
    {
        private bool _quickWorkflowEventsAttached;

        private void OnQuickWorkflowContentRendered(object sender, EventArgs e)
        {
            if (_quickWorkflowEventsAttached) return;
            _quickWorkflowEventsAttached = true;

            // The XAML selection handler runs before this attached handler. It preserves
            // _creatingNew only when OnNewClick intentionally clears selection; this handler
            // exits draft mode only when a real Family is selected, then refreshes the QS form.
            FamilyList.SelectionChanged += OnQuickFamilySelectionChanged;
            NewCategoryCombo.SelectionChanged += OnQuickCategorySelectionChanged;
            ConfigureQuickMaterialChoices();
            RefreshQuickWorkflow();
        }

        private void ConfigureQuickMaterialChoices()
        {
            QuickMaterialCombo.ItemsSource = new[]
            {
                "Bê tông",
                "Gạch",
                "Thép",
                "Gỗ",
                "Kính",
                "Hoàn thiện",
                "Chống thấm"
            };
        }

        private void OnQuickFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (FamilyList.SelectedItem != null) _creatingNew = false;
            RefreshQuickWorkflow();
        }

        private void OnQuickCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || !_creatingNew) return;
            RefreshQuickWorkflow();
        }

        private void OnQuickCreateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("Tạo nhanh Family");
                ElementCategory? preferredCategory = null;
                if (FamilyList.SelectedItem is ProjectFamily selected) preferredCategory = selected.Category;
                if (!preferredCategory.HasValue) preferredCategory = (NewCategoryCombo.SelectedItem as CategoryChoice)?.Category;

                OnNewClick(sender, e);
                if (preferredCategory.HasValue)
                {
                    var choice = (NewCategoryCombo.ItemsSource as IEnumerable<CategoryChoice>)?
                        .FirstOrDefault(x => x.Category == preferredCategory.Value);
                    if (choice != null) NewCategoryCombo.SelectedItem = choice;
                }

                var category = ResolveQuickCategory()
                    ?? throw new InvalidOperationException("Chọn Category trước khi Tạo nhanh Family.");
                var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
                if (!schema.SupportsQuickForm)
                    throw new InvalidOperationException(category + " chưa có QS quick form; dùng Thuộc tính nâng cao cho category này.");

                PopulateQuickFields(category, null, overwriteWithDefaults: true);
                var quickValues = ReadQuickValues(category);
                FamilyNameBox.Text = ProjectFamilyQuickSchemaService.SuggestName(category, quickValues);
                QuickMaterialCombo.Text = schema.DefaultMaterial;
                SetStatus("Tạo nhanh " + category + ": nhập thông số quen thuộc theo mm rồi chọn Lưu, Tạo & sử dụng, Auto Family hoặc Lưu & Vẽ.");
            }
            catch (Exception ex)
            {
                SetStatus("Tạo nhanh Family lỗi: " + ex.Message);
            }
        }

        private void OnAutoFamilyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("Auto Family");
                var project = ExistingProjectMutationContext.Require(_document, "Auto Family");
                var category = ResolveQuickCategory()
                    ?? throw new InvalidOperationException("Chọn Family hoặc Category trước khi Auto Family.");
                var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
                if (!schema.SupportsQuickForm)
                    throw new InvalidOperationException(category + " chưa có QS Auto Family schema.");

                var quickValues = ReadQuickValues(category);
                var material = ReadQuickMaterial(schema);
                var matches = ProjectFamilyQuickSchemaService.FindIdentityMatches(project, category, quickValues, material);
                if (matches.Count > 1)
                    throw new InvalidOperationException(
                        "Có nhiều Family " + category + " trùng kích thước và vật liệu. Hãy chọn Family cụ thể hoặc đổi tên/thông số trước khi Auto Family.");

                var created = false;
                var previousActive = ProjectFamilyActivationService.GetActive(project);
                var family = ExecuteAtomic(project, () =>
                {
                    ProjectFamily target;
                    if (matches.Count == 1)
                    {
                        target = project.FindFamily(matches[0].Id)
                            ?? throw new InvalidOperationException("Family phù hợp đã thay đổi trước khi Auto Family commit.");
                    }
                    else
                    {
                        var suggested = ProjectFamilyQuickSchemaService.SuggestName(category, quickValues);
                        var requested = _creatingNew ? (FamilyNameBox.Text ?? string.Empty).Trim() : string.Empty;
                        var baseName = requested.Length > 0 ? requested : suggested;
                        var uniqueName = ProjectFamilyQuickSchemaService.MakeUniqueName(project, category, baseName);
                        target = ProjectFamilyService.Create(
                            project,
                            "family-" + Guid.NewGuid().ToString("N"),
                            uniqueName,
                            category);
                        created = true;
                        AuditTrail.ForProject(project).Record(
                            "family.create",
                            string.Empty,
                            target.Id + " • " + target.Category + " • " + target.Name + " • auto-family");
                    }

                    ApplyQuickFamilyValues(project, target, quickValues, material, "auto-family");
                    ActivateQuickFamily(project, target, previousActive, "auto-family");
                    AuditTrail.ForProject(project).Record(
                        "family.quick.auto",
                        string.Empty,
                        target.Id + " • " + target.Category + " • " + target.Name + " • " + (created ? "created" : "reused"));
                    return target;
                }, "Auto Family");

                _creatingNew = false;
                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    "Auto Family " + (created ? "đã tạo" : "đã dùng lại") + " “" + family.Name + "” • đã đặt Active.",
                    "Auto Family");
            }
            catch (Exception ex)
            {
                SetStatus("Auto Family lỗi: " + ex.Message);
            }
        }

        private void OnQuickSaveClick(object sender, RoutedEventArgs e)
        {
            SaveQuickFamily(activateAfterSave: false, drawAfterSave: false, operation: "Lưu");
        }

        private void OnCreateAndUseClick(object sender, RoutedEventArgs e)
        {
            SaveQuickFamily(activateAfterSave: true, drawAfterSave: false, operation: "Tạo & sử dụng");
        }

        private void OnSaveAndDrawClick(object sender, RoutedEventArgs e)
        {
            SaveQuickFamily(activateAfterSave: true, drawAfterSave: true, operation: "Lưu & Vẽ");
        }

        private void SaveQuickFamily(bool activateAfterSave, bool drawAfterSave, string operation)
        {
            try
            {
                EnsureActive(operation);
                var project = ExistingProjectMutationContext.Require(_document, operation);
                var category = ResolveQuickCategory()
                    ?? throw new InvalidOperationException("Chọn Family hoặc Category trước khi " + operation + ".");
                var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
                if (!schema.SupportsQuickForm)
                    throw new InvalidOperationException(category + " chưa có QS quick form; dùng workflow chuyên biệt hoặc Thuộc tính nâng cao.");
                var creating = _creatingNew || !(FamilyList.SelectedItem is ProjectFamily);

                if (drawAfterSave)
                {
                    var routeProbe = new ProjectFamily("family-quick-route-probe", "Quick route probe", category);
                    if (!global::QS3D.BricsCAD.V25.ActiveFamilyQuickDrawCommands.SupportsFamily(routeProbe))
                        throw new InvalidOperationException(
                            category + " chưa có QS3DDRAWACTIVE an toàn. Family chưa được thay đổi; dùng workflow chuyên biệt của category này.");
                }

                var quickValues = ReadQuickValues(category);
                var material = ReadQuickMaterial(schema);
                var requestedName = (FamilyNameBox.Text ?? string.Empty).Trim();
                if (creating && requestedName.Length == 0)
                {
                    requestedName = ProjectFamilyQuickSchemaService.MakeUniqueName(
                        project,
                        category,
                        ProjectFamilyQuickSchemaService.SuggestName(category, quickValues));
                }

                var previousActive = activateAfterSave ? ProjectFamilyActivationService.GetActive(project) : null;
                var family = ExecuteAtomic(project, () =>
                {
                    ProjectFamily target;
                    if (creating)
                    {
                        target = ProjectFamilyService.Create(
                            project,
                            "family-" + Guid.NewGuid().ToString("N"),
                            requestedName,
                            category);
                        AuditTrail.ForProject(project).Record(
                            "family.create",
                            string.Empty,
                            target.Id + " • " + target.Category + " • " + target.Name + " • quick-workflow");
                    }
                    else
                    {
                        target = RequireSelectedFamily(project);
                        if (target.Category != category)
                            throw new InvalidOperationException("Category của Family đã thay đổi. Hãy Refresh Family Manager và thử lại.");

                        var beforeName = target.Name;
                        if (requestedName.Length > 0)
                            ProjectFamilyService.Rename(project, target.Id, requestedName);
                        if (!string.Equals(beforeName, target.Name, StringComparison.Ordinal))
                            AuditTrail.ForProject(project).Record(
                                "family.rename",
                                string.Empty,
                                target.Id + " • " + beforeName + " -> " + target.Name + " • quick-workflow");
                    }

                    ApplyQuickFamilyValues(project, target, quickValues, material, "quick-workflow");
                    if (activateAfterSave)
                        ActivateQuickFamily(project, target, previousActive, "quick-workflow");
                    AuditTrail.ForProject(project).Record(
                        drawAfterSave ? "family.quick.save-and-draw" :
                        activateAfterSave ? "family.quick.create-and-use" : "family.quick.save",
                        string.Empty,
                        target.Id + " • " + target.Category + " • " + target.Name + " • qs=" + quickValues.Count + " • material=" + material);
                    return target;
                }, operation);

                _creatingNew = false;
                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    activateAfterSave
                        ? "Đã lưu và đặt active Family “" + family.Name + "” • " + family.Category + " • UI mm → internal m • vật liệu “" + material + "”."
                        : "Đã lưu Family “" + family.Name + "” • " + family.Category + " • không đổi Family Active • UI mm → internal m.",
                    operation);

                if (!drawAfterSave) return;

                if (!ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document))
                    throw new InvalidOperationException(
                        "Family đã lưu/active nhưng DWG active đã đổi trước khi vẽ. Kích hoạt lại đúng bản vẽ rồi chạy QS3DDRAWACTIVE.");

                SetStatus("Đã lưu Family “" + family.Name + "”. Đang chuyển sang QS3DDRAWACTIVE…");
                Close();
                _document.SendStringToExecute("QS3DDRAWACTIVE ", true, false, false);
            }
            catch (Exception ex)
            {
                SetStatus(operation + " lỗi: " + ex.Message);
            }
        }

        private static void ApplyQuickFamilyValues(
            ProjectState project,
            ProjectFamily target,
            IReadOnlyDictionary<string, string> quickValues,
            string material,
            string auditSource)
        {
            foreach (var pair in quickValues)
                SetQuickPropertyWithAudit(project, target, pair.Key, pair.Value, auditSource);
            SetQuickPropertyWithAudit(project, target, "Material", material, auditSource);
        }

        private static void SetQuickPropertyWithAudit(
            ProjectState project,
            ProjectFamily target,
            string key,
            string value,
            string auditSource)
        {
            var beforeVersion = project.ChangeVersion;
            var update = ProjectFamilyService.SetProperty(project, target.Id, key, value);
            if (project.ChangeVersion == beforeVersion) return;
            AuditTrail.ForProject(project).Record(
                "family.property.set",
                string.Empty,
                target.Id + " • " + key + "=" + value + " • inherited=" + update.InheritedInstancesUpdated +
                " • overrides=" + update.OverridesPreserved + " • " + auditSource);
        }

        private static void ActivateQuickFamily(
            ProjectState project,
            ProjectFamily target,
            ProjectFamily? previousActive,
            string auditSource)
        {
            ProjectFamilyActivationService.SetActive(project, target.Id);
            if (previousActive != null && string.Equals(previousActive.Id, target.Id, StringComparison.OrdinalIgnoreCase)) return;
            AuditTrail.ForProject(project).Record(
                "family.activate",
                string.Empty,
                (previousActive?.Id ?? string.Empty) + " -> " + target.Id + " • " + target.Name + " • " + auditSource);
        }

        private void RefreshQuickWorkflow()
        {
            try
            {
                var category = ResolveQuickCategory();
                ProjectFamily? family = null;
                if (category.HasValue && ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    family = ResolveQuickFamily(project);

                if (!category.HasValue)
                {
                    CollapseQuickFields();
                    SetQuickField(QuickWidthBox, false, string.Empty);
                    SetQuickField(QuickDepthBox, false, string.Empty);
                    SetQuickField(QuickHeightBox, false, string.Empty);
                    SetQuickField(QuickThicknessBox, false, string.Empty);
                    SetQuickField(QuickBottomOffsetBox, false, string.Empty);
                    QuickMaterialCombo.IsEnabled = false;
                    QuickMaterialCombo.Text = string.Empty;
                    QuickCategoryHintText.Text = "Chọn cấu kiện hoặc Family để mở form QS phù hợp. Nhập theo mm; QS3D tự lưu schema nội bộ.";
                    return;
                }

                PopulateQuickFields(category.Value, family, overwriteWithDefaults: false);
            }
            catch (Exception ex)
            {
                QuickCategoryHintText.Text = "Không đọc được QS form: " + ex.Message;
            }
        }

        private void PopulateQuickFields(ElementCategory category, ProjectFamily? family, bool overwriteWithDefaults)
        {
            var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
            ApplyQuickFieldVisibility(schema);
            PopulateQuickField(QuickWidthBox, "WidthM", schema, family, overwriteWithDefaults);
            PopulateQuickField(QuickDepthBox, "DepthM", schema, family, overwriteWithDefaults);
            PopulateQuickField(QuickHeightBox, "HeightM", schema, family, overwriteWithDefaults);
            PopulateQuickField(QuickThicknessBox, "ThicknessM", schema, family, overwriteWithDefaults);
            PopulateQuickField(QuickBottomOffsetBox, "BottomOffsetM", schema, family, overwriteWithDefaults);

            QuickMaterialCombo.IsEnabled = schema.SupportsQuickForm;
            if (!schema.SupportsQuickForm)
            {
                QuickMaterialCombo.Text = string.Empty;
                QuickCategoryHintText.Text = category + ": chưa có QS quick schema; mở Thuộc tính nâng cao nếu cần chỉnh Key/Value kỹ thuật.";
                return;
            }

            if (!overwriteWithDefaults && family != null &&
                family.Properties.TryGetValue("Material", out var existingMaterial) &&
                !string.IsNullOrWhiteSpace(existingMaterial))
                QuickMaterialCombo.Text = existingMaterial;
            else
                QuickMaterialCombo.Text = schema.DefaultMaterial;

            QuickCategoryHintText.Text = FriendlyCategory(category) + " • chỉ hiện field phù hợp • nhập theo mm • QS3D tự đổi sang m và quản lý schema phía sau.";
        }

        private void ApplyQuickFieldVisibility(ProjectFamilyQuickSchema schema)
        {
            QuickWidthField.Visibility = schema.Contains("WidthM") ? Visibility.Visible : Visibility.Collapsed;
            QuickDepthField.Visibility = schema.Contains("DepthM") ? Visibility.Visible : Visibility.Collapsed;
            QuickHeightField.Visibility = schema.Contains("HeightM") ? Visibility.Visible : Visibility.Collapsed;
            QuickThicknessField.Visibility = schema.Contains("ThicknessM") ? Visibility.Visible : Visibility.Collapsed;
            QuickBottomOffsetField.Visibility = schema.Contains("BottomOffsetM") ? Visibility.Visible : Visibility.Collapsed;
            QuickMaterialField.Visibility = schema.SupportsQuickForm ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CollapseQuickFields()
        {
            QuickWidthField.Visibility = Visibility.Collapsed;
            QuickDepthField.Visibility = Visibility.Collapsed;
            QuickHeightField.Visibility = Visibility.Collapsed;
            QuickThicknessField.Visibility = Visibility.Collapsed;
            QuickBottomOffsetField.Visibility = Visibility.Collapsed;
            QuickMaterialField.Visibility = Visibility.Collapsed;
        }

        private static void PopulateQuickField(
            TextBox box,
            string key,
            ProjectFamilyQuickSchema schema,
            ProjectFamily? family,
            bool overwriteWithDefaults)
        {
            if (!schema.Contains(key))
            {
                SetQuickField(box, false, string.Empty);
                return;
            }

            string value;
            if (!overwriteWithDefaults && family != null &&
                family.Properties.TryGetValue(key, out var existing) &&
                !string.IsNullOrWhiteSpace(existing))
            {
                value = ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters(key, existing, CultureInfo.CurrentCulture);
            }
            else if (schema.DefaultsM.TryGetValue(key, out var fallback))
            {
                value = (fallback * ProjectFamilyQuickSchemaService.MillimetersPerMeter)
                    .ToString("0.###", CultureInfo.CurrentCulture);
            }
            else value = string.Empty;
            SetQuickField(box, true, value);
        }

        private static void SetQuickField(TextBox box, bool enabled, string value)
        {
            box.IsEnabled = enabled;
            box.Text = enabled ? value : string.Empty;
        }

        private Dictionary<string, string> ReadQuickValues(ElementCategory category)
        {
            var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
            if (!schema.SupportsQuickForm)
                throw new InvalidOperationException(category + " chưa có QS quick schema.");

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in schema.FormKeys)
            {
                var box = QuickBox(key);
                var raw = box.Text;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException(FriendlyField(key) + " là bắt buộc cho " + FriendlyCategory(category) + ".");
                var positive = !string.Equals(key, "BottomOffsetM", StringComparison.OrdinalIgnoreCase);
                var meters = ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters(
                    FriendlyField(key),
                    raw,
                    CultureInfo.CurrentCulture,
                    positive);
                values[key] = meters.ToString("R", CultureInfo.InvariantCulture);
            }
            return values;
        }

        private string ReadQuickMaterial(ProjectFamilyQuickSchema schema)
        {
            var material = (QuickMaterialCombo.Text ?? string.Empty).Trim();
            if (material.Length == 0) material = schema.DefaultMaterial;
            if (material.Length == 0) throw new InvalidOperationException("Chọn hoặc nhập Vật liệu trước khi lưu Family.");
            return material;
        }

        private TextBox QuickBox(string key)
        {
            switch (key)
            {
                case "WidthM": return QuickWidthBox;
                case "DepthM": return QuickDepthBox;
                case "HeightM": return QuickHeightBox;
                case "ThicknessM": return QuickThicknessBox;
                case "BottomOffsetM": return QuickBottomOffsetBox;
                default: throw new InvalidOperationException("QS quick schema chứa field không được UI hỗ trợ: " + key + ".");
            }
        }

        private ElementCategory? ResolveQuickCategory()
        {
            if (_creatingNew)
                return (NewCategoryCombo.SelectedItem as CategoryChoice)?.Category;
            return (FamilyList.SelectedItem as ProjectFamily)?.Category;
        }

        private ProjectFamily? ResolveQuickFamily(ProjectState project)
        {
            if (_creatingNew || !(FamilyList.SelectedItem is ProjectFamily selected)) return null;
            return project.FindFamily(selected.Id);
        }

        private static string FriendlyField(string key)
        {
            switch (key)
            {
                case "WidthM": return "Bề rộng";
                case "DepthM": return "Bề sâu";
                case "HeightM": return "Chiều cao";
                case "ThicknessM": return "Bề dày";
                case "BottomOffsetM": return "Offset đáy";
                default: return key;
            }
        }

        private static string FriendlyCategory(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam: return "Dầm";
                case ElementCategory.Column: return "Cột";
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.StructuralWall:
                case ElementCategory.WallPier:
                case ElementCategory.GlassWall: return "Tường";
                case ElementCategory.Slab: return "Sàn";
                case ElementCategory.Foundation: return "Móng";
                default: return category.ToString();
            }
        }
    }
}
