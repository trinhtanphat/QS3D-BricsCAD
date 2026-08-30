using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BricscadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class UpdateCenterWindow : Window
    {
        private static readonly Brush TextPrimary = new SolidColorBrush(Color.FromRgb(239, 243, 250));
        private static readonly Brush TextSecondary = new SolidColorBrush(Color.FromRgb(178, 189, 207));
        private static readonly Brush TextMuted = new SolidColorBrush(Color.FromRgb(133, 148, 171));
        private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(75, 128, 238));
        private static readonly Brush AccentSoft = new SolidColorBrush(Color.FromRgb(125, 164, 248));
        private static readonly Brush Success = new SolidColorBrush(Color.FromRgb(88, 201, 151));
        private static readonly Brush Warning = new SolidColorBrush(Color.FromRgb(245, 184, 83));
        private static readonly Brush CardBackground = new SolidColorBrush(Color.FromRgb(31, 38, 49));
        private static readonly Brush PanelBackground = new SolidColorBrush(Color.FromRgb(19, 24, 32));
        private static readonly Brush BorderStroke = new SolidColorBrush(Color.FromRgb(54, 65, 82));

        private readonly TextBlock _title;
        private readonly TextBlock _status;
        private readonly TextBlock _versions;
        private readonly TextBlock _runtimeIdentity;
        private readonly TextBlock _detail;
        private readonly TextBox _notes;
        private readonly ProgressBar _progressBar;
        private readonly TextBlock _progressStage;
        private readonly TextBlock _progressPercent;
        private readonly CheckBox _updateOnCloseCheckBox;
        private readonly TextBlock _updateOnCloseHelp;
        private readonly Button _refreshButton;
        private readonly Button _updateButton;
        private readonly Button _releaseButton;
        private UpdateCheckResult? _result;
        private bool _coordinatorAttached;
        private bool _previewDownloading;
        private bool _changingUpdateOnClose;
#if !BRICSCAD_V26
        private bool _previewScheduled;
        private string? _previewScheduledDetail;
