using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.Updates
{
    internal enum UpdateState
    {
        Idle,
        Checking,
        UpToDate,
        UpdateAvailable,
        ManualInstallRequired,
        Scheduled,
        Error
    }

    internal sealed class UpdateCheckResult
    {
        internal UpdateCheckResult(UpdateState state, SemanticReleaseVersion currentVersion, UpdateReleaseInfo? release, string? message, string? detail)
        {
            State = state;
            CurrentVersion = currentVersion;
            Release = release;
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        internal UpdateState State { get; }
        internal SemanticReleaseVersion CurrentVersion { get; }
        internal UpdateReleaseInfo? Release { get; }
        internal string Message { get; }
        internal string Detail { get; }
        internal bool HasUpdate => Release is UpdateReleaseInfo release && release.Version.CompareTo(CurrentVersion) > 0;
        internal bool CanAutoInstall => State == UpdateState.UpdateAvailable && Release is UpdateReleaseInfo release && release.ManifestUri != null && !SecureUpdateLauncher.IsScheduled;
    }

    internal sealed class UpdateCoordinator
    {
        private readonly object _sync = new object();
        private readonly GitHubReleaseClient _client = new GitHubReleaseClient();
        private readonly UpdateManifestProbe _manifestProbe = new UpdateManifestProbe();
        private Dispatcher? _dispatcher;
        private Task<UpdateCheckResult>? _inFlight;
        private int _inFlightGeneration = -1;
        private UpdateCheckResult _last;
        private int _generation;
        private bool _started;

        private UpdateCoordinator()
        {
            var current = GetCurrentVersion();
            _last = new UpdateCheckResult(UpdateState.Idle, current, null, "Chưa kiểm tra cập nhật.", string.Empty);
        }

        internal static UpdateCoordinator Instance { get; } = new UpdateCoordinator();

        internal event EventHandler<UpdateCheckResult>? StateChanged;
        internal event EventHandler<UpdateCheckResult>? AutomaticUpdateFound;

        internal UpdateCheckResult LastResult
        {
            get { lock (_sync) return _last; }
        }

        internal void Start()
        {
            lock (_sync)
            {
                if (_started) return;
                _started = true;
                _generation++;
                _dispatcher = Dispatcher.CurrentDispatcher;
            }
            _ = CheckAsync(true);
        }

        internal void Stop()
        {
            lock (_sync)
            {
                _started = false;
                _generation++;
                _inFlight = null;
                _inFlightGeneration = -1;
            }
        }

        internal Task<UpdateCheckResult> RefreshAsync()
        {
            return CheckAsync(false);
        }

        internal async Task<UpdateCheckResult> ScheduleLatestAsync()
        {
            var generation = CaptureGeneration();
            var fresh = await CheckAsync(false).ConfigureAwait(false);
            var release = fresh.Release;
            if (!fresh.CanAutoInstall || release == null)
                return fresh;

            if (!TryScheduleCurrentGeneration(generation, release, out var lifecycleCurrent, out var error))
            {
                var failed = new UpdateCheckResult(UpdateState.Error, fresh.CurrentVersion, release, "Không thể lên lịch cập nhật.", error);
                if (lifecycleCurrent) Publish(failed, false);
                return failed;
            }

            var scheduled = new UpdateCheckResult(
                UpdateState.Scheduled,
                fresh.CurrentVersion,
                release,
                "Đã lên lịch cập nhật.",
                "QS3D sẽ yêu cầu BricsCAD đóng theo cơ chế cửa sổ bình thường để giữ nguyên các nhắc lưu bản vẽ. Nếu bạn hủy đóng, updater chỉ tiếp tục chờ; khi mọi BricsCAD đã thoát, nó mới xác minh chữ ký, cập nhật và mở lại sau khi thành công.");
            if (IsGenerationCurrent(generation)) Publish(scheduled, false);
            return scheduled;
        }

        private int CaptureGeneration()
        {
            lock (_sync) return _generation;
        }

        private bool TryScheduleCurrentGeneration(int generation, UpdateReleaseInfo release, out bool lifecycleCurrent, out string error)
        {
            lock (_sync)
            {
                lifecycleCurrent = _started && generation == _generation;
                if (!lifecycleCurrent)
                {
                    error = "Phiên cập nhật đã thay đổi hoặc đã dừng trước khi lên lịch. Mở lại Update Center và thử lại.";
                    return false;
                }
                return SecureUpdateLauncher.TrySchedule(release, out error);
            }
        }

        private Task<UpdateCheckResult> CheckAsync(bool automatic)
        {
            lock (_sync)
            {
                if (!_started)
                    return Task.FromResult(_last);

                var generation = _generation;
                if (_inFlight != null && !_inFlight.IsCompleted && _inFlightGeneration == generation)
                    return _inFlight;

                _inFlightGeneration = generation;
                _inFlight = CheckCoreAsync(automatic, generation);
                return _inFlight;
            }
        }

        private async Task<UpdateCheckResult> CheckCoreAsync(bool automatic, int generation)
        {
            var current = GetCurrentVersion();
            Publish(new UpdateCheckResult(UpdateState.Checking, current, null, "Đang kiểm tra GitHub Releases…", string.Empty), false);

            try
            {
                var releases = await _client.GetPublishedReleasesAsync().ConfigureAwait(false);
                var candidates = releases
                    .Where(release => release != null)
                    .Where(release => current.IsPrerelease || !release.IsPrerelease)
                    .OrderByDescending(release => release.Version)
                    .ToArray();
                var latest = candidates.FirstOrDefault();

                UpdateCheckResult result;
                if (latest == null || latest.Version.CompareTo(current) <= 0)
                {
                    result = new UpdateCheckResult(UpdateState.UpToDate, current, latest, "QS3D đang ở bản mới nhất của kênh cập nhật.", string.Empty);
                }
                else if (!latest.HasSignedUpdateManifest)
                {
                    result = new UpdateCheckResult(
                        UpdateState.ManualInstallRequired,
                        current,
                        latest,
                        "Có bản QS3D mới " + latest.Tag + ", nhưng release này không có signed update manifest.",
                        "Bạn có thể mở trang release để cài thủ công. One-click update bị khóa để không hạ chuẩn bảo mật.");
                }
                else if (!SecureUpdateLauncher.TryGetCurrentSignerThumbprint(out var signerThumbprint, out var signerReason))
                {
                    result = new UpdateCheckResult(
                        UpdateState.ManualInstallRequired,
                        current,
                        latest,
                        "Có bản QS3D mới " + latest.Tag + ", nhưng bản đang chạy chưa có trust anchor cho one-click update.",
                        signerReason);
                }
                else
                {
                    var manifestProbe = await _manifestProbe.ValidateAsync(latest, signerThumbprint).ConfigureAwait(false);
                    if (!manifestProbe.IsEligible)
                    {
                        result = new UpdateCheckResult(
                            UpdateState.ManualInstallRequired,
                            current,
                            latest,
                            "Có bản QS3D mới " + latest.Tag + ", nhưng update manifest chưa vượt qua kiểm tra trước khi đóng BricsCAD.",
                            manifestProbe.Detail + " Bạn vẫn có thể mở trang release để kiểm tra/cài thủ công.");
                    }
                    else
                    {
                        result = new UpdateCheckResult(
                            UpdateState.UpdateAvailable,
                            current,
                            latest,
                            "Có bản QS3D mới " + latest.Tag + ".",
                            "Signed update manifest đã được xác minh trước khi đóng BricsCAD; package/chữ ký/hashes sẽ được xác minh lại bởi updater sau khi host thoát.");
                    }
                }

                if (!IsGenerationCurrent(generation)) return result;
                Publish(result, automatic && result.HasUpdate);
                return result;
            }
            catch (Exception ex)
            {
                var result = new UpdateCheckResult(UpdateState.Error, current, null, "Không kiểm tra được cập nhật. QS3D vẫn tiếp tục hoạt động bình thường.", ex.Message);
                if (IsGenerationCurrent(generation)) Publish(result, false);
                return result;
            }
        }

        private bool IsGenerationCurrent(int generation)
        {
            lock (_sync) return _started && generation == _generation;
        }

        private void Publish(UpdateCheckResult result, bool automaticNotification)
        {
            lock (_sync) _last = result;
            var dispatcher = _dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted) return;

            Action publish = () =>
            {
                StateChanged?.Invoke(this, result);
                if (automaticNotification) AutomaticUpdateFound?.Invoke(this, result);
            };

            if (dispatcher.CheckAccess()) publish();
            else dispatcher.BeginInvoke(publish, DispatcherPriority.Background);
        }

        private static SemanticReleaseVersion GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informational = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .Select(attribute => attribute.InformationalVersion)
                .FirstOrDefault();
            return SemanticReleaseVersion.FromRunningVersion(informational, assembly.GetName().Version);
        }
    }
}
