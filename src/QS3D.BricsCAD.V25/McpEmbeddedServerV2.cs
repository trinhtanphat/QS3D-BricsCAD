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
using QS3D.Core.Agent;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Hardened embedded Streamable-HTTP MCP transport. The listener remains loopback-only;
    /// public reachability is provided by the QS3D-managed Cloudflare tunnel. CAD/UI work is
    /// delegated to McpCadAgentRuntime so the protocol/auth layer never executes arbitrary OS code.
    /// </summary>
    internal static class McpEmbeddedServer
    {
        private const int PreferredPort = 8765;
        private const int MaxPortAttempts = 16;
        private const int MaxHeaderBytes = 64 * 1024;
        private const int MaxBodyBytes = 1024 * 1024;
        private const int MaxConcurrentClients = 16;
        private const int AdmissionRejectWriteTimeoutMilliseconds = 1000;
        private const int MaxSessions = 128;
        private const string ModernProtocolVersion = "2026-07-28";
        private const string ProtocolVersion = "2025-11-25";
        private const string PreviousProtocolVersion = "2025-06-18";
        private const string LegacyProtocolVersion = "2025-03-26";
        private const string ServerVersion = "embedded-7";
        private const string BearerEnvironment = "QS3D_MCP_BEARER_TOKEN";
        private const string TokenFileName = "mcp-bearer-token.txt";
        private const string LocalTunnelAuthorizationHeader = "X-QS3D-MCP-Local-Authorization";

        private static readonly object Sync = new object();
        private static readonly object SessionSync = new object();
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly SemaphoreSlim ClientSlots = new SemaphoreSlim(MaxConcurrentClients, MaxConcurrentClients);
        private static readonly ConcurrentDictionary<string, SessionState> Sessions =
            new ConcurrentDictionary<string, SessionState>(StringComparer.Ordinal);

        private static TcpListener? _listener;
        private static Thread? _listenerThread;
        private static volatile bool _stopping;
        private static int _boundPort = PreferredPort;
        private static string _bearerToken = string.Empty;
        private static string _tokenSource = string.Empty;
        private static string _lastError = string.Empty;
        private static DateTime _lastOAuthMcpActivityUtc = DateTime.MinValue;
        private static string _lastOAuthMcpMethod = string.Empty;
        private static string _lastOAuthMcpPublicUrl = string.Empty;

        public static Uri Endpoint { get { return new Uri("http://127.0.0.1:" + Volatile.Read(ref _boundPort).ToString(CultureInfo.InvariantCulture) + "/mcp"); } }
        public static Uri HealthEndpoint { get { return new Uri("http://127.0.0.1:" + Volatile.Read(ref _boundPort).ToString(CultureInfo.InvariantCulture) + "/healthz"); } }
        public static bool IsPreferredPort { get { return Volatile.Read(ref _boundPort) == PreferredPort; } }
        public static string TokenFilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", TokenFileName); } }
        public static string AuditFilePath { get { return McpCadAgentRuntime.AuditFilePath; } }
        public static bool IsRunning { get { lock (Sync) return _listener != null && !_stopping; } }
        public static string LastError { get { lock (Sync) return _lastError; } }
        public static string TokenSource { get { EnsureBearerToken(); lock (Sync) return _tokenSource; } }
        public static string PublicUrl { get { return McpPublicEndpointResolver.Resolve(); } }
        public static DateTime LastOAuthMcpActivityUtc { get { lock (Sync) return _lastOAuthMcpActivityUtc; } }
        public static string LastOAuthMcpMethod { get { lock (Sync) return _lastOAuthMcpMethod; } }
        public static string LastOAuthMcpPublicUrl { get { lock (Sync) return _lastOAuthMcpPublicUrl; } }

        public static void Start()
        {
            lock (Sync)
            {
                if (_listener != null && !_stopping) return;
                EnsureBearerToken();
                _stopping = false;
                _lastError = string.Empty;
                _lastOAuthMcpActivityUtc = DateTime.MinValue;
                _lastOAuthMcpMethod = string.Empty;
                _lastOAuthMcpPublicUrl = string.Empty;
                McpCadAgentRuntime.ResetForServerStart();
                int boundPort;
                var listener = StartLoopbackListener(out boundPort);
                Volatile.Write(ref _boundPort, boundPort);
                _listener = listener;
                _listenerThread = new Thread(() => ServeLoop(listener)) { IsBackground = true, Name = "QS3D MCP loopback server v2" };
                _listenerThread.Start();
            }
        }

        private static TcpListener StartLoopbackListener(out int boundPort)
        {
            SocketException? lastAddressInUse = null;
            for (var offset = 0; offset < MaxPortAttempts; offset++)
            {
                var port = PreferredPort + offset;
                var listener = new TcpListener(IPAddress.Loopback, port);
                try
                {
                    listener.Server.NoDelay = true;
                    listener.Start(32);
                    boundPort = port;
                    return listener;
                }
                catch (SocketException ex)
                {
                    try { listener.Stop(); } catch { }
                    if (ex.SocketErrorCode != SocketError.AddressAlreadyInUse) throw;
                    lastAddressInUse = ex;
                }
            }

            boundPort = PreferredPort;
            var lastPort = PreferredPort + MaxPortAttempts - 1;
            throw new InvalidOperationException(
                "QS3D MCP could not bind any loopback port from " + PreferredPort.ToString(CultureInfo.InvariantCulture)
                + " through " + lastPort.ToString(CultureInfo.InvariantCulture)
                + ". Close the stale MCP/BricsCAD instance that owns those ports and retry.",
                lastAddressInUse);
        }

        public static void EnsureStarted() { if (!IsRunning) Start(); }

        public static void Stop()
        {
            Thread? thread;
            lock (Sync)
            {
                _stopping = true;
                McpCadAgentRuntime.StopAutomation();
                thread = _listenerThread;
                try { if (_listener != null) _listener.Stop(); } catch { }
                _listener = null;
                _listenerThread = null;
                Volatile.Write(ref _boundPort, PreferredPort);
                lock (SessionSync) Sessions.Clear();
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

        private static bool OwnsListener(TcpListener listener)
        {
            lock (Sync)
            {
                return !_stopping && ReferenceEquals(_listener, listener);
            }
        }

        private static void ServeLoop(TcpListener listener)
        {
            while (OwnsListener(listener))
            {
                TcpClient? client = null;
                try
                {
                    if (!OwnsListener(listener)) return;
                    client = listener.AcceptTcpClient();
                    if (!OwnsListener(listener))
                    {
                        try { client.Close(); } catch { }
                        client = null;
                        return;
                    }
                    client.NoDelay = true;
                    if (!ClientSlots.Wait(0))
                    {
                        TryWriteOverloadResponse(client);
                        client = null;
                        continue;
                    }
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                    client = null;
                }
                catch (SocketException ex)
                {
                    if (!OwnsListener(listener)) return;
                    SetLastError("socket: " + ex.Message);
                    Thread.Sleep(100);
                }
                catch (ObjectDisposedException) { return; }
                catch (Exception ex)
                {
                    if (!OwnsListener(listener)) return;
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

        private static void TryWriteOverloadResponse(TcpClient client)
        {
            if (client == null) return;
            try
            {
                using (var stream = client.GetStream())
                {
                    stream.WriteTimeout = AdmissionRejectWriteTimeoutMilliseconds;
                    TryWriteResponse(stream, 503, "Service Unavailable",
                        "{\"error\":\"MCP server is at concurrent-client capacity; retry later.\"}", null);
                }
            }
            catch { }
            finally { try { client.Close(); } catch { } }
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
                if (headerEnd + 4 > MaxHeaderBytes)
                    throw new HttpProtocolException(431, "Request Header Fields Too Large", "MCP HTTP headers exceed 64 KiB.");

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

                var target = requestParts[1].Trim();
                var queryIndex = target.IndexOf('?');
                var path = queryIndex >= 0 ? target.Substring(0, queryIndex) : target;
                var query = queryIndex >= 0 && queryIndex + 1 < target.Length ? target.Substring(queryIndex + 1) : string.Empty;
                return new HttpRequest(requestParts[0].Trim().ToUpperInvariant(), path, query, headers, bodyText);
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
                   || string.Equals(name, LocalTunnelAuthorizationHeader, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Origin", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Mcp-Session-Id", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "MCP-Protocol-Version", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Mcp-Method", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "Mcp-Name", StringComparison.OrdinalIgnoreCase);
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

        private static bool IsAllowedOrigin(IDictionary<string, string> headers, string publicMcpUrl)
        {
            string origin;
            if (!headers.TryGetValue("Origin", out origin)) return true;
            if (string.IsNullOrWhiteSpace(origin)) return false;

            Uri uri;
            if (!Uri.TryCreate(origin, UriKind.Absolute, out uri)) return false;
            if (!string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
                return false;
            if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/") return false;
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;
            return uri.IsLoopback || IsChatGptOrigin(uri) || IsSameOriginAsPublicMcp(uri, publicMcpUrl);
        }

        private static bool IsChatGptOrigin(Uri origin)
        {
            return origin != null
                   && string.Equals(origin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(origin.DnsSafeHost, "chatgpt.com", StringComparison.OrdinalIgnoreCase)
                   && origin.IsDefaultPort;
        }

        private static bool IsSameOriginAsPublicMcp(Uri origin, string publicMcpUrl)
        {
            if (origin == null || string.IsNullOrWhiteSpace(publicMcpUrl)) return false;
            Uri publicUri;
            if (!Uri.TryCreate(publicMcpUrl.Trim(), UriKind.Absolute, out publicUri)) return false;
            if (!string.Equals(publicUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
            return string.Equals(origin.Scheme, publicUri.Scheme, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(origin.DnsSafeHost, publicUri.DnsSafeHost, StringComparison.OrdinalIgnoreCase)
                   && origin.Port == publicUri.Port;
        }

        private static void HandleRequest(NetworkStream stream, HttpRequest request)
        {
            var publicMcpUrl = PublicUrl;
            McpOAuthHttpResponse oauthResponse;
            if (McpOAuthAuthorizationServer.TryHandle(
                request.Method,
                request.Path,
                request.Query,
                request.Headers,
                request.Body,
                publicMcpUrl,
                GetBearerToken(),
                out oauthResponse))
            {
                WriteResponse(stream, oauthResponse.StatusCode, oauthResponse.Reason, oauthResponse.Body,
                    oauthResponse.Headers, oauthResponse.ContentType);
                return;
            }

            if (!IsAllowedOrigin(request.Headers, publicMcpUrl))
            {
                WriteResponse(stream, 403, "Forbidden", "{\"error\":\"invalid MCP Origin\"}", null);
                return;
            }
            if (request.Method == "GET" && string.Equals(request.Path, "/healthz", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 200, "OK", "{\"ok\":true,\"service\":\"qs3d-bricscad-mcp\",\"running\":true,\"version\":\"" + ServerVersion + "\"}", null);
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
            bool oauthAccessToken;
            if (!Authorize(request.Headers, publicMcpUrl, out oauthAccessToken))
            {
                WriteResponse(stream, 401, "Unauthorized", JsonRpcError("null", -32001, "Bearer authorization required."),
                    new Dictionary<string, string> { ["WWW-Authenticate"] = McpOAuthAuthorizationServer.BuildBearerChallenge(publicMcpUrl) });
                return;
            }
            if (oauthAccessToken) RecordOAuthMcpActivity(request.Method, publicMcpUrl);
            if (request.Method == "DELETE")
            {
                string sessionId;
                if (!request.Headers.TryGetValue("Mcp-Session-Id", out sessionId) || string.IsNullOrWhiteSpace(sessionId))
                {
                    WriteResponse(stream, 400, "Bad Request", JsonRpcError("null", -32002, "Mcp-Session-Id is required."), null);
                    return;
                }
                string sessionError;
                int sessionStatusCode;
                if (!TryDeleteSession(request.Headers, sessionId, out sessionError, out sessionStatusCode))
                {
                    WriteResponse(stream, sessionStatusCode, sessionStatusCode == 404 ? "Not Found" : "Bad Request",
                        JsonRpcError("null", -32002, sessionError), null);
                    return;
                }
                WriteResponse(stream, 204, "No Content", string.Empty, null);
                return;
            }
            if (request.Method != "POST")
            {
                WriteResponse(stream, 405, "Method Not Allowed", "{\"error\":\"method not allowed\"}", new Dictionary<string, string> { ["Allow"] = "POST, DELETE" });
                return;
            }

            var trustedOpenAiTunnelRequest = IsValidLocalTunnelAuthorization(request.Headers);
            string contentType;
            var hasJsonContentType =
                request.Headers.TryGetValue("Content-Type", out contentType)
                && IsJsonContentType(contentType);
            if (!hasJsonContentType && !trustedOpenAiTunnelRequest)
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

            string requestProtocolVersion;
            var modernRequest = request.Headers.TryGetValue("MCP-Protocol-Version", out requestProtocolVersion)
                                && string.Equals(requestProtocolVersion, ModernProtocolVersion, StringComparison.Ordinal);
            if (modernRequest)
            {
                string routingError;
                if (!TryValidateModernRoutingHeaders(request.Headers, method, request.Body, out routingError))
                {
                    WriteResponse(stream, 400, "Bad Request", JsonRpcError(hasId ? id : "null", -32020, routingError), null);
                    return;
                }
                HandleModernRequest(stream, request, method, id, hasId);
                return;
            }

            if (string.Equals(method, "server/discover", StringComparison.Ordinal))
            {
                WriteResponse(stream, 400, "Bad Request",
                    JsonRpcError(hasId ? id : "null", -32022,
                        "Unsupported protocol version. Supported modern version: " + ModernProtocolVersion + "."), null);
                return;
            }

            if (string.Equals(method, "initialize", StringComparison.Ordinal))
            {
                HandleInitialize(stream, request.Body, id, hasId);
                return;
            }

            SessionState session;
            string sessionValidationError;
            int sessionValidationStatusCode;
            if (!TryValidateSession(request.Headers, out session, out sessionValidationError, out sessionValidationStatusCode))
            {
                WriteResponse(stream, sessionValidationStatusCode, sessionValidationStatusCode == 404 ? "Not Found" : "Bad Request",
                    JsonRpcError(hasId ? id : "null", -32002, sessionValidationError), null);
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
                WriteResponse(stream, 200, "OK", ToolsListResponse(id, false), ProtocolHeader(session));
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

        private static void HandleModernRequest(NetworkStream stream, HttpRequest request, string method, string id, bool hasId)
        {
            if (!hasId)
            {
                WriteResponse(stream, 202, "Accepted", string.Empty, ModernProtocolHeader());
                return;
            }
            if (string.Equals(method, "server/discover", StringComparison.Ordinal))
            {
                var result = "{\"jsonrpc\":\"2.0\",\"id\":" + id
                             + ",\"result\":{\"resultType\":\"complete\","
                             + "\"supportedVersions\":[\"" + ModernProtocolVersion + "\"],"
                             + "\"capabilities\":{\"tools\":{}},"
                             + "\"instructions\":\"QS3D full CAD agent. Ordinary mutations require confirmMutation=true; emergency stop and cancel remain available without confirmation.\","
                             + "\"ttlMs\":0,\"cacheScope\":\"private\"," + ModernServerInfoMeta() + "}}";
                WriteResponse(stream, 200, "OK", result, ModernProtocolHeader());
                return;
            }
            if (string.Equals(method, "ping", StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK",
                    "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{\"resultType\":\"complete\"," + ModernServerInfoMeta() + "}}",
                    ModernProtocolHeader());
                return;
            }
            if (string.Equals(method, "tools/list", StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", ToolsListResponse(id, true), ModernProtocolHeader());
                return;
            }
            if (string.Equals(method, "tools/call", StringComparison.Ordinal))
            {
                string toolName;
                string arguments;
                string error;
                if (!TryExtractToolCall(request.Body, out toolName, out arguments, out error))
                {
                    WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602, error), ModernProtocolHeader());
                    return;
                }
                var modernResult = AddModernCompleteResult(CallTool(toolName, arguments));
                WriteResponse(stream, 200, "OK",
                    "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + modernResult + "}",
                    ModernProtocolHeader());
                return;
            }
            WriteResponse(stream, 200, "OK", JsonRpcError(id, -32601, "Method not found."), ModernProtocolHeader());
        }

        private static bool TryValidateModernRoutingHeaders(
            IDictionary<string, string> headers,
            string method,
            string body,
            out string error)
        {
            error = string.Empty;
            string protocol;
            if (!headers.TryGetValue("MCP-Protocol-Version", out protocol)
                || !string.Equals(protocol, ModernProtocolVersion, StringComparison.Ordinal))
            {
                error = "MCP-Protocol-Version must be " + ModernProtocolVersion + ".";
                return false;
            }
            if (!TryValidateModernRequestMeta(body, protocol, out error)) return false;
            string headerMethod;
            if (!headers.TryGetValue("Mcp-Method", out headerMethod)
                || !string.Equals(headerMethod, method, StringComparison.Ordinal))
            {
                error = "Mcp-Method must match the JSON-RPC method.";
                return false;
            }
            if (string.Equals(method, "tools/call", StringComparison.Ordinal))
            {
                string toolName;
                string arguments;
                string toolError;
                if (!TryExtractToolCall(body, out toolName, out arguments, out toolError))
                {
                    error = toolError;
                    return false;
                }
                string headerName;
                if (!headers.TryGetValue("Mcp-Name", out headerName)
                    || !string.Equals(headerName, toolName, StringComparison.Ordinal))
                {
                    error = "Mcp-Name must match tools/call params.name.";
                    return false;
                }
            }
            return true;
        }

        private static bool TryValidateModernRequestMeta(string body, string protocol, out string error)
        {
            error = string.Empty;
            string parameters;
            if (!TryExtractObjectProperty(body, "params", out parameters))
            {
                error = "Modern MCP requests require object params containing _meta.";
                return false;
            }
            string metadata;
            if (!TryExtractObjectProperty(parameters, "_meta", out metadata))
            {
                error = "Modern MCP requests require params._meta.";
                return false;
            }
            string metaProtocol;
            try { metaProtocol = McpTopLevelJson.ExtractString(metadata, "io.modelcontextprotocol/protocolVersion"); }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
            if (!string.Equals(metaProtocol, protocol, StringComparison.Ordinal))
            {
                error = "params._meta protocolVersion must match MCP-Protocol-Version.";
                return false;
            }

            string rawCapabilities;
            bool found;
            if (!TryFindPropertyValue(metadata, "io.modelcontextprotocol/clientCapabilities", out rawCapabilities, out found, out error)) return false;
            if (!found || !LooksLikeJsonObject(rawCapabilities))
            {
                error = "params._meta clientCapabilities must be an object.";
                return false;
            }

            string rawClientInfo;
            if (!TryFindPropertyValue(metadata, "io.modelcontextprotocol/clientInfo", out rawClientInfo, out found, out error)) return false;
            if (found && !LooksLikeJsonObject(rawClientInfo))
            {
                error = "params._meta clientInfo must be an object when present.";
                return false;
            }
            return true;
        }

        private static string AddModernCompleteResult(string result)
        {
            var raw = (result ?? string.Empty).Trim();
            if (raw.Length >= 2 && raw[0] == '{' && raw[raw.Length - 1] == '}')
            {
                var inner = raw.Substring(1, raw.Length - 2);
                return "{\"resultType\":\"complete\""
                       + (inner.Length == 0 ? string.Empty : "," + inner)
                       + "," + ModernServerInfoMeta() + "}";
            }
            return "{\"resultType\":\"complete\",\"content\":[{\"type\":\"text\",\"text\":\""
                   + JsonEscape(raw) + "\"}],\"isError\":true," + ModernServerInfoMeta() + "}";
        }

        private static string ModernServerInfoMeta()
        {
            return "\"_meta\":{\"io.modelcontextprotocol/serverInfo\":{\"name\":\"qs3d-bricscad\",\"version\":\""
                   + ServerVersion + "\"}}";
        }

        private static IDictionary<string, string> ModernProtocolHeader()
        {
            return new Dictionary<string, string> { ["MCP-Protocol-Version"] = ModernProtocolVersion };
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
                && !string.Equals(requested, PreviousProtocolVersion, StringComparison.Ordinal)
                && !string.Equals(requested, LegacyProtocolVersion, StringComparison.Ordinal))
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(id, -32602,
                    "Unsupported initialize protocolVersion. Supported: " + ProtocolVersion + ", " + PreviousProtocolVersion + ", " + LegacyProtocolVersion + "."), null);
                return;
            }
            string sessionId;
            if (!TryCreateSession(requested, out sessionId))
            {
                WriteResponse(stream, 200, "OK", JsonRpcError(id, -32003, "MCP session capacity reached."), null);
                return;
            }
            var result = "{\"jsonrpc\":\"2.0\",\"id\":" + id
                         + ",\"result\":{\"protocolVersion\":\"" + requested
                         + "\",\"capabilities\":{\"tools\":{\"listChanged\":false}},"
                         + "\"serverInfo\":{\"name\":\"qs3d-bricscad\",\"version\":\"" + ServerVersion + "\"},"
                         + "\"instructions\":\"QS3D full CAD agent. Prefer direct CAD API tools, use bounded command workflows for advanced native features, and BricsCAD-process UI input only as fallback. All ordinary mutations require confirmMutation=true. Emergency stop/cancel remain available without confirmation.\"}}";
            WriteResponse(stream, 200, "OK", result, new Dictionary<string, string>
            {
                ["Mcp-Session-Id"] = sessionId,
                ["MCP-Protocol-Version"] = requested
            });
        }

        private static string ToolsListResponse(string id, bool modern)
        {
            var tools = new List<string>
            {
                Tool("connector_info", "Return embedded MCP endpoint, protocol, public endpoint and automation state.", ""),
                Tool("mcp_status", "Return separated MCP, BricsCAD, CAD-direct, desktop and QS3D-domain capability state.", ""),
                Tool("bricscad_status", "Read privacy-safe BricsCAD host/document status without QS3D business state.", ""),
                Tool("qs3d_status", "Deprecated compatibility alias for QS3D domain-only status.", ""),
                Tool("qs3d_domain_status", "Read QS3D business-domain health and context without CAD host fields.", ""),
                Tool("cad_active_document", "Read privacy-safe active document identity without local filesystem path.", ""),
                Tool("cad_selection", "Read current implied selection handles/types/layers.", ""),
                Tool("cad_database_snapshot", "Read bounded ModelSpace entity snapshot.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":1000}"),
                Tool("cad_entity_inspect", "Inspect one entity by hexadecimal handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32}", "handle"),
                Tool("cad_view_state", "Read command-active and current view/window state.", ""),
                Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "\"timeoutMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":7000,\"default\":5000}"),
                Tool("cad_sysvar", "Read one privacy-safe allowlisted BricsCAD system variable.", "\"name\":{\"type\":\"string\",\"enum\":[\"CMDACTIVE\",\"INSUNITS\",\"CLAYER\",\"CTAB\",\"TILEMODE\",\"DWGNAME\",\"CVPORT\",\"ORTHOMODE\",\"OSMODE\"]}", "name"),
                Tool("cad_writer_acquire", "Acquire the process-global DWG writer lease for a bounded multi-step MCP mutation workflow.", "\"leaseSeconds\":{\"type\":\"integer\",\"minimum\":15,\"maximum\":300,\"default\":120}"),
                Tool("cad_writer_status", "Read process-global DWG writer lease/native-command barrier status without exposing tokens.", ""),
                Tool("cad_writer_release", "Release the active DWG writer lease using the matching opaque token.", "", "writerToken"),
                Tool("cad_create_line", "Create native Line in ModelSpace.", Numeric("x1","y1","z1","x2","y2","z2") + CommonLayerConfirm(), "x1","y1","x2","y2","confirmMutation"),
                Tool("cad_create_circle", "Create native Circle in ModelSpace.", Numeric("x","y","z","radius") + CommonLayerConfirm(), "x","y","radius","confirmMutation"),
                Tool("cad_create_arc", "Create native Arc in ModelSpace from center/radius/start/end degrees.", Numeric("x","y","z","radius","startAngleDeg","endAngleDeg") + CommonLayerConfirm(), "x","y","radius","startAngleDeg","endAngleDeg","confirmMutation"),
                Tool("cad_create_polyline", "Create native 2D Polyline; points use x,y;x,y format.", "\"points\":{\"type\":\"string\",\"maxLength\":16000},\"closed\":{\"type\":\"boolean\"},\"elevation\":{\"type\":\"number\"}" + CommonLayerConfirm(), "points","confirmMutation"),
                Tool("cad_create_text", "Create native single-line DBText.", "\"text\":{\"type\":\"string\",\"maxLength\":4000}," + Numeric("x","y","z","height","rotationDeg") + CommonLayerConfirm(), "text","x","y","height","confirmMutation"),
                Tool("cad_create_mtext", "Create native multiline MText.", "\"text\":{\"type\":\"string\",\"maxLength\":16000}," + Numeric("x","y","z","height","width","rotationDeg") + CommonLayerConfirm(), "text","x","y","height","confirmMutation"),
                Tool("cad_entity_transform", "Move, rotate or scale one entity by handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"action\":{\"type\":\"string\",\"enum\":[\"move\",\"rotate\",\"scale\"]}," + Numeric("dx","dy","dz","angleDeg","factor") + "," + ConfirmProperty(), "handle","action","confirmMutation"),
                Tool("cad_entity_delete", "Erase one entity by handle.", "\"handle\":{\"type\":\"string\",\"maxLength\":32}," + ConfirmProperty(), "handle","confirmMutation"),
                Tool("cad_entity_set_layer", "Move one entity to a layer, creating that layer if needed.", "\"handle\":{\"type\":\"string\",\"maxLength\":32},\"layer\":{\"type\":\"string\",\"maxLength\":255}," + ConfirmProperty(), "handle","layer","confirmMutation"),
                Tool("cad_layer", "Create layer or make layer current.", "\"action\":{\"type\":\"string\",\"enum\":[\"create\",\"set_current\"]},\"name\":{\"type\":\"string\",\"maxLength\":255}," + ConfirmProperty(), "action","name","confirmMutation"),
                Tool("cad_command_catalog", "Return allowlisted native commands available to cad_command_sequence.", ""),
                Tool("cad_command_sequence", "Run one allowlisted BricsCAD command with bounded newline-delimited prompt inputs.", "\"command\":{\"type\":\"string\",\"maxLength\":40},\"inputs\":{\"type\":\"string\",\"maxLength\":16000}," + ConfirmProperty(), "command","confirmMutation"),
                Tool("qs3d_run_command", "Run one QS3D* business command name.", "\"command\":{\"type\":\"string\",\"pattern\":\"^QS3D[A-Za-z0-9_]*$\",\"maxLength\":80}," + ConfirmProperty(), "command","confirmMutation"),
                Tool("qs3d_place_single_footing", "Place the active QS3D Móng đơn Family at drawing x,y using active Floor semantics.", "\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}," + ConfirmProperty(), "x","y","confirmMutation"),
                Tool("cad_ui_click", "Click inside active BricsCAD-process window only.", "\"x\":{\"type\":\"integer\"},\"y\":{\"type\":\"integer\"},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"count\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":3}," + ConfirmProperty(), "x","y","button","confirmMutation"),
                Tool("cad_ui_type", "Type bounded Unicode text into active BricsCAD-process window only.", "\"text\":{\"type\":\"string\",\"maxLength\":8000},\"pressEnter\":{\"type\":\"boolean\"}," + ConfirmProperty(), "text","confirmMutation"),
                Tool("cad_ui_key", "Press named key in active BricsCAD-process window only.", "\"key\":{\"type\":\"string\",\"maxLength\":16},\"ctrl\":{\"type\":\"boolean\"},\"alt\":{\"type\":\"boolean\"},\"shift\":{\"type\":\"boolean\"}," + ConfirmProperty(), "key","confirmMutation"),
                Tool("cad_agent_stop", "Emergency-stop mutations/UI input and deliver ESC twice; no confirmation required.", ""),
                Tool("cad_agent_resume", "Resume autonomous mutations after emergency stop.", ConfirmProperty(), "confirmMutation"),
                Tool("cad_audit_tail", "Read latest bounded local mutation audit entries.", "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}"),
                Tool("cad_cancel_command", "Deliver ESC twice to cancel current BricsCAD command; no confirmation required.", "")
            };
            foreach (var descriptor in McpDesktopAutomationRuntime.ToolDescriptors())
                tools.Add(WithToolAnnotations(descriptor));
            foreach (var descriptor in McpCadDirectModelRuntime.ToolDescriptors())
                tools.Add(WithToolAnnotations(descriptor));
            var resultPrefix = modern
                ? "{\"resultType\":\"complete\",\"tools\":["
                : "{\"tools\":[";
            var resultSuffix = modern
                ? "],\"ttlMs\":0,\"cacheScope\":\"private\"," + ModernServerInfoMeta() + "}"
                : "]}";
            return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + resultPrefix + string.Join(",", tools) + resultSuffix + "}";
        }

        private static string CallTool(string tool, string arguments)
        {
            try
            {
                if (string.Equals(tool, "connector_info", StringComparison.Ordinal))
                {
                    var publicUrl = PublicUrl;
                    return ToolSuccess("{\"protocol\":\"" + ModernProtocolVersion + "\",\"legacyProtocol\":\"" + ProtocolVersion + "\",\"serverVersion\":\"" + ServerVersion
                        + "\",\"endpoint\":\"" + JsonEscape(Endpoint.ToString())
                        + "\",\"publicUrl\":\"" + JsonEscape(publicUrl) + "\",\"auth\":\"oauth2.1+legacy_bearer\",\"singleRepository\":true,"
                        + "\"fullCadAgent\":true,\"structuredContent\":true,\"modernMetaEnvelope\":true,\"toolAnnotations\":true,\"automationStopped\":"
                        + (McpCadAgentRuntime.AutomationStopped ? "true" : "false") + "}");
                }
                if (string.Equals(tool, "cad_writer_acquire", StringComparison.Ordinal))
                    return ToolSuccess(McpCadMutationCoordinator.AcquireWriterLease(
                        WriterLeaseSeconds(arguments),
                        detail => McpCadAgentRuntime.AuditDomainMutation("cad_writer_acquire", detail)));
                if (string.Equals(tool, "cad_writer_status", StringComparison.Ordinal))
                    return ToolSuccess(McpCadMutationCoordinator.StatusJson());
                if (string.Equals(tool, "cad_writer_release", StringComparison.Ordinal))
                    return ToolSuccess(McpCadMutationCoordinator.ReleaseWriterLease(
                        McpTopLevelJson.ExtractString(arguments ?? "{}", "writerToken"),
                        detail => McpCadAgentRuntime.AuditDomainMutation("cad_writer_release", detail)));
                var runtimeResult = McpCadAgentRuntime.Call(tool, arguments);
                if (string.Equals(tool, "desktop_screenshot", StringComparison.Ordinal))
                    return ScreenshotToolSuccess(runtimeResult);
                return ToolSuccess(runtimeResult);
            }
            catch (McpToolContractException ex)
            {
                var lane = McpToolCapabilityContract.LaneName(ex.Lane);
                var repairJson = McpSelfHealingRepairRuntime.RecordFailure(
                    tool, ex.Code, lane, ex.Message, ex, true);
                return ToolError(ex.Code, lane, ex.Message, repairJson);
            }
            catch (Exception ex)
            {
                var failure = McpToolCapabilityContract.ClassifyFailure(tool, ex);
                var lane = McpToolCapabilityContract.LaneName(failure.Lane);
                var repairJson = McpSelfHealingRepairRuntime.RecordFailure(
                    tool, failure.Code, lane, failure.Message, ex, false);
                return ToolError(failure.Code, lane, failure.Message, repairJson);
            }
        }

        private static string ScreenshotToolSuccess(string jsonValue)
        {
            var raw = string.IsNullOrWhiteSpace(jsonValue) ? "{}" : jsonValue.Trim();
            var pngBase64 = McpTopLevelJson.ExtractString(raw, "pngBase64");
            var mimeType = McpTopLevelJson.ExtractString(raw, "mimeType");
            if (string.IsNullOrWhiteSpace(pngBase64))
                throw new InvalidOperationException("Screenshot result did not contain PNG image data.");
            if (!string.Equals(mimeType, "image/png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Screenshot result MIME type was not image/png.");

            var scope = McpTopLevelJson.ExtractString(raw, "scope");
            var windowHandle = McpTopLevelJson.ExtractString(raw, "windowHandle");
            var width = RequiredJsonInteger(raw, "width");
            var height = RequiredJsonInteger(raw, "height");
            var bytes = RequiredJsonInteger(raw, "bytes");
            var metadata = "{\"scope\":\"" + JsonEscape(scope)
                           + "\",\"windowHandle\":\"" + JsonEscape(windowHandle)
                           + "\",\"mimeType\":\"image/png\",\"width\":" + width.ToString(CultureInfo.InvariantCulture)
                           + ",\"height\":" + height.ToString(CultureInfo.InvariantCulture)
                           + ",\"bytes\":" + bytes.ToString(CultureInfo.InvariantCulture) + "}";
            return "{\"content\":[{\"type\":\"image\",\"data\":\"" + JsonEscape(pngBase64)
                   + "\",\"mimeType\":\"image/png\"}],\"structuredContent\":{\"data\":" + metadata + "},\"isError\":false}";
        }

        private static int RequiredJsonInteger(string json, string property)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(json, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) throw new InvalidOperationException("Screenshot result is missing " + property + ".");
            return value;
        }

        private static int WriterLeaseSeconds(string json)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(json ?? "{}", "leaseSeconds", out value, out found, out error))
                throw new InvalidOperationException(error);
            return found ? value : 120;
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

        private static bool LooksLikeJsonObject(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var trimmed = value.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[trimmed.Length - 1] == '}';
        }

        private static string ToolError(string code, string lane, string message, string repairJson = null)
        {
            var safeCode = string.IsNullOrWhiteSpace(code) ? McpToolCapabilityContract.ToolFailedCode : code;
            var safeLane = string.IsNullOrWhiteSpace(lane) ? "unknown" : lane;
            var safeMessage = string.IsNullOrWhiteSpace(message) ? "MCP tool failed." : message;
            var repair = string.IsNullOrWhiteSpace(repairJson) ? string.Empty : ",\"repair\":" + repairJson;
            return "{\"content\":[{\"type\":\"text\",\"text\":\"" + JsonEscape(safeCode + ": " + safeMessage)
                   + "\"}],\"structuredContent\":{\"error\":{\"code\":\"" + JsonEscape(safeCode)
                   + "\",\"lane\":\"" + JsonEscape(safeLane) + "\",\"message\":\"" + JsonEscape(safeMessage)
                   + "\"" + repair + "}},\"isError\":true}";
        }

        private static string ExecutionModeProperties()
        {
            return "\"executionMode\":{\"type\":\"string\",\"enum\":[\"AUTO\",\"CAD_DIRECT\",\"QS3D_DOMAIN\"]}"
                   + ",\"execution_mode\":{\"type\":\"string\",\"enum\":[\"AUTO\",\"CAD_DIRECT\",\"QS3D_DOMAIN\"]}";
        }

        private static string WriterTokenProperty()
        {
            return "\"writerToken\":{\"type\":\"string\",\"pattern\":\"^[0-9A-Fa-f]{32}$\",\"minLength\":32,\"maxLength\":32}";
        }

        private static string MergeToolProperties(string properties)
        {
            var common = ExecutionModeProperties() + "," + WriterTokenProperty();
            return string.IsNullOrWhiteSpace(properties) ? common : common + "," + properties;
        }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + JsonEscape(name) + "\",\"description\":\"" + JsonEscape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + MergeToolProperties(properties)
                   + "},\"additionalProperties\":false" + requiredJson + "},\"annotations\":" + ToolAnnotations(name) + "}";
        }

        private static string WithExecutionModeProperties(string descriptor)
        {
            var raw = (descriptor ?? string.Empty).Trim();
            if (!LooksLikeJsonObject(raw)) return raw;
            var additions = new List<string>();
            if (raw.IndexOf("\"executionMode\"", StringComparison.Ordinal) < 0)
                additions.Add(ExecutionModeProperties());
            if (raw.IndexOf("\"writerToken\"", StringComparison.Ordinal) < 0)
                additions.Add(WriterTokenProperty());
            if (additions.Count == 0) return raw;
            const string marker = "\"properties\":{";
            var index = raw.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) return raw;
            var insertion = index + marker.Length;
            var common = string.Join(",", additions);
            var suffix = insertion < raw.Length && raw[insertion] == '}' ? common : common + ",";
            return raw.Insert(insertion, suffix);
        }

        private static string WithToolAnnotations(string descriptor)
        {
            var raw = (descriptor ?? string.Empty).Trim();
            if (!LooksLikeJsonObject(raw)) return raw;
            raw = WithExecutionModeProperties(raw);
            if (raw.IndexOf("\"annotations\"", StringComparison.Ordinal) >= 0) return raw;
            string name;
            try { name = McpTopLevelJson.ExtractString(raw, "name"); }
            catch (InvalidOperationException) { return raw; }
            return raw.Substring(0, raw.Length - 1) + ",\"annotations\":" + ToolAnnotations(name) + "}";
        }

        private static string ToolAnnotations(string name)
        {
            var readOnly = IsReadOnlyTool(name);
            var destructive = !readOnly && IsDestructiveTool(name);
            var idempotent = readOnly || IsIdempotentMutationTool(name);
            var openWorld = (name ?? string.Empty).StartsWith("desktop_", StringComparison.Ordinal);
            return "{\"readOnlyHint\":" + JsonBool(readOnly)
                   + ",\"destructiveHint\":" + JsonBool(destructive)
                   + ",\"idempotentHint\":" + JsonBool(idempotent)
                   + ",\"openWorldHint\":" + JsonBool(openWorld) + "}";
        }

        private static bool IsReadOnlyTool(string name)
        {
            switch (name ?? string.Empty)
            {
                case "connector_info":
                case "mcp_status":
                case "bricscad_status":
                case "qs3d_status":
                case "qs3d_domain_status":
                case "cad_active_document":
                case "cad_selection":
                case "cad_database_snapshot":
                case "cad_entity_inspect":
                case "cad_view_state":
                case "cad_wait_idle":
                case "cad_sysvar":
                case "cad_writer_status":
                case "cad_command_catalog":
                case "cad_audit_tail":
                case "desktop_cursor_position":
                case "desktop_window_list":
                case "desktop_foreground_window":
                case "desktop_wait_for_window":
                case "desktop_clipboard_read":
                case "desktop_screenshot":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsDestructiveTool(string name)
        {
            switch (name ?? string.Empty)
            {
                case "cad_entity_transform":
                case "cad_entity_delete":
                case "cad_entity_set_layer":
                case "cad_command_sequence":
                case "qs3d_run_command":
                case "qs3d_place_single_footing":
                case "cad_ui_click":
                case "cad_ui_type":
                case "cad_ui_key":
                case "desktop_window_focus":
                case "desktop_mouse_move":
                case "desktop_mouse_click":
                case "desktop_mouse_scroll":
                case "desktop_mouse_drag":
                case "desktop_type":
                case "desktop_key":
                case "desktop_clipboard_write":
                case "desktop_sequence":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsIdempotentMutationTool(string name)
        {
            switch (name ?? string.Empty)
            {
                case "cad_agent_stop":
                case "cad_agent_resume":
                case "cad_cancel_command":
                    return true;
                default:
                    return false;
            }
        }

        private static string JsonBool(bool value) { return value ? "true" : "false"; }

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

        private static bool IsValidLocalTunnelAuthorization(IDictionary<string, string> headers)
        {
            if (McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel) return false;
            string authorization;
            if (!headers.TryGetValue(LocalTunnelAuthorizationHeader, out authorization)) return false;
            string token;
            if (!TryExtractBearerToken(authorization, out token)) return false;
            return ConstantTimeEquals(token, GetBearerToken());
        }

        private static bool Authorize(IDictionary<string, string> headers, string publicMcpUrl, out bool oauthAccessToken)
        {
            oauthAccessToken = false;

            if (McpTransportCoordinator.SelectedProvider == McpTransportProvider.OpenAiSecureTunnel
                && headers.ContainsKey(LocalTunnelAuthorizationHeader))
            {
                return IsValidLocalTunnelAuthorization(headers);
            }

            string authorization;
            if (!headers.TryGetValue("Authorization", out authorization)) return false;
            string bearerToken;
            if (!TryExtractBearerToken(authorization, out bearerToken)) return false;
            if (ConstantTimeEquals(bearerToken, GetBearerToken())) return true;
            if (!McpOAuthAuthorizationServer.TryValidateAccessToken(headers, publicMcpUrl, GetBearerToken())) return false;
            oauthAccessToken = true;
            return true;
        }

        private static bool TryExtractBearerToken(string authorization, out string token)
        {
            token = string.Empty;
            const string prefix = "Bearer ";
            if (string.IsNullOrWhiteSpace(authorization)
                || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            token = authorization.Substring(prefix.Length).Trim();
            return token.Length > 0;
        }

        private static void RecordOAuthMcpActivity(string method, string publicMcpUrl)
        {
            var safeMethod = (method ?? string.Empty).Trim().ToUpperInvariant();
            if (safeMethod.Length > 16) safeMethod = safeMethod.Substring(0, 16);
            var safePublicUrl = (publicMcpUrl ?? string.Empty).Trim();
            if (safePublicUrl.Length > 2048) safePublicUrl = safePublicUrl.Substring(0, 2048);
            lock (Sync)
            {
                _lastOAuthMcpActivityUtc = DateTime.UtcNow;
                _lastOAuthMcpMethod = safeMethod;
                _lastOAuthMcpPublicUrl = safePublicUrl;
            }
        }

        private static bool TryCreateSession(string protocolVersion, out string sessionId)
        {
            lock (SessionSync)
            {
                CleanupSessionsLocked();
                if (Sessions.Count >= MaxSessions)
                {
                    sessionId = string.Empty;
                    return false;
                }
                do
                {
                    sessionId = Guid.NewGuid().ToString("N");
                }
                while (!Sessions.TryAdd(sessionId, new SessionState(DateTime.UtcNow, protocolVersion)));
                return true;
            }
        }

        private static bool TryDeleteSession(
            IDictionary<string, string> headers,
            string sessionId,
            out string error,
            out int statusCode)
        {
            error = string.Empty;
            statusCode = 400;
            lock (SessionSync)
            {
                CleanupSessionsLocked();
                SessionState stored;
                if (!Sessions.TryGetValue(sessionId, out stored))
                {
                    statusCode = 404;
                    error = "Unknown or expired MCP session.";
                    return false;
                }
                if (!TryValidateProtocolVersionHeader(headers, stored.ProtocolVersion, out error))
                    return false;
                SessionState ignored;
                if (!Sessions.TryRemove(sessionId, out ignored))
                {
                    statusCode = 404;
                    error = "Unknown or expired MCP session.";
                    return false;
                }
                return true;
            }
        }

        private static bool TryValidateSession(
            IDictionary<string, string> headers,
            out SessionState state,
            out string error,
            out int statusCode)
        {
            state = null!;
            error = string.Empty;
            statusCode = 400;
            string sessionId;
            if (!headers.TryGetValue("Mcp-Session-Id", out sessionId) || string.IsNullOrWhiteSpace(sessionId))
            {
                error = "Mcp-Session-Id is required after initialize.";
                return false;
            }

            lock (SessionSync)
            {
                CleanupSessionsLocked();
                SessionState stored;
                if (!Sessions.TryGetValue(sessionId, out stored))
                {
                    statusCode = 404;
                    error = "Unknown or expired MCP session.";
                    return false;
                }
                if (!TryValidateProtocolVersionHeader(headers, stored.ProtocolVersion, out error))
                    return false;
                state = new SessionState(DateTime.UtcNow, stored.ProtocolVersion);
                if (!Sessions.TryUpdate(sessionId, state, stored))
                {
                    statusCode = 404;
                    state = null!;
                    error = "Unknown or expired MCP session.";
                    return false;
                }
                return true;
            }
        }

        private static bool TryValidateProtocolVersionHeader(
            IDictionary<string, string> headers,
            string expectedProtocolVersion,
            out string error)
        {
            error = string.Empty;
            string version;
            if (!headers.TryGetValue("MCP-Protocol-Version", out version)) return true;
            if (!string.Equals(version, expectedProtocolVersion, StringComparison.Ordinal))
            {
                error = "MCP-Protocol-Version is invalid or does not match initialized session.";
                return false;
            }
            return true;
        }

        private static void CleanupSessionsLocked()
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

        private static void WriteResponse(
            NetworkStream stream,
            int statusCode,
            string reason,
            string body,
            IDictionary<string, string>? extraHeaders,
            string? contentType = null)
        {
            var payload = string.IsNullOrEmpty(body) ? new byte[0] : Encoding.UTF8.GetBytes(body);
            var header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(reason)
                .Append("\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\n");
            if (payload.Length > 0)
            {
                var safeContentType = string.IsNullOrWhiteSpace(contentType) ? "application/json; charset=utf-8" : contentType!.Trim();
                if (safeContentType.Length > 128 || safeContentType.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                    safeContentType = "application/json; charset=utf-8";
                header.Append("Content-Type: ").Append(safeContentType).Append("\r\n");
            }
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

        private static void TryWriteResponse(NetworkStream stream, int statusCode, string reason, string body, IDictionary<string, string>? extraHeaders)
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

                var generated = GenerateToken();
                PersistBearerTokenAtomically(path, generated);
                _bearerToken = generated;
                _tokenSource = "generated verified token file";
            }
        }

        private static void PersistBearerTokenAtomically(string path, string token)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Could not resolve MCP config directory.");
            Directory.CreateDirectory(directory);

            var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(token);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(path)) File.Replace(tempPath, path, null, true);
                else File.Move(tempPath, path);

                var verified = File.ReadAllText(path, Encoding.UTF8).Trim();
                if (!ConstantTimeEquals(verified, token))
                    throw new InvalidOperationException("MCP bearer token persistence verification failed.");
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
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
            public HttpRequest(string method, string path, string query, IDictionary<string, string> headers, string body)
            { Method = method; Path = path; Query = query ?? string.Empty; Headers = headers; Body = body ?? string.Empty; }
            public string Method { get; private set; }
            public string Path { get; private set; }
            public string Query { get; private set; }
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
