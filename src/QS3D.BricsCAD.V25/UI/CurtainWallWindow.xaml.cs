using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class CurtainWallWindow : Window
    {
        private readonly Document _document;
        private ProjectState? _boundProject;
        private bool _loading;

        public CurtainWallWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshAll();
        }

        private void InitializeAndRefresh() => RefreshAll();
        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnFamilyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            try
            {
                EnsureActive("đổi Family đang xem trong Vách Kính Hub");
                LoadSelectedFamily();
                RefreshSummary();
            }
            catch (Exception ex) { SetStatus("Đọc Family Vách Kính lỗi: " + ex.Message); }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("lưu Family Vách Kính");
                if (!ExistingProjectMutationContext.TryGet(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Vách Kính Hub không tạo project thay thế; hãy nạp project rồi Refresh.");
                EnsureBoundProject(project, "lưu Family Vách Kính");
                if (!(FamilyCombo.SelectedItem is ProjectFamily selectedFamily)) return;
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ThicknessM"] = Positive(ThicknessBox.Text, "Bề dày kính").ToString("R", CultureInfo.InvariantCulture),
                    ["HeightM"] = Positive(HeightBox.Text, "Chiều cao vách").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainMaxPanelWidthM"] = Positive(MaxPanelWidthBox.Text, "Panel rộng tối đa").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainMaxPanelHeightM"] = Positive(MaxPanelHeightBox.Text, "Panel cao tối đa").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainPerimeterFrameWidthM"] = NonNegative(PerimeterFrameBox.Text, "Khung biên").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainMullionWidthM"] = NonNegative(MullionBox.Text, "Mullion đứng").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainTransomWidthM"] = NonNegative(TransomBox.Text, "Transom ngang").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainFrameDepthM"] = Positive(FrameDepthBox.Text, "Độ sâu khung 3D").ToString("R", CultureInfo.InvariantCulture),
                    ["Material"] = Required(GlassMaterialBox.Text, "Vật liệu kính"),
                    ["CurtainFrameMaterial"] = Required(FrameMaterialBox.Text, "Vật liệu khung")
                };

                var family = project.FindFamily(selectedFamily.Id)
                    ?? throw new InvalidOperationException("Family Vách Kính đã chọn không còn tồn tại trong project hiện tại. Hãy Refresh và chọn lại Family.");
                if (family.Category != ElementCategory.GlassWall)
                    throw new InvalidOperationException("Family đã chọn không còn là Family Vách Kính trong project hiện tại. Hãy Refresh và chọn lại Family.");
                var rollback = ProjectStateSnapshot.Capture(project);
                var inherited = 0;
                var overrides = 0;
                var regenerated = 0;
                try
                {
                    foreach (var pair in values)
                        ApplyFamilyValue(project, family, pair.Key, pair.Value, ref inherited, ref overrides);

                    regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Lưu Family Vách Kính");
                    throw;
                }

                TrySyncCommittedUi("Family Vách Kính", () =>
                {
                    PaletteCoordinator.RefreshProject();
                    RefreshSummary();
                    SetStatus("Đã lưu Family • kế thừa " + inherited + " giá trị instance • giữ " + overrides + " override • regen " + regenerated + " cấu kiện.");
                });
            }
            catch (Exception ex) { SetStatus("Lưu Vách Kính lỗi: " + ex.Message); }
        }

        private void OnRecalculateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("tính lại Vách Kính");
                if (!ExistingProjectMutationContext.TryGet(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Vách Kính Hub không tạo project thay thế; hãy nạp project rồi Refresh.");
                EnsureBoundProject(project, "tính lại Vách Kính");
                var rollback = ProjectStateSnapshot.Capture(project);
                var count = 0;
                try
                {
                    var dirtyStateChanged = false;
                    foreach (var element in project.Elements.Where(x => x.Category == ElementCategory.GlassWall))
                    {
                        var beforeDirty = element.Dirty;
                        element.MarkDirty(ElementDirtyFlags.Quantity);
                        if (element.Dirty != beforeDirty) dirtyStateChanged = true;
                    }
                    if (dirtyStateChanged) project.Touch();
                    count = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Tính lại Vách Kính");
                    throw;
                }

                TrySyncCommittedUi("Tính lại Vách Kính", () =>
                {
                    PaletteCoordinator.RefreshProject();
                    RefreshSummary();
                    SetStatus("Đã tính lại " + count + " cấu kiện dirty.");
                });
            }
            catch (Exception ex) { SetStatus("Tính lại Vách Kính lỗi: " + ex.Message); }
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            var normalizedCommand = command.Trim();
            try
            {
                EnsureActive("chạy " + normalizedCommand);
                _document.SendStringToExecute(normalizedCommand + " ", true, false, false);
                SetStatus("Đã gửi lệnh " + normalizedCommand + " sang “" + DrawingLabel(_document) + "”.");
            }
            catch (Exception ex) { SetStatus("Chạy " + normalizedCommand + " lỗi: " + ex.Message); }
        }

        private void RefreshAll()
        {
            _boundProject = null;
            try
            {
                EnsureActive("làm mới Vách Kính Hub");
                Title = "QS3D • Vách Kính • " + DrawingLabel(_document);
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    ClearProjectView();
                    SetStatus("QS3D project hiện hành không còn khả dụng. Vách Kính Hub không tạo project mới; hãy bóc/nạp project rồi Refresh.");
                    return;
                }

                var selectedId = (FamilyCombo.SelectedItem as ProjectFamily)?.Id;
                var families = project.Families.Where(x => x.Category == ElementCategory.GlassWall).OrderBy(x => x.Name).ToList();
                _loading = true;
                try
                {
                    FamilyCombo.ItemsSource = families;
                    FamilyCombo.SelectedItem = families.FirstOrDefault(x => string.Equals(x.Id, selectedId, StringComparison.OrdinalIgnoreCase)) ?? families.FirstOrDefault();
                    LoadSelectedFamily();
                }
                finally { _loading = false; }
                RefreshSummary(project);
                _boundProject = project;
                SetStatus(families.Count == 0 ? "Chưa có Family Vách Kính. Chọn đối tượng CAD rồi bấm “Bóc Vách Kính”." : "Đã nạp " + families.Count + " Family Vách Kính.");
            }
            catch (Exception ex)
            {
                _boundProject = null;
                SetStatus("Đọc Vách Kính lỗi: " + ex.Message);
            }
        }

        private void LoadSelectedFamily()
        {
            if (!(FamilyCombo.SelectedItem is ProjectFamily family))
            {
                foreach (var box in Boxes()) box.Text = string.Empty;
                return;
            }
            ThicknessBox.Text = Value(family, "ThicknessM", "0.012");
            HeightBox.Text = Value(family, "HeightM", "3.6");
            MaxPanelWidthBox.Text = Value(family, "CurtainMaxPanelWidthM", "1.2");
            MaxPanelHeightBox.Text = Value(family, "CurtainMaxPanelHeightM", "1.5");
            PerimeterFrameBox.Text = Value(family, "CurtainPerimeterFrameWidthM", "0.05");
            MullionBox.Text = Value(family, "CurtainMullionWidthM", "0.05");
            TransomBox.Text = Value(family, "CurtainTransomWidthM", "0.05");
            FrameDepthBox.Text = Value(family, "CurtainFrameDepthM", "0.05");
            GlassMaterialBox.Text = Value(family, "Material", "Kính");
            FrameMaterialBox.Text = Value(family, "CurtainFrameMaterial", "Nhôm");
        }

        private void RefreshSummary()
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project) ||
                _boundProject == null ||
                !ReferenceEquals(_boundProject, project))
            {
                ClearSummary();
                return;
            }
            RefreshSummary(project);
        }

        private void RefreshSummary(ProjectState project)
        {
            var familyId = (FamilyCombo.SelectedItem as ProjectFamily)?.Id;
            var elements = project.Elements.Where(x => x.Category == ElementCategory.GlassWall && (familyId == null || string.Equals(x.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))).ToList();
            var panelCount = 0;
            var glassAreaM2 = 0d;
            var frameLengthM = 0d;
            foreach (var element in elements)
            {
                panelCount = QuantityReportMath.AddCount(panelCount, QInt(element, "CurtainPanelCount"));
                glassAreaM2 = QuantityReportMath.Add(glassAreaM2, Q(element, "CurtainNetGlassAreaM2"), element.Id + "/CurtainNetGlassAreaM2");
                frameLengthM = QuantityReportMath.Add(frameLengthM, Q(element, "CurtainFrameLengthM"), element.Id + "/CurtainFrameLengthM");
            }
            WallCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
            PanelCountText.Text = panelCount.ToString(CultureInfo.InvariantCulture);
            GlassAreaText.Text = glassAreaM2.ToString("0.###", CultureInfo.InvariantCulture) + " m²";
            FrameLengthText.Text = frameLengthM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
        }

        private void ClearProjectView()
        {
            _boundProject = null;
            _loading = true;
            try
            {
                FamilyCombo.ItemsSource = Array.Empty<ProjectFamily>();
                FamilyCombo.SelectedItem = null;
                LoadSelectedFamily();
            }
            finally { _loading = false; }
            ClearSummary();
        }

        private void ClearSummary()
        {
            WallCountText.Text = "0";
            PanelCountText.Text = "0";
            GlassAreaText.Text = "0 m²";
            FrameLengthText.Text = "0 m";
        }

        private void EnsureBoundProject(ProjectState project, string operation)
        {
            if (_boundProject == null)
                throw new InvalidOperationException("Vách Kính Hub chưa được bind vào QS3D project hiện hành. Hãy Refresh trước khi " + operation + ".");
            if (!ReferenceEquals(_boundProject, project))
                throw new InvalidOperationException("QS3D project của bản vẽ đã được nạp lại hoặc thay thế. Hãy Refresh Vách Kính Hub trước khi " + operation + ".");
        }

        private static void ApplyFamilyValue(ProjectState project, ProjectFamily family, string key, string next, ref int inherited, ref int overrides)
        {
            var update = ProjectFamilyService.SetProperty(project, family.Id, key, next);
            inherited += update.InheritedInstancesUpdated;
            overrides += update.OverridesPreserved;
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

        private void TrySyncCommittedUi(string operation, Action sync)
        {
            try { sync(); }
            catch (Exception uiError)
            {
                try { _document.Editor.WriteMessage("\nQS3D " + operation + " đã commit; UI sync warning: " + uiError.Message); }
                catch { }
            }
        }

        private static string Value(ProjectFamily family, string key, string fallback) =>
            family.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        private static double Positive(string raw, string label)
        {
            var value = Number(raw, label);
            if (!(value > 0d)) throw new InvalidOperationException(label + " phải > 0.");
            return value;
        }

        private static double NonNegative(string raw, string label)
        {
            var value = Number(raw, label);
            if (value < 0d) throw new InvalidOperationException(label + " phải >= 0.");
            return value;
        }

        private static double Number(string raw, string label)
        {
            if (!double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new InvalidOperationException(label + " không phải số hợp lệ.");
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " phải hữu hạn.");
            return value;
        }

        private static string Required(string raw, string label)
        {
            var value = (raw ?? string.Empty).Trim();
            if (value.Length == 0) throw new InvalidOperationException(label + " không được để trống.");
            return value;
        }

        private IEnumerable<TextBox> Boxes()
        {
            yield return ThicknessBox; yield return HeightBox; yield return MaxPanelWidthBox; yield return MaxPanelHeightBox;
            yield return PerimeterFrameBox; yield return MullionBox; yield return TransomBox; yield return FrameDepthBox;
            yield return GlassMaterialBox; yield return FrameMaterialBox;
        }

        private static double Q(ProjectElement element, string key)
        {
            if (!element.Quantities.TryGetValue(key, out var value)) return 0d;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " phải là quantity hữu hạn và >= 0.");
            return value;
        }

        private static int QInt(ProjectElement element, string key)
        {
            var value = Q(element, key);
            var rounded = Math.Round(value);
            if (Math.Abs(value - rounded) > 1e-9d || rounded > int.MaxValue)
                throw new InvalidOperationException(element.Id + "/" + key + " phải là số nguyên trong Int32.");
            return (int)rounded;
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Vách Kính Hub trước khi " + operation + ".");
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
