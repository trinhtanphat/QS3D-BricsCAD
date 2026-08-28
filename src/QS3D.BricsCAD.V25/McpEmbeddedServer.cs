using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Net;
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
    /// It owns both the direct CAD API surface and a BricsCAD-window-only UI automation fallback.
    /// Network work stays off the CAD thread; database/editor work is marshalled through
    /// ExecuteInApplicationContext and every mutation is explicitly confirmed and audited.
    /// </summary>
    internal static class McpEmbeddedServer
    {
        private const int Port = 8765;
        private const int MaxHeaderBytes = 64 * 1024;
        private const int MaxBodyBytes = 1024 * 1024;
        private const int CadDispatchTimeoutMilliseconds = 15000;
        private const string ProtocolVersion = "2025-06-18";
        private const string BearerEnvironment = "QS3D_MCP_BEARER_TOKEN";
        private const string PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL";
        private const string TokenFileName = "mcp-bearer-token.txt";
        private const string AuditFileName = "mcp-agent-audit.jsonl";

        private static readonly object Sync = new object();
        private static readonly ConcurrentDictionary<string, DateTime> Sessions =
            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);
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
                        if (request != null) HandleRequest(stream, request);
                    }
                }
                catch (Exception ex) { SetLastError("request: " + ex.Message); }
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
                for (var i = 1; i < lines.Length; i++)
                {
                    var separator = lines[i].IndexOf(':');
                    if (separator > 0) headers[lines[i].Substring(0, separator).Trim()] = lines[i].Substring(separator + 1).Trim();
                }

                var contentLength = 0;
                string lengthText;
                if (headers.TryGetValue("Content-Length", out lengthText)
                    && (!int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out contentLength)
                        || contentLength < 0 || contentLength > MaxBodyBytes))
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
                return new HttpRequest(requestParts[0].Trim().ToUpperInvariant(), requestParts[1].Trim(), headers,
                    contentLength == 0 ? string.Empty : Encoding.UTF8.GetString(body));
            }
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
            if (request.Method == "OPTIONS") { WriteResponse(stream, 204, "No Content", string.Empty, null); return; }
            if (request.Method == "GET" && string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"use MCP POST\"}", new Dictionary<string, string> { ["Allow"] = "POST" });
                return;
            }
            if (request.Method != "POST" || !string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 404, "Not Found", "{\"error\":\"not found\"}", null);
                return;
            }
            if (!Authorize(request.Headers))
            {
                WriteResponse(stream, 401, "Unauthorized",
                    "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32001,\"message\":\"Bearer authorization required.\"}}",
                    new Dictionary<string, string> { ["WWW-Authenticate"] = "Bearer" });
                return;
            }

            var method = ExtractString(request.Body, "method");
            var id = ExtractId(request.Body);
            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                var requested = ExtractString(request.Body, "protocolVersion");
                var selected = string.Equals(requested, "2025-03-26", StringComparison.Ordinal) ? "2025-03-26" : ProtocolVersion;
                var sessionId = Guid.NewGuid().ToString("N");
                Sessions[sessionId] = DateTime.UtcNow;
                var response = "{\"jsonrpc\":\"2.0\",\"id\":" + id
                               + ",\"result\":{\"protocolVersion\":\"" + selected
                               + "\",\"capabilities\":{\"tools\":{\"listChanged\":false}},"
                               + "\"serverInfo\":{\"name\":\"qs3d-bricscad\",\"version\":\"embedded-2\"},"
                               + "\"instructions\":\"QS3D embedded BricsCAD MCP. Prefer direct CAD API tools, use cad_command_sequence for supported command-line workflows, and use BricsCAD-window-only mouse/keyboard tools only as a UI fallback. Every mutation requires confirmMutation=true.\"}}";
                WriteResponse(stream, 200, "OK", response, new Dictionary<string, string>
                {
                    ["Mcp-Session-Id"] = sessionId,
                    ["MCP-Protocol-Version"] = selected
                });
                return;
            }

            string sessionError;
            if (!TryValidateSession(request.Headers, out sessionError))
            {
                WriteResponse(stream, 400, "Bad Request", JsonRpcError(id, -32002, sessionError), null);
                return;
            }
            if (string.Equals(method, "notifications/initialized", StringComparison.Ordinal)) { WriteResponse(stream, 202, "Accepted", string.Empty, null); return; }
            if (string.Equals(method, "ping", StringComparison.Ordinal)) { WriteResponse(stream, 200, "OK", "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{}}", null); return; }
            if (string.Equals(method, "tools/list", StringComparison.Ordinal)) { WriteResponse(stream, 200, "OK", ToolsListResponse(id), null); return; }
            if (string.Equals(method, "tools/call", StringComparison.Ordinal))
            {
                var result = CallTool(ExtractToolName(request.Body), request.Body);
                WriteResponse(stream, 200, "OK", "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + result + "}", null);
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
            return ConstantTimeEquals(authorization.Substring(prefix.Length).Trim(), GetBearerToken());
        }

        private static bool TryValidateSession(IDictionary<string, string> headers, out string error)
        {
            CleanupSessions();
            string sessionId;
            if (!headers.TryGetValue("Mcp-Session-Id", out sessionId) || string.IsNullOrWhiteSpace(sessionId))
            { error = "Mcp-Session-Id is required after initialize."; return false; }
            DateTime lastSeen;
            if (!Sessions.TryGetValue(sessionId, out lastSeen)) { error = "Unknown or expired MCP session."; return false; }
            if (DateTime.UtcNow - lastSeen > TimeSpan.FromHours(4))
            {
                DateTime ignored; Sessions.TryRemove(sessionId, out ignored); error = "MCP session expired."; return false;
            }
            Sessions[sessionId] = DateTime.UtcNow; error = string.Empty; return true;
        }

        private static void CleanupSessions()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(4);
            foreach (var pair in Sessions)
            {
                if (pair.Value >= cutoff) continue;
                DateTime ignored; Sessions.TryRemove(pair.Key, out ignored);
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
                   + "," + Tool("cad_view_state", "Read command-active state, current view center/size and BricsCAD window size.", "{}")
                   + "," + Tool("cad_wait_idle", "Wait until BricsCAD reports CMDACTIVE=0 or timeout.", "\"timeoutMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":30000}")
                   + "," + Tool("cad_create_line", "Create one native Line in ModelSpace.", NumericProperties("x1","y1","z1","x2","y2","z2") + ",\"layer\":{\"type\":\"string\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("x1","y1","x2","y2","confirmMutation"))
                   + "," + Tool("cad_create_circle", "Create one native Circle in ModelSpace.", NumericProperties("x","y","z","radius") + ",\"layer\":{\"type\":\"string\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("x","y","radius","confirmMutation"))
                   + "," + Tool("cad_create_polyline", "Create a native 2D Polyline. points format: x,y;x,y;...", "\"points\":{\"type\":\"string\",\"maxLength\":16000},\"closed\":{\"type\":\"boolean\"},\"elevation\":{\"type\":\"number\"},\"layer\":{\"type\":\"string\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("points","confirmMutation"))
                   + "," + Tool("cad_create_text", "Create native DBText in ModelSpace.", "\"text\":{\"type\":\"string\",\"maxLength\":4000}," + NumericProperties("x","y","z","height","rotationDeg") + ",\"layer\":{\"type\":\"string\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("text","x","y","height","confirmMutation"))
                   + "," + Tool("cad_entity_transform", "Move, rotate or scale one entity by handle.", "\"handle\":{\"type\":\"string\"},\"action\":{\"type\":\"string\",\"enum\":[\"move\",\"rotate\",\"scale\"]}," + NumericProperties("dx","dy","dz","angleDeg","factor") + ",\"confirmMutation\":{\"type\":\"boolean\"}", Required("handle","action","confirmMutation"))
                   + "," + Tool("cad_entity_delete", "Erase one entity by handle.", "\"handle\":{\"type\":\"string\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("handle","confirmMutation"))
                   + "," + Tool("cad_layer", "Create a layer or make it current.", "\"action\":{\"type\":\"string\",\"enum\":[\"create\",\"set_current\"]},\"name\":{\"type\":\"string\",\"maxLength\":255},\"confirmMutation\":{\"type\":\"boolean\"}", Required("action","name","confirmMutation"))
                   + "," + Tool("cad_command_catalog", "Return the allowlisted BricsCAD commands available to cad_command_sequence.", "{}")
                   + "," + Tool("cad_command_sequence", "Run one allowlisted BricsCAD command with newline-delimited command-line inputs. Covers hatch/dimensions/blocks/xrefs/layout/plot/open/save and advanced CAD workflows.", "\"command\":{\"type\":\"string\",\"maxLength\":40},\"inputs\":{\"type\":\"string\",\"maxLength\":16000},\"confirmMutation\":{\"type\":\"boolean\"}", Required("command","confirmMutation"))
                   + "," + Tool("qs3d_run_command", "Start one allowlisted QS3D command in the active document.", "\"command\":{\"type\":\"string\",\"pattern\":\"^QS3D[A-Za-z0-9_]*$\",\"maxLength\":80},\"confirmMutation\":{\"type\":\"boolean\"}", Required("command","confirmMutation"))
                   + "," + Tool("cad_ui_click", "Click inside the BricsCAD client window only, using client-relative pixels.", "\"x\":{\"type\":\"integer\"},\"y\":{\"type\":\"integer\"},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"count\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":3},\"confirmMutation\":{\"type\":\"boolean\"}", Required("x","y","button","confirmMutation"))
                   + "," + Tool("cad_ui_type", "Type Unicode text into the BricsCAD foreground window only.", "\"text\":{\"type\":\"string\",\"maxLength\":8000},\"pressEnter\":{\"type\":\"boolean\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("text","confirmMutation"))
                   + "," + Tool("cad_ui_key", "Press a named key in BricsCAD with optional Ctrl/Alt/Shift modifiers.", "\"key\":{\"type\":\"string\"},\"ctrl\":{\"type\":\"boolean\"},\"alt\":{\"type\":\"boolean\"},\"shift\":{\"type\":\"boolean\"},\"confirmMutation\":{\"type\":\"boolean\"}", Required("key","confirmMutation"))
                   + "," + Tool("cad_agent_stop", "Emergency-stop autonomous input and send ESC twice to BricsCAD.", "{}")
                   + "," + Tool("cad_agent_resume", "Re-enable autonomous mutation/UI tools after an emergency stop.", "\"confirmMutation\":{\"type\":\"boolean\"}", Required("confirmMutation"))
                   + "," + Tool("cad_audit_tail", "Read the latest bounded MCP mutation audit entries.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}")
                   + "," + Tool("cad_cancel_command", "Send two ESC characters to cancel the current CAD command.", "{}")
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

        private static string CallTool(string toolName, string requestBody)
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
                    case "cad_database_snapshot": return ToolSuccess(InvokeCad(() => BuildDatabaseSnapshotJson(ExtractInteger(requestBody, "limit", 250, 1, 1000))));
                    case "cad_view_state": return ToolSuccess(InvokeCad(BuildViewStateJson));
                    case "cad_wait_idle": return ToolSuccess(WaitUntilIdle(ExtractInteger(requestBody, "timeoutMs", 10000, 100, 30000)));
                    case "cad_create_line": return RequireMutation(requestBody, "cad_create_line", () => CreateLine(requestBody));
                    case "cad_create_circle": return RequireMutation(requestBody, "cad_create_circle", () => CreateCircle(requestBody));
                    case "cad_create_polyline": return RequireMutation(requestBody, "cad_create_polyline", () => CreatePolyline(requestBody));
                    case "cad_create_text": return RequireMutation(requestBody, "cad_create_text", () => CreateText(requestBody));
                    case "cad_entity_transform": return RequireMutation(requestBody, "cad_entity_transform", () => TransformEntity(requestBody));
                    case "cad_entity_delete": return RequireMutation(requestBody, "cad_entity_delete", () => DeleteEntity(requestBody));
                    case "cad_layer": return RequireMutation(requestBody, "cad_layer", () => LayerAction(requestBody));
                    case "cad_command_catalog": return ToolSuccess(CommandCatalogJson());
                    case "cad_command_sequence": return RequireMutation(requestBody, "cad_command_sequence", () => RunCadCommandSequence(requestBody));
                    case "qs3d_run_command": return RequireMutation(requestBody, "qs3d_run_command", () => RunQs3dCommand(requestBody));
                    case "cad_ui_click": return RequireMutation(requestBody, "cad_ui_click", () => UiClick(requestBody));
                    case "cad_ui_type": return RequireMutation(requestBody, "cad_ui_type", () => UiType(requestBody));
                    case "cad_ui_key": return RequireMutation(requestBody, "cad_ui_key", () => UiKey(requestBody));
                    case "cad_agent_stop":
                        _automationStopped = true;
                        Audit("cad_agent_stop", "emergency stop");
                        return ToolSuccess(InvokeCad(() => { var d = RequireDocument(); d.SendStringToExecute("\u001b\u001b", true, false, true); return "{\"stopped\":true}"; }));
                    case "cad_agent_resume":
                        if (!ExtractBoolean(requestBody, "confirmMutation")) return ToolError("confirmMutation=true is required before resuming automation.");
                        _automationStopped = false; Audit("cad_agent_resume", "resume"); return ToolSuccess("{\"stopped\":false}");
                    case "cad_audit_tail": return ToolSuccess(ReadAuditTail(ExtractInteger(requestBody, "limit", 25, 1, 100)));
                    case "cad_cancel_command":
                        return ToolSuccess(InvokeCad(() => { var d = RequireDocument(); d.SendStringToExecute("\u001b\u001b", true, false, true); return "{\"accepted\":true,\"escapeCount\":2}"; }));
                    default: return ToolError("Unknown MCP tool: " + toolName);
                }
            }
            catch (Exception ex) { return ToolError(ex.Message); }
        }

        private static string RequireMutation(string body, string tool, Func<string> action)
        {
            if (_automationStopped) return ToolError("Automation is emergency-stopped. Call cad_agent_resume with confirmMutation=true first.");
            if (!ExtractBoolean(body, "confirmMutation")) return ToolError("confirmMutation=true is required for " + tool + ".");
            var result = action();
            return ToolSuccess(result);
        }

        private static string CreateLine(string body)
        {
            var x1 = RequireDouble(body, "x1"); var y1 = RequireDouble(body, "y1"); var z1 = ExtractDouble(body, "z1", 0d);
            var x2 = RequireDouble(body, "x2"); var y2 = RequireDouble(body, "y2"); var z2 = ExtractDouble(body, "z2", 0d);
            var layer = ExtractString(body, "layer");
            return InvokeCad(() => AddEntity(new Line(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)), layer, "cad_create_line"));
        }

        private static string CreateCircle(string body)
        {
            var x = RequireDouble(body, "x"); var y = RequireDouble(body, "y"); var z = ExtractDouble(body, "z", 0d);
            var radius = RequireDouble(body, "radius"); if (!(radius > 0d)) throw new InvalidOperationException("radius must be > 0.");
            var layer = ExtractString(body, "layer");
            return InvokeCad(() => AddEntity(new Circle(new Point3d(x, y, z), Vector3d.ZAxis, radius), layer, "cad_create_circle"));
        }

        private static string CreatePolyline(string body)
        {
            var points = ExtractString(body, "points");
            var closed = ExtractBoolean(body, "closed");
            var elevation = ExtractDouble(body, "elevation", 0d);
            var layer = ExtractString(body, "layer");
            var parsed = ParsePoints2d(points);
            if (parsed.Count < 2) throw new InvalidOperationException("Polyline requires at least two x,y points.");
            return InvokeCad(() =>
            {
                var polyline = new Polyline(parsed.Count);
                for (var i = 0; i < parsed.Count; i++) polyline.AddVertexAt(i, parsed[i], 0d, 0d, 0d);
                polyline.Closed = closed; polyline.Elevation = elevation;
                return AddEntity(polyline, layer, "cad_create_polyline");
            });
        }

        private static string CreateText(string body)
        {
            var text = ExtractString(body, "text"); if (string.IsNullOrEmpty(text)) throw new InvalidOperationException("text is required.");
            var x = RequireDouble(body, "x"); var y = RequireDouble(body, "y"); var z = ExtractDouble(body, "z", 0d);
            var height = RequireDouble(body, "height"); if (!(height > 0d)) throw new InvalidOperationException("height must be > 0.");
            var rotation = ExtractDouble(body, "rotationDeg", 0d) * Math.PI / 180d;
            var layer = ExtractString(body, "layer");
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
                var id = model.AppendEntity(entity); transaction.AddNewlyCreatedDBObject(entity, true); transaction.Commit();
                var handle = id.Handle.ToString(); Audit(auditTool, "handle=" + handle);
                return "{\"created\":true,\"handle\":\"" + JsonEscape(handle) + "\",\"type\":\"" + JsonEscape(entity.GetType().Name) + "\"}";
            }
        }

        private static void ApplyLayer(Transaction transaction, Database database, Entity entity, string layer)
        {
            if (string.IsNullOrWhiteSpace(layer)) return;
            var name = layer.Trim();
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (!table.Has(name))
            {
                table.UpgradeOpen();
                var record = new LayerTableRecord { Name = name };
                table.Add(record); transaction.AddNewlyCreatedDBObject(record, true);
            }
            entity.Layer = name;
        }

        private static string TransformEntity(string body)
        {
            var handle = ExtractString(body, "handle"); var action = ExtractString(body, "action").Trim().ToLowerInvariant();
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var entity = OpenEntityByHandle(transaction, document.Database, handle, OpenMode.ForWrite);
                    if (action == "move") entity.TransformBy(Matrix3d.Displacement(new Vector3d(ExtractDouble(body,"dx",0d), ExtractDouble(body,"dy",0d), ExtractDouble(body,"dz",0d))));
                    else if (action == "rotate")
                    {
                        var center = EntityCenter(entity); var radians = RequireDouble(body, "angleDeg") * Math.PI / 180d;
                        entity.TransformBy(Matrix3d.Rotation(radians, Vector3d.ZAxis, center));
                    }
                    else if (action == "scale")
                    {
                        var factor = RequireDouble(body, "factor"); if (!(factor > 0d)) throw new InvalidOperationException("factor must be > 0.");
                        entity.TransformBy(Matrix3d.Scaling(factor, EntityCenter(entity)));
                    }
                    else throw new InvalidOperationException("action must be move, rotate or scale.");
                    transaction.Commit(); Audit("cad_entity_transform", "handle=" + handle + "; action=" + action);
                    return "{\"updated\":true,\"handle\":\"" + JsonEscape(handle) + "\",\"action\":\"" + JsonEscape(action) + "\"}";
                }
            });
        }

        private static string DeleteEntity(string body)
        {
            var handle = ExtractString(body, "handle");
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    OpenEntityByHandle(transaction, document.Database, handle, OpenMode.ForWrite).Erase();
                    transaction.Commit(); Audit("cad_entity_delete", "handle=" + handle);
                    return "{\"erased\":true,\"handle\":\"" + JsonEscape(handle) + "\"}";
                }
            });
        }

        private static Entity OpenEntityByHandle(Transaction transaction, Database database, string handleText, OpenMode mode)
        {
            long value;
            if (!long.TryParse((handleText ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                throw new InvalidOperationException("Invalid entity handle.");
            var id = database.GetObjectId(false, new Handle(value), 0);
            var entity = transaction.GetObject(id, mode, false) as Entity;
            if (entity == null) throw new InvalidOperationException("Entity handle was not found.");
            return entity;
        }

        private static Point3d EntityCenter(Entity entity)
        {
            try
            {
                var extents = entity.GeometricExtents;
                return new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2d, (extents.MinPoint.Y + extents.MaxPoint.Y) / 2d, (extents.MinPoint.Z + extents.MaxPoint.Z) / 2d);
            }
            catch { return Point3d.Origin; }
        }

        private static string LayerAction(string body)
        {
            var action = ExtractString(body, "action").Trim().ToLowerInvariant();
            var name = ExtractString(body, "name").Trim(); if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Layer name is required.");
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
                        table.UpgradeOpen(); var record = new LayerTableRecord { Name = name }; id = table.Add(record); transaction.AddNewlyCreatedDBObject(record, true);
                    }
                    if (action == "set_current") document.Database.Clayer = id;
                    else if (action != "create") throw new InvalidOperationException("action must be create or set_current.");
                    transaction.Commit(); Audit("cad_layer", "action=" + action + "; name=" + name);
                    return "{\"ok\":true,\"action\":\"" + JsonEscape(action) + "\",\"name\":\"" + JsonEscape(name) + "\"}";
                }
            });
        }

        private static string RunCadCommandSequence(string body)
        {
            var command = ExtractString(body, "command").Trim().TrimStart('_').ToUpperInvariant();
            if (!AllowedCadCommands.Contains(command)) throw new InvalidOperationException("Command is not in the QS3D MCP CAD allowlist. Use cad_command_catalog.");
            var inputs = ExtractString(body, "inputs");
            if (inputs.Length > 16000 || inputs.IndexOf('\0') >= 0 || inputs.IndexOf('\u001b') >= 0)
                throw new InvalidOperationException("inputs exceeds bounds or contains forbidden control characters.");
            return InvokeCad(() =>
            {
                var document = RequireDocument();
                var script = "_." + command + "\n" + inputs.Replace("\r\n", "\n").Replace('\r', '\n');
                if (!script.EndsWith("\n", StringComparison.Ordinal)) script += "\n";
                document.SendStringToExecute(script, true, false, true);
                Audit("cad_command_sequence", "command=" + command + "; inputChars=" + inputs.Length.ToString(CultureInfo.InvariantCulture));
                return "{\"accepted\":true,\"command\":\"" + JsonEscape(command) + "\",\"inputChars\":" + inputs.Length.ToString(CultureInfo.InvariantCulture) + "}";
            });
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
            var commands = new List<string>(AllowedCadCommands); commands.Sort(StringComparer.OrdinalIgnoreCase);
            var builder = new StringBuilder("{\"commands\":[");
            for (var i = 0; i < commands.Count; i++) { if (i > 0) builder.Append(','); builder.Append('"').Append(JsonEscape(commands[i])).Append('"'); }
            return builder.Append("]}").ToString();
        }

        private static string UiClick(string body)
        {
            var x = ExtractInteger(body, "x", -1, -1, 100000); var y = ExtractInteger(body, "y", -1, -1, 100000);
            var button = ExtractString(body, "button").Trim().ToLowerInvariant(); var count = ExtractInteger(body, "count", 1, 1, 3);
            var hwnd = CurrentProcessWindow(); RECT rect; if (!GetClientRect(hwnd, out rect)) throw new InvalidOperationException("Could not read BricsCAD client rectangle.");
            if (x < 0 || y < 0 || x >= rect.Right - rect.Left || y >= rect.Bottom - rect.Top) throw new InvalidOperationException("Click coordinates must stay inside the BricsCAD client window.");
            POINT point = new POINT { X = x, Y = y }; if (!ClientToScreen(hwnd, ref point)) throw new InvalidOperationException("Could not map BricsCAD client coordinates.");
            SetForegroundWindow(hwnd); SetCursorPos(point.X, point.Y);
            uint down; uint up;
            if (button == "left") { down = 0x0002; up = 0x0004; }
            else if (button == "right") { down = 0x0008; up = 0x0010; }
            else if (button == "middle") { down = 0x0020; up = 0x0040; }
            else throw new InvalidOperationException("button must be left, right or middle.");
            for (var i = 0; i < count; i++) { mouse_event(down, 0, 0, 0, UIntPtr.Zero); mouse_event(up, 0, 0, 0, UIntPtr.Zero); Thread.Sleep(40); }
            Audit("cad_ui_click", "x=" + x + "; y=" + y + "; button=" + button + "; count=" + count);
            return "{\"clicked\":true,\"x\":" + x + ",\"y\":" + y + ",\"button\":\"" + JsonEscape(button) + "\",\"count\":" + count + "}";
        }

        private static string UiType(string body)
        {
            var text = ExtractString(body, "text"); if (text.Length > 8000) throw new InvalidOperationException("text exceeds 8000 characters.");
            var hwnd = CurrentProcessWindow(); SetForegroundWindow(hwnd); SendUnicodeText(text);
            if (ExtractBoolean(body, "pressEnter")) SendVirtualKey(0x0D, false, false, false);
            Audit("cad_ui_type", "chars=" + text.Length.ToString(CultureInfo.InvariantCulture) + "; enter=" + ExtractBoolean(body, "pressEnter"));
            return "{\"typed\":true,\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string UiKey(string body)
        {
            var key = ExtractString(body, "key").Trim().ToUpperInvariant(); var vk = VirtualKey(key);
            var ctrl = ExtractBoolean(body, "ctrl"); var alt = ExtractBoolean(body, "alt"); var shift = ExtractBoolean(body, "shift");
            SetForegroundWindow(CurrentProcessWindow()); SendVirtualKey(vk, ctrl, alt, shift);
            Audit("cad_ui_key", "key=" + key + "; ctrl=" + ctrl + "; alt=" + alt + "; shift=" + shift);
            return "{\"pressed\":true,\"key\":\"" + JsonEscape(key) + "\"}";
        }

        private static IntPtr CurrentProcessWindow()
        {
            var handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (handle == IntPtr.Zero) throw new InvalidOperationException("BricsCAD main window handle is unavailable.");
            return handle;
        }

        private static void SendUnicodeText(string text)
        {
            foreach (var ch in text)
            {
                var inputs = new[]
                {
                    new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = 0x0004 } } },
                    new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = 0x0004 | 0x0002 } } }
                };
                if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) != inputs.Length)
                    throw new InvalidOperationException("Windows SendInput rejected Unicode keyboard input.");
            }
        }

        private static void SendVirtualKey(ushort key, bool ctrl, bool alt, bool shift)
        {
            var list = new List<INPUT>();
            if (ctrl) list.Add(KeyInput(0x11, false)); if (alt) list.Add(KeyInput(0x12, false)); if (shift) list.Add(KeyInput(0x10, false));
            list.Add(KeyInput(key, false)); list.Add(KeyInput(key, true));
            if (shift) list.Add(KeyInput(0x10, true)); if (alt) list.Add(KeyInput(0x12, true)); if (ctrl) list.Add(KeyInput(0x11, true));
            var array = list.ToArray(); if (SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT))) != array.Length)
                throw new InvalidOperationException("Windows SendInput rejected keyboard input.");
        }

        private static INPUT KeyInput(ushort key, bool up) => new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = up ? 0x0002u : 0u } } };

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
                int value; if (int.TryParse(active, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value == 0)
                    return "{\"idle\":true,\"elapsedMs\":" + ((int)(DateTime.UtcNow - started).TotalMilliseconds).ToString(CultureInfo.InvariantCulture) + "}";
                Thread.Sleep(100);
            }
            return "{\"idle\":false,\"timeoutMs\":" + timeoutMs.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string BuildStatusJson()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            return "{\"product\":\"QS3D-BricsCAD\",\"processId\":" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)
                   + ",\"bricscadVersion\":\"" + JsonEscape(Convert.ToString(Application.Version) ?? string.Empty)
                   + "\",\"activeDocument\":\"" + JsonEscape(document?.Name ?? string.Empty)
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
            var document = RequireDocument(); var view = document.Editor.GetCurrentView(); RECT rect; var hwnd = CurrentProcessWindow(); GetClientRect(hwnd, out rect);
            var active = Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? "0";
            return "{\"commandActive\":" + active + ",\"center\":{\"x\":" + Number(view.CenterPoint.X) + ",\"y\":" + Number(view.CenterPoint.Y)
                   + "},\"width\":" + Number(view.Width) + ",\"height\":" + Number(view.Height)
                   + ",\"clientWidth\":" + (rect.Right - rect.Left).ToString(CultureInfo.InvariantCulture)
                   + ",\"clientHeight\":" + (rect.Bottom - rect.Top).ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string BuildSelectionJson()
        {
            var document = RequireDocument(); var result = document.Editor.SelectImplied();
            if (result.Status != PromptStatus.OK || result.Value == null) return "[]";
            var output = new StringBuilder("["); var first = true;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in result.Value.GetObjectIds()) { if (!first) output.Append(','); first = false; output.Append(DescribeEntity(transaction, id, false)); }
            }
            return output.Append(']').ToString();
        }

        private static string BuildDatabaseSnapshotJson(int limit)
        {
            var document = RequireDocument(); var output = new StringBuilder(); output.Append("{\"limit\":").Append(limit).Append(",\"entities\":["); var count = 0;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in modelSpace) { if (count >= limit) break; if (count > 0) output.Append(','); output.Append(DescribeEntity(transaction, id, true)); count++; }
            }
            return output.Append("],\"truncated\":").Append(count >= limit ? "true" : "false").Append('}').ToString();
        }

        private static string DescribeEntity(Transaction transaction, ObjectId id, bool includeExtents)
        {
            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; var output = new StringBuilder();
            output.Append("{\"handle\":\"").Append(JsonEscape(id.Handle.ToString())).Append("\",\"type\":\"")
                .Append(JsonEscape(entity == null ? string.Empty : entity.GetType().Name)).Append("\",\"layer\":\"")
                .Append(JsonEscape(entity == null ? string.Empty : entity.Layer)).Append('"');
            if (entity != null && includeExtents) { output.Append(",\"extents\":"); try { output.Append(ExtentsJson(entity.GeometricExtents)); } catch { output.Append("null"); } }
            return output.Append('}').ToString();
        }

        private static string ExtentsJson(Extents3d e) => "{\"min\":" + PointJson(e.MinPoint) + ",\"max\":" + PointJson(e.MaxPoint) + "}";
        private static string PointJson(Point3d p) => "{\"x\":" + Number(p.X) + ",\"y\":" + Number(p.Y) + ",\"z\":" + Number(p.Z) + "}";
        private static string Number(double value) => double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("R", CultureInfo.InvariantCulture);

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document.");
            return document;
        }

        private sealed class CadWorkItem
        {
            public Func<string>? Action; public string Result = string.Empty; public Exception? Error; public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }

        private static string InvokeCad(Func<string> action)
        {
            var item = new CadWorkItem { Action = action };
            Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadWork, item);
            if (!item.Done.Wait(CadDispatchTimeoutMilliseconds)) throw new TimeoutException("Timed out waiting for the BricsCAD application context.");
            if (item.Error != null) throw new InvalidOperationException(item.Error.Message, item.Error);
            return item.Result;
        }

        private static void ExecuteCadWork(object data)
        {
            var item = (CadWorkItem)data;
            try { item.Result = item.Action == null ? string.Empty : item.Action(); } catch (Exception ex) { item.Error = ex; } finally { item.Done.Set(); }
        }

        private static List<Point2d> ParsePoints2d(string value)
        {
            var points = new List<Point2d>();
            foreach (var part in (value ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split(','); double x; double y;
                if (pair.Length != 2 || !double.TryParse(pair[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) || !double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    throw new InvalidOperationException("points must use invariant x,y;x,y format.");
                if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)) throw new InvalidOperationException("points must be finite.");
                points.Add(new Point2d(x, y));
            }
            return points;
        }

        private static double RequireDouble(string json, string property)
        {
            var match = NumberMatch(json, property); double value;
            if (!match.Success || !double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(property + " must be a finite number.");
            return value;
        }

        private static double ExtractDouble(string json, string property, double fallback)
        {
            var match = NumberMatch(json, property); double value;
            if (!match.Success || !double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value)) return fallback;
            return value;
        }

        private static Match NumberMatch(string json, string property) => Regex.Match(json ?? string.Empty,
            "\"" + Regex.Escape(property) + "\"\\s*:\\s*(?<value>-?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string ToolSuccess(string jsonValue)
        {
            var text = string.IsNullOrWhiteSpace(jsonValue) ? "{}" : jsonValue;
            return "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(text) + "\"}],\"isError\":false}";
        }
        private static string ToolError(string message) => "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(message ?? "MCP tool failed.") + "\"}],\"isError\":true}";
        private static string JsonRpcError(string id, int code, string message) => "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"error\":{\"code\":" + code.ToString(CultureInfo.InvariantCulture) + ",\"message\":\"" + JsonEscape(message) + "\"}}";

        private static string ExtractToolName(string json)
        {
            var source = json ?? string.Empty; var index = source.IndexOf("\"params\"", StringComparison.OrdinalIgnoreCase);
            return ExtractString(index >= 0 ? source.Substring(index) : source, "name");
        }
        private static string ExtractString(string json, string property)
        {
            var match = Regex.Match(json ?? string.Empty, "\"" + Regex.Escape(property) + "\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success ? JsonUnescape(match.Groups["value"].Value) : string.Empty;
        }
        private static bool ExtractBoolean(string json, string property)
        {
            var match = Regex.Match(json ?? string.Empty, "\"" + Regex.Escape(property) + "\"\\s*:\\s*(?<value>true|false)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success && string.Equals(match.Groups["value"].Value, "true", StringComparison.OrdinalIgnoreCase);
        }
        private static int ExtractInteger(string json, string property, int fallback, int minimum, int maximum)
        {
            var match = Regex.Match(json ?? string.Empty, "\"" + Regex.Escape(property) + "\"\\s*:\\s*(?<value>-?[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int value; if (!match.Success || !int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) return fallback;
            return Math.Max(minimum, Math.Min(maximum, value));
        }
        private static string ExtractId(string json)
        {
            var match = Regex.Match(json ?? string.Empty, "\"id\"\\s*:\\s*(?<value>\"(?:\\\\.|[^\"])*\"|-?[0-9]+(?:\\.[0-9]+)?|null)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success ? match.Groups["value"].Value : "null";
        }
        private static string JsonUnescape(string value) => string.IsNullOrEmpty(value) ? string.Empty : Regex.Unescape(value);

        internal static string JsonEscape(string value)
        {
            if (value == null) return string.Empty; var b = new StringBuilder(value.Length + 16);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': b.Append("\\\\"); break; case '"': b.Append("\\\""); break; case '\r': b.Append("\\r"); break;
                    case '\n': b.Append("\\n"); break; case '\t': b.Append("\\t"); break; case '\b': b.Append("\\b"); break; case '\f': b.Append("\\f"); break;
                    default: if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); else b.Append(c); break;
                }
            }
            return b.ToString();
        }

        private static void Audit(string tool, string detail)
        {
            try
            {
                var path = AuditFilePath; var dir = Path.GetDirectoryName(path); if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                var line = "{\"utc\":\"" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\",\"tool\":\"" + JsonEscape(tool)
                           + "\",\"detail\":\"" + JsonEscape(detail ?? string.Empty) + "\"}" + Environment.NewLine;
                File.AppendAllText(path, line, new UTF8Encoding(false));
            }
            catch { }
        }

        private static string ReadAuditTail(int limit)
        {
            try
            {
                if (!File.Exists(AuditFilePath)) return "{\"entries\":[]}";
                var lines = File.ReadAllLines(AuditFilePath, Encoding.UTF8); var start = Math.Max(0, lines.Length - limit); var b = new StringBuilder("{\"entries\":[");
                for (var i = start; i < lines.Length; i++) { if (i > start) b.Append(','); b.Append(lines[i]); }
                return b.Append("]}").ToString();
            }
            catch (Exception ex) { return "{\"entries\":[],\"error\":\"" + JsonEscape(ex.Message) + "\"}"; }
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string reason, string body, IDictionary<string, string>? extraHeaders)
        {
            var payload = string.IsNullOrEmpty(body) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body); var header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(reason).Append("\r\nConnection: close\r\nCache-Control: no-store\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: ").Append(payload.Length).Append("\r\n");
            if (extraHeaders != null) foreach (var pair in extraHeaders) header.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
            header.Append("\r\n"); var headerBytes = Encoding.ASCII.GetBytes(header.ToString()); stream.Write(headerBytes, 0, headerBytes.Length); if (payload.Length > 0) stream.Write(payload, 0, payload.Length); stream.Flush();
        }

        private static bool ConstantTimeEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? string.Empty); var b = Encoding.UTF8.GetBytes(right ?? string.Empty); var difference = a.Length ^ b.Length; var count = Math.Max(a.Length, b.Length);
            for (var i = 0; i < count; i++) difference |= (i < a.Length ? a[i] : (byte)0) ^ (i < b.Length ? b[i] : (byte)0);
            return difference == 0;
        }

        private static void EnsureBearerToken()
        {
            lock (Sync)
            {
                if (!string.IsNullOrWhiteSpace(_bearerToken)) return;
                var environment = (Environment.GetEnvironmentVariable(BearerEnvironment) ?? string.Empty).Trim();
                if (environment.Length >= 16) { _bearerToken = environment; _tokenSource = "environment " + BearerEnvironment; return; }
                var path = TokenFilePath;
                try
                {
                    if (File.Exists(path))
                    {
                        var saved = File.ReadAllText(path, Encoding.UTF8).Trim(); if (saved.Length >= 16) { _bearerToken = saved; _tokenSource = "saved token file"; return; }
                    }
                    var bytes = new byte[32]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes); var token = new StringBuilder(64);
                    foreach (var value in bytes) token.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                    var directory = Path.GetDirectoryName(path); if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Could not resolve MCP configuration directory.");
                    Directory.CreateDirectory(directory); File.WriteAllText(path, token.ToString(), new UTF8Encoding(false)); _bearerToken = token.ToString(); _tokenSource = "generated token file";
                }
                catch
                {
                    var bytes = new byte[32]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes); var token = new StringBuilder(64);
                    foreach (var value in bytes) token.Append(value.ToString("x2", CultureInfo.InvariantCulture)); _bearerToken = token.ToString(); _tokenSource = "ephemeral process token";
                }
            }
        }

        private static void SetLastError(string message) { lock (Sync) _lastError = message ?? string.Empty; }

        private sealed class HttpRequest
        {
            public HttpRequest(string method, string path, IDictionary<string, string> headers, string body) { Method = method; Path = path; Headers = headers; Body = body ?? string.Empty; }
            public string Method { get; } public string Path { get; } public IDictionary<string, string> Headers { get; } public string Body { get; }
        }

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
        [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    }
}
