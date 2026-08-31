using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ReferenceSearchWindow : Window
    {
        private const int MaxQueryLength = 512;
        private readonly Document _document;
        private readonly IntPtr _nativeDatabaseIdentity;

        public ReferenceSearchWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _nativeDatabaseIdentity = GetNativeDatabaseIdentity(_document);
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) =>
            {
                Title = "QS3D • Tham khảo thi công • " + DrawingLabel(_document);
                QueryBox.Focus();
                QueryBox.SelectAll();
            };
        }

        internal bool IsBoundTo(Document document, IntPtr nativeDatabaseIdentity)
        {
            return nativeDatabaseIdentity != IntPtr.Zero
                && _nativeDatabaseIdentity == nativeDatabaseIdentity
                && ReferenceEquals(_document, document);
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string kind)) return;
            OpenSearch(kind);
        }

        private void OnQuickQueryClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is string query) || string.IsNullOrWhiteSpace(query)) return;
            QueryBox.Text = query.Trim();
            QueryBox.Focus();
            QueryBox.CaretIndex = QueryBox.Text.Length;
            SetStatus("Đã chọn từ khóa nhanh • Enter để mở Hình ảnh hoặc chọn loại kết quả.");
        }

        private void OnQueryKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            OpenSearch("images");
        }

        private void OpenSearch(string kind)
        {
            try
            {
                EnsureActive();
                var query = NormalizeQuery(QueryBox.Text);
                if (TechnicalContextCheck.IsChecked == true)
                    query = AppendTechnicalContext(query);

                var url = BuildSearchUrl(kind, query);
                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                SetStatus("Đã mở " + KindLabel(kind) + " trong trình duyệt mặc định • SafeSearch bật khi nhà cung cấp hỗ trợ.");
            }
            catch (Exception ex)
            {
                SetStatus("Tra cứu tham khảo lỗi: " + ex.Message);
            }
        }

        private void EnsureActive()
        {
            var active = Application.DocumentManager.MdiActiveDocument;
            if (!ReferenceEquals(active, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Tham khảo thi công trước khi mở kết quả.");

            var activeIdentity = GetNativeDatabaseIdentity(active);
            if (activeIdentity != _nativeDatabaseIdentity)
                throw new InvalidOperationException("Bản vẽ Tham khảo thi công đã đổi native database; hãy đóng cửa sổ và mở lại từ bản vẽ hiện tại.");
        }

        private static string NormalizeQuery(string? raw)
        {
            var query = (raw ?? string.Empty).Trim();
            if (query.Length == 0)
                throw new InvalidOperationException("Nhập từ khóa cần tham khảo trước khi tìm.");
            if (query.Length > MaxQueryLength)
                throw new InvalidOperationException("Từ khóa quá dài. Giới hạn " + MaxQueryLength + " ký tự.");
            return query;
        }

        private static string AppendTechnicalContext(string query)
        {
            return AppendBoundedSuffix(query, " kỹ thuật xây dựng chi tiết thi công", "ngữ cảnh kỹ thuật");
        }

        private static string AppendBoundedSuffix(string query, string suffix, string context)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (suffix == null) throw new ArgumentNullException(nameof(suffix));
            if (query.Length + suffix.Length > MaxQueryLength)
                throw new InvalidOperationException("Từ khóa quá dài sau khi thêm " + context + ". Giới hạn " + MaxQueryLength + " ký tự.");
            return query + suffix;
        }

        private static string BuildSearchUrl(string kind, string query)
        {
            var normalizedKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            var effectiveQuery = normalizedKind == "shorts"
                ? AppendBoundedSuffix(query, " video ngắn shorts", "ngữ cảnh video ngắn")
                : query;
            var encoded = Uri.EscapeDataString(effectiveQuery);

            switch (normalizedKind)
            {
                case "images":
                    return "https://www.google.com/search?tbm=isch&safe=active&q=" + encoded;
                case "web":
                    return "https://www.google.com/search?safe=active&q=" + encoded;
                case "video":
                    return "https://www.google.com/search?tbm=vid&safe=active&q=" + encoded;
                case "shopping":
                    return "https://www.google.com/search?tbm=shop&safe=active&q=" + encoded;
                case "shorts":
                    return "https://www.google.com/search?tbm=vid&safe=active&q=" + encoded;
                case "news":
                    return "https://www.google.com/search?tbm=nws&safe=active&q=" + encoded;
                default:
                    throw new InvalidOperationException("Loại kết quả không được hỗ trợ: " + normalizedKind + ".");
            }
        }

        private static string KindLabel(string kind)
        {
            switch ((kind ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "images": return "Hình ảnh";
                case "web": return "Web";
                case "video": return "Video";
                case "shopping": return "Mua sắm";
                case "shorts": return "Video ngắn";
                case "news": return "Tin tức";
                default: return "kết quả";
            }
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Reference Search requires a BricsCAD document database.");

            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Reference Search requires a live native BricsCAD database.");
            return identity;
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}
