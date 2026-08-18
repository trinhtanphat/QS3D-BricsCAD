using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// QS3D-owned localization only. It deliberately does not change BricsCAD's
    /// process-wide CurrentUICulture, so the host and third-party plugins are untouched.
    /// </summary>
    internal static class UiLocalization
    {
        internal const string Vietnamese = "vi-VN";
        internal const string English = "en-US";
        internal const string ChineseSimplified = "zh-CN";
        internal const string ChineseTraditional = "zh-TW";
        internal const string Russian = "ru-RU";

        private const string LanguageFileName = "ui-language.txt";
        private static readonly object Sync = new object();
        private static readonly List<WeakReference> Roots = new List<WeakReference>();
        private static string? _currentLanguageCode;

        internal sealed class LanguageOption
        {
            internal LanguageOption(string code, string displayName)
            {
                Code = code;
                DisplayName = displayName;
            }

            internal string Code { get; }
            internal string DisplayName { get; }
        }

        private static readonly LanguageOption[] LanguageOptions =
        {
            new LanguageOption(Vietnamese, "Tiếng Việt"),
            new LanguageOption(English, "English"),
            new LanguageOption(ChineseSimplified, "简体中文"),
            new LanguageOption(ChineseTraditional, "繁體中文"),
            new LanguageOption(Russian, "Русский"),
        };

        // Rows are keyed by the canonical Vietnamese source text. Each array is:
        // vi-VN, en-US, zh-CN, zh-TW, ru-RU.
        private static readonly Dictionary<string, string[]> Rows =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Khởi đầu"] = new[] { "Khởi đầu", "Start", "开始", "開始", "Начало" },
                ["Mô hình"] = new[] { "Mô hình", "Model", "模型", "模型", "Модель" },
                ["BQ"] = new[] { "BQ", "BQ", "BQ", "BQ", "BQ" },
                ["Kiểm tra"] = new[] { "Kiểm tra", "Check", "检查", "檢查", "Проверка" },
                ["Dự án"] = new[] { "Dự án", "Project", "项目", "專案", "Проект" },
                ["Dự án gần đây"] = new[] { "Dự án gần đây", "Recent projects", "最近的项目", "最近的專案", "Недавние проекты" },
                ["Gần đây"] = new[] { "Gần đây", "Recent", "最近", "最近", "Недавние" },
                ["Tạo mới"] = new[] { "Tạo mới", "New", "新建", "新增", "Создать" },
                ["Mở"] = new[] { "Mở", "Open", "打开", "開啟", "Открыть" },
                ["Đóng"] = new[] { "Đóng", "Close", "关闭", "關閉", "Закрыть" },
                ["Lưu"] = new[] { "Lưu", "Save", "保存", "儲存", "Сохранить" },
                ["Lưu thay đổi"] = new[] { "Lưu thay đổi", "Save changes", "保存更改", "儲存變更", "Сохранить изменения" },
                ["Hủy"] = new[] { "Hủy", "Cancel", "取消", "取消", "Отмена" },
                ["Áp dụng"] = new[] { "Áp dụng", "Apply", "应用", "套用", "Применить" },
                ["Làm mới"] = new[] { "Làm mới", "Refresh", "刷新", "重新整理", "Обновить" },
                ["Xóa"] = new[] { "Xóa", "Delete", "删除", "刪除", "Удалить" },
                ["Chọn"] = new[] { "Chọn", "Select", "选择", "選取", "Выбрать" },
                ["Chọn tất cả"] = new[] { "Chọn tất cả", "Select all", "全选", "全選", "Выбрать все" },
                ["Bỏ chọn"] = new[] { "Bỏ chọn", "Clear selection", "取消选择", "取消選取", "Снять выбор" },
                ["Tìm kiếm"] = new[] { "Tìm kiếm", "Search", "搜索", "搜尋", "Поиск" },
                ["Bộ lọc"] = new[] { "Bộ lọc", "Filter", "筛选", "篩選", "Фильтр" },
                ["Thuộc tính"] = new[] { "Thuộc tính", "Properties", "属性", "屬性", "Свойства" },
                ["Cấu kiện"] = new[] { "Cấu kiện", "Element", "构件", "構件", "Элемент" },
                ["Tên"] = new[] { "Tên", "Name", "名称", "名稱", "Имя" },
                ["Giá trị"] = new[] { "Giá trị", "Value", "值", "值", "Значение" },
                ["Trạng thái"] = new[] { "Trạng thái", "Status", "状态", "狀態", "Состояние" },
                ["Mô tả"] = new[] { "Mô tả", "Description", "描述", "描述", "Описание" },
                ["Số lượng"] = new[] { "Số lượng", "Quantity", "数量", "數量", "Количество" },
                ["Đơn vị"] = new[] { "Đơn vị", "Unit", "单位", "單位", "Единица" },
                ["Vật liệu"] = new[] { "Vật liệu", "Material", "材质", "材質", "Материал" },
                ["Khu vực"] = new[] { "Khu vực", "Area", "区域", "區域", "Область" },
                ["Tầng"] = new[] { "Tầng", "Level", "楼层", "樓層", "Уровень" },
                ["Nhóm"] = new[] { "Nhóm", "Group", "组", "群組", "Группа" },
                ["Cài đặt"] = new[] { "Cài đặt", "Settings", "设置", "設定", "Настройки" },
                ["Trợ giúp"] = new[] { "Trợ giúp", "Help", "帮助", "說明", "Справка" },
                ["Ngôn ngữ"] = new[] { "Ngôn ngữ", "Language", "语言", "語言", "Язык" },
                ["Chọn ngôn ngữ giao diện"] = new[] { "Chọn ngôn ngữ giao diện", "Choose interface language", "选择界面语言", "選擇介面語言", "Выберите язык интерфейса" },
                ["Ngôn ngữ được lưu cho các lần mở QS3D tiếp theo."] = new[] { "Ngôn ngữ được lưu cho các lần mở QS3D tiếp theo.", "The language is saved for future QS3D sessions.", "语言设置会保存并用于以后启动 QS3D。", "語言設定會儲存並用於之後啟動 QS3D。", "Язык будет сохранён для следующих запусков QS3D." },
                ["Tiếng Việt"] = new[] { "Tiếng Việt", "Vietnamese", "越南语", "越南語", "Вьетнамский" },
                ["Thành công"] = new[] { "Thành công", "Success", "成功", "成功", "Успешно" },
                ["Lỗi"] = new[] { "Lỗi", "Error", "错误", "錯誤", "Ошибка" },
                ["Có"] = new[] { "Có", "Yes", "是", "是", "Да" },
                ["Không"] = new[] { "Không", "No", "否", "否", "Нет" },
            };

        private static readonly Dictionary<string, string> CanonicalByAny = BuildCanonicalLookup();

        internal static IEnumerable<LanguageOption> SupportedLanguages => LanguageOptions;

        internal static string CurrentLanguageCode
        {
            get
            {
                lock (Sync)
                {
                    if (_currentLanguageCode == null)
                    {
                        _currentLanguageCode = LoadPersistedLanguage();
                    }

                    return _currentLanguageCode;
                }
            }
        }

        internal static void RegisterAndApply(FrameworkElement root)
        {
            if (root == null)
            {
                return;
            }

            lock (Sync)
            {
                bool alreadyTracked = false;
                for (int index = Roots.Count - 1; index >= 0; index--)
                {
                    var existing = Roots[index].Target as FrameworkElement;
                    if (existing == null)
                    {
                        Roots.RemoveAt(index);
                    }
                    else if (ReferenceEquals(existing, root))
                    {
                        alreadyTracked = true;
                    }
                }

                if (!alreadyTracked)
                {
                    Roots.Add(new WeakReference(root));
                }
            }

            Apply(root);
        }

        internal static void SetLanguage(string? languageCode)
        {
            string normalized = NormalizeLanguageCode(languageCode);
            lock (Sync)
            {
                _currentLanguageCode = normalized;
                PersistLanguage(normalized);
            }

            foreach (FrameworkElement root in SnapshotRoots())
            {
                Apply(root);
            }
        }

        internal static string T(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source ?? string.Empty;
            }

            if (!CanonicalByAny.TryGetValue(source, out string canonical)
                || !Rows.TryGetValue(canonical, out string[] row))
            {
                return source;
            }

            return row[GetLanguageIndex(CurrentLanguageCode)];
        }

        internal static void Apply(FrameworkElement root)
        {
            if (root == null)
            {
                return;
            }

            if (!root.Dispatcher.CheckAccess())
            {
                root.Dispatcher.BeginInvoke(new Action(() => Apply(root)));
                return;
            }

            var pending = new Stack<DependencyObject>();
            var visited = new HashSet<DependencyObject>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                DependencyObject current = pending.Pop();
                if (!visited.Add(current))
                {
                    continue;
                }

                ApplyStrings(current);

                if (current is Visual)
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(current);
                    for (int index = childCount - 1; index >= 0; index--)
                    {
                        pending.Push(VisualTreeHelper.GetChild(current, index));
                    }
                }

                if (current is FrameworkElement || current is FrameworkContentElement)
                {
                    foreach (object child in LogicalTreeHelper.GetChildren(current))
                    {
                        if (child is DependencyObject dependencyChild)
                        {
                            pending.Push(dependencyChild);
                        }
                    }
                }
            }
        }

        private static void ApplyStrings(DependencyObject target)
        {
            if (target is Window)
            {
                ApplyStringProperty(target, Window.TitleProperty);
            }

            if (target is TextBlock)
            {
                ApplyStringProperty(target, TextBlock.TextProperty);
            }

            if (target is ContentControl)
            {
                ApplyStringProperty(target, ContentControl.ContentProperty);
            }

            if (target is HeaderedContentControl)
            {
                ApplyStringProperty(target, HeaderedContentControl.HeaderProperty);
            }

            if (target is HeaderedItemsControl)
            {
                ApplyStringProperty(target, HeaderedItemsControl.HeaderProperty);
            }

            ApplyStringProperty(target, ToolTipService.ToolTipProperty);
        }

        private static void ApplyStringProperty(DependencyObject target, DependencyProperty property)
        {
            ValueSource source = DependencyPropertyHelper.GetValueSource(target, property);
            if (source.IsExpression)
            {
                return;
            }

            if (!(target.GetValue(property) is string current))
            {
                return;
            }

            string translated = T(current);
            if (!string.Equals(current, translated, StringComparison.Ordinal))
            {
                // SetCurrentValue preserves a property's existing source (style/default)
                // unlike SetValue, and expression-backed values are skipped above.
                target.SetCurrentValue(property, translated);
            }
        }

        private static List<FrameworkElement> SnapshotRoots()
        {
            var roots = new List<FrameworkElement>();
            lock (Sync)
            {
                for (int index = Roots.Count - 1; index >= 0; index--)
                {
                    var root = Roots[index].Target as FrameworkElement;
                    if (root == null)
                    {
                        Roots.RemoveAt(index);
                    }
                    else
                    {
                        roots.Add(root);
                    }
                }
            }

            return roots;
        }

        private static string LoadPersistedLanguage()
        {
            try
            {
                string path = GetLanguageFilePath();
                return File.Exists(path)
                    ? NormalizeLanguageCode(File.ReadAllText(path).Trim())
                    : Vietnamese;
            }
            catch
            {
                return Vietnamese;
            }
        }

        private static void PersistLanguage(string languageCode)
        {
            try
            {
                string path = GetLanguageFilePath();
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, languageCode);
            }
            catch
            {
                // A read-only profile must never prevent QS3D from opening or switching
                // language for the current process.
            }
        }

        private static string GetLanguageFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QS3D",
                LanguageFileName);
        }

        private static string NormalizeLanguageCode(string? languageCode)
        {
            string value = (languageCode ?? string.Empty).Trim();

            if (value.Equals(English, StringComparison.OrdinalIgnoreCase)
                || value.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                return English;
            }

            if (value.Equals(ChineseSimplified, StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-SG", StringComparison.OrdinalIgnoreCase))
            {
                return ChineseSimplified;
            }

            if (value.Equals(ChineseTraditional, StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-HK", StringComparison.OrdinalIgnoreCase)
                || value.Equals("zh-MO", StringComparison.OrdinalIgnoreCase))
            {
                return ChineseTraditional;
            }

            if (value.Equals(Russian, StringComparison.OrdinalIgnoreCase)
                || value.Equals("ru", StringComparison.OrdinalIgnoreCase))
            {
                return Russian;
            }

            return Vietnamese;
        }

        private static int GetLanguageIndex(string languageCode)
        {
            if (languageCode == English) return 1;
            if (languageCode == ChineseSimplified) return 2;
            if (languageCode == ChineseTraditional) return 3;
            if (languageCode == Russian) return 4;
            return 0;
        }

        private static Dictionary<string, string> BuildCanonicalLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> pair in Rows)
            {
                foreach (string value in pair.Value)
                {
                    // A repeated translation such as "模型" is intentionally associated
                    // with the same canonical row only when first seen.
                    if (!lookup.ContainsKey(value))
                    {
                        lookup.Add(value, pair.Key);
                    }
                }
            }

            return lookup;
        }
    }
}
