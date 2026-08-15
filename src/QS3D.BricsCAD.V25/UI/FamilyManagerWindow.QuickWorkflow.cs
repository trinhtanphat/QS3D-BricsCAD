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
        private const double QuickMillimetersPerMeter = 1000d;
        private bool _quickWorkflowEventsAttached;

        private sealed class QuickFamilyDefaults
        {
            public double? WidthM { get; set; }
            public double? DepthM { get; set; }
            public double? HeightM { get; set; }
            public double? ThicknessM { get; set; }
            public double? BottomOffsetM { get; set; }
        }

        private void OnQuickWorkflowContentRendered(object sender, EventArgs e)
        {
            if (_quickWorkflowEventsAttached) return;
            _quickWorkflowEventsAttached = true;

            // Attach after the XAML handlers so this narrow guard repairs the existing New-mode
            // ordering race: OnNewClick clears FamilyList.SelectedItem, the original selection
            // handler runs first and resets _creatingNew=false, then this handler restores draft
            // mode when the resulting selection is actually empty.
            FamilyList.SelectionChanged += OnQuickFamilySelectionChanged;
            NewCategoryCombo.SelectionChanged += OnQuickCategorySelectionChanged;
            ConfigureQuickWorkflowMillimeterDisplay();
            RefreshQuickWorkflow();
        }

        private void ConfigureQuickWorkflowMillimeterDisplay()
        {
            SetQuickFieldPresentation(QuickWidthBox, "Rộng • WidthM (mm)", "WidthM • nhập mm; QS3D lưu nội bộ theo mét");
            SetQuickFieldPresentation(QuickDepthBox, "Sâu • DepthM (mm)", "DepthM • nhập mm; QS3D lưu nội bộ theo mét");
            SetQuickFieldPresentation(QuickHeightBox, "Cao • HeightM (mm)", "HeightM • nhập mm; QS3D lưu nội bộ theo mét");
            SetQuickFieldPresentation(QuickThicknessBox, "Dày • ThicknessM (mm)", "ThicknessM • nhập mm; QS3D lưu nội bộ theo mét");
            SetQuickFieldPresentation(QuickBottomOffsetBox, "Offset đáy • BottomOffsetM (mm)", "BottomOffsetM • nhập mm; có thể âm; QS3D lưu nội bộ theo mét");
        }

        private static void SetQuickFieldPresentation(TextBox box, string label, string tooltip)
        {
            box.ToolTip = tooltip;
            if (!(box.Parent is StackPanel panel)) return;
            var text = panel.Children.OfType<TextBlock>().FirstOrDefault();
            if (text != null) text.Text = label;
        }

        private void OnQuickFamilySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            _creatingNew = FamilyList.SelectedItem == null;
            RefreshQuickWorkflow();
        }

        private void OnQuickCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || !_creatingNew) return;
            RefreshQuickWorkflow();
        }

        private void OnAutoFamilyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("tạo Auto Family");
                var category = ResolveQuickCategory()
                    ?? throw new InvalidOperationException("Chọn Family hoặc Category trước khi Auto Family.");
                var project = ExistingProjectMutationContext.Require(_document, "Auto Family");
                var family = ResolveQuickFamily(project);

                PopulateQuickFields(category, family, overwriteWithDefaults: true);
                if (_creatingNew && string.IsNullOrWhiteSpace(FamilyNameBox.Text))
                    FamilyNameBox.Text = NextQuickFamilyName(project, category);

                SetStatus(
                    "Auto Family đã điền các tham số QS chuẩn cho " + category +
                    " theo mm. Chưa có dữ liệu nào được commit; bấm Tạo & sử dụng hoặc Lưu & Vẽ để áp dụng.");
            }
            catch (Exception ex)
            {
                SetStatus("Auto Family lỗi: " + ex.Message);
            }
        }

        private void OnCreateAndUseClick(object sender, RoutedEventArgs e)
        {
            SaveQuickFamily(drawAfterSave: false);
        }

        private void OnSaveAndDrawClick(object sender, RoutedEventArgs e)
        {
            SaveQuickFamily(drawAfterSave: true);
        }

        private void SaveQuickFamily(bool drawAfterSave)
        {
            var operation = drawAfterSave ? "Lưu & Vẽ" : "Tạo & sử dụng";
            try
            {
                EnsureActive(operation);
                var project = ExistingProjectMutationContext.Require(_document, operation);
                var category = ResolveQuickCategory()
                    ?? throw new InvalidOperationException("Chọn Family hoặc Category trước khi " + operation + ".");
                var creating = _creatingNew || !(FamilyList.SelectedItem is ProjectFamily);

                if (drawAfterSave)
                {
                    var routeProbe = new ProjectFamily("family-quick-route-probe", "Quick route probe", category);
                    if (!global::QS3D.BricsCAD.V25.ActiveFamilyQuickDrawCommands.SupportsFamily(routeProbe))
                        throw new InvalidOperationException(
                            category + " chưa có QS3DDRAWACTIVE an toàn. Family chưa được thay đổi; dùng workflow chuyên biệt của category này.");
                }

                var quickValues = ReadQuickValues(category);
                var requestedName = (FamilyNameBox.Text ?? string.Empty).Trim();
                if (creating && requestedName.Length == 0)
                    requestedName = NextQuickFamilyName(project, category);

                var previousActive = ProjectFamilyActivationService.GetActive(project);
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

                    foreach (var pair in quickValues)
                    {
                        ProjectFamilyService.SetProperty(project, target.Id, pair.Key, pair.Value);
                        AuditTrail.ForProject(project).Record(
                            "family.property.set",
                            string.Empty,
                            target.Id + " • " + pair.Key + "=" + pair.Value + " • quick-workflow");
                    }

                    ProjectFamilyActivationService.SetActive(project, target.Id);
                    if (previousActive == null || !string.Equals(previousActive.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                        AuditTrail.ForProject(project).Record(
                            "family.activate",
                            string.Empty,
                            (previousActive?.Id ?? string.Empty) + " -> " + target.Id + " • " + target.Name + " • quick-workflow");

                    AuditTrail.ForProject(project).Record(
                        drawAfterSave ? "family.quick.save-and-draw" : "family.quick.create-and-use",
                        string.Empty,
                        target.Id + " • " + target.Category + " • " + target.Name + " • qs=" + quickValues.Count);
                    return target;
                }, operation);

                _creatingNew = false;
                RefreshAfterCommit(
                    () => RefreshAll(family.Id),
                    "Đã lưu và đặt active Family “" + family.Name + "” • " + family.Category +
                    " • " + quickValues.Count + " tham số QS (UI mm → internal m).",
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
                    SetQuickField(QuickWidthBox, false, string.Empty);
                    SetQuickField(QuickDepthBox, false, string.Empty);
                    SetQuickField(QuickHeightBox, false, string.Empty);
                    SetQuickField(QuickThicknessBox, false, string.Empty);
                    SetQuickField(QuickBottomOffsetBox, false, string.Empty);
                    QuickCategoryHintText.Text = "Chọn Family hoặc Category để mở form QS phù hợp. Nhập: mm • lưu nội bộ: m.";
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
            var keys = QuickKeys(category);
            var defaults = DefaultsFor(category);

            PopulateQuickField(QuickWidthBox, "WidthM", keys, family, defaults.WidthM, overwriteWithDefaults);
            PopulateQuickField(QuickDepthBox, "DepthM", keys, family, defaults.DepthM, overwriteWithDefaults);
            PopulateQuickField(QuickHeightBox, "HeightM", keys, family, defaults.HeightM, overwriteWithDefaults);
            PopulateQuickField(QuickThicknessBox, "ThicknessM", keys, family, defaults.ThicknessM, overwriteWithDefaults);
            PopulateQuickField(QuickBottomOffsetBox, "BottomOffsetM", keys, family, defaults.BottomOffsetM, overwriteWithDefaults);

            QuickCategoryHintText.Text = keys.Count == 0
                ? category + ": Direct Draw giữ nguyên raw Family properties; category này chưa có structural QS quick-template riêng."
                : category + " • QS keys: " + string.Join(" • ", keys) + " • nhập mm → lưu nội bộ m.";
        }

        private static void PopulateQuickField(
            TextBox box,
            string key,
            ISet<string> keys,
            ProjectFamily? family,
            double? fallback,
            bool overwriteWithDefaults)
        {
            if (!keys.Contains(key))
            {
                SetQuickField(box, false, string.Empty);
                return;
            }

            string value;
            if (!overwriteWithDefaults && family != null &&
                family.Properties.TryGetValue(key, out var existing) &&
                !string.IsNullOrWhiteSpace(existing))
            {
                value = FormatQuickMillimeters(key, existing);
            }
            else
            {
                value = fallback.HasValue
                    ? (fallback.Value * QuickMillimetersPerMeter).ToString("0.###", CultureInfo.CurrentCulture)
                    : string.Empty;
            }
            SetQuickField(box, true, value);
        }

        private static string FormatQuickMillimeters(string key, string internalMeters)
        {
            var raw = (internalMeters ?? string.Empty).Trim();
            double meters;
            var parsed = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out meters) ||
                         double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out meters);
            if (!parsed || double.IsNaN(meters) || double.IsInfinity(meters))
                throw new InvalidOperationException(key + " đang có giá trị nội bộ không hợp lệ: “" + raw + "”.");
            return (meters * QuickMillimetersPerMeter).ToString("0.###", CultureInfo.CurrentCulture);
        }

        private static void SetQuickField(TextBox box, bool enabled, string value)
        {
            box.IsEnabled = enabled;
            box.Text = enabled ? value : string.Empty;
        }

        private Dictionary<string, string> ReadQuickValues(ElementCategory category)
        {
            var keys = QuickKeys(category);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ReadQuickValue(values, keys, "WidthM", QuickWidthBox.Text, positive: true);
            ReadQuickValue(values, keys, "DepthM", QuickDepthBox.Text, positive: true);
            ReadQuickValue(values, keys, "HeightM", QuickHeightBox.Text, positive: true);
            ReadQuickValue(values, keys, "ThicknessM", QuickThicknessBox.Text, positive: true);
            ReadQuickValue(values, keys, "BottomOffsetM", QuickBottomOffsetBox.Text, positive: false);
            return values;
        }

        private static void ReadQuickValue(
            IDictionary<string, string> values,
            ISet<string> keys,
            string key,
            string text,
            bool positive)
        {
            if (!keys.Contains(key) || string.IsNullOrWhiteSpace(text)) return;
            var valueMeters = ParseQuickMillimeterNumber(key, text, positive);
            values[key] = valueMeters.ToString("R", CultureInfo.InvariantCulture);
        }

        private static double ParseQuickMillimeterNumber(string key, string text, bool positive)
        {
            var raw = (text ?? string.Empty).Trim();
            double valueMm;
            var parsed = double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out valueMm) ||
                         double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out valueMm);
            if (!parsed || double.IsNaN(valueMm) || double.IsInfinity(valueMm))
                throw new InvalidOperationException(key + " phải là số hữu hạn hợp lệ (mm). Giá trị hiện tại: “" + raw + "”.");
            if (positive && valueMm <= 0d)
                throw new InvalidOperationException(key + " phải lớn hơn 0 mm.");
            return valueMm / QuickMillimetersPerMeter;
        }

        private ElementCategory? ResolveQuickCategory()
        {
            if (!_creatingNew && FamilyList.SelectedItem is ProjectFamily selected)
                return selected.Category;
            return (NewCategoryCombo.SelectedItem as CategoryChoice)?.Category;
        }

        private ProjectFamily? ResolveQuickFamily(ProjectState project)
        {
            if (_creatingNew || !(FamilyList.SelectedItem is ProjectFamily selected)) return null;
            return project.FindFamily(selected.Id);
        }

        private static HashSet<string> QuickKeys(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                    return new HashSet<string>(new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, StringComparer.OrdinalIgnoreCase);
                case ElementCategory.Beam:
                    return new HashSet<string>(new[] { "WidthM", "HeightM", "BottomOffsetM" }, StringComparer.OrdinalIgnoreCase);
                case ElementCategory.Column:
                    return new HashSet<string>(new[] { "WidthM", "DepthM", "HeightM", "BottomOffsetM" }, StringComparer.OrdinalIgnoreCase);
                case ElementCategory.Slab:
                case ElementCategory.Foundation:
                    return new HashSet<string>(new[] { "ThicknessM", "BottomOffsetM" }, StringComparer.OrdinalIgnoreCase);
                default:
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static QuickFamilyDefaults DefaultsFor(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                    return new QuickFamilyDefaults { ThicknessM = 0.2d, HeightM = 3.6d, BottomOffsetM = 0d };
                case ElementCategory.Beam:
                    return new QuickFamilyDefaults { WidthM = 0.3d, HeightM = 0.5d, BottomOffsetM = 0d };
                case ElementCategory.Column:
                    return new QuickFamilyDefaults { WidthM = 0.4d, DepthM = 0.4d, HeightM = 3.6d, BottomOffsetM = 0d };
                case ElementCategory.Slab:
                    return new QuickFamilyDefaults { ThicknessM = 0.12d, BottomOffsetM = 0d };
                case ElementCategory.GlassWall:
                    return new QuickFamilyDefaults { ThicknessM = 0.012d, HeightM = 3.6d, BottomOffsetM = 0d };
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                    return new QuickFamilyDefaults { ThicknessM = 0.2d, HeightM = 3.6d, BottomOffsetM = 0d };
                case ElementCategory.Foundation:
                    return new QuickFamilyDefaults { ThicknessM = 0.5d, BottomOffsetM = 0d };
                default:
                    return new QuickFamilyDefaults();
            }
        }

        private static string NextQuickFamilyName(ProjectState project, ElementCategory category)
        {
            var prefix = category + " Auto";
            if (!project.Families.Any(x => x.Category == category && string.Equals(x.Name, prefix, StringComparison.OrdinalIgnoreCase)))
                return prefix;

            for (var index = 2; index <= 10000; index++)
            {
                var candidate = prefix + " " + index.ToString(CultureInfo.InvariantCulture);
                if (!project.Families.Any(x => x.Category == category && string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                    return candidate;
            }
            throw new InvalidOperationException("Không tìm được tên Auto Family duy nhất cho " + category + ".");
        }
    }
}