#endif

        internal UpdateCenterWindow()
        {
            Title = "QS3D Update Center";
            Width = 690;
            Height = 665;
            MinWidth = 580;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(20, 25, 33));
            Foreground = TextPrimary;
            FontFamily = new FontFamily("Segoe UI");

            var root = new Grid { Margin = new Thickness(26, 22, 26, 22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _title = new TextBlock
            {
                Text = "Cập nhật QS3D",
                FontSize = 25,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimary,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_title, 0);
            root.Children.Add(_title);

            _versions = new TextBlock
            {
                Foreground = TextSecondary,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 7),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_versions, 1);
            root.Children.Add(_versions);

            _runtimeIdentity = new TextBlock
            {
                Foreground = TextMuted,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 16),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_runtimeIdentity, 2);
            root.Children.Add(_runtimeIdentity);

            var stateCard = new Border
            {
                Background = CardBackground,
                BorderBrush = BorderStroke,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(17, 15, 17, 15),
                Margin = new Thickness(0, 0, 0, 15)
            };
            var stateStack = new StackPanel();
            _status = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimary,
                TextWrapping = TextWrapping.Wrap
            };
            _detail = new TextBlock
            {
                Foreground = TextSecondary,
                Margin = new Thickness(0, 7, 0, 0),
                LineHeight = 19,
                TextWrapping = TextWrapping.Wrap
            };
            stateStack.Children.Add(_status);
            stateStack.Children.Add(_detail);

            var progressHeader = new Grid { Margin = new Thickness(0, 15, 0, 6) };
            progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _progressStage = new TextBlock
            {
                Text = "Sẵn sàng",
                Foreground = TextSecondary,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _progressPercent = new TextBlock
            {
                Text = "0%",
                Foreground = AccentSoft,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(12, 0, 0, 0)
            };
            Grid.SetColumn(_progressStage, 0);
            Grid.SetColumn(_progressPercent, 1);
            progressHeader.Children.Add(_progressStage);
            progressHeader.Children.Add(_progressPercent);
            stateStack.Children.Add(progressHeader);

            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 8,
                Background = new SolidColorBrush(Color.FromRgb(46, 55, 70)),
                Foreground = Accent,
                BorderThickness = new Thickness(0),
                IsIndeterminate = false
            };
            stateStack.Children.Add(_progressBar);

            _updateOnCloseCheckBox = new CheckBox
            {
                Content = "Cập nhật khi đóng BricsCAD",
                IsChecked = UpdatePreferences.InstallOnExit,
                Foreground = TextPrimary,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _updateOnCloseCheckBox.Checked += (_, __) => PersistUpdateOnClose(true);
            _updateOnCloseCheckBox.Unchecked += (_, __) => PersistUpdateOnClose(false);
            stateStack.Children.Add(_updateOnCloseCheckBox);

            _updateOnCloseHelp = new TextBlock
            {
                Foreground = TextMuted,
                FontSize = 11,
                Margin = new Thickness(22, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            RefreshUpdateOnCloseHelp();
            stateStack.Children.Add(_updateOnCloseHelp);

            stateCard.Child = stateStack;
            Grid.SetRow(stateCard, 3);
            root.Children.Add(stateCard);

            var notesPanel = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            notesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            notesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var notesLabel = new TextBlock
            {
                Text = "Ghi chú phát hành",
                Foreground = TextSecondary,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(2, 0, 0, 7)
            };
            Grid.SetRow(notesLabel, 0);
            notesPanel.Children.Add(notesLabel);

            _notes = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = PanelBackground,
                Foreground = new SolidColorBrush(Color.FromRgb(216, 224, 237)),
                BorderBrush = BorderStroke,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(13, 11, 13, 11),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                CaretBrush = AccentSoft,
                SelectionBrush = new SolidColorBrush(Color.FromRgb(61, 91, 148))
            };
            Grid.SetRow(_notes, 1);
            notesPanel.Children.Add(_notes);
            Grid.SetRow(notesPanel, 4);
            root.Children.Add(notesPanel);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
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
            Closed += (_, __) => DetachCoordinator();

            try
            {
                UpdateCoordinator.Instance.StateChanged += OnStateChanged;
                _coordinatorAttached = true;
                Apply(UpdateCoordinator.Instance.LastResult);
            }
            catch
            {
                DetachCoordinator();
                throw;
            }
        }

        internal void Apply(UpdateCheckResult? result)
        {
            if (result == null) return;
            _result = result;

            var currentOriginal = result.CurrentVersion?.Original ?? "unknown";
            var currentDisplay = ToDisplayVersion(currentOriginal);
            var latest = result.Release?.Tag ?? "—";
            var assembly = Assembly.GetExecutingAssembly();
            var loadedPath = string.IsNullOrWhiteSpace(assembly.Location) ? "<unknown>" : assembly.Location;
            var buildIdentity = GetBuildIdentity(currentOriginal);

            Title = "QS3D Update Center — " + currentDisplay;
            _title.Text = "Cập nhật QS3D " + currentDisplay;
            ApplyVersionHighlights(currentDisplay, latest);
            _runtimeIdentity.Text = string.IsNullOrWhiteSpace(buildIdentity)
                ? "DLL đang chạy: " + loadedPath
                : "Build: " + buildIdentity + "    •    DLL đang chạy: " + loadedPath;
            _runtimeIdentity.ToolTip = "Product version đầy đủ: " + currentOriginal + "\n" + loadedPath;
            _notes.Text = result.Release?.Notes ?? "Ghi chú release sẽ hiển thị ở đây khi có dữ liệu.";

            var checking = result.State == UpdateState.Checking;
            var hasManualRelease = result.State == UpdateState.ManualInstallRequired && result.Release?.PageUri != null;
#if BRICSCAD_V26
            var hasPreviewDownload = false;
#else
            var hasPreviewDownload = result.State == UpdateState.ManualInstallRequired && result.Release?.HasVerifiedPreviewPackage == true;
#endif
#if !BRICSCAD_V26
            var previewScheduled = _previewScheduled;
#else
            var previewScheduled = false;
#endif

            _status.Text = result.Message;
            _detail.Text = result.Detail;

#if !BRICSCAD_V26
            if (hasPreviewDownload && !_previewDownloading && !previewScheduled)
            {
                _status.Text = "Gói preview + SHA-256 đã sẵn sàng";
                _status.Foreground = Success;
                _detail.Text = IsUpdateOnCloseEnabled()
                    ? "Gói preview đã có ZIP + SHA-256 hợp lệ để cài một chạm. Bạn có thể tiếp tục làm việc; QS3D sẽ cài khi bạn tự đóng BricsCAD và sau đó tự mở lại BricsCAD."
                    : "Tải, xác minh SHA-256, đóng BricsCAD an toàn, cài đặt rồi tự mở lại BricsCAD.";
            }
            else
#endif
            {
                _status.Foreground = result.State == UpdateState.Error ? Warning : TextPrimary;
            }

            _refreshButton.IsEnabled = !_previewDownloading && !previewScheduled && !checking && result.State != UpdateState.Scheduled;
            _updateButton.IsEnabled = !_previewDownloading && !previewScheduled && (result.CanAutoInstall || hasPreviewDownload || hasManualRelease);
            _releaseButton.IsEnabled = !_previewDownloading && result.Release?.PageUri != null;
            _updateOnCloseCheckBox.IsEnabled = !_previewDownloading && !previewScheduled && result.State != UpdateState.Scheduled;

            if (_previewDownloading)
            {
                _updateButton.Content = "Đang tải…";
                _updateButton.ToolTip = "Đang tải package từ GitHub Release và kiểm tra SHA-256.";
            }
#if !BRICSCAD_V26
            else if (previewScheduled)
            {
                _updateButton.Content = "Đã lên lịch";
                _updateButton.ToolTip = "Package preview đã xác minh sẽ chỉ thay DLL sau khi BricsCAD thoát; BricsCAD sẽ tự mở lại.";
                _status.Text = IsUpdateOnCloseEnabled()
                    ? "Sẵn sàng • Chờ bạn đóng BricsCAD"
                    : "Đã xác minh • Đang chờ BricsCAD đóng";
                _status.Foreground = Success;
                _detail.Text = _previewScheduledDetail ??
                    "QS3D đang chờ BricsCAD thoát, sau đó thay payload V25/Core đã xác minh, kiểm tra lại hash và tự mở lại BricsCAD.";
                SetProgress("Sẵn sàng • Chờ BricsCAD đóng", 96, false);
            }
#endif
            else if (result.State == UpdateState.Scheduled)
            {
                _updateButton.Content = "Đã lên lịch";
                _updateButton.ToolTip = "Cập nhật đã được lên lịch và đang chờ BricsCAD đóng an toàn.";
                SetProgress("Sẵn sàng • Chờ BricsCAD đóng", 96, false);
            }
            else if (hasPreviewDownload)
            {
                _updateButton.Content = "Tải & cài đặt";
                _updateButton.ToolTip = IsUpdateOnCloseEnabled()
                    ? "Tải và xác minh ngay; chỉ cài sau khi bạn tự đóng BricsCAD. BricsCAD sẽ tự mở lại."
                    : "Tải package preview, xác minh SHA-256, stage an toàn, đóng BricsCAD, cài rồi tự mở lại.";
                SetProgress("Sẵn sàng để tải và xác minh", 0, false);
            }
            else if (hasManualRelease)
            {
                _updateButton.Content = "Cài thủ công";
                _updateButton.ToolTip = "Mở GitHub Release để tải bản mới. Release này chưa có package + checksum đủ điều kiện cho tải trực tiếp.";
                SetProgress("Chờ package + checksum hợp lệ", 0, false);
            }
            else
            {
                _updateButton.Content = "Cập nhật ngay";
                _updateButton.ToolTip = result.CanAutoInstall ? "Xác minh và lên lịch cập nhật an toàn." : "Chưa có bản cập nhật tự động hợp lệ.";
                if (!_previewDownloading)
                {
                    if (checking) SetProgress("Đang kiểm tra GitHub Releases…", 12, true);
                    else if (result.State == UpdateState.UpToDate) SetProgress("Đang dùng phiên bản mới nhất", 100, false);
                    else if (result.State == UpdateState.Error) SetProgress("Kiểm tra cập nhật gặp lỗi", 0, false);
                    else SetProgress("Sẵn sàng", 0, false);
                }
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
#if !BRICSCAD_V26
            if (_previewScheduled) return;
#endif
            // Never install from a stale window snapshot. A new preview can be published while
            // Update Center remains open, so resolve the newest release again at click time.
            var current = await UpdateCoordinator.Instance.RefreshAsync();
            Apply(current);

            if (current?.State == UpdateState.ManualInstallRequired && current.Release != null)
            {
#if !BRICSCAD_V26
                if (current.Release.HasVerifiedPreviewPackage)
                {
                    await DownloadPreviewAsync(current.Release);
                    return;
                }
#endif
                if (current.Release.PageUri != null)
                {
                    OpenReleasePage();
                    return;
                }
            }

            await ScheduleUpdateAsync();
        }

#if !BRICSCAD_V26
        private async System.Threading.Tasks.Task DownloadPreviewAsync(UpdateReleaseInfo release)
        {
            if (_previewDownloading || _previewScheduled) return;
            _previewDownloading = true;
            Apply(_result);
            _status.Text = "Đang tải và xác minh bản preview…";
            _status.Foreground = AccentSoft;
            _detail.Text = "QS3D đang tải checksum và package từ đúng GitHub Release. Package chỉ được stage sau khi SHA-256 khớp.";
            SetProgress("Đang kết nối GitHub Release…", 3, true);

            try
            {
                var progress = new Progress<UpdateDownloadProgress>(ApplyDownloadProgress);
                var verified = await new VerifiedReleaseDownloader().DownloadAsync(release, progress);
                SetProgress("SHA-256 hợp lệ • đang stage payload…", 84, false);
                _detail.Text = "Package đã tải xong và SHA-256 khớp. QS3D đang tạo backup rollback và chuẩn bị updater tách rời.";

                if (!VerifiedPreviewInstaller.TrySchedule(verified.Path, verified.Sha256, out var installError))
                    throw new InvalidOperationException("Không thể stage package preview: " + installError);

                _previewScheduled = true;
                var restartCopy = " Sau khi thay và kiểm tra hash xong, BricsCAD sẽ tự mở lại đúng bricscad.exe hiện tại.";
                if (IsUpdateOnCloseEnabled())
                {
                    _previewScheduledDetail =
                        "SHA-256 đã xác minh: " + verified.Sha256 +
                        ". Bạn có thể tiếp tục làm việc. Updater sẽ chờ bạn tự đóng BricsCAD, backup DLL hiện tại, thay payload V25/Core, rollback nếu có lỗi." + restartCopy;
                    SetProgress("Sẵn sàng • Chờ bạn đóng BricsCAD", 96, false);
                    return;
                }

                _previewScheduledDetail =
                    "SHA-256 đã xác minh: " + verified.Sha256 +
                    ". Updater tách rời đang chờ BricsCAD thoát, sau đó backup DLL hiện tại, thay payload V25/Core và rollback nếu có lỗi." + restartCopy;
                SetProgress("Đã sẵn sàng • đang yêu cầu BricsCAD đóng…", 97, false);

                if (!SecureUpdateLauncher.TryRequestGracefulHostClose(out var closeError))
                {
                    _previewScheduledDetail += " " + closeError + " Bạn có thể tự đóng BricsCAD để tiếp tục cài đặt.";
                    MessageBox.Show(
                        this,
                        closeError + "\n\nPackage đã được stage an toàn. Hãy tự đóng BricsCAD để updater hoàn tất và tự mở lại BricsCAD.",
                        "QS3D Update Center",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Không chuẩn bị được bản preview an toàn: " + ex.Message,
                    "QS3D Update Center",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _previewDownloading = false;
                Apply(_result);
            }
        }
#endif

        private async System.Threading.Tasks.Task ScheduleUpdateAsync()
        {
            _updateButton.IsEnabled = false;
            SetProgress("Đang xác minh manifest cập nhật…", 30, true);
            var result = await UpdateCoordinator.Instance.ScheduleLatestAsync();
            Apply(result);
            if (result.State != UpdateState.Scheduled) return;

            if (IsUpdateOnCloseEnabled())
            {
                SetProgress("Sẵn sàng • Chờ bạn đóng BricsCAD", 96, false);
                return;
            }

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

        private void ApplyDownloadProgress(UpdateDownloadProgress progress)
        {
            if (progress == null) return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => ApplyDownloadProgress(progress)));
                return;
            }

            var overall = Math.Max(4, Math.Min(80, 4 + (int)Math.Round(progress.Percent * 0.76d)));
            SetProgress(progress.Stage, overall, false);
            if (progress.BytesReceived > 0)
            {
                var received = FormatBytes(progress.BytesReceived);
                var total = progress.TotalBytes > 0 ? " / " + FormatBytes(progress.TotalBytes) : string.Empty;
                _detail.Text = "Đang tải từ GitHub: " + received + total + ". File chỉ được dùng sau khi SHA-256 khớp checksum release.";
            }
        }

        private void PersistUpdateOnClose(bool enabled)
        {
            if (_changingUpdateOnClose) return;
            if (UpdatePreferences.TrySetInstallOnExit(enabled, out var error))
            {
                RefreshUpdateOnCloseHelp();
                if (_result != null) Apply(_result);
                return;
            }

            _changingUpdateOnClose = true;
            try { _updateOnCloseCheckBox.IsChecked = !enabled; }
            finally { _changingUpdateOnClose = false; }

            RefreshUpdateOnCloseHelp();
            MessageBox.Show(this, error, "QS3D Update Center", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void RefreshUpdateOnCloseHelp()
        {
            _updateOnCloseHelp.Text = IsUpdateOnCloseEnabled()
                ? "Đang bật: tải/xác minh trước, không tự đóng BricsCAD; cài khi bạn tự đóng và sau đó tự mở lại."
                : "Mặc định tắt: bấm Tải & cài đặt sẽ chuẩn bị update, yêu cầu đóng BricsCAD rồi tự mở lại sau khi cài.";
        }

        private bool IsUpdateOnCloseEnabled() => _updateOnCloseCheckBox.IsChecked == true;

        private void SetProgress(string stage, int percent, bool indeterminate)
        {
            var bounded = Math.Max(0, Math.Min(100, percent));
            _progressStage.Text = stage ?? string.Empty;
            _progressBar.IsIndeterminate = indeterminate;
            if (!indeterminate) _progressBar.Value = bounded;
            _progressPercent.Text = indeterminate ? "…" : bounded.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
        }

        private void ApplyVersionHighlights(string currentDisplay, string latest)
        {
            _versions.Inlines.Clear();
            _versions.Inlines.Add(new Run("Phiên bản hiện tại ") { Foreground = TextMuted });
            _versions.Inlines.Add(new Run(currentDisplay) { Foreground = Warning, FontWeight = FontWeights.Bold });
            _versions.Inlines.Add(new Run("   →   ") { Foreground = TextMuted });
            _versions.Inlines.Add(new Run("Phiên bản mới ") { Foreground = TextMuted });
            _versions.Inlines.Add(new Run(latest) { Foreground = Success, FontWeight = FontWeights.Bold });
        }

        private void OpenReleasePage()
        {
            var uri = _result?.Release?.PageUri;
            if (uri == null) return;
            try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
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

        private static string FormatBytes(long value)
        {
            if (value < 1024) return value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " B";
            var kb = value / 1024d;
            if (kb < 1024d) return kb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
            var mb = kb / 1024d;
            return mb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
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
                ? new SolidColorBrush(Color.FromRgb(48, 101, 219))
                : new SolidColorBrush(Color.FromRgb(44, 54, 69));

            return new Button
            {
                Content = text,
                MinWidth = primary ? 138 : 122,
                Height = 38,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(15, 0, 15, 0),
                Background = normal,
                Foreground = TextPrimary,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                FocusVisualStyle = null,
                Template = CreateButtonTemplate(primary, normal)
            };
        }

        private static ControlTemplate CreateButtonTemplate(bool primary, Brush normal)
        {
            var hover = primary
                ? new SolidColorBrush(Color.FromRgb(58, 116, 235))
                : new SolidColorBrush(Color.FromRgb(57, 69, 87));
            var pressed = primary
                ? new SolidColorBrush(Color.FromRgb(38, 84, 191))
                : new SolidColorBrush(Color.FromRgb(35, 44, 57));
            var disabled = new SolidColorBrush(Color.FromRgb(38, 46, 58));

            var chrome = new FrameworkElementFactory(typeof(Border), "Chrome");
            chrome.SetValue(Border.BackgroundProperty, normal);
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            chrome.SetValue(Border.PaddingProperty, new Thickness(15, 0, 15, 0));
            chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            chrome.AppendChild(contentPresenter);

            var template = new ControlTemplate(typeof(Button)) { VisualTree = chrome };
            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, hover, "Chrome"));
            template.Triggers.Add(hoverTrigger);
            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, pressed, "Chrome"));
            template.Triggers.Add(pressedTrigger);
            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.BackgroundProperty, disabled, "Chrome"));
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.56, "Chrome"));
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
            try { window.Close(); }
            catch { }
            finally
            {
                window.DetachCoordinator();
                if (ReferenceEquals(_window, window)) _window = null;
            }
        }
    }
}
