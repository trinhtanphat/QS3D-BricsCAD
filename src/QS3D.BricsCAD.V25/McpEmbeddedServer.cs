using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Embedded loopback-only Streamable-HTTP MCP endpoint for ChatGPT/custom MCP clients.
    /// It runs inside the BricsCAD-hosted plugin so QS3D does not require a second repository
    /// or a development-only probe DLL. Network work stays on background threads; every CAD
    /// database/editor operation is marshalled back through ExecuteInApplicationContext.
    /// </summary>
    internal static class McpEmbeddedServer
    {
        private const int Port = 8765;
        private const int MaxHeaderBytes = 64 * 1024;
        private const int MaxBodyBytes = 1024 * 1024;
        private const int CadDispatchTimeoutMilliseconds = 10000;
        private const string ProtocolVersion = "2025-06-18";
        private const string BearerEnvironment = "QS3D_MCP_BEARER_TOKEN";
        private const string PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL";
        private const string TokenFileName = "mcp-bearer-token.txt";

        private static readonly object Sync = new object();
        private static readonly ConcurrentDictionary<string, DateTime> Sessions =
            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);
        private static TcpListener? _listener;
        private static Thread? _listenerThread;
        private static volatile bool _stopping;
        private static string _lastError = string.Empty;
        private static string _bearerToken = string.Empty;
        private static string _tokenSource = string.Empty;

        public static Uri Endpoint => new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/mcp");
        public static Uri HealthEndpoint => new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/healthz");

        public static bool IsRunning
        {
            get
            {
                lock (Sync) return _listener != null && !_stopping;
            }
        }

        public static string LastError
        {
            get
            {
                lock (Sync) return _lastError;
            }
        }

        public static string TokenFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QS3D",
            TokenFileName);

        public static string TokenSource
        {
            get
            {
                EnsureBearerToken();
                lock (Sync) return _tokenSource;
            }
        }

        public static string PublicUrl
        {
            get
            {
                var value = (Environment.GetEnvironmentVariable(PublicUrlEnvironment) ?? string.Empty).Trim();
                return value;
            }
        }

        public static void Start()
        {
            lock (Sync)
            {
                if (_listener != null && !_stopping) return;
                EnsureBearerToken();
                _stopping = false;
                _lastError = string.Empty;

                var listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Server.NoDelay = true;
                listener.Start(32);
                _listener = listener;

                _listenerThread = new Thread(ServeLoop)
                {
                    IsBackground = true,
                    Name = "QS3D MCP loopback server"
                };
                _listenerThread.Start();
            }
        }

        public static void EnsureStarted()
        {
            if (IsRunning) return;
            Start();
        }

        public static void Stop()
        {
            Thread? thread;
            lock (Sync)
            {
                _stopping = true;
                thread = _listenerThread;
                try { _listener?.Stop(); }
                catch { }
                _listener = null;
                _listenerThread = null;
                Sessions.Clear();
            }

            if (thread != null && thread != Thread.CurrentThread)
            {
                try { thread.Join(1000); }
                catch { }
            }
        }

        public static string GetBearerToken()
        {
            EnsureBearerToken();
            lock (Sync) return _bearerToken;
        }

        public static string Describe()
        {
            var state = IsRunning ? "RUNNING" : "STOPPED";
            var error = LastError;
            var publicUrl = PublicUrl;
            return state
                   + "; local=" + Endpoint
                   + "; auth=" + TokenSource
                   + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "; public=" + publicUrl)
                   + (string.IsNullOrWhiteSpace(error) ? string.Empty : "; lastError=" + error);
        }

        private static void ServeLoop()
        {
            while (!_stopping)
            {
                TcpClient? client = null;
                try
                {
                    var listener = _listener;
                    if (listener == null) return;
                    client = listener.AcceptTcpClient();
                    client.NoDelay = true;
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                    client = null;
                }
                catch (SocketException ex)
                {
                    if (_stopping) return;
                    SetLastError("socket: " + ex.Message);
                    Thread.Sleep(100);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (_stopping) return;
                    SetLastError("listener: " + ex.Message);
                    Thread.Sleep(100);
                }
                finally
                {
                    try { client?.Dispose(); }
                    catch { }
                }
            }
        }

        private static void HandleClient(object? state)
        {
            using (var client = state as TcpClient)
            {
                if (client == null) return;
                try
                {
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = 10000;
                        stream.WriteTimeout = 10000;
                        var request = ReadRequest(stream);
                        if (request == null) return;
                        HandleRequest(stream, request);
                    }
                }
                catch (Exception ex)
                {
                    SetLastError("request: " + ex.Message);
                }
            }
        }

        private static HttpRequest? ReadRequest(NetworkStream stream)
        {
            var buffer = new byte[4096];
            using (var accumulated = new MemoryStream())
            {
                var headerEnd = -1;
                while (headerEnd < 0)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0) return null;
                    accumulated.Write(buffer, 0, read);
                    if (accumulated.Length > MaxHeaderBytes + MaxBodyBytes)
                        throw new InvalidOperationException("MCP HTTP request exceeds configured bounds.");
                    headerEnd = FindHeaderEnd(accumulated.GetBuffer(), (int)accumulated.Length);
                    if (headerEnd < 0 && accumulated.Length > MaxHeaderBytes)
                        throw new InvalidOperationException("MCP HTTP headers exceed 64 KiB.");
                }

                var all = accumulated.ToArray();
                var headerText = Encoding.ASCII.GetString(all, 0, headerEnd);
                var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) throw new InvalidOperationException("Invalid MCP HTTP request line.");
                var requestParts = lines[0].Split(' ');
                if (requestParts.Length < 2) throw new InvalidOperationException("Invalid MCP HTTP request line.");

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 1; index < lines.Length; index++)
                {
                    var separator = lines[index].IndexOf(':');
                    if (separator <= 0) continue;
                    headers[lines[index].Substring(0, separator).Trim()] = lines[index].Substring(separator + 1).Trim();
                }

                var contentLength = 0;
                string lengthText;
                if (headers.TryGetValue("Content-Length", out lengthText)
                    && (!int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out contentLength)
                        || contentLength < 0
                        || contentLength > MaxBodyBytes))
                    throw new InvalidOperationException("Invalid MCP HTTP Content-Length.");

                var bodyOffset = headerEnd + 4;
                var body = new byte[contentLength];
                var available = Math.Max(0, Math.Min(contentLength, all.Length - bodyOffset));
                if (available > 0) Buffer.BlockCopy(all, bodyOffset, body, 0, available);
                var written = available;
                while (written < contentLength)
                {
                    var read = stream.Read(body, written, contentLength - written);
                    if (read <= 0) throw new EndOfStreamException("MCP HTTP body ended early.");
                    written += read;
                }

                return new HttpRequest(
                    requestParts[0].Trim().ToUpperInvariant(),
                    requestParts[1].Trim(),
                    headers,
                    contentLength == 0 ? string.Empty : Encoding.UTF8.GetString(body));
            }
        }

        private static int FindHeaderEnd(byte[] bytes, int count)
        {
            for (var index = 0; index + 3 < count; index++)
            {
                if (bytes[index] == 13 && bytes[index + 1] == 10 && bytes[index + 2] == 13 && bytes[index + 3] == 10)
                    return index;
            }
            return -1;
        }

        private static void HandleRequest(NetworkStream stream, HttpRequest request)
        {
            if (request.Method == "GET" && string.Equals(request.Path, "/healthz", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 200, "OK", "{\"ok\":true,\"service\":\"qs3d-bricscad-mcp\",\"running\":true}", null);
                return;
            }

            if (request.Method == "OPTIONS")
            {
                WriteResponse(stream, 204, "No Content", string.Empty, null);
                return;
            }

            if (request.Method == "GET" && string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"server-sent event stream not enabled; use MCP POST\"}", new Dictionary<string, string> { ["Allow"] = "POST" });
                return;
            }

            if (request.Method != "POST" || !string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 404, "Not Found", "{\"error\":\"not found\"}", null);
                return;
            }

            if (!Authorize(request.Headers))
            {
                WriteResponse(
                    stream,
                    401,
                    "Unauthorized",
                    "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32001,\"message\":\"Bearer authorization required.\"}}",
                    new Dictionary<string, string> { ["WWW-Authenticate"] = "Bearer" });
                return;
            }

            var method = ExtractString(request.Body, "method");
            var id = ExtractId(request.Body);

            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                var requested = ExtractString(request.Body, "protocolVersion");
                var selected = string.Equals(requested, "2025-03-26", StringComparison.Ordinal)
                    ? "2025-03-26"
                    : ProtocolVersion;
                var sessionId = Guid.NewGuid().ToString("N");
                Sessions[sessionId] = DateTime.UtcNow;
                var response =
                    "{\"jsonrpc\":\"2.0\",\"id\":" + id
                    + ",\"result\":{\"protocolVersion\":\"" + selected
                    + "\",\"capabilities\":{\"tools\":{\"listChanged\":false}},"
                    + "\"serverInfo\":{\"name\":\"qs3d-bricscad\",\"version\":\"embedded-1\"},"
                    + "\"instructions\":\"QS3D embedded BricsCAD MCP. Read tools are direct CAD snapshots; qs3d_run_command accepts only QS3D command names and requires confirmMutation=true.\"}}";
                WriteResponse(
                    stream,
                    200,
                    "OK",
                    response,
                    new Dictionary<string, string>
                    {
                        ["Mcp-Session-Id"] = sessionId,
                        ["MCP-Protocol-Version"] = selected
                    });
                return;
            }

            string sessionError;
            if (!TryValidateSession(request.Headers, out sessionError))
            {
                WriteResponse(
                    stream,
                    400,
                    "Bad Request",
                    JsonRpcError(id, -32002, sessionError),
                    null);
                return;
            }

            if (string.Equals(method, "notifications/initialized", StringComparison.Ordinal))
            {
                WriteResponse(stream, 202, "Accepted", string.Empty, null);
                return;
            }

            if (string.Equals(method, "ping", StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{}}", null);
                return;
            }

            if (string.Equals(method, "tools/list", StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", ToolsListResponse(id), null);
                return;
            }

            if (string.Equals(method, "tools/call", StringComparison.Ordinal))
            {
                var toolName = ExtractToolName(request.Body);
                var result = CallTool(toolName, request.Body);
                WriteResponse(
                    stream,
                    200,
                    "OK",
                    "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + result + "}",
                    null);
                return;
            }

            WriteResponse(stream, 200, "OK", JsonRpcError(id, -32601, "Method not found."), null);
        }

        private static bool Authorize(IDictionary<string, string> headers)
        {
            string authorization;
            if (!headers.TryGetValue("Authorization", out authorization)) return false;
            const string prefix = "Bearer ";
            if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            var supplied = authorization.Substring(prefix.Length).Trim();
            var expected = GetBearerToken();
            return ConstantTimeEquals(supplied, expected);
        }

        private static bool TryValidateSession(IDictionary<string, string> headers, out string error)
        {
            CleanupSessions();
            string sessionId;
            if (!headers.TryGetValue("Mcp-Session-Id", out sessionId) || string.IsNullOrWhiteSpace(sessionId))
            {
                error = "Mcp-Session-Id is required after initialize.";
                return false;
            }

            DateTime lastSeen;
            if (!Sessions.TryGetValue(sessionId, out lastSeen))
            {
                error = "Unknown or expired MCP session.";
                return false;
            }

            if (DateTime.UtcNow - lastSeen > TimeSpan.FromHours(4))
            {
                DateTime ignored;
                Sessions.TryRemove(sessionId, out ignored);
                error = "MCP session expired.";
                return false;
            }

            Sessions[sessionId] = DateTime.UtcNow;
            error = string.Empty;
            return true;
        }

        private static void CleanupSessions()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(4);
            foreach (var pair in Sessions)
            {
                if (pair.Value >= cutoff) continue;
                DateTime ignored;
                Sessions.TryRemove(pair.Key, out ignored);
            }
        }

        private static string ToolsListResponse(string id)
        {
            return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{\"tools\":["
                   + Tool("connector_info", "Return embedded MCP endpoint, protocol and authentication state.", "{}")
                   + "," + Tool("qs3d_status", "Read BricsCAD/QS3D host status and active document.", "{}")
                   + "," + Tool("cad_active_document", "Read active BricsCAD document identity.", "{}")
                   + "," + Tool("cad_selection", "Read current implied selection handles, types and layers.", "{}")
                   + "," + Tool(
                       "cad_database_snapshot",
                       "Read a bounded ModelSpace entity snapshot.",
                       "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1000}")
                   + "," + Tool(
                       "qs3d_run_command",
                       "Start one allowlisted QS3D command in the active BricsCAD document. Interactive commands may continue prompting in BricsCAD.",
                       "\"command\":{\"type\":\"string\",\"pattern\":\"^QS3D[A-Za-z0-9_]*$\",\"maxLength\":80},"
                       + "\"confirmMutation\":{\"type\":\"boolean\"}",
                       "\"command\"","\"confirmMutation\"")
                   + "," + Tool("cad_cancel_command", "Send two ESC characters to cancel the current CAD command.", "{}")
                   + "]}}";
        }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[" + string.Join(",", required) + "]";
            var propertyJson = properties == "{}" ? string.Empty : properties;
            return "{\"name\":\"" + JsonEscape(name)
                   + "\",\"description\":\"" + JsonEscape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{"
                   + propertyJson
                   + "},\"additionalProperties\":false" + requiredJson + "}}";
        }

        private static string CallTool(string toolName, string requestBody)
        {
            try
            {
                switch (toolName)
                {
                    case "connector_info":
                        return ToolSuccess(
                            "{\"protocol\":\"" + ProtocolVersion
                            + "\",\"endpoint\":\"" + JsonEscape(Endpoint.ToString())
                            + "\",\"publicUrl\":\"" + JsonEscape(PublicUrl)
                            + "\",\"auth\":\"bearer\",\"tokenSource\":\"" + JsonEscape(TokenSource)
                            + "\",\"singleRepository\":true}");

                    case "qs3d_status":
                        return ToolSuccess(InvokeCad(BuildStatusJson));

                    case "cad_active_document":
                        return ToolSuccess(InvokeCad(BuildActiveDocumentJson));

                    case "cad_selection":
                        return ToolSuccess(InvokeCad(BuildSelectionJson));

                    case "cad_database_snapshot":
                        var limit = ExtractInteger(requestBody, "limit", 250, 1, 1000);
                        return ToolSuccess(InvokeCad(() => BuildDatabaseSnapshotJson(limit)));

                    case "qs3d_run_command":
                        var command = ExtractString(requestBody, "command").Trim();
                        var confirmed = ExtractBoolean(requestBody, "confirmMutation");
                        if (!confirmed)
                            return ToolError("confirmMutation=true is required before starting a QS3D command.");
                        if (command.Length == 0 || command.Length > 80
                            || !Regex.IsMatch(command, "^QS3D[A-Za-z0-9_]*$", RegexOptions.CultureInvariant))
                            return ToolError("Only one QS3D command name matching ^QS3D[A-Za-z0-9_]*$ is allowed.");
                        return ToolSuccess(InvokeCad(() =>
                        {
                            var document = RequireDocument();
                            document.SendStringToExecute(command + "\n", true, false, true);
                            return "{\"accepted\":true,\"command\":\"" + JsonEscape(command.ToUpperInvariant()) + "\"}";
                        }));

                    case "cad_cancel_command":
                        return ToolSuccess(InvokeCad(() =>
                        {
                            var document = RequireDocument();
                            document.SendStringToExecute("\u001b\u001b", true, false, true);
                            return "{\"accepted\":true,\"escapeCount\":2}";
                        }));

                    default:
                        return ToolError("Unknown MCP tool: " + toolName);
                }
            }
            catch (Exception ex)
            {
                return ToolError(ex.Message);
            }
        }

        private static string BuildStatusJson()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            return "{\"product\":\"QS3D-BricsCAD\",\"processId\":"
                   + System.Diagnostics.Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)
                   + ",\"bricscadVersion\":\"" + JsonEscape(Convert.ToString(Application.Version) ?? string.Empty)
                   + "\",\"activeDocument\":\"" + JsonEscape(document?.Name ?? string.Empty)
                   + "\",\"mcpProtocol\":\"" + ProtocolVersion + "\"}";
        }

        private static string BuildActiveDocumentJson()
        {
            var document = RequireDocument();
            return "{\"name\":\"" + JsonEscape(document.Name)
                   + "\",\"fileName\":\"" + JsonEscape(document.Database.Filename ?? string.Empty)
                   + "\",\"databaseHandleSeed\":\"" + JsonEscape(document.Database.Handseed.ToString())
                   + "\"}";
        }

        private static string BuildSelectionJson()
        {
            var document = RequireDocument();
            var result = document.Editor.SelectImplied();
            if (result.Status != PromptStatus.OK || result.Value == null) return "[]";

            var output = new StringBuilder("[");
            var first = true;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in result.Value.GetObjectIds())
                {
                    if (!first) output.Append(',');
                    first = false;
                    output.Append(DescribeEntity(transaction, id, false));
                }
            }
            output.Append(']');
            return output.ToString();
        }

        private static string BuildDatabaseSnapshotJson(int limit)
        {
            var document = RequireDocument();
            var output = new StringBuilder();
            output.Append("{\"limit\":").Append(limit.ToString(CultureInfo.InvariantCulture)).Append(",\"entities\":[");
            var count = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in modelSpace)
                {
                    if (count >= limit) break;
                    if (count > 0) output.Append(',');
                    output.Append(DescribeEntity(transaction, id, true));
                    count++;
                }
            }
            output.Append("],\"truncated\":").Append(count >= limit ? "true" : "false").Append('}');
            return output.ToString();
        }

        private static string DescribeEntity(Transaction transaction, ObjectId id, bool includeExtents)
        {
            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            var output = new StringBuilder();
            output.Append("{\"handle\":\"").Append(JsonEscape(id.Handle.ToString()))
                .Append("\",\"type\":\"").Append(JsonEscape(entity == null ? string.Empty : entity.GetType().Name))
                .Append("\",\"layer\":\"").Append(JsonEscape(entity == null ? string.Empty : entity.Layer)).Append('"');

            if (entity != null && includeExtents)
            {
                output.Append(",\"extents\":");
                try { output.Append(ExtentsJson(entity.GeometricExtents)); }
                catch { output.Append("null"); }
            }

            output.Append('}');
            return output.ToString();
        }

        private static string ExtentsJson(Extents3d extents)
        {
            return "{\"min\":" + PointJson(extents.MinPoint) + ",\"max\":" + PointJson(extents.MaxPoint) + "}";
        }

        private static string PointJson(Point3d point)
        {
            return "{\"x\":" + Number(point.X)
                   + ",\"y\":" + Number(point.Y)
                   + ",\"z\":" + Number(point.Z) + "}";
        }

        private static string Number(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? "null"
                : value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document.");
            return document;
        }

        private sealed class CadWorkItem
        {
            public Func<string>? Action;
            public string Result = string.Empty;
            public Exception? Error;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }

        private static string InvokeCad(Func<string> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var item = new CadWorkItem { Action = action };
            Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadWork, item);
            if (!item.Done.Wait(CadDispatchTimeoutMilliseconds))
                throw new TimeoutException("Timed out waiting for the BricsCAD application context.");
            if (item.Error != null) throw new InvalidOperationException(item.Error.Message, item.Error);
            return item.Result;
        }

        private static void ExecuteCadWork(object data)
        {
            var item = (CadWorkItem)data;
            try { item.Result = item.Action == null ? string.Empty : item.Action(); }
            catch (Exception ex) { item.Error = ex; }
            finally { item.Done.Set(); }
        }

        private static string ToolSuccess(string jsonValue)
        {
            var text = string.IsNullOrWhiteSpace(jsonValue) ? "{}" : jsonValue;
            return "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(text) + "\"}],\"isError\":false}";
        }

        private static string ToolError(string message)
        {
            return "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(message ?? "MCP tool failed.") + "\"}],\"isError\":true}";
        }

        private static string JsonRpcError(string id, int code, string message)
        {
            return "{\"jsonrpc\":\"2.0\",\"id\":" + id
                   + ",\"error\":{\"code\":" + code.ToString(CultureInfo.InvariantCulture)
                   + ",\"message\":\"" + JsonEscape(message) + "\"}}";
        }

        private static string ExtractToolName(string json)
        {
            var source = json ?? string.Empty;
            var paramsIndex = source.IndexOf("\"params\"", StringComparison.OrdinalIgnoreCase);
            return ExtractString(paramsIndex >= 0 ? source.Substring(paramsIndex) : source, "name");
        }

        private static string ExtractString(string json, string property)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(property) + "\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) return string.Empty;
            return JsonUnescape(match.Groups["value"].Value);
        }

        private static bool ExtractBoolean(string json, string property)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(property) + "\"\\s*:\\s*(?<value>true|false)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success && string.Equals(match.Groups["value"].Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static int ExtractInteger(string json, string property, int fallback, int minimum, int maximum)
        {
            if (string.IsNullOrWhiteSpace(json)) return fallback;
            var match = Regex.Match(
                json,
                "\"" + Regex.Escape(property) + "\"\\s*:\\s*(?<value>-?[0-9]+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int value;
            if (!match.Success || !int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return fallback;
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static string ExtractId(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "null";
            var match = Regex.Match(
                json,
                "\"id\"\\s*:\\s*(?<value>\"(?:\\\\.|[^\"])*\"|-?[0-9]+(?:\\.[0-9]+)?|null)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success ? match.Groups["value"].Value : "null";
        }

        private static string JsonUnescape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return Regex.Unescape(value);
        }

        internal static string JsonEscape(string value)
        {
            if (value == null) return string.Empty;
            var builder = new StringBuilder(value.Length + 16);
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (character < 32)
                            builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }

        private static void WriteResponse(
            NetworkStream stream,
            int statusCode,
            string reason,
            string body,
            IDictionary<string, string>? extraHeaders)
        {
            var payload = string.IsNullOrEmpty(body) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
            var header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(reason).Append("\r\n");
            header.Append("Connection: close\r\n");
            header.Append("Cache-Control: no-store\r\n");
            header.Append("Content-Type: application/json; charset=utf-8\r\n");
            header.Append("Content-Length: ").Append(payload.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            if (extraHeaders != null)
            {
                foreach (var pair in extraHeaders)
                    header.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
            }
            header.Append("\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (payload.Length > 0) stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private static bool ConstantTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);
            var difference = leftBytes.Length ^ rightBytes.Length;
            var count = Math.Max(leftBytes.Length, rightBytes.Length);
            for (var index = 0; index < count; index++)
            {
                var a = index < leftBytes.Length ? leftBytes[index] : (byte)0;
                var b = index < rightBytes.Length ? rightBytes[index] : (byte)0;
                difference |= a ^ b;
            }
            return difference == 0;
        }

        private static void EnsureBearerToken()
        {
            lock (Sync)
            {
                if (!string.IsNullOrWhiteSpace(_bearerToken)) return;

                var environment = (Environment.GetEnvironmentVariable(BearerEnvironment) ?? string.Empty).Trim();
                if (environment.Length >= 16)
                {
                    _bearerToken = environment;
                    _tokenSource = "environment " + BearerEnvironment;
                    return;
                }

                var path = TokenFilePath;
                try
                {
                    if (File.Exists(path))
                    {
                        var saved = File.ReadAllText(path, Encoding.UTF8).Trim();
                        if (saved.Length >= 16)
                        {
                            _bearerToken = saved;
                            _tokenSource = "saved token file";
                            return;
                        }
                    }

                    var bytes = new byte[32];
                    using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                    var token = new StringBuilder(64);
                    foreach (var value in bytes) token.Append(value.ToString("x2", CultureInfo.InvariantCulture));

                    var directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory))
                        throw new InvalidOperationException("Could not resolve the MCP configuration directory.");
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path, token.ToString(), new UTF8Encoding(false));
                    _bearerToken = token.ToString();
                    _tokenSource = "generated token file";
                }
                catch
                {
                    var bytes = new byte[32];
                    using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                    var token = new StringBuilder(64);
                    foreach (var value in bytes) token.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                    _bearerToken = token.ToString();
                    _tokenSource = "ephemeral process token";
                }
            }
        }

        private static void SetLastError(string message)
        {
            lock (Sync) _lastError = message ?? string.Empty;
        }

        private sealed class HttpRequest
        {
            public HttpRequest(string method, string path, IDictionary<string, string> headers, string body)
            {
                Method = method;
                Path = path;
                Headers = headers;
                Body = body ?? string.Empty;
            }

            public string Method { get; }
            public string Path { get; }
            public IDictionary<string, string> Headers { get; }
            public string Body { get; }
        }
    }
}
