using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Hardened embedded Streamable-HTTP MCP transport. The listener remains loopback-only;
    /// public reachability is provided by the QS3D-managed Cloudflare tunnel. CAD/UI work is
    /// delegated to McpCadAgentRuntime so the protocol/auth layer never executes arbitrary OS code.
    /// </summary>
    internal static class McpEmbeddedServer
    {
        private const int Port = 8765;
        private const int MaxHeaderBytes = 64 * 1024;
        private const int MaxBodyBytes = 1024 * 1024;
        private const int MaxConcurrentClients = 16;
        private const int MaxSessions = 128;
        private const string ProtocolVersion = "2025-06-18";
        private const string LegacyProtocolVersion = "2025-03-26";
        private const string BearerEnvironment = "QS3D_MCP_BEARER_TOKEN";
        private const string TokenFileName = "mcp-bearer-token.txt";

        private static readonly object Sync = new object();
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly SemaphoreSlim ClientSlots = new SemaphoreSlim(MaxConcurrentClients, MaxConcurrentClients);
        private static readonly ConcurrentDictionary<string, SessionState> Sessions =
            new ConcurrentDictionary<string, SessionState>(StringComparer.Ordinal);

        private static TcpListener _listener;
        private static Thread _listenerThread;
        private static volatile bool _stopping;
        private static string _bearerToken = string.Empty;
        private static string _tokenSource = string.Empty;
        private static string _lastError = string.Empty;

        public static Uri Endpoint { get { return new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/mcp"); } }
        public static Uri HealthEndpoint { get { return new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/healthz"); } }
        public static string TokenFilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", TokenFileName); } }
        public static string AuditFilePath { get { return McpCadAgentRuntime.AuditFilePath; } }
        public static bool IsRunning { get { lock (Sync) return _listener != null && !_stopping; } }
        public static string LastError { get { lock (Sync) return _lastError; } }
        public static string TokenSource { get { EnsureBearerToken(); lock (Sync) return _tokenSource; } }
        public static string PublicUrl { get { return McpPublicEndpointResolver.Resolve(); } }

        public static void Start()
        {
            lock (Sync)
            {
                if (_listener != null && !_stopping) return;
                EnsureBearerToken();
                _stopping = false;
                _lastError = string.Empty;
                McpCadAgentRuntime.ResetForServerStart();
                var listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Server.NoDelay = true;
                listener.Start(32);
                _listener = listener;
                _listenerThread = new Thread(ServeLoop) { IsBackground = true, Name = "QS3D MCP loopback server v2" };
                _listenerThread.Start();
            }
        }

        public static void EnsureStarted() { if (!IsRunning) Start(); }

        public static void Stop()
        {
            Thread thread;
            lock (Sync)
            {
                _stopping = true;
                McpCadAgentRuntime.StopAutomation();
                thread = _listenerThread;
                try { if (_listener != null) _listener.Stop(); } catch { }
                _listener = null;
                _listenerThread = null;
                Sessions.Clear();
            }
            if (thread != null && thread != Thread.CurrentThread)
            {
                try { thread.Join(1000); } catch { }
            }
        }

        public static string GetBearerToken() { EnsureBearerToken(); lock (Sync) return _bearerToken; }

        public static string Describe()
        {
            var publicUrl = PublicUrl;
            return (IsRunning ? "RUNNING" : "STOPPED")
                   + "; local=" + Endpoint
                   + "; auth=" + TokenSource
                   + "; automation=" + (McpCadAgentRuntime.AutomationStopped ? "STOPPED" : "READY")
                   + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "; public=" + publicUrl)
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; lastError=" + LastError);
        }

        private static void ServeLoop()
        {
            while (!_stopping)
            {
                TcpClient client = null;
                try
                {
                    var listener = _listener;
                    if (listener == null) return;
                    client = listener.AcceptTcpClient();
                    client.NoDelay = true;
                    if (!ClientSlots.Wait(0))
                    {
                        try { client.Close(); } catch { }
                        client = null;
                        continue;
                    }
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                    client = null;
                }
                catch (SocketException ex)
                {
                    if (_stopping) return;
                    SetLastError("socket: " + ex.Message);
                    Thread.Sleep(100);
                }
                catch (ObjectDisposedException) { return; }
                catch (Exception ex)
                {
                    if (_stopping) return;
                    SetLastError("listener: " + ex.Message);
                    Thread.Sleep(100);
                }
                finally { try { if (client != null) client.Dispose(); } catch { } }
            }
        }

        private static void HandleClient(object state)
        {
            try
            {
                using (var client = state as TcpClient)
                {
                    if (client == null) return;
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = 10000;
                        stream.WriteTimeout = 10000;
                        try
                        {
                            var request = ReadRequest(stream);
                            if (request != null) HandleRequest(stream, request);
                        }
                        catch (HttpProtocolException ex)
                        {
                            TryWriteResponse(stream, ex.StatusCode, ex.Reason,
                                "{\"error\":\"" + JsonEscape(ex.Message) + "\"}", null);
                        }
                    }
                }
            }
            catch (Exception ex) { SetLastError("request: " + ex.Message); }
            finally { ClientSlots.Release(); }
        }

        private static HttpRequest ReadRequest(NetworkStream stream)
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
                        throw new HttpProtocolException(413, "Payload Too Large", "MCP HTTP request exceeds configured bounds.");
                    headerEnd = FindHeaderEnd(accumulated.GetBuffer(), (int)accumulated.Length);
                    if (headerEnd < 0 && accumulated.Length > MaxHeaderBytes)
                        throw new HttpProtocolException(431, "Request Header Fields Too Large", "MCP HTTP headers exceed 64 KiB.");
                }

                var all = accumulated.ToArray();
                var headerText = Encoding.ASCII.GetString(all, 0, headerEnd);
                var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                var requestParts = lines.Length == 0
                    ? new string[0]
                    : lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (requestParts.Length != 3
                    || (!string.Equals(requestParts[2], "HTTP/1.1", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(requestParts[2], "HTTP/1.0", StringComparison.OrdinalIgnoreCase)))
                    throw new HttpProtocolException(400, "Bad Request", "Invalid MCP HTTP request line.");

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 1; i < lines.Length; i++)
                {
                    if (lines[i].Length == 0) continue;
                    var separator = lines[i].IndexOf(':');
                    if (separator <= 0) throw new HttpProtocolException(400, "Bad Request", "Malformed HTTP header.");
                    var name = lines[i].Substring(0, separator);
                    var value = lines[i].Substring(separator + 1).Trim();
                    if (!IsHttpFieldName(name) || value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                        throw new HttpProtocolException(400, "Bad Request", "Malformed HTTP header.");
                    if (headers.ContainsKey(name) && IsCriticalSingletonHeader(name))
                        throw new HttpProtocolException(400, "Bad Request", "Duplicate security-sensitive HTTP header.");
                    headers[name] = value;
                }

                if (headers.ContainsKey("Transfer-Encoding"))
                    throw new HttpProtocolException(400, "Bad Request", "Transfer-Encoding is not supported; use Content-Length.");

                var contentLength = 0;
                string contentLengthText;
                if (headers.TryGetValue("Content-Length", out contentLengthText))
                {
                    if (!int.TryParse(contentLengthText, NumberStyles.None, CultureInfo.InvariantCulture, out contentLength)
                        || contentLength < 0 || contentLength > MaxBodyBytes)
                        throw new HttpProtocolException(400, "Bad Request", "Invalid MCP HTTP Content-Length.");
                }

                var bodyOffset = headerEnd + 4;
                var body = new byte[contentLength];
                var available = Math.Max(0, Math.Min(contentLength, all.Length - bodyOffset));
                if (available > 0) Buffer.BlockCopy(all, bodyOffset, body, 0, available);
                var written = available;
                while (written < contentLength)
                {
                    var read = stream.Read(body, written, contentLength - written);
                    if (read <= 0) throw new HttpProtocolException(400, "Bad Request", "MCP HTTP body ended early.");
                    written += read;
                }

                string bodyText;
                try { bodyText = contentLength == 0 ? string.Empty : StrictUtf8.GetString(body); }
                catch (DecoderFallbackException)
                {
                    throw new HttpProtocolException(400, "Bad Request", "Invalid UTF-8 in MCP HTTP body.");
                }

                var path = requestParts[1].Trim();
                var query = path.IndexOf('?');
                if (query >= 0) path = path.Substring(0, query);
                return new HttpRequest(requestParts[0].Trim().ToUpperInvariant(), path, headers, bodyText);
            }
        }

        private static int FindHeaderEnd(byte[] bytes, int count)
        {
            for (var i = 0; i + 3 < count; i++)
                if (bytes[i] == 13 && bytes[i + 1] == 10 && bytes[i + 2] == 13 && bytes[i + 3] == 10) return i;
            return -1;
        }

        private static bool IsCriticalSingletonHeader(string name)
        {
            return string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Mcp-Session-Id", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "MCP-Protocol-Version", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHttpFieldName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var ch in name)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) continue;
                switch (ch)
                {
                    case '!': case '#': case '$': case '%': case '&': case '\'': case '*': case '+':
                    case '-': case '.': case '^': case '_': case '`': case '|': case '~':
                        continue;
                    default:
                        return false;
                }
            }
            return true;
        }

        private static bool IsJsonContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            var separator = contentType.IndexOf(';');
            var mediaType = (separator < 0 ? contentType : contentType.Substring(0, separator)).Trim();
            return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
        }

        private static void HandleRequest(NetworkStream stream, HttpRequest request)
        {
            if (request.Method == "GET" && string.Equals(request.Path, "/healthz", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 200, "OK", "{\"ok\":true,\"service\":\"qs3d-bricscad-mcp\",\"running\":true,\"version\":\"embedded-5\"}", null);
                return;
            }
            if (request.Method == "OPTIONS" && string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 204, "No Content", string.Empty, new Dictionary<string, string> { ["Allow"] = "POST, DELETE" });
                return;
            }
            if (request.Method == "GET" && string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"server event stream disabled; use Streamable HTTP POST\"}",
                    new Dictionary<string, string> { ["Allow"] = "POST, DELETE" });
                return;
            }
            if (!string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 404, "Not Found", "{\"error\":\"not found\"}", null);
                return;
            }
            if (!Authorize(request.Headers))
            {
                WriteResponse(stream, 401, "Unauthorized", JsonRpcError("null", -32001, "Bearer authorization required."),
                    new Dictionary<string, string> { ["WWW-Authenticate"] = "Bearer" });
                return;
            }
            if (request.Method == "DELETE")
            {
                string sessionId;
                if (!request.Headers.TryGetValue("Mcp-Session-Id", out sessionId) || string.IsNullOrWhiteSpace(sessionId))
                {
                    WriteResponse(stream, 400, "Bad Request", JsonRpcError("null", -32002, "Mcp-Session-Id is required."), null);
                    return;
                }
                SessionState removed;
                Sessions.TryRemove(sessionId, out removed);
                WriteResponse(stream, 204, "No Content", string.Empty, null);
                return;
            }
            if (request.Method != "POST")
            {
                WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"method not allowed\"}", new Dictionary<string, string> { ["Allow"] = "POST, DELETE" });
                return;
            }

            string contentType;
            if (!request.Headers.TryGetValue("Content-Type", out contentType)
                || !IsJsonContentType(contentType))
            {
                WriteResponse(stream, 415, "Unsupported Media Type", "{\"error\":\"Content-Type application/json is required\"}", null);
                return;
            }
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                WriteResponse(stream, 400, "Bad Request", JsonRpcError("null", -32700, "JSON-RPC body is required."), null);
                return;
            }

            string jsonRpc;
            string method;
            string id;
            bool hasId;
            try
            {
                jsonRpc = McpTopLevelJson.ExtractString(request.Body, "jsonrpc");
                method = McpTopLevelJson.ExtractString(request.Body, "method");
                id = McpTopLevelJson.ExtractId(request.Body);
                hasId = McpTopLevelJson.HasProperty(request.Body, "id");
            }
            catch (InvalidOperationException ex)
            {
                WriteResponse(stream, 200, "OK", JsonRpcError("null", -32600, ex.Message), null);
                return;
            }

            if (!string.Equals(jsonRpc, "2.0", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(method))
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(hasId ? id : "null", -32600, "Invalid JSON-RPC 2.0 request."), null);
                return;
            }

            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                HandleInitialize(stream, request.Body, id, hasId);
                return;
            }

            SessionState session;
            string sessionError;
            if (!TryValidateSession(request.Headers, out session, out sessionError))
            {
                WriteResponse(stream, 400, "Bad Request", JsonRpcError(hasId ? id : "null", -32002, sessionError), null);
                return;
            }

            if (string.Equals(method, "notifications/initialized", StringComparison.Ordinal)
                || string.Equals(method, "notifications/cancelled", StringComparison.Ordinal)
                || (!hasId && method.StartsWith("notifications/", StringComparison.Ordinal)))
            {
                WriteResponse(stream, 202, "Accepted", string.Empty, ProtocolHeader(session));
                return;
            }
            if (!hasId)
            {
                WriteResponse(stream, 202, "Accepted", string.Empty, ProtocolHeader(session));
                return;
            }
            if (string.Equals(method, "ping", StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{}}", ProtocolHeader(session));
                return;
            }
            if (string.Equals(method, "tools/list", StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", ToolsListResponse(id), ProtocolHeader(session));
                return;
            }
            if (string.Equals(method, "tools/call", StringComparison.Ordinal))
            {
                string toolName;
                string arguments;
                string error;
                if (!TryExtractToolCall(request.Body, out toolName, out arguments, out error))
                {
                    WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602, error), ProtocolHeader(session));
                    return;
                }
                WriteResponse(stream, 200, "OK", "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + CallTool(toolName, arguments) + "}", ProtocolHeader(session));
                return;
            }
            WriteResponse(stream, 200, "OK", JsonRpcError(id, -32601, "Method not found."), ProtocolHeader(session));
        }

        private static void HandleInitialize(NetworkStream stream, string body, string id, bool hasId)
        {
            if (!hasId)
            {
                WriteResponse(stream, 200, "OK", JsonRpcError("null", -32600, "initialize must include a JSON-RPC id."), null);
                return;
            }
            string parameters;
            if (!TryExtractObjectProperty(body, "params", out parameters))
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602, "initialize requires object params."), null);
                return;
            }
            var requested = McpTopLevelJson.ExtractString(parameters, "protocolVersion");
            if (!string.Equals(requested, ProtocolVersion, StringComparison.Ordinal)
                && !string.Equals(requested, LegacyProtocolVersion, StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602,
                    "Unsupported MCP protocolVersion. Supported: " + ProtocolVersion + ", " + LegacyProtocolVersion + "."), null);
                return;
            }
            CleanupSessions();
            if (Sessions.Count >= MaxSessions)
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(id, -32003, "MCP session capacity reached."), null);
                return;
            }
            var sessionId = Guid.NewGuid().ToString("N");
            Sessions[sessionId] = new SessionState(DateTime.UtcNow, requested);
            var result = "{\"jsonrpc\":\"2.0\",\"id\":" + id
                         + ",\"result\":{\"protocolVersion\":\"" + requested
                         + "\",\"capabilities\":{\"tools\":{\"listChanged\":false}},"
                         + "\"serverInfo\":{\"name\":\"qs3d-bricscad\",\"version\":\"embedded-5\"},"
                         + "\"instructions\":\"QS3D full CAD agent. Prefer direct CAD API tools, use bounded command workflows for advanced native features, and BricsCAD-process UI input only as fallback. All ordinary mutations require confirmMutation=true. Emergency stop/cancel remain available without confirmation.\"}}";
            WriteResponse(stream, 200, "OK", result, new Dictionary<string, string>
            {
                ["Mcp-Session-Id"] = sessionId,
                ["MCP-Protocol-Version"] = requested
            });
        }

        private static string ToolsListResponse(string id)
        {
            var tools = new List<string>
            {
                Tool("connector_info", "Return embedded MCP endpoint, protocol, public endpoint and automation state.", ""),
                Tool("qs3d_status", "Read privacy-safe BricsCAD/QS3D host status.", ""),
                Tool("cad_active_document", "Read privacy-safe active document identity without local filesystem path.", ""),
                Tool("cad_selection", "Read current implied selection handles/types/layers.", ""),
                Tool("cad_database_snapshot", "Read bounded ModelSpace entity snapshot.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1000}"),
                Tool("cad_entity_inspect", "Inspect one entity by hexadecimal handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32}", "handle"),
                Tool("cad_view_state", "Read command-active and current view/window state.", ""),
                Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "\"timeoutMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":30000}"),
                Tool("cad_sysvar", "Read one privacy-safe allowlisted BricsCAD system variable.", "\"name\":{\"type\":\"string\",\"enum\":[\"CMDACTIVE\",\"INSUNITS\",\"CLAYER\",\"CTAB\",\"TILEMODE\",\"DWGNAME\",\"CVPORT\",\"ORTHOMODE\",\"OSMODE\"]}", "name"),
                Tool("cad_create_line", "Create native Line in ModelSpace.", Numeric("x1","y1","z1","x2","y2","z2") + CommonLayerConfirm(), "x1","y1","x2","y2","confirmMutation"),
                Tool("cad_create_circle", "Create native Circle in ModelSpace.", Numeric("x","y","z","radius") + CommonLayerConfirm(), "x","y","radius","confirmMutation"),
                Tool("cad_create_arc", "Create native Arc in ModelSpace from center/radius/start/end degrees.", Numeric("x","y","z","radius","startAngleDeg","endAngleDeg") + CommonLayerConfirm(), "x","y","radius","startAngleDeg","endAngleDeg","confirmMutation"),
                Tool("cad_create_polyline", "Create native 2D Polyline; points use x,y;x,y format.", "\"points\":{\"type\":\"string\",\"maxLength\":16000},\"closed\":{\"type\":\"boolean\"},\"elevation\":{\"type\":\"number\"}" + CommonLayerConfirm(), "points","confirmMutation"),
                Tool("cad_create_text", "Create native single-line DBText.", "\"text\":{\"type\":\"string\",\"maxLength\":4000}," + Numeric("x","y","z","height","rotationDeg") + CommonLayerConfirm(), "text","x","y","height","confirmMutation"),
                Tool("cad_create_mtext", "Create native multiline MText.", "\"text\":{\"type\":\"string\",\"maxLength\":16000}," + Numeric("x","y","z","height","width","rotationDeg") + CommonLayerConfirm(), "text","x","y","height","confirmMutation"),
                Tool("cad_entity_transform", "Move, rotate or scale one entity by handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"action\":{\"type\":\"string\",\"enum\":[\"move\",\"rotate\",\"scale\"]}," + Numeric("dx","dy","dz","angleDeg","factor") + ConfirmProperty(), "handle","action","confirmMutation"),
                Tool("cad_entity_delete", "Erase one entity by handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32}," + ConfirmProperty(), "handle","confirmMutation"),
                Tool("cad_entity_set_layer", "Move one entity to a layer, creating that layer if needed.", "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"layer\":{\"type\":\"string\",\"maxLength\":255}," + ConfirmProperty(), "handle","layer","confirmMutation"),
                Tool("cad_layer", "Create layer or make layer current.", "\"action\":{\"type\":\"string\",\"enum\":[\"create\",\"set_current\"]},\"name\":{\"type\":\"string\",\"maxLength\":255}," + ConfirmProperty(), "action","name","confirmMutation"),
                Tool("cad_command_catalog", "Return allowlisted native commands available to cad_command_sequence.", ""),
                Tool("cad_command_sequence", "Run one allowlisted BricsCAD command with bounded newline-delimited prompt inputs.", "\"command\":{\"type\":\"string\",\"maxLength\":40},\"inputs\":{\"type\":\"string\",\"maxLength\":16000}," + ConfirmProperty(), "command","confirmMutation"),
                Tool("qs3d_run_command", "Run one QS3D* command name.", "\"command\":{\"type\":\"string\",\"pattern\":\"^QS3D[A-Za-z0-9_]*$\",\"maxLength\":80}," + ConfirmProperty(), "command","confirmMutation"),
                Tool("cad_ui_click", "Click inside active BricsCAD-process window only.", "\"x\":{\"type\":\"integer\"},\"y\":{\"type\":\"integer\"},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"count\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":3}," + ConfirmProperty(), "x","y","button","confirmMutation"),
                Tool("cad_ui_type", "Type bounded Unicode text into active BricsCAD-process window only.", "\"text\":{\"type\":\"string\",\"maxLength\":8000},\"pressEnter\":{\"type\":\"boolean\"}," + ConfirmProperty(), "text","confirmMutation"),
                Tool("cad_ui_key", "Press named key in active BricsCAD-process window only.", "\"key\":{\"type\":\"string\",\"maxLength\":16},\"ctrl\":{\"type\":\"boolean\"},\"alt\":{\"type\":\"boolean\"},\"shift\":{\"type\":\"boolean\"}," + ConfirmProperty(), "key","confirmMutation"),
                Tool("cad_agent_stop", "Emergency-stop mutations/UI input and deliver ESC twice; no confirmation required.", ""),
                Tool("cad_agent_resume", "Resume autonomous mutations after emergency stop.", ConfirmProperty(), "confirmMutation"),
                Tool("cad_audit_tail", "Read latest bounded local mutation audit entries.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}"),
                Tool("cad_cancel_command", "Deliver ESC twice to cancel current BricsCAD command; no confirmation required.", "")
            };
            return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{\"tools\":[" + string.Join(",", tools) + "]}}";
        }

        private static string CallTool(string tool, string arguments)
        {
            try
            {
                if (string.Equals(tool, "connector_info", StringComparison.Ordinal))
                {
                    var publicUrl = PublicUrl;
                    return ToolSuccess("{\"protocol\":\"" + ProtocolVersion + "\",\"endpoint\":\"" + JsonEscape(Endpoint.ToString())
                        + "\",\"publicUrl\":\"" + JsonEscape(publicUrl) + "\",\"auth\":\"bearer\",\"singleRepository\":true,"
                        + "\"fullCadAgent\":true,\"structuredContent\":true,\"automationStopped\":"
                        + (McpCadAgentRuntime.AutomationStopped ? "true" : "false") + "}");
                }
                return ToolSuccess(McpCadAgentRuntime.Call(tool, arguments));
            }
            catch (Exception ex) { return ToolError(ex.Message); }
        }

        private static string ToolSuccess(string jsonValue)
        {
            var raw = string.IsNullOrWhiteSpace(jsonValue) ? "{}" : jsonValue.Trim();
            if (!LooksLikeJsonValue(raw)) raw = "{\"value\":\"" + JsonEscape(raw) + "\"}";
            return "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(raw)
                   + "\"}],\"structuredContent\":{\"data\":" + raw + "},\"isError\":false}";
        }

        private static bool LooksLikeJsonValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var first = value[0];
            var last = value[value.Length - 1];
            return (first == '{' && last == '}') || (first == '[' && last == ']');
        }

        private static string ToolError(string message)
        {
            return "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(message ?? "MCP tool failed.") + "\"}],\"isError\":true}";
        }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + JsonEscape(name) + "\",\"description\":\"" + JsonEscape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + (properties ?? string.Empty)
                   + "},\"additionalProperties\":false" + requiredJson + "}}";
        }

        private static string Numeric(params string[] names)
        {
            var parts = new List<string>();
            foreach (var name in names) parts.Add("\"" + name + "\":{\"type\":\"number\"}");
            return string.Join(",", parts);
        }

        private static string CommonLayerConfirm()
        {
            return ",\"layer\":{\"type\":\"string\",\"maxLength\":255}," + ConfirmProperty();
        }

        private static string ConfirmProperty() { return "\"confirmMutation\":{\"type\":\"boolean\"}"; }

        private static bool TryExtractToolCall(string body, out string name, out string arguments, out string error)
        {
            name = string.Empty;
            arguments = "{}";
            error = string.Empty;
            try
            {
                string parameters;
                if (!TryExtractObjectProperty(body, "params", out parameters))
                {
                    error = "tools/call requires object params.";
                    return false;
                }
                name = McpTopLevelJson.ExtractString(parameters, "name").Trim();
                if (name.Length == 0 || name.Length > 128)
                {
                    error = "tools/call params.name is required and <=128 characters.";
                    return false;
                }
                string rawArguments;
                bool found;
                if (!TryFindPropertyValue(parameters, "arguments", out rawArguments, out found, out error)) return false;
                if (found)
                {
                    var candidate = rawArguments;
                    if (candidate.Length < 2 || candidate[0] != '{' || candidate[candidate.Length - 1] != '}')
                    {
                        error = "tools/call params.arguments must be an object.";
                        return false;
                    }
                    arguments = candidate;
                }
                return true;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryExtractObjectProperty(string json, string property, out string objectJson)
        {
            objectJson = string.Empty;
            string raw;
            bool found;
            string error;
            if (!TryFindPropertyValue(json, property, out raw, out found, out error)) return false;
            if (!found) return false;
            var candidate = raw;
            if (candidate.Length < 2 || candidate[0] != '{' || candidate[candidate.Length - 1] != '}') return false;
            objectJson = candidate;
            return true;
        }

        private static bool TryFindPropertyValue(string json, string property, out string raw, out bool found, out string error)
        {
            return McpTopLevelJson.TryFindPropertyValue(json, property, out raw, out found, out error);
        }

        private static bool Authorize(IDictionary<string, string> headers)
        {
            string authorization;
            if (!headers.TryGetValue("Authorization", out authorization)) return false;
            const string prefix = "Bearer ";
            if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            return ConstantTimeEquals(authorization.Substring(prefix.Length).Trim(), GetBearerToken());
        }

        private static bool TryValidateSession(IDictionary<string, string> headers, out SessionState state, out string error)
        {
            CleanupSessions();
            state = null;
            string sessionId;
            if (!headers.TryGetValue("Mcp-Session-Id", out sessionId) || string.IsNullOrWhiteSpace(sessionId))
            { error = "Mcp-Session-Id is required after initialize."; return false; }
            SessionState stored;
            if (!Sessions.TryGetValue(sessionId, out stored)) { error = "Unknown or expired MCP session."; return false; }
            if (DateTime.UtcNow - stored.LastSeenUtc > TimeSpan.FromHours(4))
            {
                SessionState ignored;
                Sessions.TryRemove(sessionId, out ignored);
                error = "MCP session expired.";
                return false;
            }
            string version;
            if (headers.TryGetValue("MCP-Protocol-Version", out version)
                && !string.IsNullOrWhiteSpace(version)
                && !string.Equals(version, stored.ProtocolVersion, StringComparison.Ordinal))
            {
                error = "MCP-Protocol-Version does not match initialized session.";
                return false;
            }
            state = new SessionState(DateTime.UtcNow, stored.ProtocolVersion);
            Sessions[sessionId] = state;
            error = string.Empty;
            return true;
        }

        private static void CleanupSessions()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(4);
            foreach (var pair in Sessions)
            {
                if (pair.Value.LastSeenUtc >= cutoff) continue;
                SessionState ignored;
                Sessions.TryRemove(pair.Key, out ignored);
            }
        }

        private static IDictionary<string, string> ProtocolHeader(SessionState state)
        {
            return new Dictionary<string, string> { ["MCP-Protocol-Version"] = state.ProtocolVersion };
        }

        private static string JsonRpcError(string id, int code, string message)
        {
            return "{\"jsonrpc\":\"2.0\",\"id\":" + (string.IsNullOrWhiteSpace(id) ? "null" : id)
                   + ",\"error\":{\"code\":" + code.ToString(CultureInfo.InvariantCulture)
                   + ",\"message\":\"" + JsonEscape(message ?? string.Empty) + "\"}}";
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string reason, string body, IDictionary<string, string> extraHeaders)
        {
            var payload = string.IsNullOrEmpty(body) ? new byte[0] : Encoding.UTF8.GetBytes(body);
            var header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(reason)
                .Append("\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\n");
            if (payload.Length > 0) header.Append("Content-Type: application/json; charset=utf-8\r\n");
            header.Append("Content-Length: ").Append(payload.Length).Append("\r\n");
            if (extraHeaders != null)
            {
                foreach (var pair in extraHeaders)
                {
                    if (pair.Key.IndexOfAny(new[] { '\r', '\n' }) >= 0 || pair.Value.IndexOfAny(new[] { '\r', '\n' }) >= 0) continue;
                    header.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
                }
            }
            header.Append("\r\n");
            var headers = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headers, 0, headers.Length);
            if (payload.Length > 0) stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private static void TryWriteResponse(NetworkStream stream, int statusCode, string reason, string body, IDictionary<string, string> extraHeaders)
        {
            try { WriteResponse(stream, statusCode, reason, body, extraHeaders); } catch { }
        }

        private static bool ConstantTimeEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var b = Encoding.UTF8.GetBytes(right ?? string.Empty);
            var difference = a.Length ^ b.Length;
            var count = Math.Max(a.Length, b.Length);
            for (var i = 0; i < count; i++) difference |= (i < a.Length ? a[i] : (byte)0) ^ (i < b.Length ? b[i] : (byte)0);
            return difference == 0;
        }

        private static void EnsureBearerToken()
        {
            lock (Sync)
            {
                if (!string.IsNullOrWhiteSpace(_bearerToken)) return;
                var environment = (Environment.GetEnvironmentVariable(BearerEnvironment) ?? string.Empty).Trim();
                if (environment.Length >= 32)
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
                        if (saved.Length >= 32)
                        {
                            _bearerToken = saved;
                            _tokenSource = "saved token file";
                            return;
                        }
                    }
                    _bearerToken = GenerateToken();
                    var directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Could not resolve MCP config directory.");
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path, _bearerToken, new UTF8Encoding(false));
                    _tokenSource = "generated token file";
                }
                catch
                {
                    _bearerToken = GenerateToken();
                    _tokenSource = "ephemeral process token";
                }
            }
        }

        private static string GenerateToken()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            var builder = new StringBuilder(64);
            foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        internal static string JsonEscape(string value)
        {
            if (value == null) return string.Empty;
            var builder = new StringBuilder(value.Length + 16);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (c < 32) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }

        private static void SetLastError(string message) { lock (Sync) _lastError = message ?? string.Empty; }

        private sealed class SessionState
        {
            public SessionState(DateTime lastSeenUtc, string protocolVersion) { LastSeenUtc = lastSeenUtc; ProtocolVersion = protocolVersion; }
            public DateTime LastSeenUtc { get; private set; }
            public string ProtocolVersion { get; private set; }
        }

        private sealed class HttpRequest
        {
            public HttpRequest(string method, string path, IDictionary<string, string> headers, string body)
            { Method = method; Path = path; Headers = headers; Body = body ?? string.Empty; }
            public string Method { get; private set; }
            public string Path { get; private set; }
            public IDictionary<string, string> Headers { get; private set; }
            public string Body { get; private set; }
        }

        private sealed class HttpProtocolException : Exception
        {
            public HttpProtocolException(int statusCode, string reason, string message) : base(message)
            { StatusCode = statusCode; Reason = reason; }
            public int StatusCode { get; private set; }
            public string Reason { get; private set; }
        }
    }
}
