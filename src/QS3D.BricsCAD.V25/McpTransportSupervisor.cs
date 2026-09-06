using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25
{
    internal enum McpTransportHealth
    {
        Stopped,
        Starting,
        Ready,
        Degraded,
        Backoff,
        FailedOver
    }

    internal sealed class McpTransportSupervisorSnapshot
    {
        public McpTransportProvider PreferredProvider { get; set; }
        public McpTransportProvider? ActiveProvider { get; set; }
        public McpTransportHealth Health { get; set; }
        public int RestartCount { get; set; }
        public DateTime NextRetryUtc { get; set; }
        public string FailoverReason { get; set; } = string.Empty;
        public int? OwnedPid { get; set; }
    }

    /// <summary>
    /// Process-global lifecycle supervisor for durable MCP transports. It never creates a second
    /// embedded MCP server and never touches CAD mutation dispatch; it only owns external tunnel
    /// child lifecycle. Quick Tunnel remains an explicit test-only path and is not failover-eligible.
    ///
    /// Live concurrent Cloudflare/OpenAI qualification is intentionally LOCAL_ONLY. CI validates
    /// this deterministic state machine and ownership boundary only.
    /// </summary>
    internal static class McpTransportSupervisor
    {
        internal const int MaxRestartAttempts = 3;
        private const int MaxFailoverTransitions = 2;
        private const int SupervisorPeriodMilliseconds = 5000;
        private const int RestartBackoffBaseSeconds = 5;
        private const int RestartBackoffMaxSeconds = 120;
        private const int CloudflarePublicProbeTimeoutMilliseconds = 4000;

        private static readonly object Sync = new object();
        private static Timer? _timer;
        private static int _busy;
        private static bool _managing;
        private static McpTransportProvider _preferredProvider = McpTransportProvider.OpenAiSecureTunnel;
        private static McpTransportProvider? _activeProvider;
        private static McpTransportHealth _health = McpTransportHealth.Stopped;
        private static int _restartCount;
        private static int _failoverCount;
        private static DateTime _nextRetryUtc = DateTime.MinValue;
        private static string _failoverReason = string.Empty;
        private static int? _ownedPid;

        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "Transport", "OwnedProcesses");

        internal static bool IsManaging
        {
            get { lock (Sync) return _managing; }
        }

        internal static McpTransportSupervisorSnapshot Snapshot
        {
            get
            {
                lock (Sync)
                {
                    return new McpTransportSupervisorSnapshot
                    {
                        PreferredProvider = _preferredProvider,
                        ActiveProvider = _activeProvider,
                        Health = _health,
                        RestartCount = _restartCount,
                        NextRetryUtc = _nextRetryUtc,
                        FailoverReason = _failoverReason,
                        OwnedPid = _ownedPid
                    };
                }
            }
        }

        internal static string Describe()
        {
            var snapshot = Snapshot;
            return "preferred=" + snapshot.PreferredProvider
                   + "; active=" + (snapshot.ActiveProvider.HasValue ? snapshot.ActiveProvider.Value.ToString() : "none")
                   + "; health=" + snapshot.Health
                   + "; restarts=" + snapshot.RestartCount
                   + (snapshot.NextRetryUtc == DateTime.MinValue ? string.Empty : "; nextRetryUtc=" + snapshot.NextRetryUtc.ToString("o"))
                   + (snapshot.OwnedPid.HasValue ? "; ownedPid=" + snapshot.OwnedPid.Value.ToString() : string.Empty)
                   + (string.IsNullOrWhiteSpace(snapshot.FailoverReason) ? string.Empty : "; failover=" + snapshot.FailoverReason);
        }

        internal static void TryAutoStartPreferred(McpTransportProvider preferredProvider)
        {
            if (!IsDurableProvider(preferredProvider)) return;
            if (!IsAutoStartEnabled(preferredProvider)) return;

            lock (Sync)
            {
                _managing = true;
                _preferredProvider = preferredProvider;
                _activeProvider = preferredProvider;
                _health = McpTransportHealth.Starting;
                _restartCount = 0;
                _failoverCount = 0;
                _nextRetryUtc = DateTime.MinValue;
                _failoverReason = string.Empty;
                EnsureTimerUnsafe();
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;
                try { RunOneIteration("autostart"); }
                finally { Interlocked.Exchange(ref _busy, 0); }
            });
        }

        internal static void StopForHostShutdown()
        {
            Timer? timer;
            lock (Sync)
            {
                _managing = false;
                timer = _timer;
                _timer = null;
                _activeProvider = null;
                _health = McpTransportHealth.Stopped;
                _restartCount = 0;
                _failoverCount = 0;
                _nextRetryUtc = DateTime.MinValue;
                _failoverReason = string.Empty;
                _ownedPid = null;
            }
            if (timer != null) { try { timer.Dispose(); } catch { } }

            StopProvider(McpTransportProvider.OpenAiSecureTunnel);
            StopProvider(McpTransportProvider.CloudflareNamedTunnel);
            try { McpCloudflareTunnelManager.StopForHostShutdown(); } catch { }
        }

        private static void EnsureTimerUnsafe()
        {
            if (_timer != null) return;
            _timer = new Timer(SupervisorTick, null, SupervisorPeriodMilliseconds, SupervisorPeriodMilliseconds);
        }

        private static void SupervisorTick(object? state)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;
            try { RunOneIteration("watchdog"); }
            finally { Interlocked.Exchange(ref _busy, 0); }
        }

        private static void RunOneIteration(string reason)
        {
            McpTransportProvider active;
            lock (Sync)
            {
                if (!_managing || !_activeProvider.HasValue) return;
                active = _activeProvider.Value;
                if (DateTime.UtcNow < _nextRetryUtc)
                {
                    _health = McpTransportHealth.Backoff;
                    return;
                }
            }

            if (IsProviderHealthy(active))
            {
                CaptureOwnedProcess(active);
                lock (Sync)
                {
                    _health = McpTransportHealth.Ready;
                    _restartCount = 0;
                    _nextRetryUtc = DateTime.MinValue;
                }
                return;
            }

            string error;
            if (TryStartProvider(active, out error))
            {
                lock (Sync)
                {
                    _health = _failoverCount > 0 ? McpTransportHealth.FailedOver : McpTransportHealth.Ready;
                    _restartCount = 0;
                    _nextRetryUtc = DateTime.MinValue;
                }
                return;
            }

            int attempt;
            lock (Sync)
            {
                _restartCount = Math.Min(_restartCount + 1, 30);
                attempt = _restartCount;
                _health = McpTransportHealth.Degraded;
            }

            if (attempt >= MaxRestartAttempts)
            {
                McpTransportProvider fallback;
                bool canFailover;
                lock (Sync) canFailover = _failoverCount < MaxFailoverTransitions;
                if (canFailover && TryGetFallbackProvider(active, out fallback))
                {
                    var failoverReason = active + " restart budget exhausted after " + attempt.ToString()
                                         + " attempt(s) during " + reason
                                         + (string.IsNullOrWhiteSpace(error) ? string.Empty : ": " + Limit(error, 240));
                    StopProvider(active);
                    lock (Sync)
                    {
                        _failoverCount++;
                        _activeProvider = fallback;
                        _restartCount = 0;
                        _nextRetryUtc = DateTime.MinValue;
                        _failoverReason = failoverReason;
                        _health = McpTransportHealth.Starting;
                    }

                    string fallbackError;
                    if (TryStartProvider(fallback, out fallbackError))
                    {
                        // Failover is sticky: keep persisted selected-provider identity aligned with
                        // the route that is actually active, avoiding registration/public-URL drift.
                        McpTransportCoordinator.SetSelectedProvider(fallback);
                        lock (Sync) _health = McpTransportHealth.FailedOver;
                        return;
                    }

                    lock (Sync)
                    {
                        _restartCount = 1;
                        _health = McpTransportHealth.Degraded;
                        _failoverReason = failoverReason + "; fallback start failed: " + Limit(fallbackError, 240);
                        _nextRetryUtc = DateTime.UtcNow + ComputeRestartBackoff(_restartCount);
                    }
                    return;
                }
            }

            lock (Sync)
            {
                _nextRetryUtc = DateTime.UtcNow + ComputeRestartBackoff(attempt);
                _health = McpTransportHealth.Backoff;
            }
        }

        private static bool TryStartProvider(McpTransportProvider provider, out string error)
        {
            error = string.Empty;
            if (!IsDurableProvider(provider))
            {
                error = "Provider is not supervisor-eligible.";
                return false;
            }
            if (!IsProviderConfigured(provider))
            {
                error = provider + " is not configured.";
                return false;
            }

            var executable = GetExpectedExecutable(provider);
            string cleanup;
            if (!TryCleanupStaleOwnedProcess(provider, executable, out cleanup))
            {
                error = cleanup;
                return false;
            }

            // Exactly one external route may be active. This does not create/replace the embedded
            // MCP listener and therefore cannot create a second CAD mutation writer boundary.
            StopOtherDurableProvider(provider);
            try { McpCloudflareTunnelManager.StopForHostShutdown(); } catch { }

            bool started;
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
            {
                started = McpOpenAiSecureTunnelManager.StartForSupervisor(out error);
            }
            else
            {
                started = McpCloudflareAccountTunnelManager.StartForSupervisor(out error);
            }

            if (!started || !IsProviderRunning(provider)) return false;
            CaptureOwnedProcess(provider);
            return true;
        }

        private static void StopOtherDurableProvider(McpTransportProvider active)
        {
            if (active != McpTransportProvider.OpenAiSecureTunnel)
                StopProvider(McpTransportProvider.OpenAiSecureTunnel);
            if (active != McpTransportProvider.CloudflareNamedTunnel)
                StopProvider(McpTransportProvider.CloudflareNamedTunnel);
        }

        private static void StopProvider(McpTransportProvider provider)
        {
            // Manager stop paths clear ownership only when they actually own a live child.
            // Do not erase a crash-surviving sidecar here: it is the proof needed by the next
            // start to distinguish a QS3D-owned orphan from an unrelated tunnel process.
            try
            {
                if (provider == McpTransportProvider.OpenAiSecureTunnel)
                    McpOpenAiSecureTunnelManager.StopForHostShutdown();
                else if (provider == McpTransportProvider.CloudflareNamedTunnel)
                    McpCloudflareAccountTunnelManager.StopForHostShutdown();
            }
            catch { }
        }

        internal static bool TryGetFallbackProvider(McpTransportProvider failedProvider, out McpTransportProvider fallback)
        {
            fallback = failedProvider == McpTransportProvider.OpenAiSecureTunnel
                ? McpTransportProvider.CloudflareNamedTunnel
                : McpTransportProvider.OpenAiSecureTunnel;
            return IsDurableProvider(failedProvider)
                   && IsDurableProvider(fallback)
                   && IsAutoStartEnabled(fallback)
                   && IsProviderConfigured(fallback);
        }

        private static bool IsProviderConfigured(McpTransportProvider provider)
        {
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                return McpOpenAiSecureTunnelManager.IsConfigured;
            if (provider == McpTransportProvider.CloudflareNamedTunnel)
                return McpCloudflareAccountTunnelManager.IsConfigured;
            return false;
        }

        private static bool IsProviderRunning(McpTransportProvider provider)
        {
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                return McpOpenAiSecureTunnelManager.IsRunning;
            if (provider == McpTransportProvider.CloudflareNamedTunnel)
                return McpCloudflareAccountTunnelManager.IsRunning;
            return false;
        }

        private static bool IsProviderHealthy(McpTransportProvider provider)
        {
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                return McpOpenAiSecureTunnelManager.IsRunning && McpOpenAiSecureTunnelManager.IsReady;
            if (provider == McpTransportProvider.CloudflareNamedTunnel)
                return McpCloudflareAccountTunnelManager.IsRunning && IsCloudflarePublicHealthy();
            return false;
        }

        private static bool IsCloudflarePublicHealthy()
        {
            var hostname = McpCloudflareTunnelManager.NormalizeHostname(
                McpCloudflareAccountTunnelManager.SavedHostname);
            if (string.IsNullOrWhiteSpace(hostname)) return false;

            try
            {
                var dnsTask = Dns.GetHostAddressesAsync(hostname);
                if (!dnsTask.Wait(CloudflarePublicProbeTimeoutMilliseconds)) return false;
                var addresses = dnsTask.Result;
                var usableAddress = false;
                foreach (var address in addresses)
                {
                    if (IPAddress.IsLoopback(address)
                        || IPAddress.Any.Equals(address)
                        || IPAddress.IPv6Any.Equals(address)) continue;
                    usableAddress = true;
                    break;
                }
                if (!usableAddress) return false;
            }
            catch { return false; }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://" + hostname + "/mcp");
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                request.Timeout = CloudflarePublicProbeTimeoutMilliseconds;
                request.ReadWriteTimeout = CloudflarePublicProbeTimeoutMilliseconds;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return IsReachableMcpStatus(response.StatusCode);
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response == null) return false;
                using (response) return IsReachableMcpStatus(response.StatusCode);
            }
            catch { return false; }
        }

        private static bool IsReachableMcpStatus(HttpStatusCode status)
        {
            var code = (int)status;
            return (code >= 200 && code < 300)
                   || code == 400
                   || code == 401
                   || code == 403
                   || code == 404
                   || code == 405;
        }

        private static bool IsDurableProvider(McpTransportProvider provider)
        {
            return provider == McpTransportProvider.OpenAiSecureTunnel
                   || provider == McpTransportProvider.CloudflareNamedTunnel;
        }

        private static bool IsAutoStartEnabled(McpTransportProvider provider)
        {
            string path;
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP");
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                path = Path.Combine(root, "OpenAiSecureTunnel", "autostart.txt");
            else if (provider == McpTransportProvider.CloudflareNamedTunnel)
                path = Path.Combine(root, "CloudflareAccount", "autostart.txt");
            else
                return false;
            try { return File.Exists(path) && File.ReadAllText(path, Encoding.UTF8).Trim() == "1"; }
            catch { return false; }
        }

        private static string GetExpectedExecutable(McpTransportProvider provider)
        {
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                return McpOpenAiSecureTunnelManager.SavedClientPath;
            if (provider == McpTransportProvider.CloudflareNamedTunnel)
                return McpCloudflareAccountTunnelManager.CloudflaredPath;
            return string.Empty;
        }

        private static void CaptureOwnedProcess(McpTransportProvider provider)
        {
            Process? process;
            if (!TryGetManagerProcess(provider, out process) || process == null) return;
            try
            {
                if (process.HasExited) return;
                string ignored;
                if (RegisterOwnedProcess(provider, process, GetExpectedExecutable(provider), out ignored))
                {
                    lock (Sync) _ownedPid = process.Id;
                }
            }
            catch { }
        }

        private static bool TryGetManagerProcess(McpTransportProvider provider, out Process? process)
        {
            process = provider == McpTransportProvider.OpenAiSecureTunnel
                ? McpOpenAiSecureTunnelManager.OwnedProcess
                : provider == McpTransportProvider.CloudflareNamedTunnel
                    ? McpCloudflareAccountTunnelManager.OwnedProcess
                    : null;
            return process != null;
        }

        internal static bool RegisterOwnedProcess(
            McpTransportProvider provider, Process process, string expectedExecutable, out string error)
        {
            error = string.Empty;
            if (process == null) { error = "Owned process is null."; return false; }
            try
            {
                if (process.HasExited) { error = "Owned process already exited."; return false; }
                var expected = NormalizePath(expectedExecutable);
                if (string.IsNullOrWhiteSpace(expected))
                {
                    error = "Owned process executable path is invalid.";
                    return false;
                }

                var record = new OwnedProcessRecord
                {
                    Provider = provider,
                    Pid = process.Id,
                    StartUtcTicks = process.StartTime.ToUniversalTime().Ticks,
                    Executable = expected
                };
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(OwnedProcessPath(provider), SerializeRecord(record), new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                error = "Cannot persist owned tunnel process identity: " + ex.Message;
                return false;
            }
        }

        internal static bool TryCleanupStaleOwnedProcess(
            McpTransportProvider provider, string expectedExecutable, out string message)
        {
            message = string.Empty;
            OwnedProcessRecord record;
            if (!TryReadRecord(provider, out record)) return true;

            var expected = NormalizePath(expectedExecutable);
            if (string.IsNullOrWhiteSpace(expected)
                || !string.Equals(expected, NormalizePath(record.Executable), StringComparison.OrdinalIgnoreCase))
            {
                // The record no longer describes the configured executable. Drop metadata only;
                // never terminate a process whose identity cannot be proven.
                ClearOwnedProcess(provider);
                message = "Discarded stale ownership metadata with executable mismatch.";
                return true;
            }

            Process? process = null;
            try
            {
                process = Process.GetProcessById(record.Pid);
                if (process.HasExited)
                {
                    ClearOwnedProcess(provider);
                    return true;
                }

                var actualStart = process.StartTime.ToUniversalTime().Ticks;
                var actualExecutable = NormalizePath(process.MainModule?.FileName ?? string.Empty);
                if (actualStart != record.StartUtcTicks
                    || string.IsNullOrWhiteSpace(actualExecutable)
                    || !string.Equals(actualExecutable, expected, StringComparison.OrdinalIgnoreCase))
                {
                    // PID reuse or executable drift: never kill. The old ownership record is no
                    // longer authoritative, so remove only the metadata.
                    ClearOwnedProcess(provider);
                    message = "Discarded stale ownership metadata after PID identity mismatch.";
                    return true;
                }

                process.Kill();
                try { process.WaitForExit(2500); } catch { }
                if (!process.HasExited)
                {
                    message = "QS3D-owned stale tunnel process did not exit after termination request.";
                    return false;
                }
                ClearOwnedProcess(provider);
                message = "Cleaned QS3D-owned stale tunnel pid=" + record.Pid.ToString() + ".";
                return true;
            }
            catch (ArgumentException)
            {
                ClearOwnedProcess(provider);
                return true;
            }
            catch (Exception ex)
            {
                // Fail closed: lack of identity/process access is not permission to kill anything.
                message = "Cannot prove/clean QS3D-owned stale tunnel process: " + ex.Message;
                return false;
            }
            finally { try { process?.Dispose(); } catch { } }
        }

        internal static void ClearOwnedProcess(McpTransportProvider provider)
        {
            try
            {
                var path = OwnedProcessPath(provider);
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
            lock (Sync)
            {
                if (_activeProvider.HasValue && _activeProvider.Value == provider) _ownedPid = null;
            }
        }

        internal static TimeSpan ComputeRestartBackoff(int attempt)
        {
            var boundedAttempt = Math.Max(1, Math.Min(attempt, 16));
            var exponent = Math.Min(boundedAttempt - 1, 10);
            var seconds = RestartBackoffBaseSeconds * (1 << exponent);
            return TimeSpan.FromSeconds(Math.Min(RestartBackoffMaxSeconds, seconds));
        }

        private static string OwnedProcessPath(McpTransportProvider provider)
        {
            return Path.Combine(SettingsDirectory, provider.ToString() + ".owner");
        }

        private static string SerializeRecord(OwnedProcessRecord record)
        {
            return "provider=" + record.Provider + "\n"
                   + "pid=" + record.Pid.ToString() + "\n"
                   + "startUtcTicks=" + record.StartUtcTicks.ToString() + "\n"
                   + "executable=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(record.Executable ?? string.Empty)) + "\n";
        }

        private static bool TryReadRecord(McpTransportProvider provider, out OwnedProcessRecord record)
        {
            record = new OwnedProcessRecord { Provider = provider };
            try
            {
                var path = OwnedProcessPath(provider);
                if (!File.Exists(path)) return false;
                foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var separator = raw.IndexOf('=');
                    if (separator <= 0) continue;
                    var key = raw.Substring(0, separator);
                    var value = raw.Substring(separator + 1);
                    if (key == "provider")
                    {
                        McpTransportProvider parsed;
                        if (!Enum.TryParse(value, true, out parsed) || parsed != provider) return false;
                        record.Provider = parsed;
                    }
                    else if (key == "pid")
                    {
                        int pid;
                        if (!int.TryParse(value, out pid) || pid <= 0) return false;
                        record.Pid = pid;
                    }
                    else if (key == "startUtcTicks")
                    {
                        long ticks;
                        if (!long.TryParse(value, out ticks) || ticks <= 0) return false;
                        record.StartUtcTicks = ticks;
                    }
                    else if (key == "executable")
                    {
                        record.Executable = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                    }
                }
                return record.Pid > 0 && record.StartUtcTicks > 0 && !string.IsNullOrWhiteSpace(record.Executable);
            }
            catch
            {
                // Corrupt metadata grants no process authority. Remove the sidecar only.
                ClearOwnedProcess(provider);
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            try { return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path.Trim()); }
            catch { return string.Empty; }
        }

        private static string Limit(string value, int maximum)
        {
            value = value ?? string.Empty;
            return value.Length <= maximum ? value : value.Substring(0, maximum) + "...";
        }

        private sealed class OwnedProcessRecord
        {
            public McpTransportProvider Provider;
            public int Pid;
            public long StartUtcTicks;
            public string Executable = string.Empty;
        }
    }
}