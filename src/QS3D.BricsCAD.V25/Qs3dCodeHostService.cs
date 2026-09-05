using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Owns one process-local QS3D Code session, capability state and named-pipe lifecycle.
    /// </summary>
    internal static class Qs3dCodeHostService
    {
        private static readonly object Gate = new object();
        private static readonly string HostInstanceId = Guid.NewGuid().ToString("N");
        private static Qs3dCodeLocalIpcServer? _server;
        private static string _sessionId = string.Empty;
        private static string _pipeName = string.Empty;
        private static string _stateFilePath = string.Empty;
        private static bool _started;

        internal static bool AutomationStopped { get { return McpCadAgentRuntime.AutomationStopped; } }

        internal static void Start()
        {
            lock (Gate)
            {
                if (_started) return;

                var processId = Process.GetCurrentProcess().Id;
                var sessionId = Guid.NewGuid().ToString("N");
                var capability = CreateCapability();
                var pipeName = CreatePipeName(processId, sessionId);
                var stateFilePath = CreateStateFilePath(processId);
                var server = new Qs3dCodeLocalIpcServer(pipeName, capability, HandleAuthenticatedRequest);

                try
                {
                    server.Start();
                    _server = server;
                    _sessionId = sessionId;
                    _pipeName = pipeName;
                    _stateFilePath = stateFilePath;
                    _started = true;
                    WriteStateFile(stateFilePath, pipeName, capability, processId, sessionId);
                    McpDiagnosticHub.Record("qs3d-code", "info", "host-start", "Authenticated local QS3D Code host bridge started.");
                }
                catch
                {
                    _started = false;
                    _server = null;
                    _sessionId = string.Empty;
                    _pipeName = string.Empty;
                    _stateFilePath = string.Empty;
                    try { server.Dispose(); } catch { }
                    TryDelete(stateFilePath);
                    throw;
                }
            }
        }

        internal static void Stop()
        {
            Qs3dCodeLocalIpcServer? server;
            string stateFilePath;
            lock (Gate)
            {
                if (!_started) return;
                _started = false;
                server = _server;
                stateFilePath = _stateFilePath;
                _server = null;
                _sessionId = string.Empty;
                _pipeName = string.Empty;
                _stateFilePath = string.Empty;
            }

            TryDelete(stateFilePath);
            try { if (server != null) server.Dispose(); } catch { }
            McpDiagnosticHub.Record("qs3d-code", "info", "host-stop", "Local QS3D Code host bridge stopped and session state was removed.");
        }

        internal static Qs3dCodeHostIdentity GetHostIdentity()
        {
            lock (Gate)
            {
                if (!_started)
                    throw new InvalidOperationException("host_unavailable: QS3D Code host service is not running.");
                return new Qs3dCodeHostIdentity(
                    HostInstanceId,
                    _sessionId,
                    Process.GetCurrentProcess().Id,
                    HostMajor());
            }
        }

        internal static void EmergencyStop()
        {
            McpCadAgentRuntime.StopAutomation();
            var stopped = McpCadAgentRuntime.AutomationStopped;
            McpDiagnosticHub.Record("qs3d-code", stopped ? "warning" : "error", "emergency-stop",
                stopped ? "QS3D Code requested the shared CAD automation emergency stop." : "Shared CAD automation stop did not latch.");
        }

        private static string HandleAuthenticatedRequest(string requestJson)
        {
            var request = new Qs3dCodeHostRequest
            {
                OperationId = McpTopLevelJson.ExtractString(requestJson, "operationId") ?? string.Empty,
                PermissionClass = McpTopLevelJson.ExtractString(requestJson, "permissionClass") ?? string.Empty,
                HostId = McpTopLevelJson.ExtractString(requestJson, "hostId") ?? string.Empty,
                SessionId = McpTopLevelJson.ExtractString(requestJson, "sessionId") ?? string.Empty,
                DrawingId = McpTopLevelJson.ExtractString(requestJson, "drawingId") ?? string.Empty,
                ArgumentsJson = McpTopLevelJson.ExtractString(requestJson, "argumentsJson") ?? string.Empty,
                WriterToken = McpTopLevelJson.ExtractString(requestJson, "writerToken") ?? string.Empty
            };
            return SerializeResult(Qs3dCodeHostBridge.Execute(request));
        }

        private static string SerializeResult(Qs3dCodeHostResult result)
        {
            if (result == null) return "{\"ok\":false,\"errorCode\":\"host_error\"}";
            var host = result.HostIdentity;
            var active = result.ActiveDocumentIdentity;
            var builder = new StringBuilder(1024)
                .Append("{\"ok\":").Append(result.Ok ? "true" : "false")
                .Append(",\"operationId\":\"").Append(Escape(result.OperationId)).Append('"');

            if (host != null)
            {
                builder.Append(",\"host\":{\"hostId\":\"").Append(Escape(host.HostId))
                    .Append("\",\"sessionId\":\"").Append(Escape(host.SessionId))
                    .Append("\",\"processId\":").Append(host.ProcessId.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"hostMajor\":\"").Append(Escape(host.HostMajor)).Append("\"}");
            }
            if (active != null)
            {
                builder.Append(",\"activeDrawing\":{\"drawingId\":\"").Append(Escape(active.DrawingId))
                    .Append("\",\"displayName\":\"").Append(Escape(active.DisplayName))
                    .Append("\",\"isNamed\":").Append(active.IsNamed ? "true" : "false").Append('}');
            }
            if (result.PayloadJson.Length != 0)
                builder.Append(",\"payloadJson\":\"").Append(Escape(Bound(result.PayloadJson, 262144))).Append('"');
            if (result.ErrorCode.Length != 0)
                builder.Append(",\"errorCode\":\"").Append(Escape(result.ErrorCode)).Append('"');
            if (result.Message.Length != 0)
                builder.Append(",\"message\":\"").Append(Escape(Bound(result.Message, 2048))).Append('"');
            return builder.Append('}').ToString();
        }

        private static void WriteStateFile(
            string path,
            string pipeName,
            string capability,
            int processId,
            string sessionId)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Could not resolve QS3D Code user-local state directory.");
            Directory.CreateDirectory(directory);
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            var json = "{\"version\":1,\"hostId\":\"" + Escape(HostInstanceId)
                       + "\",\"sessionId\":\"" + Escape(sessionId)
                       + "\",\"processId\":" + processId.ToString(CultureInfo.InvariantCulture)
                       + ",\"hostMajor\":\"" + Escape(HostMajor())
                       + "\",\"pipeName\":\"" + Escape(pipeName)
                       + "\",\"capability\":\"" + Escape(capability) + "\"}";
            File.WriteAllText(temp, json, new UTF8Encoding(false));
            try
            {
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }
            finally
            {
                TryDelete(temp);
            }
        }

        private static string CreateStateFilePath(int processId)
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root)) root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("No user-local application-data directory is available.");
            return Path.Combine(root, "QS3D", "CodeHost", "host-" + processId.ToString(CultureInfo.InvariantCulture) + ".json");
        }

        private static string CreatePipeName(int processId, string sessionId)
        {
            var userKey = Environment.UserName ?? string.Empty;
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    if (identity != null && identity.User != null) userKey = identity.User.Value;
                }
            }
            catch { }
            var suffix = HashToken(userKey + "\n" + processId.ToString(CultureInfo.InvariantCulture) + "\n" + sessionId).Substring(0, 24);
            return "qs3d-code-" + suffix;
        }

        private static string CreateCapability()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string HashToken(string? value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string HostMajor()
        {
#if BRICSCAD_V26
            return "V26";
#else
            return "V25";
#endif
        }

        private static string Escape(string? value)
        {
            var builder = new StringBuilder((value ?? string.Empty).Length + 16);
            foreach (var ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (ch < 32) builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }

        private static string Bound(string? value, int maxCharacters)
        {
            var text = value ?? string.Empty;
            return text.Length <= maxCharacters ? text : text.Substring(0, maxCharacters);
        }

        private static void TryDelete(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
