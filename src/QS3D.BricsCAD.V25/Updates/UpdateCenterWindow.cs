using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BricscadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class UpdateCenterWindow : Window
    {
        private readonly TextBlock _status;
        private readonly TextBlock _versions;
        private readonly TextBlock _detail;
        private readonly TextBox _notes;
        private readonly Button _refreshButton;
        private readonly Button _updateButton;
        private readonly Button _releaseButton;
        private UpdateCheckResult _result;

        internal UpdateCenterWindow()
        {
            Title = "QS3D Update Center";
            Width = 620;
            Height = 520;
            MinWidth = 540;
            MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(24, 28, 36));
            Foreground = Brushes.White;

            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "Cập nhật QS3D",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            _versions = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(164, 174, 190)),
                Margin = new Thickness(0, 0, 0, 18)
            };
            Grid.SetRow(_versions, 1);
            root.Children.Add(_versions);

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
            Grid.SetRow(stateCard, 2);
            root.Children.Add(stateCard);

            _notes = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(19, 23, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 234)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 67, 82)),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(_notes, 3);
            root.Children.Add(_notes);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            _releaseButton = MakeButton("Mở trang release", false);
            _releaseButton.Click += (_, __) => OpenReleasePage();
            _refreshButton = MakeButton("Kiểm tra lại", false);
            _refreshButton.Click += async (_, __) => await UpdateCoordinator.Instance.RefreshAsync();
            _updateButton = MakeButton("Cập nhật ngay", true);
            _updateButton.Click += async (_, __) => await ScheduleUpdateAsync();
            actions.Children.Add(_releaseButton);
            actions.Children.Add(_refreshButton);
            actions.Children.Add(_updateButton);
            Grid.SetRow(actions, 4);
            root.Children.Add(actions);

            Content = root;
            UpdateCoordinator.Instance.StateChanged += OnStateChanged;
            Closed += (_, __) => UpdateCoordinator.Instance.StateChanged -= OnStateChanged;
            Apply(UpdateCoordinator.Instance.LastResult);
        }

        internal void Apply(UpdateCheckResult result)
        {
            if (result == null) return;
            _result = result;
            _status.Text = result.Message;
            _detail.Text = result.Detail;

            var current = result.CurrentVersion?.Original ?? "unknown";
            var latest = result.Release?.Tag ?? "—";
            _versions.Text = "Đang chạy: " + current + "    •    GitHub mới nhất: " + latest;
            _notes.Text = result.Release?.Notes ?? "Ghi chú release sẽ hiển thị ở đây khi có dữ liệu.";

            var checking = result.State == UpdateState.Checking;
            _refreshButton.IsEnabled = !checking && result.State != UpdateState.Scheduled;
            _updateButton.IsEnabled = result.CanAutoInstall;
            _releaseButton.IsEnabled = result.Release?.PageUri != null;
            _updateButton.Content = result.State == UpdateState.Scheduled ? "Đã lên lịch" : "Cập nhật ngay";
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

        private static Button MakeButton(string text, bool primary)
        {
            return new Button
            {
                Content = text,
                MinWidth = primary ? 130 : 118,
                Height = 36,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(14, 0, 14, 0),
                Background = primary ? new SolidColorBrush(Color.FromRgb(56, 116, 255)) : new SolidColorBrush(Color.FromRgb(47, 55, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
        }
    }

    internal static class UpdateCenterWindowHost
    {
        private static UpdateCenterWindow _window;

        internal static void Show(UpdateCheckResult result = null, bool activate = true)
        {
            if (_window == null || !_window.IsLoaded)
            {
                _window = new UpdateCenterWindow();
                _window.Closed += (_, __) => _window = null;
            }

            if (result != null) _window.Apply(result);
            if (!_window.IsVisible)
                BricscadApplication.ShowModelessWindow(IntPtr.Zero, _window, true);
            else if (activate)
                _window.Activate();
        }

        internal static void Close()
        {
            if (_window == null) return;
            try { _window.Close(); }
            catch { }
            _window = null;
        }
    }
}