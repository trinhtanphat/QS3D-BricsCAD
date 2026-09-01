using System;
using System.Net;
using System.Threading;

namespace QS3D.BricsCAD.V25
{
    internal static class McpEmbeddedServerWatchdog
    {
        private const int ProbePeriodMilliseconds = 5000;
        private const int ProbeTimeoutMilliseconds = 1200;
        private const int FailuresBeforeRecovery = 2;
        private static readonly object Sync = new object();
        private static Timer? _timer;
        private static int _consecutiveFailures;
        private static int _probeActive;
        private static bool _stopping;

        internal static void Start()
        {
            lock (Sync)
            {
                if (_timer != null) return;
                _stopping = false;
                _consecutiveFailures = 0;
                _timer = new Timer(_ => Probe(), null, ProbePeriodMilliseconds, ProbePeriodMilliseconds);
            }
        }

        internal static void Stop()
        {
            Timer? timer;
            lock (Sync)
            {
                _stopping = true;
                timer = _timer;
                _timer = null;
                _consecutiveFailures = 0;
            }
            try { timer?.Dispose(); } catch { }
        }

        private static void Probe()
        {
            if (Interlocked.CompareExchange(ref _probeActive, 1, 0) != 0) return;
            try
            {
                lock (Sync)
                {
                    if (_stopping || _timer == null) return;
                }

                if (HealthProbeSucceeds())
                {
                    lock (Sync) _consecutiveFailures = 0;
                    return;
                }

                var shouldRecover = false;
                lock (Sync)
                {
                    if (_stopping || _timer == null) return;
                    _consecutiveFailures++;
                    shouldRecover = _consecutiveFailures >= FailuresBeforeRecovery;
                }
                if (!shouldRecover) return;

                try
                {
                    McpDiagnosticHub.Record("mcp", "warning", "embedded-listener-recovery",
                        "MCP health probe failed repeatedly; restarting embedded listener.", null);
                }
                catch { }

                try { McpEmbeddedServer.Stop(); } catch { }
                try
                {
                    McpEmbeddedServer.Start();
                    lock (Sync) _consecutiveFailures = 0;
                    try
                    {
                        McpDiagnosticHub.Record("mcp", "info", "embedded-listener-recovered",
                            "MCP embedded listener recovered on " + McpEmbeddedServer.Endpoint + ".", null);
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    lock (Sync) _consecutiveFailures = FailuresBeforeRecovery;
                    try { McpDiagnosticHub.Record("mcp", "error", "embedded-listener-recovery-failed", ex.Message, null); }
                    catch { }
                }
            }
            finally
            {
                Volatile.Write(ref _probeActive, 0);
            }
        }

        private static bool HealthProbeSucceeds()
        {
            try
            {
#pragma warning disable SYSLIB0014
                var request = WebRequest.CreateHttp(McpEmbeddedServer.HealthEndpoint);
#pragma warning restore SYSLIB0014
                request.Method = "GET";
                request.Proxy = null;
                request.KeepAlive = false;
                request.Timeout = ProbeTimeoutMilliseconds;
                request.ReadWriteTimeout = ProbeTimeoutMilliseconds;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return response.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }
    }
}
