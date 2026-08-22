using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class CurtainWallWindow : Window
    {
        private bool _loading;

        public CurtainWallWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshAll();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshAll();

        private void OnFamilyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            LoadSelectedFamily();
            RefreshSummary();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null || !(FamilyCombo.SelectedItem is ProjectFamily family)) return;
            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ThicknessM"] = Positive(ThicknessBox.Text, "Bề dày kính").ToString("R", CultureInfo.InvariantCulture),
                    ["HeightM"] = Positive(HeightBox.Text, "Chiều cao vách").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainMaxPanelWidthM"] = Positive(MaxPanelWidthBox.Text, "Panel rộng tối đa").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainMaxPanelHeightM"] = Positive(MaxPanelHeightBox.Text, "Panel cao tối đa").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainPerimeterFrameWidthM"] = NonNegative(PerimeterFrameBox.Text, "Khung biên").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainMullionWidthM"] = NonNegative(MullionBox.Text, "Mullion đứng").ToString("R", CultureInfo.InvariantCulture),
                    ["CurtainTransomWidthM"] = NonNegative(TransomBox.Text, "Transom ngang").ToString("R", CultureInfo.InvariantCulture),
                    ["Material"] = Required(GlassMaterialBox.Text, "Vật liệu kính"),
                    ["CurtainFrameMaterial"] = Required(FrameMaterialBox.Text, "Vật liệu khung")
                };

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var inherited = 0;
                var overrides = 0;
                foreach (var pair in values)
                    ApplyFamilyValue(project, family, pair.Key, pair.Value, ref inherited, ref overrides);

                project.Touch();
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                PaletteCoordinator.RefreshProject();
                RefreshSummary();
                SetStatus("Đã lưu Family • kế thừa " + inherited + " giá trị instance • giữ " + overrides + " override • regen " + regenerated + " cấu kiện.");
            }
            catch (Exception ex) { SetStatus("Lưu Vách Kính lỗi: " + ex.Message); }
        }

        private void OnRecalculateClick(object sender, RoutedEventArgs e)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                foreach (var element in project.Elements.Where(x => x.Category == ElementCategory.GlassWall))
                    element.MarkDirty(ElementDirtyFlags.Quantity);
                var count = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                PaletteCoordinator.RefreshProject();
                RefreshSummary();
                SetStatus("Đã tính lại " + count + " cấu kiện dirty.");
            }
            catch (Exception ex) { SetStatus("Tính lại Vách Kính lỗi: " + ex.Message); }
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string command) || string.IsNullOrWhiteSpace(command)) return;
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            SetStatus("Chạy " + command + "…");
            document.SendStringToExecute(command + " ", true, false, false);
        }

        private void RefreshAll()
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(document);
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
            RefreshSummary();
            SetStatus(families.Count == 0 ? "Chưa có Family Vách Kính. Chọn đối tượng CAD rồi bấm “Bóc Vách Kính”." : "Đã nạp " + families.Count + " Family Vách Kính.");
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
            GlassMaterialBox.Text = Value(family, "Material", "Kính");
            FrameMaterialBox.Text = Value(family, "CurtainFrameMaterial", "Nhôm");
        }

        private void RefreshSummary()
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var family = FamilyCombo.SelectedItem as ProjectFamily;
            var elements = project.Elements.Where(x => x.Category == ElementCategory.GlassWall && (family == null || string.Equals(x.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase))).ToList();
            WallCountText.Text = elements.Count.ToString(CultureInfo.InvariantCulture);
            PanelCountText.Text = elements.Sum(x => QInt(x, "CurtainPanelCount")).ToString(CultureInfo.InvariantCulture);
            GlassAreaText.Text = elements.Sum(x => Q(x, "CurtainNetGlassAreaM2")).ToString("0.###", CultureInfo.InvariantCulture) + " m²";
            FrameLengthText.Text = elements.Sum(x => Q(x, "CurtainFrameLengthM")).ToString("0.###", CultureInfo.InvariantCulture) + " m";
        }

        private static void ApplyFamilyValue(ProjectState project, ProjectFamily family, string key, string next, ref int inherited, ref int overrides)
        {
            var hadPrevious = family.Properties.TryGetValue(key, out var previousRaw);
            var previous = previousRaw ?? string.Empty;
            if (hadPrevious && string.Equals(previous, next, StringComparison.Ordinal)) return;
            family.Properties[key] = next;
            foreach (var element in project.Elements.Where(x => string.Equals(x.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase)))
            {
                var hasInstance = element.Properties.TryGetValue(key, out var instanceRaw);
                var instance = instanceRaw ?? string.Empty;
                if (!hasInstance || (hadPrevious && string.Equals(instance, previous, StringComparison.Ordinal)))
                {
                    element.SetProperty(key, next);
                    inherited++;
                }
                else
                {
                    element.MarkDirty(ElementDirtyFlags.All);
                    overrides++;
                }
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
            yield return PerimeterFrameBox; yield return MullionBox; yield return TransomBox; yield return GlassMaterialBox; yield return FrameMaterialBox;
        }

        private static double Q(ProjectElement element, string key)
        {
            if (!element.Quantities.TryGetValue(key, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d) return 0d;
            return value;
        }

        private static int QInt(ProjectElement element, string key)
        {
            var value = Q(element, key);
            if (value > int.MaxValue) return int.MaxValue;
            return (int)Math.Round(value);
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            PaletteCoordinator.SetStatus(StatusText.Text);
        }
    }
}
