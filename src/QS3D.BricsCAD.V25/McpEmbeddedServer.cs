using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
    /// Network work stays off the CAD thread. Database/editor work is marshalled through
    /// ExecuteInApplicationContext; mutating agent tools are confirmation-gated and audited.
    /// </summary>
    internal static class McpEmbeddedServer
    {
        private const int Port = 8765;
        private const int MaxHeaderBytes = 64 * 1024;
        private const int MaxBodyBytes = 1024 * 1024;
        private const int CadDispatchTimeoutMilliseconds = 15000;
        private const int CadWorkQueued = 0;
        private const int CadWorkRunning = 1;
        private const int CadWorkCancelledBeforeStart = 2;
        private const int MaxConcurrentClients = 16;
        private const int MaxSessions = 128;
        private const string ProtocolVersion = "2025-06-18";
        private const string LegacyProtocolVersion = "2025-03-26";
        private const string BearerEnvironment = "QS3D_MCP_BEARER_TOKEN";
        private const string PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL";
        private const string TokenFileName = "mcp-bearer-token.txt";
        private const string AuditFileName = "mcp-agent-audit.jsonl";
        private const long MaxAuditBytes = 4L * 1024L * 1024L;

        private static readonly object Sync = new object();
        private static readonly object AuditSync = new object();
        private static readonly SemaphoreSlim ClientSlots = new SemaphoreSlim(MaxConcurrentClients, MaxConcurrentClients);
        private static readonly ConcurrentDictionary<string, SessionState> Sessions =
            new ConcurrentDictionary<string, SessionState>(StringComparer.Ordinal);
        private static readonly HashSet<string> AllowedCadCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LINE", "PLINE", "3DPOLY", "CIRCLE", "ARC", "RECTANG", "POLYGON", "ELLIPSE", "SPLINE", "POINT",
            "HATCH", "-HATCH", "BOUNDARY", "REGION", "BOX", "CYLINDER", "SPHERE", "CONE", "WEDGE", "TORUS",
            "EXTRUDE", "PRESSPULL", "REVOLVE", "SWEEP", "LOFT", "UNION", "SUBTRACT", "INTERSECT", "SLICE",
            "MOVE", "COPY", "ROTATE", "SCALE", "MIRROR", "OFFSET", "TRIM", "EXTEND", "FILLET", "CHAMFER",
            "STRETCH", "ARRAY", "ERASE", "EXPLODE", "JOIN", "PEDIT", "MATCHPROP", "CHPROP", "PROPERTIES",
            "LAYER", "-LAYER", "LINETYPE", "-LINETYPE", "COLOR", "STYLE", "-STYLE", "TEXT", "DTEXT", "MTEXT",
            "DIM", "DIMLINEAR", "DIMALIGNED", "DIMANGULAR", "DIMRADIUS", "DIMDIAMETER", "DIMSTYLE", "-DIMSTYLE",
            "LEADER", "MLEADER", "BLOCK", "-BLOCK", "WBLOCK", "INSERT", "-INSERT", "XREF", "-XREF", "IMAGEATTACH",
            "LAYOUT", "-LAYOUT", "MVIEW", "MSPACE", "PSPACE", "PLOT", "-PLOT", "PAGESETUP", "ZOOM", "PAN",
            "REGEN", "REGENALL", "UCS", "PLAN", "VPOINT", "VIEW", "-VIEW", "SELECT", "QSELECT", "ISOLATEOBJECTS",
            "UNISOLATEOBJECTS", "UNDO", "REDO", "QSAVE", "SAVEAS", "OPEN", "NEW", "CLOSE", "PURGE", "-PURGE",
            "AUDIT", "OVERKILL"
        };
        private static readonly HashSet<string> NoInputCadCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "REGEN", "REGENALL", "QSAVE", "REDO", "UNISOLATEOBJECTS"
        };

        private static TcpListener? _listener;
        private static Thread? _listenerThread;
        private static volatile bool _stopping;
        private static volatile bool _automationStopped;
        private static string _lastError = string.Empty;
        private static string _bearerToken = string.Empty;
        private static string _tokenSource = string.Empty;

        public static Uri Endpoint => new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/mcp");
        public static Uri HealthEndpoint => new Uri("http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture) + "/healthz");
        public static string TokenFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", TokenFileName);
        public static string AuditFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", AuditFileName);
        public static bool IsRunning { get { lock (Sync) return _listener != null && !_stopping; } }
        public static string LastError { get { lock (Sync) return _lastError; } }
        public static string TokenSource { get { EnsureBearerToken(); lock (Sync) return _tokenSource; } }
        public static string PublicUrl => (Environment.GetEnvironmentVariable(PublicUrlEnvironment) ?? string.Empty).Trim();

        public static void Start()
        {
            lock (Sync)
            {
                if (_listener != null && !_stopping) return;
                EnsureBearerToken();
                _stopping = false;
                _automationStopped = false;
                _lastError = string.Empty;
                var listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Server.NoDelay = true;
                listener.Start(32);
                _listener = listener;
                _listenerThread = new Thread(ServeLoop) { IsBackground = true, Name = "QS3D MCP loopback server" };
                _listenerThread.Start();
            }
        }

        public static void EnsureStarted() { if (!IsRunning) Start(); }

        public static void Stop()
        {
            Thread? thread;
            lock (Sync)
            {
                _stopping = true;
                _automationStopped = true;
                thread = _listenerThread;
                try { _listener?.Stop(); } catch { }
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
            return (IsRunning ? "RUNNING" : "STOPPED")
                   + "; local=" + Endpoint
                   + "; auth=" + TokenSource
                   + "; automation=" + (_automationStopped ? "STOPPED" : "READY")
                   + (string.IsNullOrWhiteSpace(PublicUrl) ? string.Empty : "; public=" + PublicUrl)
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; lastError=" + LastError);
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
                finally { try { client?.Dispose(); } catch { } }
            }
        }

        private static void HandleClient(object? state)
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
                            WriteResponse(stream, ex.StatusCode, ex.Reason, "{\"error\":\"" + JsonEscape(ex.Message) + "\"}", null);
                        }
                    }
                }
            }
            catch (Exception ex) { SetLastError("request: " + ex.Message); }
            finally { ClientSlots.Release(); }
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
                        throw new HttpProtocolException(413, "Payload Too Large", "MCP HTTP request exceeds configured bounds.");
                    headerEnd = FindHeaderEnd(accumulated.GetBuffer(), (int)accumulated.Length);
                    if (headerEnd < 0 && accumulated.Length > MaxHeaderBytes)
                        throw new HttpProtocolException(431, "Request Header Fields Too Large", "MCP HTTP headers exceed 64 KiB.");
                }

                var all = accumulated.ToArray();
                var headerText = Encoding.ASCII.GetString(all, 0, headerEnd);
                var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                var requestParts = lines.Length == 0 ? Array.Empty<string>() : lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (requestParts.Length != 3 || (!string.Equals(requestParts[2], "HTTP/1.1", StringComparison.OrdinalIgnoreCase)
                                                 && !string.Equals(requestParts[2], "HTTP/1.0", StringComparison.OrdinalIgnoreCase)))
                    throw new HttpProtocolException(400, "Bad Request", "Invalid MCP HTTP request line.");

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 1; i < lines.Length; i++)
                {
                    if (lines[i].Length == 0) continue;
                    var separator = lines[i].IndexOf(':');
                    if (separator <= 0) throw new HttpProtocolException(400, "Bad Request", "Malformed HTTP header.");
                    var name = lines[i].Substring(0, separator).Trim();
                    var value = lines[i].Substring(separator + 1).Trim();
                    if (name.Length == 0 || value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                        throw new HttpProtocolException(400, "Bad Request", "Malformed HTTP header.");
                    if (headers.ContainsKey(name) && IsCriticalSingletonHeader(name))
                        throw new HttpProtocolException(400, "Bad Request", "Duplicate security-sensitive HTTP header.");
                    headers[name] = value;
                }

                string transferEncoding;
                if (headers.TryGetValue("Transfer-Encoding", out transferEncoding) && !string.IsNullOrWhiteSpace(transferEncoding))
                    throw new HttpProtocolException(400, "Bad Request", "Transfer-Encoding is not supported; use Content-Length.");

                var contentLength = 0;
                string lengthText;
                if (headers.TryGetValue("Content-Length", out lengthText)
                    && (!int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out contentLength)
                        || contentLength < 0 || contentLength > MaxBodyBytes))
                    throw new HttpProtocolException(400, "Bad Request", "Invalid MCP HTTP Content-Length.");

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

                var path = requestParts[1].Trim();
                var query = path.IndexOf('?');
                if (query >= 0) path = path.Substring(0, query);
                return new HttpRequest(requestParts[0].Trim().ToUpperInvariant(), path, headers,
                    contentLength == 0 ? string.Empty : Encoding.UTF8.GetString(body));
            }
        }

        private static bool IsCriticalSingletonHeader(string name)
        {
            return string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Mcp-Session-Id", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "MCP-Protocol-Version", StringComparison.OrdinalIgnoreCase);
        }

        private static int FindHeaderEnd(byte[] bytes, int count)
        {
            for (var i = 0; i + 3 < count; i++)
                if (bytes[i] == 13 && bytes[i + 1] == 10 && bytes[i + 2] == 13 && bytes[i + 3] == 10) return i;
            return -1;
        }

        private static void HandleRequest(NetworkStream stream, HttpRequest request)
        {
            if (request.Method == "GET" && string.Equals(request.Path, "/healthz", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 200, "OK", "{\"ok\":true,\"service\":\"qs3d-bricscad-mcp\",\"running\":true}", null);
                return;
            }
            if (request.Method == "OPTIONS" && string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 204, "No Content", string.Empty, new Dictionary<string, string> { ["Allow"] = "POST, DELETE" });
                return;
            }
            if (request.Method == "GET" && string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"server-sent event stream is not enabled; use MCP POST\"}",
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
                || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 415, "Unsupported Media Type", "{\"error\":\"Content-Type application/json is required\"}", null);
                return;
            }
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                WriteResponse(stream, 400, "Bad Request", JsonRpcError("null", -32700, "JSON-RPC body is required."), null);
                return;
            }

            var jsonRpc = ExtractTopLevelString(request.Body, "jsonrpc");
            var method = ExtractTopLevelString(request.Body, "method");
            var id = ExtractTopLevelId(request.Body);
            var hasId = HasTopLevelProperty(request.Body, "id");
            if (!string.Equals(jsonRpc, "2.0", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(method))
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(hasId ? id : "null", -32600, "Invalid JSON-RPC 2.0 request."), null);
                return;
            }

            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                if (!hasId)
                {
                    WriteResponse(stream, 200, "OK", JsonRpcError("null", -32600, "initialize must include a JSON-RPC id."), null);
                    return;
                }
                string initializeParameters;
                if (!TryExtractObjectProperty(request.Body, "params", out initializeParameters))
                {
                    WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602, "initialize requires an object params value."), null);
                    return;
                }
                var requested = ExtractTopLevelString(initializeParameters, "protocolVersion");
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
                    WriteResponse(stream, 200, "OK", JsonRpcError(id, -32003, "MCP session capacity reached; close or expire an existing session."), null);
                    return;
                }
                var sessionId = Guid.NewGuid().ToString("N");
                Sessions[sessionId] = new SessionState(DateTime.UtcNow, requested);
                var response = "{\"jsonrpc\":\"2.0\",\"id\":" + id
                               + ",\"result\":{\"protocolVersion\":\"" + requested
                               + "\",\"capabilities\":{\"tools\":{\"listChanged\":false}},"
                               + "\"serverInfo\":{\"name\":\"qs3d-bricscad\",\"version\":\"embedded-4\"},"
                               + "\"instructions\":\"QS3D embedded BricsCAD MCP. Prefer direct CAD API tools. Use cad_command_sequence only for allowlisted command-line workflows and BricsCAD-window UI tools only as a fallback. Every ordinary mutation requires confirmMutation=true; emergency stop/cancel stay available without confirmation.\"}}";
                WriteResponse(stream, 200, "OK", response, new Dictionary<string, string>
                {
                    ["Mcp-Session-Id"] = sessionId,
                    ["MCP-Protocol-Version"] = requested
                });
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
                string toolParseError;
                if (!TryExtractToolCall(request.Body, out toolName, out arguments, out toolParseError))
                {
                    WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602, toolParseError), ProtocolHeader(session));
                    return;
                }
                var result = CallTool(toolName, arguments);
                WriteResponse(stream, 200, "OK", "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + result + "}", ProtocolHeader(session));
                return;
            }
            WriteResponse(stream, 200, "OK", JsonRpcError(id, -32601, "Method not found."), ProtocolHeader(session));
        }

        private static IDictionary<string, string> ProtocolHeader(SessionState session)
        {
            return new Dictionary<string, string> { ["MCP-Protocol-Version"] = session.ProtocolVersion };
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
            state = new SessionState(DateTime.MinValue, ProtocolVersion);
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
                error = "MCP-Protocol-Version does not match the initialized session.";
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

        private static string ToolsListResponse(string id)
        {
            return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{\"tools\":["
                   + Tool("connector_info", "Return embedded MCP endpoint, protocol, authentication and automation state.", "{}")
                   + "," + Tool("qs3d_status", "Read BricsCAD/QS3D host status and active document.", "{}")
                   + "," + Tool("cad_active_document", "Read active BricsCAD document identity.", "{}")
                   + "," + Tool("cad_selection", "Read current implied selection handles, types and layers.", "{}")
                   + "," + Tool("cad_database_snapshot", "Read a bounded ModelSpace entity snapshot.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1000}")
                   + "," + Tool("cad_entity_inspect", "Read one entity by hexadecimal handle including type, layer and extents.", "\"handle\":{\"type\":\"string\",\"maxLength\":32}", Required("handle"))
                   + "," + Tool("cad_view_state", "Read command-active state, current view center/size and active BricsCAD window size.", "{}")
                   + "," + Tool("cad_wait_idle", "Wait until BricsCAD reports CMDACTIVE=0 or timeout.", "\"timeoutMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":30000}")
                   + "," + Tool("cad_create_line", "Create one native Line in ModelSpace.", NumericProperties("x1","y1","z1","x2","y2","z2") + ",\"layer\":{\"type\":\"string\",\"maxLength\":255},\"confirmMutation\":{\"type\":\"boolean\"}", Required("x1","y1","x2","y2","confirmMutation"))
                   + "," + Tool("cad_create_circle", "Create one native Circle in ModelSpace.", NumericProperties("x","y","z","radius") + ",\"layer\":{\"type\":\"string\",\"maxLength\":255},\"confirmMutation\":{\"type\":\"boolean\"}", Required("x","y","radius","confirmMutation"))
                   + "," + Tool("cad_create_polyline", "Create a native 2D Polyline. points format: x,y;x,y;...", "\"points\":{\"type\":\"string\",\"maxLength\":16000},\"closed\":{\"type\":\"boolean\"},\"elevation\":{\"type\":\"number\"},\"layer\":{\"type\":\"string\",\"maxLength\":255},\"confirmMutation\":{\"type\":\"boolean\"}", Required("points","confirmMutation"))
                   + "," + Tool("cad_create_text", "Create native single-line DBText in ModelSpace.", "\"text\":{\"type\":\"string\",\"maxLength\":4000}," + NumericProperties("x","y","z","height","rotationDeg") + ",\"layer\":{\"type\":\"string\",\"maxLength\":255},\"confirmMutation\":{\"type\":\"boolean\"}", Required("text","x","y","height","confirmMutation"))
                   + "," + Tool("cad_entity_transform", "Move, rotate or scale one entity by handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"action\":{\"type\":\"string\",\"enum\":[\"move\",\"rotate\",\"scale\"]}," + NumericProperties("dx","dy","dz","angleDeg","factor") + ",\"confirmMutation\":{\"type\":\"boolean\"}", Required("handle","action","confirmMutation"))
                   + "," + Tool("cad_entity_delete", "Erase one entity by handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"confirmMutation\":{\"type\":\"boolean\"}", Required("handle","confirmMutation"))
                   + "," + Tool("cad_layer", "Create a layer or make it current.", "\"action\":{\"type\":\"string\",\"enum\":[\"create\",\"set_current\"]},\"name\":{\"type\":\"string\",\"maxLength\":255},\"confirmMutation\":{\"type\":\"boolean\"}", Required("action","name","confirmMutation"))
                   + "," + Tool("cad_command_catalog", "Return the allowlisted BricsCAD commands available to cad_command_sequence.", "{}")
                   + "," + Tool("cad_command_sequence", "Run one allowlisted BricsCAD command with bounded newline-delimited prompt inputs. Input chaining after a blank terminator and known command injection are rejected.", "\"command\":{\"type\":\"string\",\"maxLength\":40},\"inputs\":{\"type\":\"string\",\"maxLength\":16000},\"confirmMutation\":{\"type\":\"boolean\"}", Required("command","confirmMutation"))
                   + "," + Tool("qs3d_run_command", "Start one allowlisted QS3D command in the active document.", "\"command\":{\"type\":\"string\",\"pattern\":\"^QS3D[A-Za-z0-9_]*$\",\"maxLength\":80},\"confirmMutation\":{\"type\":\"boolean\"}", Required("command","confirmMutation"))
                   + "," + Tool("cad_ui_click", "Click inside the active BricsCAD-process window only, using client-relative pixels.", "\"x\":{\"type\":\"integer\"},\"y\":{\"type\":\"integer\"},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"count\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":3},\"confirmMutation\":{\"type\":\"boolean\"}", Required("x","y","button","confirmMutation"))
                   + "," + Tool("cad_ui_type", "Type printable Unicode text into the active BricsCAD-process window only.", "\"text\":{\"type\":\"string\",\"maxLength\":8000},\"pressEnter\":{\"type\":\"boolean\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("text","confirmMutation"))
                   + "," + Tool("cad_ui_key", "Press a named key in the active BricsCAD-process window with optional Ctrl/Alt/Shift modifiers.", "\"key\":{\"type\":\"string\",\"maxLength\":16},\"ctrl\":{\"type\":\"boolean\"},\"alt\":{\"type\":\"boolean\"},\"shift\":{\"type\":\"boolean\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("key","confirmMutation"))
                   + "," + Tool("cad_agent_stop", "Emergency-stop autonomous input and send ESC twice to BricsCAD, with foreground-input fallback if CAD-context dispatch is unavailable. Deliberately available without confirmation.", "{}")
                   + "," + Tool("cad_agent_resume", "Re-enable autonomous mutation/UI tools after an emergency stop.", "\"confirmMutation\":{\"type\":\"boolean\"}", Required("confirmMutation"))
                   + "," + Tool("cad_audit_tail", "Read the latest bounded MCP mutation audit entries.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}")
                   + "," + Tool("cad_cancel_command", "Send ESC twice to cancel the current CAD command, with foreground-input fallback if CAD-context dispatch is unavailable. Deliberately available without confirmation.", "{}")
                   + "]}}";
        }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0 ? string.Empty : ",\"required\":[" + string.Join(",", required) + "]";
            var propertyJson = properties == "{}" ? string.Empty : properties;
            return "{\"name\":\"" + JsonEscape(name) + "\",\"description\":\"" + JsonEscape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + propertyJson
                   + "},\"additionalProperties\":false" + requiredJson + "}}";
        }

        private static string NumericProperties(params string[] names)
        {
            var parts = new List<string>();
            foreach (var name in names) parts.Add("\"" + name + "\":{\"type\":\"number\"}");
            return string.Join(",", parts);
        }

        private static string[] Required(params string[] names)
        {
            var output = new string[names.Length];
            for (var i = 0; i < names.Length; i++) output[i] = "\"" + names[i] + "\"";
            return output;
        }

        private static string CallTool(string toolName, string arguments)
        {
            try
            {
                switch (toolName)
                {
                    case "connector_info":
                        return ToolSuccess("{\"protocol\":\"" + ProtocolVersion + "\",\"endpoint\":\"" + JsonEscape(Endpoint.ToString())
                            + "\",\"publicUrl\":\"" + JsonEscape(PublicUrl) + "\",\"auth\":\"bearer\",\"tokenSource\":\""
                            + JsonEscape(TokenSource) + "\",\"singleRepository\":true,\"fullCadAgent\":true,\"automationStopped\":"
                            + (_automationStopped ? "true" : "false") + "}");
                    case "qs3d_status": return ToolSuccess(InvokeCad(BuildStatusJson));
                    case "cad_active_document": return ToolSuccess(InvokeCad(BuildActiveDocumentJson));
                    case "cad_selection": return ToolSuccess(InvokeCad(BuildSelectionJson));
                    case "cad_database_snapshot": return ToolSuccess(InvokeCad(() => BuildDatabaseSnapshotJson(ExtractInteger(arguments, "limit", 250, 1, 1000))));
                    case "cad_entity_inspect": return ToolSuccess(InspectEntity(arguments));
                    case "cad_view_state": return ToolSuccess(InvokeCad(BuildViewStateJson));
                    case "cad_wait_idle": return ToolSuccess(WaitUntilIdle(ExtractInteger(arguments, "timeoutMs", 10000, 100, 30000)));
                    case "cad_create_line": return RequireMutation(arguments, "cad_create_line", () => CreateLine(arguments));
                    case "cad_create_circle": return RequireMutation(arguments, "cad_create_circle", () => CreateCircle(arguments));
                    case "cad_create_polyline": return RequireMutation(arguments, "cad_create_polyline", () => CreatePolyline(arguments));
                    case "cad_create_text": return RequireMutation(arguments, "cad_create_text", () => CreateText(arguments));
                    case "cad_entity_transform": return RequireMutation(arguments, "cad_entity_transform", () => TransformEntity(arguments));
                    case "cad_entity_delete": return RequireMutation(arguments, "cad_entity_delete", () => DeleteEntity(arguments));
                    case "cad_layer": return RequireMutation(arguments, "cad_layer", () => LayerAction(arguments));
                    case "cad_command_catalog": return ToolSuccess(CommandCatalogJson());
                    case "cad_command_sequence": return RequireMutation(arguments, "cad_command_sequence", () => RunCadCommandSequence(arguments));
                    case "qs3d_run_command": return RequireMutation(arguments, "qs3d_run_command", () => RunQs3dCommand(arguments));
                    case "cad_ui_click": return RequireMutation(arguments, "cad_ui_click", () => UiClick(arguments));
                    case "cad_ui_type": return RequireMutation(arguments, "cad_ui_type", () => UiType(arguments));
                    case "cad_ui_key": return RequireMutation(arguments, "cad_ui_key", () => UiKey(arguments));
                    case "cad_agent_stop": return EmergencyStop();
                    case "cad_agent_resume": return ResumeAgent(arguments);
                    case "cad_audit_tail": return ToolSuccess(ReadAuditTail(ExtractInteger(arguments, "limit", 25, 1, 100)));
                    case "cad_cancel_command": return CancelCurrentCommand();
                    default: return ToolError("Unknown MCP tool: " + toolName);
                }
            }
            catch (Exception ex) { return ToolError(ex.Message); }
        }

        private static string RequireMutation(string body, string tool, Func<string> action)
        {
            if (_automationStopped) return ToolError("Automation is emergency-stopped. Call cad_agent_resume with confirmMutation=true first.");
            if (!ExtractTopLevelBoolean(body, "confirmMutation")) return ToolError("confirmMutation=true is required for " + tool + ".");
            return ToolSuccess(action());
        }

        private static string EmergencyStop()
        {
            _automationStopped = true;
            Audit("cad_agent_stop", "emergency stop");
            try
            {
                var result = InvokeCad(() =>
                {
                    var document = RequireDocument();
                    document.SendStringToExecute("\u001b\u001b", true, false, true);
                    return "{\"stopped\":true,\"escapeCount\":2,\"delivery\":\"cad-context\"}";
                });
                return ToolSuccess(result);
            }
            catch (Exception ex)
            {
                if (TrySendEscapeFallback())
                {
                    Audit("cad_agent_stop", "foreground ESC fallback after cad-context failure");
                    return ToolSuccess("{\"stopped\":true,\"escapeCount\":2,\"delivery\":\"foreground-fallback\",\"cadContextError\":\"" + JsonEscape(ex.Message) + "\"}");
                }
                return ToolError("Automation stopped, but both CAD-context and foreground ESC delivery failed: " + ex.Message);
            }
        }

        private static string ResumeAgent(string body)
        {
            if (!ExtractTopLevelBoolean(body, "confirmMutation")) return ToolError("confirmMutation=true is required before resuming automation.");
            _automationStopped = false;
            Audit("cad_agent_resume", "resume");
            return ToolSuccess("{\"stopped\":false}");
        }

        private static string CancelCurrentCommand()
        {
            try
            {
                var result = InvokeCad(() =>
                {
                    var document = RequireDocument();
                    document.SendStringToExecute("\u001b\u001b", true, false, true);
                    Audit("cad_cancel_command", "escapeCount=2; delivery=cad-context");
                    return "{\"accepted\":true,\"escapeCount\":2,\"delivery\":\"cad-context\"}";
                });
                return ToolSuccess(result);
            }
            catch (Exception ex)
            {
                if (TrySendEscapeFallback())
                {
                    Audit("cad_cancel_command", "escapeCount=2; delivery=foreground-fallback");
                    return ToolSuccess("{\"accepted\":true,\"escapeCount\":2,\"delivery\":\"foreground-fallback\",\"cadContextError\":\"" + JsonEscape(ex.Message) + "\"}");
                }
                return ToolError("Could not deliver ESC through CAD context or foreground fallback: " + ex.Message);
            }
        }

        private static bool TrySendEscapeFallback()
        {
            try
            {
                var hwnd = RequireForegroundCadWindow();
                RequireSameForegroundCadWindow(hwnd);
                SendVirtualKey(0x1B, false, false, false);
                Thread.Sleep(25);
                RequireSameForegroundCadWindow(hwnd);
                SendVirtualKey(0x1B, false, false, false);
                return true;
            }
            catch { return false; }
        }

        private static string CreateLine(string body)
        {
            var x1 = RequireDouble(body, "x1"); var y1 = RequireDouble(body, "y1"); var z1 = ExtractDouble(body, "z1", 0d);
            var x2 = RequireDouble(body, "x2"); var y2 = RequireDouble(body, "y2"); var z2 = ExtractDouble(body, "z2", 0d);
            var layer = ValidateLayerName(ExtractString(body, "layer"), true);
            return InvokeCad(() => AddEntity(new Line(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)), layer, "cad_create_line"));
        }

        private static string CreateCircle(string body)
        {
            var x = RequireDouble(body, "x"); var y = RequireDouble(body, "y"); var z = ExtractDouble(body, "z", 0d);
            var radius = RequireDouble(body, "radius");
            if (!(radius > 0d)) throw new InvalidOperationException("radius must be > 0.");
            var layer = ValidateLayerName(ExtractString(body, "layer"), true);
            return InvokeCad(() => AddEntity(new Circle(new Point3d(x, y, z), Vector3d.ZAxis, radius), layer, "cad_create_circle"));
        }

        private static string CreatePolyline(string body)
        {
            var points = ExtractString(body, "points");
            if (points.Length > 16000) throw new InvalidOperationException("points exceeds 16000 characters.");
            var closed = ExtractBoolean(body, "closed");
            var elevation = ExtractDouble(body, "elevation", 0d);
            var layer = ValidateLayerName(ExtractString(body, "layer"), true);
            var parsed = ParsePoints2d(points);
            if (parsed.Count < 2) throw new InvalidOperationException("Polyline requires at least two x,y points.");
            if (parsed.Count > 2048) throw new InvalidOperationException("Polyline exceeds 2048 vertices.");
            return InvokeCad(() =>
            {
                var polyline = new Polyline(parsed.Count);
                for (var i = 0; i < parsed.Count; i++) polyline.AddVertexAt(i, parsed[i], 0d, 0d, 0d);
                polyline.Closed = closed;
                polyline.Elevation = elevation;
                return AddEntity(polyline, layer, "cad_create_polyline");
            });
        }

        private static string CreateText(string body)
        {
            var text = ExtractString(body, "text");
            ValidatePrintableText(text, "text", 4000, true);
            var x = RequireDouble(body, "x"); var y = RequireDouble(body, "y"); var z = ExtractDouble(body, "z", 0d);
            var height = RequireDouble(body, "height");
            if (!(height > 0d)) throw new InvalidOperationException("height must be > 0.");
            var rotation = ExtractDouble(body, "rotationDeg", 0d) * Math.PI / 180d;
            var layer = ValidateLayerName(ExtractString(body, "layer"), true);
            return InvokeCad(() =>
            {
                var entity = new DBText { TextString = text, Position = new Point3d(x, y, z), Height = height, Rotation = rotation };
                return AddEntity(entity, layer, "cad_create_text");
            });
        }

        private static string AddEntity(Entity entity, string layer, string auditTool)
        {
            var document = RequireDocument();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                ApplyLayer(transaction, document.Database, entity, layer);
                var table = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var id = model.AppendEntity(entity);
                transaction.AddNewlyCreatedDBObject(entity, true);
                transaction.Commit();
                var handle = id.Handle.ToString();
                Audit(auditTool, "handle=" + handle);
                return "{\"created\":true,\"handle\":\"" + JsonEscape(handle) + "\",\"type\":\"" + JsonEscape(entity.GetType().Name) + "\"}";
            }
        }

        private static void ApplyLayer(Transaction transaction, Database database, Entity entity, string layer)
        {
            if (string.IsNullOrWhiteSpace(layer)) return;
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (!table.Has(layer))
            {
                table.UpgradeOpen();
                var record = new LayerTableRecord { Name = layer };
                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);
            }
            entity.Layer = layer;
        }

        private static string InspectEntity(string body)
        {
            var handle = ExtractString(body, "handle");
            ValidateHandleText(handle);
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var entity = OpenEntityByHandle(transaction, document.Database, handle, OpenMode.ForRead);
                    return DescribeEntity(transaction, entity.ObjectId, true);
                }
            });
        }

        private static string TransformEntity(string body)
        {
            var handle = ExtractString(body, "handle");
            var action = ExtractString(body, "action").Trim().ToLowerInvariant();
            ValidateHandleText(handle);
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var entity = OpenEntityByHandle(transaction, document.Database, handle, OpenMode.ForWrite);
                    if (action == "move")
                        entity.TransformBy(Matrix3d.Displacement(new Vector3d(ExtractDouble(body, "dx", 0d), ExtractDouble(body, "dy", 0d), ExtractDouble(body, "dz", 0d))));
                    else if (action == "rotate")
                    {
                        var radians = RequireDouble(body, "angleDeg") * Math.PI / 180d;
                        entity.TransformBy(Matrix3d.Rotation(radians, Vector3d.ZAxis, EntityCenter(entity)));
                    }
                    else if (action == "scale")
                    {
                        var factor = RequireDouble(body, "factor");
                        if (!(factor > 0d)) throw new InvalidOperationException("factor must be > 0.");
                        entity.TransformBy(Matrix3d.Scaling(factor, EntityCenter(entity)));
                    }
                    else throw new InvalidOperationException("action must be move, rotate or scale.");
                    transaction.Commit();
                    Audit("cad_entity_transform", "handle=" + handle + "; action=" + action);
                    return "{\"updated\":true,\"handle\":\"" + JsonEscape(handle) + "\",\"action\":\"" + JsonEscape(action) + "\"}";
                }
            });
        }

        private static string DeleteEntity(string body)
        {
            var handle = ExtractString(body, "handle");
            ValidateHandleText(handle);
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    OpenEntityByHandle(transaction, document.Database, handle, OpenMode.ForWrite).Erase();
                    transaction.Commit();
                    Audit("cad_entity_delete", "handle=" + handle);
                    return "{\"erased\":true,\"handle\":\"" + JsonEscape(handle) + "\"}";
                }
            });
        }

        private static Entity OpenEntityByHandle(Transaction transaction, Database database, string handleText, OpenMode mode)
        {
            long value;
            if (!long.TryParse((handleText ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value <= 0)
                throw new InvalidOperationException("Invalid entity handle.");
            ObjectId id;
            try { id = database.GetObjectId(false, new Handle(value), 0); }
            catch (Exception ex) { throw new InvalidOperationException("Entity handle was not found.", ex); }
            if (id.IsNull) throw new InvalidOperationException("Entity handle was not found.");
            Entity? entity;
            try { entity = transaction.GetObject(id, mode, false) as Entity; }
            catch (Exception ex) { throw new InvalidOperationException("Entity handle was not readable.", ex); }
            if (entity == null) throw new InvalidOperationException("Object handle is not an entity or was erased.");
            return entity;
        }

        private static Point3d EntityCenter(Entity entity)
        {
            try
            {
                var extents = entity.GeometricExtents;
                return new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2d,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2d,
                    (extents.MinPoint.Z + extents.MaxPoint.Z) / 2d);
            }
            catch { return Point3d.Origin; }
        }

        private static string LayerAction(string body)
        {
            var action = ExtractString(body, "action").Trim().ToLowerInvariant();
            var name = ValidateLayerName(ExtractString(body, "name"), false);
            if (action != "create" && action != "set_current") throw new InvalidOperationException("action must be create or set_current.");
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var table = (LayerTable)transaction.GetObject(document.Database.LayerTableId, OpenMode.ForRead);
                    ObjectId id;
                    if (table.Has(name)) id = table[name];
                    else
                    {
                        table.UpgradeOpen();
                        var record = new LayerTableRecord { Name = name };
                        id = table.Add(record);
                        transaction.AddNewlyCreatedDBObject(record, true);
                    }
                    if (action == "set_current") document.Database.Clayer = id;
                    transaction.Commit();
                    Audit("cad_layer", "action=" + action + "; name=" + name);
                    return "{\"ok\":true,\"action\":\"" + JsonEscape(action) + "\",\"name\":\"" + JsonEscape(name) + "\"}";
                }
            });
        }

        private static string RunCadCommandSequence(string body)
        {
            var command = ExtractString(body, "command").Trim().TrimStart('_').TrimStart('.').ToUpperInvariant();
            if (!AllowedCadCommands.Contains(command))
                throw new InvalidOperationException("Command is not in the QS3D MCP CAD allowlist. Use cad_command_catalog.");
            var inputs = NormalizeCadInputs(ExtractString(body, "inputs"), command);
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                var script = "_." + command + "\n" + inputs;
                if (!script.EndsWith("\n", StringComparison.Ordinal)) script += "\n";
                document.SendStringToExecute(script, true, false, true);
                Audit("cad_command_sequence", "command=" + command + "; inputChars=" + inputs.Length.ToString(CultureInfo.InvariantCulture));
                return "{\"accepted\":true,\"command\":\"" + JsonEscape(command) + "\",\"inputChars\":" + inputs.Length.ToString(CultureInfo.InvariantCulture) + "}";
            });
        }

        private static string NormalizeCadInputs(string inputs, string command)
        {
            var value = (inputs ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (value.Length > 16000 || value.IndexOf('\0') >= 0 || value.IndexOf('\u001b') >= 0 || value.IndexOf('\u0003') >= 0)
                throw new InvalidOperationException("inputs exceeds bounds or contains forbidden control characters.");
            if (NoInputCadCommands.Contains(command) && value.Trim().Length != 0)
                throw new InvalidOperationException(command + " does not accept MCP command-sequence inputs.");
            var lines = value.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length > 64) throw new InvalidOperationException("inputs exceeds 64 prompt lines.");
            var blankTerminatorSeen = false;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 1024) throw new InvalidOperationException("one command input line exceeds 1024 characters.");
                foreach (var ch in lines[i])
                    if (ch < 32 && ch != '\t') throw new InvalidOperationException("inputs contains forbidden control characters.");
                var trimmed = lines[i].Trim();
                if (trimmed.Length == 0)
                {
                    if (i < lines.Length - 1) blankTerminatorSeen = true;
                    continue;
                }
                if (blankTerminatorSeen)
                    throw new InvalidOperationException("inputs may not continue after a blank command terminator.");
                var commandLike = trimmed.TrimStart('_').TrimStart('.').ToUpperInvariant();
                if (AllowedCadCommands.Contains(commandLike) || commandLike.StartsWith("QS3D", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("inputs may not inject another CAD/QS3D command.");
            }
            return value;
        }

        private static string RunQs3dCommand(string body)
        {
            var command = ExtractString(body, "command").Trim();
            if (command.Length == 0 || command.Length > 80 || !Regex.IsMatch(command, "^QS3D[A-Za-z0-9_]*$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Only one QS3D command name matching ^QS3D[A-Za-z0-9_]*$ is allowed.");
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                document.SendStringToExecute(command + "\n", true, false, true);
                Audit("qs3d_run_command", "command=" + command.ToUpperInvariant());
                return "{\"accepted\":true,\"command\":\"" + JsonEscape(command.ToUpperInvariant()) + "\"}";
            });
        }

        private static string CommandCatalogJson()
        {
            var commands = new List<string>(AllowedCadCommands);
            commands.Sort(StringComparer.OrdinalIgnoreCase);
            var builder = new StringBuilder("{\"commands\":[");
            for (var i = 0; i < commands.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append('"').Append(JsonEscape(commands[i])).Append('"');
            }
            return builder.Append("],\"guard\":\"one allowlisted command; bounded prompt lines; no known command chaining after terminators\"}").ToString();
        }

        private static string UiClick(string body)
        {
            var x = ExtractInteger(body, "x", -1, -1, 100000);
            var y = ExtractInteger(body, "y", -1, -1, 100000);
            var button = ExtractString(body, "button").Trim().ToLowerInvariant();
            var count = ExtractInteger(body, "count", 1, 1, 3);
            var hwnd = RequireForegroundCadWindow();
            RECT rect;
            if (!GetClientRect(hwnd, out rect)) throw new InvalidOperationException("Could not read active BricsCAD window client rectangle.");
            if (x < 0 || y < 0 || x >= rect.Right - rect.Left || y >= rect.Bottom - rect.Top)
                throw new InvalidOperationException("Click coordinates must stay inside the active BricsCAD-process window.");
            POINT point = new POINT { X = x, Y = y };
            if (!ClientToScreen(hwnd, ref point)) throw new InvalidOperationException("Could not map BricsCAD client coordinates.");
            if (!SetCursorPos(point.X, point.Y)) throw new InvalidOperationException("Could not position cursor inside BricsCAD.");

            uint down;
            uint up;
            if (button == "left") { down = 0x0002; up = 0x0004; }
            else if (button == "right") { down = 0x0008; up = 0x0010; }
            else if (button == "middle") { down = 0x0020; up = 0x0040; }
            else throw new InvalidOperationException("button must be left, right or middle.");

            for (var i = 0; i < count; i++)
            {
                RequireSameForegroundCadWindow(hwnd);
                SendMouse(down);
                SendMouse(up);
                Thread.Sleep(40);
            }
            Audit("cad_ui_click", "x=" + x + "; y=" + y + "; button=" + button + "; count=" + count);
            return "{\"clicked\":true,\"x\":" + x + ",\"y\":" + y + ",\"button\":\"" + JsonEscape(button) + "\",\"count\":" + count + "}";
        }

        private static string UiType(string body)
        {
            var text = ExtractString(body, "text");
            ValidatePrintableText(text, "text", 8000, true);
            var hwnd = RequireForegroundCadWindow();
            SendUnicodeText(hwnd, text);
            var pressEnter = ExtractBoolean(body, "pressEnter");
            if (pressEnter)
            {
                RequireSameForegroundCadWindow(hwnd);
                SendVirtualKey(0x0D, false, false, false);
            }
            Audit("cad_ui_type", "chars=" + text.Length.ToString(CultureInfo.InvariantCulture) + "; enter=" + pressEnter);
            return "{\"typed\":true,\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + ",\"enter\":" + (pressEnter ? "true" : "false") + "}";
        }

        private static string UiKey(string body)
        {
            var key = ExtractString(body, "key").Trim().ToUpperInvariant();
            var vk = VirtualKey(key);
            var ctrl = ExtractBoolean(body, "ctrl");
            var alt = ExtractBoolean(body, "alt");
            var shift = ExtractBoolean(body, "shift");
            if (alt && key == "F4") throw new InvalidOperationException("Alt+F4 is blocked from MCP UI automation.");
            var hwnd = RequireForegroundCadWindow();
            RequireSameForegroundCadWindow(hwnd);
            SendVirtualKey(vk, ctrl, alt, shift);
            Audit("cad_ui_key", "key=" + key + "; ctrl=" + ctrl + "; alt=" + alt + "; shift=" + shift);
            return "{\"pressed\":true,\"key\":\"" + JsonEscape(key) + "\"}";
        }

        private static IntPtr CurrentProcessWindow()
        {
            var handle = Process.GetCurrentProcess().MainWindowHandle;
            if (handle == IntPtr.Zero || !IsCurrentProcessWindow(handle))
                throw new InvalidOperationException("BricsCAD main window handle is unavailable.");
            return handle;
        }

        private static IntPtr RequireForegroundCadWindow()
        {
            var foreground = GetForegroundWindow();
            if (foreground != IntPtr.Zero && IsCurrentProcessWindow(foreground)) return foreground;
            var main = CurrentProcessWindow();
            if (!SetForegroundWindow(main)) throw new InvalidOperationException("Could not focus the BricsCAD window; UI input was not sent.");
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(25);
                foreground = GetForegroundWindow();
                if (foreground != IntPtr.Zero && IsCurrentProcessWindow(foreground)) return foreground;
            }
            throw new InvalidOperationException("BricsCAD did not become foreground; UI input was not sent.");
        }

        private static void RequireSameForegroundCadWindow(IntPtr expected)
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero || foreground != expected || !IsCurrentProcessWindow(foreground))
                throw new InvalidOperationException("BricsCAD foreground window changed; UI input stopped before injection.");
        }

        private static bool IsCurrentProcessWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            return processId == (uint)Process.GetCurrentProcess().Id;
        }

        private static void SendUnicodeText(IntPtr hwnd, string text)
        {
            foreach (var ch in text)
            {
                RequireSameForegroundCadWindow(hwnd);
                var inputs = new[]
                {
                    new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = 0x0004 } } },
                    new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = 0x0004 | 0x0002 } } }
                };
                if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) != (uint)inputs.Length)
                    throw new InvalidOperationException("Windows SendInput rejected Unicode keyboard input.");
            }
        }

        private static void SendVirtualKey(ushort key, bool ctrl, bool alt, bool shift)
        {
            var list = new List<INPUT>();
            if (ctrl) list.Add(KeyInput(0x11, false));
            if (alt) list.Add(KeyInput(0x12, false));
            if (shift) list.Add(KeyInput(0x10, false));
            list.Add(KeyInput(key, false));
            list.Add(KeyInput(key, true));
            if (shift) list.Add(KeyInput(0x10, true));
            if (alt) list.Add(KeyInput(0x12, true));
            if (ctrl) list.Add(KeyInput(0x11, true));
            var array = list.ToArray();
            if (SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT))) != (uint)array.Length)
                throw new InvalidOperationException("Windows SendInput rejected keyboard input.");
        }

        private static void SendMouse(uint flags)
        {
            var input = new[] { new INPUT { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } } } };
            if (SendInput(1, input, Marshal.SizeOf(typeof(INPUT))) != 1)
                throw new InvalidOperationException("Windows SendInput rejected mouse input.");
        }

        private static INPUT KeyInput(ushort key, bool up)
        {
            return new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = up ? 0x0002u : 0u } } };
        }

        private static ushort VirtualKey(string key)
        {
            switch (key)
            {
                case "ENTER": return 0x0D; case "ESC": case "ESCAPE": return 0x1B; case "TAB": return 0x09;
                case "BACKSPACE": return 0x08; case "DELETE": return 0x2E; case "SPACE": return 0x20;
                case "LEFT": return 0x25; case "UP": return 0x26; case "RIGHT": return 0x27; case "DOWN": return 0x28;
                case "HOME": return 0x24; case "END": return 0x23; case "PAGEUP": return 0x21; case "PAGEDOWN": return 0x22;
                case "F1": return 0x70; case "F2": return 0x71; case "F3": return 0x72; case "F4": return 0x73; case "F5": return 0x74;
                case "F6": return 0x75; case "F7": return 0x76; case "F8": return 0x77; case "F9": return 0x78; case "F10": return 0x79;
                case "F11": return 0x7A; case "F12": return 0x7B;
            }
            if (key.Length == 1)
            {
                var ch = char.ToUpperInvariant(key[0]);
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) return ch;
            }
            throw new InvalidOperationException("Unsupported key name.");
        }

        private static string WaitUntilIdle(int timeoutMs)
        {
            var started = DateTime.UtcNow;
            while ((DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)
            {
                var active = InvokeCad(() => Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? "0");
                int value;
                if (int.TryParse(active, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value == 0)
                    return "{\"idle\":true,\"elapsedMs\":" + ((int)(DateTime.UtcNow - started).TotalMilliseconds).ToString(CultureInfo.InvariantCulture) + "}";
                Thread.Sleep(100);
            }
            return "{\"idle\":false,\"timeoutMs\":" + timeoutMs.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string BuildStatusJson()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var currentLayer = string.Empty;
            try { currentLayer = Convert.ToString(Application.GetSystemVariable("CLAYER"), CultureInfo.InvariantCulture) ?? string.Empty; } catch { }
            return "{\"product\":\"QS3D-BricsCAD\",\"processId\":" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)
                   + ",\"bricscadVersion\":\"" + JsonEscape(Convert.ToString(Application.Version) ?? string.Empty)
                   + "\",\"activeDocument\":\"" + JsonEscape(document?.Name ?? string.Empty)
                   + "\",\"currentLayer\":\"" + JsonEscape(currentLayer)
                   + "\",\"mcpProtocol\":\"" + ProtocolVersion + "\",\"fullCadAgent\":true,\"automationStopped\":" + (_automationStopped ? "true" : "false") + "}";
        }

        private static string BuildActiveDocumentJson()
        {
            var document = RequireDocument();
            return "{\"name\":\"" + JsonEscape(document.Name) + "\",\"fileName\":\"" + JsonEscape(document.Database.Filename ?? string.Empty)
                   + "\",\"databaseHandleSeed\":\"" + JsonEscape(document.Database.Handseed.ToString()) + "\"}";
        }

        private static string BuildViewStateJson()
        {
            var document = RequireDocument();
            using (var view = document.Editor.GetCurrentView())
            {
                RECT rect;
                var hwnd = CurrentProcessWindow();
                var hasRect = GetClientRect(hwnd, out rect);
                var active = Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? "0";
                return "{\"commandActive\":" + SafeJsonInteger(active) + ",\"center\":{\"x\":" + Number(view.CenterPoint.X) + ",\"y\":" + Number(view.CenterPoint.Y)
                       + "},\"width\":" + Number(view.Width) + ",\"height\":" + Number(view.Height)
                       + ",\"clientWidth\":" + (hasRect ? (rect.Right - rect.Left).ToString(CultureInfo.InvariantCulture) : "null")
                       + ",\"clientHeight\":" + (hasRect ? (rect.Bottom - rect.Top).ToString(CultureInfo.InvariantCulture) : "null") + "}";
            }
        }

        private static string SafeJsonInteger(string value)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed.ToString(CultureInfo.InvariantCulture)
                : "0";
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
                    if (id.IsNull) continue;
                    if (!first) output.Append(',');
                    first = false;
                    try { output.Append(DescribeEntity(transaction, id, false)); }
                    catch { output.Append("{\"handle\":\"" + JsonEscape(id.Handle.ToString()) + "\",\"unavailable\":true}"); }
                }
            }
            return output.Append(']').ToString();
        }

        private static string BuildDatabaseSnapshotJson(int limit)
        {
            var document = RequireDocument();
            var output = new StringBuilder();
            output.Append("{\"limit\":").Append(limit).Append(",\"entities\":[");
            var count = 0;
            var hasMore = false;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in modelSpace)
                {
                    if (id.IsNull) continue;
                    if (count >= limit) { hasMore = true; break; }
                    try
                    {
                        if (count > 0) output.Append(',');
                        output.Append(DescribeEntity(transaction, id, true));
                        count++;
                    }
                    catch { }
                }
            }
            return output.Append("],\"count\":").Append(count).Append(",\"truncated\":").Append(hasMore ? "true" : "false").Append('}').ToString();
        }

        private static string DescribeEntity(Transaction transaction, ObjectId id, bool includeExtents)
        {
            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
            if (entity == null) throw new InvalidOperationException("Object is not a readable entity.");
            var output = new StringBuilder();
            output.Append("{\"handle\":\"").Append(JsonEscape(id.Handle.ToString())).Append("\",\"type\":\"")
                .Append(JsonEscape(entity.GetType().Name)).Append("\",\"layer\":\"")
                .Append(JsonEscape(entity.Layer)).Append('"');
            if (includeExtents)
            {
                output.Append(",\"extents\":");
                try { output.Append(ExtentsJson(entity.GeometricExtents)); }
                catch { output.Append("null"); }
            }
            return output.Append('}').ToString();
        }

        private static string ExtentsJson(Extents3d e) { return "{\"min\":" + PointJson(e.MinPoint) + ",\"max\":" + PointJson(e.MaxPoint) + "}"; }
        private static string PointJson(Point3d p) { return "{\"x\":" + Number(p.X) + ",\"y\":" + Number(p.Y) + ",\"z\":" + Number(p.Z) + "}"; }
        private static string Number(double value) { return double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("R", CultureInfo.InvariantCulture); }

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
            public int DispatchState = CadWorkQueued;
            public int Abandoned;
        }

        private static string InvokeCad(Func<string> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var item = new CadWorkItem { Action = action };
            Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadWork, item);
            if (!item.Done.Wait(CadDispatchTimeoutMilliseconds))
            {
                var cancellation = Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued);
                Interlocked.Exchange(ref item.Abandoned, 1);
                try { if (item.Done.IsSet) item.Done.Dispose(); } catch (ObjectDisposedException) { }
                if (cancellation == CadWorkQueued)
                    throw new TimeoutException("Timed out waiting for the BricsCAD application context; queued work was cancelled before it started.");
                throw new TimeoutException("Timed out waiting for the BricsCAD application context after CAD work started; completion is uncertain. Do not retry automatically; inspect CAD state before deciding whether another mutation is safe.");
            }
            try
            {
                if (item.Error != null) throw new InvalidOperationException(item.Error.Message, item.Error);
                return item.Result;
            }
            finally { item.Done.Dispose(); }
        }

        private static void ExecuteCadWork(object data)
        {
            var item = (CadWorkItem)data;
            try
            {
                if (Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued) != CadWorkQueued)
                    return;
                item.Result = item.Action == null ? string.Empty : item.Action();
            }
            catch (Exception ex) { item.Error = ex; }
            finally
            {
                try { item.Done.Set(); }
                finally
                {
                    if (Volatile.Read(ref item.Abandoned) != 0)
                    {
                        try { item.Done.Dispose(); } catch (ObjectDisposedException) { }
                    }
                }
            }
        }

        private static List<Point2d> ParsePoints2d(string value)
        {
            var points = new List<Point2d>();
            foreach (var part in (value ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split(',');
                double x;
                double y;
                if (pair.Length != 2
                    || !double.TryParse(pair[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    || !double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    throw new InvalidOperationException("points must use invariant x,y;x,y format.");
                if (!IsFinite(x) || !IsFinite(y)) throw new InvalidOperationException("points must be finite.");
                points.Add(new Point2d(x, y));
            }
            return points;
        }

        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

        private static void ValidateHandleText(string handle)
        {
            if (string.IsNullOrWhiteSpace(handle) || handle.Length > 32 || !Regex.IsMatch(handle, "^[0-9A-Fa-f]+$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("handle must be a hexadecimal entity handle up to 32 characters.");
        }

        private static string ValidateLayerName(string value, bool optional)
        {
            var name = (value ?? string.Empty).Trim();
            if (name.Length == 0 && optional) return string.Empty;
            if (name.Length == 0) throw new InvalidOperationException("Layer name is required.");
            if (name.Length > 255) throw new InvalidOperationException("Layer name exceeds 255 characters.");
            foreach (var ch in name)
                if (ch < 32) throw new InvalidOperationException("Layer name contains control characters.");
            return name;
        }

        private static void ValidatePrintableText(string value, string property, int maximum, bool rejectAllControls)
        {
            if (string.IsNullOrEmpty(value)) throw new InvalidOperationException(property + " is required.");
            if (value.Length > maximum) throw new InvalidOperationException(property + " exceeds " + maximum.ToString(CultureInfo.InvariantCulture) + " characters.");
            foreach (var ch in value)
            {
                if (ch == '\0' || ch == '\u001b' || (rejectAllControls && ch < 32))
                    throw new InvalidOperationException(property + " contains forbidden control characters.");
            }
        }

        private static double RequireDouble(string json, string property)
        {
            double value;
            bool found;
            string error;
            if (!TryExtractTopLevelDouble(json, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) throw new InvalidOperationException(property + " must be a finite number.");
            return value;
        }

        private static double ExtractDouble(string json, string property, double fallback)
        {
            double value;
            bool found;
            string error;
            if (!TryExtractTopLevelDouble(json, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            return found ? value : fallback;
        }

        private static bool TryExtractTopLevelDouble(
            string json,
            string property,
            out double value,
            out bool found,
            out string error)
        {
            return McpTopLevelJson.TryExtractDouble(json, property, out value, out found, out error);
        }

        private static bool TryExtractTopLevelInteger(
            string json,
            string property,
            out int value,
            out bool found,
            out string error)
        {
            return McpTopLevelJson.TryExtractInteger(json, property, out value, out found, out error);
        }

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
                    error = "tools/call requires an object params value.";
                    return false;
                }
                name = ExtractTopLevelString(parameters, "name").Trim();
                if (name.Length == 0 || name.Length > 128)
                {
                    error = "tools/call params.name is required and must be <= 128 characters.";
                    return false;
                }
                string parsedArguments;
                if (TryExtractObjectProperty(parameters, "arguments", out parsedArguments)) arguments = parsedArguments;
                else if (HasTopLevelProperty(parameters, "arguments"))
                {
                    error = "tools/call params.arguments must be an object.";
                    return false;
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
            string rawValue;
            bool found;
            string error;
            if (!TryFindTopLevelPropertyValue(json, property, out rawValue, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return false;
            var candidate = rawValue.Trim();
            if (candidate.Length < 2 || candidate[0] != '{' || candidate[candidate.Length - 1] != '}') return false;
            objectJson = candidate;
            return true;
        }

        private static bool TryFindTopLevelPropertyValue(
            string json,
            string property,
            out string rawValue,
            out bool found,
            out string error)
        {
            return McpTopLevelJson.TryFindPropertyValue(json, property, out rawValue, out found, out error);
        }

        private static string ExtractTopLevelString(string json, string property)
        {
            return McpTopLevelJson.ExtractString(json, property);
        }

        private static bool ExtractTopLevelBoolean(string json, string property)
        {
            return McpTopLevelJson.ExtractBoolean(json, property);
        }

        private static bool HasTopLevelProperty(string json, string property)
        {
            return McpTopLevelJson.HasProperty(json, property);
        }

        private static string ExtractTopLevelId(string json)
        {
            return McpTopLevelJson.ExtractId(json);
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
            return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"error\":{\"code\":" + code.ToString(CultureInfo.InvariantCulture)
                   + ",\"message\":\"" + JsonEscape(message) + "\"}}";
        }

        private static string ExtractString(string json, string property)
        {
            return ExtractTopLevelString(json, property);
        }

        private static bool ExtractBoolean(string json, string property)
        {
            return ExtractTopLevelBoolean(json, property);
        }

        private static int ExtractInteger(string json, string property, int fallback, int minimum, int maximum)
        {
            int value;
            bool found;
            string error;
            if (!TryExtractTopLevelInteger(json, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool HasProperty(string json, string property)
        {
            return HasTopLevelProperty(json, property);
        }

        private static string ExtractId(string json)
        {
            return ExtractTopLevelId(json);
        }

        private static string JsonUnescape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var output = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch != '\\') { output.Append(ch); continue; }
                if (++i >= value.Length) throw new InvalidOperationException("Invalid JSON string escape.");
                ch = value[i];
                switch (ch)
                {
                    case '"': output.Append('"'); break;
                    case '\\': output.Append('\\'); break;
                    case '/': output.Append('/'); break;
                    case 'b': output.Append('\b'); break;
                    case 'f': output.Append('\f'); break;
                    case 'n': output.Append('\n'); break;
                    case 'r': output.Append('\r'); break;
                    case 't': output.Append('\t'); break;
                    case 'u':
                        if (i + 4 >= value.Length) throw new InvalidOperationException("Invalid JSON unicode escape.");
                        int code;
                        if (!int.TryParse(value.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            throw new InvalidOperationException("Invalid JSON unicode escape.");
                        output.Append((char)code);
                        i += 4;
                        break;
                    default: throw new InvalidOperationException("Invalid JSON string escape.");
                }
            }
            return output.ToString();
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

        private static void Audit(string tool, string detail)
        {
            try
            {
                lock (AuditSync)
                {
                    var path = AuditFilePath;
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                    RotateAuditIfNeeded(path);
                    var cleanDetail = SanitizeAuditDetail(detail);
                    var line = "{\"utc\":\"" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\",\"tool\":\"" + JsonEscape(tool)
                               + "\",\"detail\":\"" + JsonEscape(cleanDetail) + "\"}" + Environment.NewLine;
                    File.AppendAllText(path, line, new UTF8Encoding(false));
                }
            }
            catch { }
        }

        private static string SanitizeAuditDetail(string detail)
        {
            var value = detail ?? string.Empty;
            if (value.Length > 1024) value = value.Substring(0, 1024);
            var output = new StringBuilder(value.Length);
            foreach (var ch in value) output.Append(ch < 32 ? ' ' : ch);
            return output.ToString();
        }

        private static void RotateAuditIfNeeded(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length < MaxAuditBytes) return;
                var previous = path + ".1";
                try { if (File.Exists(previous)) File.Delete(previous); } catch { }
                try { File.Move(path, previous); } catch { File.WriteAllText(path, string.Empty, new UTF8Encoding(false)); }
            }
            catch { }
        }

        private static string ReadAuditTail(int limit)
        {
            try
            {
                lock (AuditSync)
                {
                    if (!File.Exists(AuditFilePath)) return "{\"entries\":[]}";
                    var lines = File.ReadAllLines(AuditFilePath, Encoding.UTF8);
                    var start = Math.Max(0, lines.Length - limit);
                    var builder = new StringBuilder("{\"entries\":[");
                    var written = 0;
                    for (var i = start; i < lines.Length; i++)
                    {
                        if (!lines[i].StartsWith("{", StringComparison.Ordinal) || !lines[i].EndsWith("}", StringComparison.Ordinal)) continue;
                        if (written++ > 0) builder.Append(',');
                        builder.Append(lines[i]);
                    }
                    return builder.Append("]}").ToString();
                }
            }
            catch (Exception ex) { return "{\"entries\":[],\"error\":\"" + JsonEscape(ex.Message) + "\"}"; }
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string reason, string body, IDictionary<string, string>? extraHeaders)
        {
            var payload = string.IsNullOrEmpty(body) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
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
            var headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (payload.Length > 0) stream.Write(payload, 0, payload.Length);
            stream.Flush();
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
                    _bearerToken = GenerateToken();
                    var directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Could not resolve MCP configuration directory.");
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
            var token = new StringBuilder(64);
            foreach (var value in bytes) token.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return token.ToString();
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

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
        [StructLayout(LayoutKind.Explicit)] private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }
        [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT
        { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT
        { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    }
}
