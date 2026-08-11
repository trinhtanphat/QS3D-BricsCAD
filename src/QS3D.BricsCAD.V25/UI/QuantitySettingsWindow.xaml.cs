using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySettingsWindow : Window
    {
        private const string UnsupportedSchemaMarker = "QS3D.QuantitySettings.UnsupportedSchema";
        private readonly QuantitySettingsStore _store;
        private QuantityCalculationSettings _loadedSettings = QuantityCalculationSettings.CreateDefault();
        private bool _updatingIntersectionBrowser;
        private bool _persistentSettingsWriteBlocked;

        public QuantitySettingsWindow(QuantitySettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            CategoryRows = new ObservableCollection<QuantityCategoryRuleRow>();
            IntersectionRows = new ObservableCollection<QuantityIntersectionRuleRow>();
            IntersectionCategoryChoices = new ObservableCollection<QuantityCategoryChoice>();

            InitializeComponent();
            DataContext = this;
            SettingsPathText.Text = _store.SettingsPath;

            try
            {
                LoadIntoView(_store.Load());
            }
            catch (Exception ex)
            {
                var unsupportedSchema = IsUnsupportedSettingsSchema(ex);
                LoadIntoView(QuantityCalculationSettings.CreateDefault());
                if (unsupportedSchema)
                {
                    _persistentSettingsWriteBlocked = true;
                    SaveSettingsButton.IsEnabled = false;
                    SettingsPathText.Text = _store.SettingsPath + "  •  CHỈ ĐỌC: schema mới hơn";
                }

                MessageBox.Show(
                    this,
                    unsupportedSchema
                        ? "File cấu hình QS3D hiện tại được tạo bởi schema mới hơn phiên bản plugin này. File gốc được giữ nguyên và cửa sổ chỉ nạp mặc định để tham khảo.\n\n‘Lưu Cài Đặt’ vào file cấu hình theo máy đã bị khóa trong cửa sổ này để tránh ghi đè dữ liệu mới hơn. Hãy cập nhật QS3D trước khi chỉnh cấu hình chính.\n\n" + ex.Message
                        : "Không đọc được cấu hình QS3D hiện tại. Cửa sổ đã nạp mặc định an toàn; file lỗi chưa bị ghi đè.\n\n" + ex.Message,
                    unsupportedSchema ? "QS3D • Cấu hình cần phiên bản mới hơn" : "QS3D • Cài đặt tính toán",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public ObservableCollection<QuantityCategoryRuleRow> CategoryRows { get; }
        public ObservableCollection<QuantityIntersectionRuleRow> IntersectionRows { get; }
        public ObservableCollection<QuantityCategoryChoice> IntersectionCategoryChoices { get; }

        private void LoadIntoView(QuantityCalculationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var copy = settings.Clone();
            copy.NormalizeAndValidate();
            _loadedSettings = copy;

            CategoryRows.Clear();
            foreach (var rule in copy.CategoryRules)
                CategoryRows.Add(new QuantityCategoryRuleRow(rule));

            IntersectionRows.Clear();
            foreach (var rule in copy.IntersectionRules)
                IntersectionRows.Add(new QuantityIntersectionRuleRow(rule));

            RebuildIntersectionBrowser();

            FormworkToleranceBox.Text = Format(copy.FormworkTolerance);
            BlindingConcreteOffsetBox.Text = Format(copy.BlindingConcreteOffset);
            MinSubtractAreaBox.Text = Format(copy.MinSubtractAreaMm2);
            MinFormworkAreaBox.Text = Format(copy.MinFormworkAreaMm2);
            MinConcreteVolumeBox.Text = Format(copy.MinConcreteVolumeM3);
            EngulfRelPercentBox.Text = Format(copy.EngulfRelPercent);
            EngulfMinAreaBox.Text = Format(copy.EngulfMinAreaMm2);
            RoomGapFillBox.Text = Format(copy.RoomGapFillMm);
            RoomSearchRadiusBox.Text = Format(copy.RoomSearchRadiusMm);
            DimColorBox.Text = copy.DimColor;
            DimTextHeightBox.Text = Format(copy.DimTextHeight);
        }

        private QuantityCalculationSettings BuildSettingsFromView()
        {
            var result = new QuantityCalculationSettings
            {
                SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion,
                FormworkTolerance = ParseNonNegative(FormworkToleranceBox.Text, "Dung sai cốp pha"),
                BlindingConcreteOffset = ParseNonNegative(BlindingConcreteOffsetBox.Text, "Offset bê tông lót"),
                MinSubtractAreaMm2 = ParseNonNegative(MinSubtractAreaBox.Text, "Diện tích trừ tối thiểu"),
                MinFormworkAreaMm2 = ParseNonNegative(MinFormworkAreaBox.Text, "Diện tích cốp pha tối thiểu"),
                MinConcreteVolumeM3 = ParseNonNegative(MinConcreteVolumeBox.Text, "Thể tích bê tông tối thiểu"),
                EngulfRelPercent = ParseNonNegative(EngulfRelPercentBox.Text, "Tỷ lệ bao phủ tương đối"),
                EngulfMinAreaMm2 = ParseNonNegative(EngulfMinAreaBox.Text, "Diện tích bao phủ tối thiểu"),
                RoomGapFillMm = ParseNonNegative(RoomGapFillBox.Text, "Lấp khe Phòng"),
                RoomSearchRadiusMm = ParseNonNegative(RoomSearchRadiusBox.Text, "Bán kính tìm Phòng"),
                DimColor = (DimColorBox.Text ?? string.Empty).Trim(),
                DimTextHeight = ParsePositive(DimTextHeightBox.Text, "Chiều cao chữ kích thước"),
                CategoryRules = CategoryRows.Select(x => x.ToSetting()).ToList(),
                IntersectionRules = IntersectionRows.Select(x => x.ToSetting()).ToList()
            };
            result.NormalizeAndValidate();
            return result;
        }

        private void RebuildIntersectionBrowser()
        {
            var previousSource = (PrimaryCategoryList.SelectedItem as QuantityCategoryChoice)?.CategoryCode;
            var previousTarget = (ReferenceCategoryList.SelectedItem as QuantityCategoryChoice)?.CategoryCode;
            var codes = CategoryRows.Select(x => x.CategoryCode)
                .Concat(IntersectionRows.Select(x => x.SourceCode))
                .Concat(IntersectionRows.Select(x => x.TargetCode))
                .Distinct()
                .OrderBy(x => QuantityCategoryDisplayName.Resolve(x), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x)
                .ToList();

            _updatingIntersectionBrowser = true;
            try
            {
                IntersectionCategoryChoices.Clear();
                foreach (var code in codes)
                    IntersectionCategoryChoices.Add(new QuantityCategoryChoice(code));

                if (IntersectionCategoryChoices.Count == 0)
                {
                    PrimaryCategoryList.SelectedItem = null;
                    ReferenceCategoryList.SelectedItem = null;
                    ClearIntersectionRuleDetail();
                    return;
                }

                var source = FindIntersectionChoice(previousSource) ?? IntersectionCategoryChoices[0];
                var target = FindIntersectionChoice(previousTarget) ?? FindFirstTargetForSource(source.CategoryCode) ?? IntersectionCategoryChoices[0];
                PrimaryCategoryList.SelectedItem = source;
                ReferenceCategoryList.SelectedItem = target;
            }
            finally
            {
                _updatingIntersectionBrowser = false;
            }

            RefreshSelectedIntersectionRule();
        }

        private QuantityCategoryChoice? FindIntersectionChoice(int? categoryCode)
        {
            if (!categoryCode.HasValue) return null;
            return IntersectionCategoryChoices.FirstOrDefault(x => x.CategoryCode == categoryCode.Value);
        }

        private QuantityCategoryChoice? FindFirstTargetForSource(int sourceCode)
        {
            var targetCode = IntersectionRows
                .Where(x => x.SourceCode == sourceCode)
                .Select(x => (int?)x.TargetCode)
                .FirstOrDefault();
            return FindIntersectionChoice(targetCode);
        }

        private void IntersectionCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingIntersectionBrowser) return;
            RefreshSelectedIntersectionRule();
        }

        private void RefreshSelectedIntersectionRule()
        {
            var source = PrimaryCategoryList.SelectedItem as QuantityCategoryChoice;
            var target = ReferenceCategoryList.SelectedItem as QuantityCategoryChoice;
            if (source == null || target == null)
            {
                ClearIntersectionRuleDetail();
                return;
            }

            SelectedRuleHeading.Text = source.DisplayName + "  →  " + target.DisplayName;
            var selected = IntersectionRows.SingleOrDefault(x => x.SourceCode == source.CategoryCode && x.TargetCode == target.CategoryCode);
            SelectedRuleEditor.DataContext = selected;
            SelectedRuleEditor.IsEnabled = selected != null;
            SelectedRuleStateText.Text = selected == null
                ? "Template hiện tại không có dòng luật cho cặp này. QS3D không tự tạo luật mới để tránh làm thay đổi payload ngoài ý muốn."
                : "Đang chỉnh đúng một luật có hướng trong ma trận hiện tại.";

            var reverse = IntersectionRows.SingleOrDefault(x => x.SourceCode == target.CategoryCode && x.TargetCode == source.CategoryCode);
            if (reverse == null)
            {
                ReverseRuleSummaryText.Text = "Không có luật chiều ngược trong template hiện tại.";
                ViewReverseRuleButton.IsEnabled = false;
                return;
            }

            ReverseRuleSummaryText.Text = target.DisplayName + " → " + source.DisplayName + ": " + SummarizeIntersectionRule(reverse);
            ViewReverseRuleButton.IsEnabled = source.CategoryCode != target.CategoryCode;
        }

        private void ClearIntersectionRuleDetail()
        {
            SelectedRuleHeading.Text = "Chọn hai loại cấu kiện";
            SelectedRuleStateText.Text = "Chọn Cấu kiện chính và Cấu kiện tham chiếu để xem một luật có hướng.";
            SelectedRuleEditor.DataContext = null;
            SelectedRuleEditor.IsEnabled = false;
            ReverseRuleSummaryText.Text = "Chưa có cặp luật để đối chiếu.";
            ViewReverseRuleButton.IsEnabled = false;
        }

        private void ViewReverseRule_Click(object sender, RoutedEventArgs e)
        {
            var source = PrimaryCategoryList.SelectedItem as QuantityCategoryChoice;
            var target = ReferenceCategoryList.SelectedItem as QuantityCategoryChoice;
            if (source == null || target == null || source.CategoryCode == target.CategoryCode) return;

            var reverse = IntersectionRows.SingleOrDefault(x => x.SourceCode == target.CategoryCode && x.TargetCode == source.CategoryCode);
            if (reverse == null) return;

            var nextSource = FindIntersectionChoice(target.CategoryCode);
            var nextTarget = FindIntersectionChoice(source.CategoryCode);
            if (nextSource == null || nextTarget == null) return;

            _updatingIntersectionBrowser = true;
            try
            {
                PrimaryCategoryList.SelectedItem = nextSource;
                ReferenceCategoryList.SelectedItem = nextTarget;
            }
            finally
            {
                _updatingIntersectionBrowser = false;
            }
            RefreshSelectedIntersectionRule();
        }

        private static string SummarizeIntersectionRule(QuantityIntersectionRuleRow rule)
        {
            var enabled = new List<string>();
            if (rule.SubtractConcrete) enabled.Add("trừ BT");
            if (rule.SubtractSideFormworkByConcrete) enabled.Add("CP Thành ← BT");
            if (rule.SubtractBottomFormworkByConcrete) enabled.Add("CP Đáy ← BT");
            if (rule.SubtractSideFormworkBySideFormwork) enabled.Add("CP Thành ← CP");
            if (rule.SubtractBottomFormworkByBottomFormwork) enabled.Add("CP Đáy ← CP");
            return enabled.Count == 0 ? "không bật phép trừ nào" : string.Join(" • ", enabled);
        }

        private void ImportTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "QS3D • Nạp template thông số tính toán",
                Filter = "JSON settings (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != true) return;

            try
            {
                var imported = _store.Import(dialog.FileName);
                LoadIntoView(imported);
                var unknown = imported.CategoryRules.Count(x => !QuantityCategoryDisplayName.IsKnown(x.Category));
                var note = unknown == 0
                    ? string.Empty
                    : "\n\nCó " + unknown + " mã cấu kiện chưa có tên QS3D; mã số vẫn được giữ nguyên để không làm mất luật.";
                MessageBox.Show(
                    this,
                    "Đã nạp " + imported.CategoryRules.Count + " loại cấu kiện và " + imported.IntersectionRules.Count + " luật giao cắt." + note +
                    (_persistentSettingsWriteBlocked
                        ? "\n\nFile cấu hình chính đang dùng schema mới hơn nên Lưu Cài Đặt vẫn bị khóa. Bạn vẫn có thể Xuất template đã nạp ra một file khác."
                        : "\n\nNhấn ‘Lưu Cài Đặt’ để áp dụng template này làm cấu hình theo máy."),
                    "QS3D • Nạp template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Không thể nạp template.", ex);
            }
        }

        private void ExportTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var current = BuildSettingsFromView();
                var dialog = new SaveFileDialog
                {
                    Title = "QS3D • Xuất template thông số tính toán",
                    Filter = "JSON settings (*.json)|*.json",
                    DefaultExt = ".json",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = "QS3D_quantity_settings.json"
                };
                if (dialog.ShowDialog(this) != true) return;
                if (_persistentSettingsWriteBlocked && SamePath(dialog.FileName, _store.SettingsPath))
                {
                    MessageBox.Show(
                        this,
                        "Không thể xuất đè lên file cấu hình theo máy đang dùng schema mới hơn. Hãy chọn một file khác hoặc cập nhật QS3D trước.",
                        "QS3D • File cấu hình đang được bảo vệ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                _store.Export(dialog.FileName, current);
                MessageBox.Show(this, "Đã xuất template:\n" + dialog.FileName, "QS3D • Xuất template", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Không thể xuất template.", ex);
            }
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(
                this,
                _persistentSettingsWriteBlocked
                    ? "Khôi phục cấu hình mặc định chỉ trong cửa sổ? File cấu hình schema mới hơn vẫn được bảo vệ và Lưu Cài Đặt tiếp tục bị khóa."
                    : "Khôi phục cấu hình mặc định QS3D trong cửa sổ? Thao tác này chưa ghi xuống máy cho tới khi bạn nhấn ‘Lưu Cài Đặt’.",
                "QS3D • Khôi phục mặc định",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes) LoadIntoView(QuantityCalculationSettings.CreateDefault());
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_persistentSettingsWriteBlocked)
            {
                MessageBox.Show(
                    this,
                    "Không thể ghi đè file cấu hình theo máy vì file hiện tại dùng schema mới hơn phiên bản QS3D này. Hãy cập nhật plugin trước. File hiện hữu chưa bị thay đổi.",
                    "QS3D • Lưu Cài Đặt bị khóa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var current = BuildSettingsFromView();
                _store.Save(current);
                _loadedSettings = current.Clone();
                MessageBox.Show(
                    this,
                    "Đã lưu " + current.CategoryRules.Count + " loại cấu kiện và " + current.IntersectionRules.Count + " luật giao cắt.\n\n" + _store.SettingsPath,
                    "QS3D • Đã lưu cài đặt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Không thể lưu cài đặt.", ex);
            }
        }

        private static bool IsUnsupportedSettingsSchema(Exception exception)
        {
            return exception is System.IO.InvalidDataException
                && Equals(exception.Data[UnsupportedSchemaMarker], true);
        }

        private static bool SamePath(string left, string right)
        {
            try
            {
                return string.Equals(
                    System.IO.Path.GetFullPath(left),
                    System.IO.Path.GetFullPath(right),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string prefix, Exception ex)
        {
            MessageBox.Show(this, prefix + "\n\n" + ex.Message, "QS3D • Cài đặt tính toán", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string Format(double value)
        {
            return value.ToString("0.########", CultureInfo.CurrentCulture);
        }

        private static double ParseNonNegative(string text, string label)
        {
            var value = ParseNumber(text, label);
            if (value < 0d) throw new InvalidOperationException(label + " không được âm.");
            return value;
        }

        private static double ParsePositive(string text, string label)
        {
            var value = ParseNumber(text, label);
            if (value <= 0d) throw new InvalidOperationException(label + " phải lớn hơn 0.");
            return value;
        }

        private static double ParseNumber(string text, string label)
        {
            double value;
            var trimmed = (text ?? string.Empty).Trim();
            if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out value) &&
                !double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException(label + " phải là một số hợp lệ.");
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " phải là một số hữu hạn.");
            return value;
        }
    }

    public sealed class QuantityCategoryRuleRow
    {
        public QuantityCategoryRuleRow(QuantityCategoryRuleSetting setting)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            CategoryCode = setting.Category;
            ExtractSide = setting.ExtractSide;
            ExtractBottom = setting.ExtractBottom;
            FaceAngleThresholdDeg = setting.FaceAngleThresholdDeg;
        }

        public int CategoryCode { get; }
        public string DisplayName => QuantityCategoryDisplayName.Resolve(CategoryCode);
        public bool ExtractSide { get; set; }
        public bool ExtractBottom { get; set; }
        public double FaceAngleThresholdDeg { get; set; }

        public QuantityCategoryRuleSetting ToSetting()
        {
            return new QuantityCategoryRuleSetting
            {
                Category = CategoryCode,
                ExtractSide = ExtractSide,
                ExtractBottom = ExtractBottom,
                FaceAngleThresholdDeg = FaceAngleThresholdDeg
            };
        }
    }

    public sealed class QuantityCategoryChoice
    {
        public QuantityCategoryChoice(int categoryCode)
        {
            CategoryCode = categoryCode;
        }

        public int CategoryCode { get; }
        public string DisplayName => QuantityCategoryDisplayName.Resolve(CategoryCode);
    }

    public sealed class QuantityIntersectionRuleRow
    {
        public QuantityIntersectionRuleRow(QuantityIntersectionRuleSetting setting)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            SourceCode = setting.Source;
            TargetCode = setting.Target;
            SubtractConcrete = setting.SubtractConcrete;
            SubtractSideFormworkByConcrete = setting.SubtractSideFormworkByConcrete;
            SubtractBottomFormworkByConcrete = setting.SubtractBottomFormworkByConcrete;
            SubtractSideFormworkBySideFormwork = setting.SubtractSideFormworkBySideFormwork;
            SubtractBottomFormworkByBottomFormwork = setting.SubtractBottomFormworkByBottomFormwork;
        }

        public int SourceCode { get; }
        public int TargetCode { get; }
        public string SourceDisplay => QuantityCategoryDisplayName.Resolve(SourceCode);
        public string TargetDisplay => QuantityCategoryDisplayName.Resolve(TargetCode);
        public bool SubtractConcrete { get; set; }
        public bool SubtractSideFormworkByConcrete { get; set; }
        public bool SubtractBottomFormworkByConcrete { get; set; }
        public bool SubtractSideFormworkBySideFormwork { get; set; }
        public bool SubtractBottomFormworkByBottomFormwork { get; set; }

        public QuantityIntersectionRuleSetting ToSetting()
        {
            return new QuantityIntersectionRuleSetting
            {
                Source = SourceCode,
                Target = TargetCode,
                SubtractConcrete = SubtractConcrete,
                SubtractSideFormworkByConcrete = SubtractSideFormworkByConcrete,
                SubtractBottomFormworkByConcrete = SubtractBottomFormworkByConcrete,
                SubtractSideFormworkBySideFormwork = SubtractSideFormworkBySideFormwork,
                SubtractBottomFormworkByBottomFormwork = SubtractBottomFormworkByBottomFormwork
            };
        }
    }

    internal static class QuantityCategoryDisplayName
    {
        public static bool IsKnown(int code)
        {
            return Enum.IsDefined(typeof(ElementCategory), code) || IsCompatibilityCodeKnown(code);
        }

        public static string Resolve(int code)
        {
            if (Enum.IsDefined(typeof(ElementCategory), code))
                return Native((ElementCategory)code);

            var compatibility = Compatibility(code);
            return compatibility == null ? "Mã cấu kiện " + code : compatibility + "  [" + code + "]";
        }

        private static string Native(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Grid: return "Lưới trục";
                case ElementCategory.Room: return "Phòng";
                case ElementCategory.FloorFinish: return "Sàn hoàn thiện";
                case ElementCategory.Waterproofing: return "Chống thấm";
                case ElementCategory.Skirting: return "Chân tường";
                case ElementCategory.WallFinish: return "Hoàn thiện tường";
                case ElementCategory.CeilingFinish: return "Hoàn thiện trần";
                case ElementCategory.Railing: return "Lan can";
                case ElementCategory.Beam: return "Dầm";
                case ElementCategory.Slab: return "Sàn";
                case ElementCategory.Column: return "Cột";
                case ElementCategory.StructuralWall: return "Vách BTCT";
                case ElementCategory.ArchitecturalWall: return "Tường kiến trúc";
                case ElementCategory.GlassWall: return "Vách kính";
                case ElementCategory.WallPier: return "Trụ tường";
                case ElementCategory.WallOpening: return "Lỗ mở tường";
                case ElementCategory.Door: return "Cửa";
                case ElementCategory.Stair: return "Cầu thang";
                case ElementCategory.Foundation: return "Móng";
                case ElementCategory.Earthwork: return "Đào đắp";
                case ElementCategory.CustomQuantity: return "KL tùy chỉnh";
                default: return category.ToString();
            }
        }

        private static bool IsCompatibilityCodeKnown(int code)
        {
            return Compatibility(code) != null;
        }

        private static string? Compatibility(int code)
        {
            switch (code)
            {
                case 201: return "Phòng";
                case 202: return "Sàn hoàn thiện";
                case 204: return "Chân tường";
                case 205: return "Hoàn thiện tường";
                case 207: return "Lan can";
                case 301: return "Dầm HCN";
                case 302: return "Giằng tường";
                case 703: return "Lanh tô";
                case 401: return "Sàn đặc";
                case 501: return "Đường dốc";
                case 601: return "Cột";
                case 701: return "Vách BTCT";
                case 704: return "Tường gạch";
                default: return null;
            }
        }
    }
}
