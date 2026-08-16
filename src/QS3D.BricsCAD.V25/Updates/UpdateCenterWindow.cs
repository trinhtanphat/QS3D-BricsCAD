using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BricscadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class UpdateCenterWindow : Window
    {
        private readonly TextBlock _title;
        private readonly TextBlock _status;
        private readonly TextBlock _versions;
        private readonly TextBlock _runtimeIdentity;
        private readonly TextBlock _detail;
        private readonly TextBox _notes;
        private readonly Button _refreshButton;
        private readonly Button _updateButton;
        private readonly Button _releaseButton;
        private UpdateCheckResult? _result;
        private bool _coordinatorAttached;
        private bool _previewDownloading;
        private string? _downloadedPackagePath;
        private string? _downloadedReleaseTag;

        internal UpdateCenterWindow()
        {
            Title = "QS3D Update Center";
            Width = 620;
            Height = 550;
            MinWidth = 540;
            MinHeight = 440;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(24, 28, 36));
            Foreground = Brushes.White;

            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _title = new TextBlock
            {
                Text = "Cập nhật QS3D",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            Grid.SetRow(_title, 0);
            root.Children.Add(_title);

            _versions = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(164, 174, 190)),
                Margin = new Thickness(0, 0, 0, 6),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_versions, 1);
            root.Children.Add(_versions);

            _runtimeIdentity = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(134, 146, 164)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 18),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_runtimeIdentity, 2);
            root.Children.Add(_runtimeIdentity);

            var stateCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(34, 40, 51)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var stateStack = new StackPanel();
            _status = new TextBlock { FontSize = 16, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
            _detail = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(190, 199, 214)),
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            stateStack.Children.Add(_status);
            stateStack.Children.Add(_detail);
            stateCard.Child = stateStack;
            Grid.SetRow(stateCard, 3);
            root.Children.Add(stateCard);

            _notes = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(19, 23, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 234)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 67, 82)),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(_notes, 4);
            root.Children.Add(_notes);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            _releaseButton = MakeButton("Mở trang release", false);
            _releaseButton.Click += (_, __) => OpenReleasePage();
            _refreshButton = MakeButton("Kiểm tra lại", false);
            _refreshButton.Click += async (_, __) => await UpdateCoordinator.Instance.RefreshAsync();
            _updateButton = MakeButton("Cập nhật ngay", true);
            _updateButton.Click += async (_, __) => await HandlePrimaryActionAsync();
            actions.Children.Add(_releaseButton);
            actions.Children.Add(_refreshButton);
            actions.Children.Add(_updateButton);
            Grid.SetRow(actions, 5);
            root.Children.Add(actions);

            Content = root;
            UpdateCoordinator.Instance.StateChanged += OnStateChanged;
            _coordinatorAttached = true;
            Closed += (_, __) => DetachCoordinator();
            Apply(UpdateCoordinator.Instance.LastResult);
        }

        internal void Apply(UpdateCheckResult? result)
        {
            if (result == null) return;
            _result = result;

            var releaseTag = result.Release?.Tag;
            if (_downloadedReleaseTag != null && !string.Equals(_downloadedReleaseTag, releaseTag, StringComparison.Ordinal))
            {
                _downloadedPackagePath = null;
                _downloadedReleaseTag = null;
            }
            if (_downloadedPackagePath != null && !File.Exists(_downloadedPackagePath))
            {
                _downloadedPackagePath = null;
                _downloadedReleaseTag = null;
            }

            _status.Text = result.Message;
            _detail.Text = result.Detail;

            var currentOriginal = result.CurrentVersion?.Original ?? "unknown";
            var currentDisplay = ToDisplayVersion(currentOriginal);
            var latest = result.Release?.Tag ?? "—";
            var assembly = Assembly.GetExecutingAssembly();
            var loadedPath = string.IsNullOrWhiteSpace(assembly.Location) ? "<unknown>" : assembly.Location;
            var buildIdentity = GetBuildIdentity(currentOriginal);

            Title = "QS3D Update Center — " + currentDisplay;
            _title.Text = "Cập nhật QS3D " + currentDisplay;
            _versions.Text = "Phiên bản hiện tại: " + currentDisplay + "    •    GitHub mới nhất: " + latest;
            _runtimeIdentity.Text = string.IsNullOrWhiteSpace(buildIdentity)
                ? "DLL đang chạy: " + loadedPath
                : "Build: " + buildIdentity + "    •    DLL đang chạy: " + loadedPath;
            _runtimeIdentity.ToolTip = "Product version đầy đủ: " + currentOriginal + "\n" + loadedPath;
            _notes.Text = result.Release?.Notes ?? "Ghi chú release sẽ hiển thị ở đây khi có dữ liệu.";

            var checking = result.State == UpdateState.Checking;
            var hasManualRelease = result.State == UpdateState.ManualInstallRequired && result.Release?.PageUri != null;
            var hasPreviewDownload = result.State == UpdateState.ManualInstallRequired && result.Release?.HasVerifiedPreviewPackage == true;
            var hasDownloadedPreview = hasPreviewDownload
                                       && _downloadedPackagePath != null
                                       && string.Equals(_downloadedReleaseTag, releaseTag, StringComparison.Ordinal)
                                       && File.Exists(_downloadedPackagePath);

            _refreshButton.IsEnabled = !_previewDownloading && !checking && result.State != UpdateState.Scheduled;
            _updateButton.IsEnabled = !_previewDownloading && (result.CanAutoInstall || hasPreviewDownload || hasManualRelease);
            _releaseButton.IsEnabled = !_previewDownloading && result.Release?.PageUri != null;

            if (_previewDownloading)
            {
                _updateButton.Content = "Đang tải…";
                _updateButton.ToolTip = "Đang tải package từ GitHub Release và kiểm tra SHA-256.";
            }
            else if (result.State == UpdateState.Scheduled)
            {
                _updateButton.Content = "Đã lên lịch";
                _updateButton.ToolTip = "Cập nhật đã được lên lịch và đang chờ BricsCAD đóng an toàn.";
            }
            else if (hasDownloadedPreview)
            {
                _updateButton.Content = "Mở file đã tải";
                _updateButton.ToolTip = "Mở Explorer tới package preview đã được kiểm tra SHA-256.";
            }
            else if (hasPreviewDownload)
            {
                _updateButton.Content = "Tải & kiểm tra";
                _updateButton.ToolTip = "Tải package preview trực tiếp từ GitHub Release và xác minh SHA-256. Package unsigned sẽ không được tự chạy/cài.";
            }
            else if (hasManualRelease)
            {
                _updateButton.Content = "Cài thủ công";
                _updateButton.ToolTip = "Mở GitHub Release để tải bản mới. Release này chưa có package + checksum đủ điều kiện cho tải trực tiếp.";
            }
            else
            {
                _updateButton.Content = "Cập nhật ngay";
                _updateButton.ToolTip = result.CanAutoInstall ? "Xác minh và lên lịch cập nhật an toàn." : "Chưa có bản cập nhật tự động hợp lệ.";
            }
        }

        internal void DetachCoordinator()
        {
            if (!_coordinatorAttached) return;
            UpdateCoordinator.Instance.StateChanged -= OnStateChanged;
            _coordinatorAttached = false;
        }

        private async System.Threading.Tasks.Task HandlePrimaryActionAsync()
        {
            var current = _result;
            if (current?.State == UpdateState.ManualInstallRequired && current.Release != null)
            {
                if (_downloadedPackagePath != null
                    && string.Equals(_downloadedReleaseTag, current.Release.Tag, StringComparison.Ordinal)
                    && File.Exists(_downloadedPackagePath))
                {
                    RevealDownloadedFile(_downloadedPackagePath);
                    return;
                }

                if (current.Release.HasVerifiedPreviewPackage)
                {
                    await DownloadPreviewAsync(current.Release);
                    return;
                }

                if (current.Release.PageUri != null)
                {
                    OpenReleasePage();
                    return;
                }
            }

            await ScheduleUpdateAsync();
        }

        private async System.Threading.Tasks.Task DownloadPreviewAsync(UpdateReleaseInfo release)
        {
            if (_previewDownloading) return;
            _previewDownloading = true;
            Apply(_result);
            _status.Text = "Đang tải bản preview…";
            _detail.Text = "QS3D đang tải package và checksum từ đúng GitHub Release. File chỉ được giữ lại sau khi SHA-256 khớp.";

            try
            {
                var verified = await new VerifiedReleaseDownloader().DownloadAsync(release);
                _downloadedPackagePath = verified.Path;
                _downloadedReleaseTag = release.Tag;
                _previewDownloading = false;
                Apply(_result);
                _status.Text = "Đã tải và kiểm tra SHA-256";
                _detail.Text = "Package preview đã được xác minh (SHA-256: " + verified.Sha256 + "). Đây là package unsigned nên QS3D không tự chạy hoặc tự cài. File: " + verified.Path;
                RevealDownloadedFile(verified.Path);
            }
            catch (Exception ex)
            {
                _previewDownloading = false;
                Apply(_result);
                MessageBox.Show(
                    this,
                    "Không tải được bản preview an toàn: " + ex.Message,
                    "QS3D Update Center",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async System.Threading.Tasks.Task ScheduleUpdateAsync()
        {
            _updateButton.IsEnabled = false;
            var result = await UpdateCoordinator.Instance.ScheduleLatestAsync();
            Apply(result);
            if (result.State != UpdateState.Scheduled) return;

            if (!SecureUpdateLauncher.TryRequestGracefulHostClose(out var closeError))
            {
                MessageBox.Show(
                    this,
                    closeError,
                    "QS3D Update Center",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void RevealDownloadedFile(string path)
        {
            if (!File.Exists(path))
            {
                _downloadedPackagePath = null;
                _downloadedReleaseTag = null;
                Apply(_result);
                MessageBox.Show(this, "File cập nhật đã tải không còn tồn tại.", "QS3D Update Center", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không mở được thư mục chứa package: " + ex.Message, "QS3D Update Center", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenReleasePage()
        {
            var uri = _result?.Release?.PageUri;
            if (uri == null) return;
            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Không mở được trang release: " + ex.Message, "QS3D Update Center", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnStateChanged(object sender, UpdateCheckResult result)
        {
            if (Dispatcher.CheckAccess()) Apply(result);
            else Dispatcher.BeginInvoke(new Action(() => Apply(result)));
        }

        private static string ToDisplayVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var trimmed = value.Trim();
            var metadataIndex = trimmed.IndexOf('+');
            if (metadataIndex >= 0) trimmed = trimmed.Substring(0, metadataIndex);
            return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? trimmed : "v" + trimmed;
        }

        private static string GetBuildIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var metadataIndex = value.IndexOf('+');
            if (metadataIndex < 0 || metadataIndex + 1 >= value.Length) return string.Empty;
            return value.Substring(metadataIndex + 1).Trim();
        }

        private static Button MakeButton(string text, bool primary)
        {
            var normal = primary
                ? new SolidColorBrush(Color.FromRgb(44, 96, 210))
                : new SolidColorBrush(Color.FromRgb(47, 55, 68));

            return new Button
            {
                Content = text,
                MinWidth = primary ? 130 : 118,
                Height = 36,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(14, 0, 14, 0),
                Background = normal,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FocusVisualStyle = null,
                Template = CreateButtonTemplate(primary, normal)
            };
        }

        private static ControlTemplate CreateButtonTemplate(bool primary, Brush normal)
        {
            var hover = primary
                ? new SolidColorBrush(Color.FromRgb(52, 108, 224))
                : new SolidColorBrush(Color.FromRgb(59, 70, 86));
            var pressed = primary
                ? new SolidColorBrush(Color.FromRgb(37, 82, 185))
                : new SolidColorBrush(Color.FromRgb(39, 48, 60));
            var disabled = new SolidColorBrush(Color.FromRgb(45, 52, 64));

            var chrome = new FrameworkElementFactory(typeof(Border), "Chrome");
            chrome.SetValue(Border.BackgroundProperty, normal);
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            chrome.SetValue(Border.PaddingProperty, new Thickness(14, 0, 14, 0));
            chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            chrome.AppendChild(content);

            var template = new ControlTemplate(typeof(Button)) { VisualTree = chrome };

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, hover, "Chrome"));
            template.Triggers.Add(hoverTrigger);

            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, pressed, "Chrome"));
            template.Triggers.Add(pressedTrigger);

            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.BackgroundProperty, disabled, "Chrome"));
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.62, "Chrome"));
            template.Triggers.Add(disabledTrigger);

            return template;
        }
    }

    internal static class UpdateCenterWindowHost
    {
        private static UpdateCenterWindow? _window;

        internal static void Show(UpdateCheckResult? result = null, bool activate = true)
        {
            var window = _window;
            if (window == null || !window.IsLoaded)
            {
                window = new UpdateCenterWindow();
                _window = window;
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_window, window)) _window = null;
                };
            }

            if (result != null) window.Apply(result);
            if (!window.IsVisible)
                BricscadApplication.ShowModelessWindow(IntPtr.Zero, window, true);
            else if (activate)
                window.Activate();
        }

        internal static void Close()
        {
            var window = _window;
            if (window == null) return;
            try
            {
                window.Close();
            }
            catch
            {
            }
            finally
            {
                window.DetachCoordinator();
                if (ReferenceEquals(_window, window)) _window = null;
            }
        }
    }
}
