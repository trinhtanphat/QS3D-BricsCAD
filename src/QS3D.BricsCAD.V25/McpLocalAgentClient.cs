using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Local-only MCP client used by the Agent Center for end-to-end protocol checks,
    /// read-only self tests and emergency controls. It talks to the same loopback endpoint
    /// used by the public tunnel and never bypasses MCP authentication/session semantics.
    /// </summary>
    internal static class McpLocalAgentClient
    {
        private const string ProtocolVersion = "2025-06-18";
        private const int MaxResponseBytes = 4 * 1024 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string CallOne(Uri endpoint, int timeoutMilliseconds, string tool, string argumentsJson)
        {
            string? session = null;
            try
            {
                session = Initialize(endpoint, timeoutMilliseconds);
                NotifyInitialized(endpoint, timeoutMilliseconds, session);
                return Call(endpoint, timeoutMilliseconds, session, tool, argumentsJson);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(session))
                {
                    try { Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, session); } catch { }
                }
            }
        }

        public static string RunReadOnlySelfTest(Uri endpoint, int timeoutMilliseconds)
        {
            string? session = null;
            try
            {
                session = Initialize(endpoint, timeoutMilliseconds);
                NotifyInitialized(endpoint, timeoutMilliseconds, session);

                var list = Send(endpoint, "POST",
                    "{\"jsonrpc\":\"2.0\",\"id\":20,\"method\":\"tools/list\",\"params\":{}}",
                    timeoutMilliseconds,
                    session);
                RequireSuccess(list, "tools/list");

                var required = new[]
                {
                    "connector_info", "qs3d_status", "cad_active_document", "cad_selection",
                    "cad_database_snapshot", "cad_entity_inspect", "cad_view_state", "cad_wait_idle", "cad_sysvar",
                    "cad_create_line", "cad_create_circle", "cad_create_arc", "cad_create_polyline", "cad_create_text", "cad_create_mtext",
                    "cad_entity_transform", "cad_entity_delete", "cad_entity_set_layer", "cad_layer", "cad_command_catalog", "cad_command_sequence",
                    "qs3d_run_command", "cad_ui_click", "cad_ui_type", "cad_ui_key",
                    "cad_agent_stop", "cad_agent_resume", "cad_audit_tail", "cad_cancel_command",
                    "desktop_cursor_position", "desktop_window_list", "desktop_foreground_window", "desktop_window_focus",
                    "desktop_mouse_move", "desktop_mouse_click", "desktop_mouse_scroll", "desktop_type", "desktop_key",
                    "desktop_clipboard_read", "desktop_clipboard_write", "desktop_screenshot"
                };

                var missing = new List<string>();
                foreach (var name in required)
                {
                    if (list.Body.IndexOf("\\\"name\\\":\\\"" + name + "\\\"", StringComparison.Ordinal) < 0
                        && list.Body.IndexOf("\"name\":\"" + name + "\"", StringComparison.Ordinal) < 0)
                        missing.Add(name);
                }
                if (missing.Count > 0)
                    throw new InvalidOperationException("tools/list thiếu: " + string.Join(", ", missing));

                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "connector_info", "{}"), "connector_info");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "qs3d_status", "{}"), "qs3d_status");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "cad_active_document", "{}"), "cad_active_document");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "cad_view_state", "{}"), "cad_view_state");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "cad_database_snapshot", "{\"limit\":20}"), "cad_database_snapshot");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "cad_command_catalog", "{}"), "cad_command_catalog");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "cad_audit_tail", "{\"limit\":3}"), "cad_audit_tail");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "desktop_cursor_position", "{}"), "desktop_cursor_position");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "desktop_window_list", "{\"limit\":5}"), "desktop_window_list");
                RequireToolSuccess(CallRaw(endpoint, timeoutMilliseconds, session, "desktop_foreground_window", "{}"), "desktop_foreground_window");

                return "SELF-TEST PASS: initialize/session/tools/list + 10 read-only CAD/desktop calls thành công; mutation và sensitive read không được chạy.";
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(session))
                {
                    try { Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, session); } catch { }
                }
            }
        }

        private static string Initialize(Uri endpoint, int timeoutMilliseconds)
        {
            var body = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
                       + ProtocolVersion
                       + "\",\"capabilities\":{},\"clientInfo\":{\"name\":\"QS3D-Agent-Center\",\"version\":\"2\"}}}";
            var result = Send(endpoint, "POST", body, timeoutMilliseconds, null);
            RequireSuccess(result, "initialize");
            if (string.IsNullOrWhiteSpace(result.SessionId))
                throw new InvalidOperationException("initialize không trả Mcp-Session-Id.");
            return result.SessionId!;
        }

        private static void NotifyInitialized(Uri endpoint, int timeoutMilliseconds, string session)
        {
            var result = Send(endpoint, "POST",
                "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}",
                timeoutMilliseconds,
                session);
            if (result.StatusCode != 202 && result.StatusCode != 204 && result.StatusCode != 200)
                throw new InvalidOperationException("notifications/initialized HTTP " + result.StatusCode + ".");
        }

        private static string Call(Uri endpoint, int timeoutMilliseconds, string session, string tool, string argumentsJson)
        {
            var result = CallRaw(endpoint, timeoutMilliseconds, session, tool, argumentsJson);
            RequireToolSuccess(result, tool);
            return tool + ": OK";
        }

        private static string CallRaw(Uri endpoint, int timeoutMilliseconds, string session, string tool, string argumentsJson)
        {
            var safeArguments = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson.Trim();
            if (!safeArguments.StartsWith("{", StringComparison.Ordinal) || !safeArguments.EndsWith("}", StringComparison.Ordinal))
                throw new InvalidOperationException("Local MCP tool arguments must be a JSON object.");

            var request = "{\"jsonrpc\":\"2.0\",\"id\":30,\"method\":\"tools/call\",\"params\":{\"name\":\""
                          + McpEmbeddedServer.JsonEscape(tool)
                          + "\",\"arguments\":" + safeArguments + "}}";
            var result = Send(endpoint, "POST", request, timeoutMilliseconds, session);
            RequireSuccess(result, tool);
            return result.Body;
        }

        private static void RequireToolSuccess(string body, string operation)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException(operation + " returned an empty response.");
            if (body.IndexOf("\\\"isError\\\":true", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("\"isError\":true", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(operation + " returned MCP isError=true.");
        }

        private static void RequireSuccess(LocalHttpResult result, string operation)
        {
            if (result.StatusCode < 200 || result.StatusCode >= 300)
                throw new InvalidOperationException(operation + " HTTP " + result.StatusCode + ".");
            if (!string.IsNullOrWhiteSpace(result.Body)
                && Regex.IsMatch(result.Body, "\\\"error\\\"\\s*:", RegexOptions.IgnoreCase))
                throw new InvalidOperationException(operation + " returned JSON-RPC error.");
        }

        private static void ValidateLocalEndpoint(Uri endpoint)
        {
            var expected = McpEmbeddedServer.Endpoint;
            if (endpoint == null
                || !endpoint.IsAbsoluteUri
                || !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || !endpoint.IsLoopback
                || !string.IsNullOrEmpty(endpoint.UserInfo)
                || !string.Equals(endpoint.Host, expected.Host, StringComparison.OrdinalIgnoreCase)
                || endpoint.Port != expected.Port
                || !string.Equals(endpoint.AbsolutePath, "/mcp", StringComparison.Ordinal)
                || !string.IsNullOrEmpty(endpoint.Query)
                || !string.IsNullOrEmpty(endpoint.Fragment))
                throw new InvalidOperationException("Local MCP endpoint must match the current embedded loopback http://.../mcp endpoint.");
        }

        private static LocalHttpResult Send(Uri endpoint, string method, string body, int timeoutMilliseconds, string? session)
        {
            ValidateLocalEndpoint(endpoint);
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
#pragma warning restore SYSLIB0014
            request.AllowAutoRedirect = false;
            request.Method = method;
            request.Accept = "application/json, text/event-stream";
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + McpEmbeddedServer.GetBearerToken();
            if (!string.IsNullOrWhiteSpace(session)) request.Headers["Mcp-Session-Id"] = session;

            if (!string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                request.ContentType = "application/json";
                var payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
                request.ContentLength = payload.Length;
                using (var stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
            }
            else request.ContentLength = 0;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.ContentLength > MaxResponseBytes)
                    throw new InvalidOperationException("Local MCP response exceeds the allowed size.");

                var responseBody = string.Empty;
                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                        responseBody = NormalizeBody(ReadBoundedResponseBody(stream, response.ContentLength));
                }
                return new LocalHttpResult((int)response.StatusCode, response.Headers["Mcp-Session-Id"], responseBody);
            }
        }

        private static string ReadBoundedResponseBody(Stream stream, long advertisedLength)
        {
            if (stream == null) return string.Empty;
            if (advertisedLength > MaxResponseBytes)
                throw new InvalidOperationException("Local MCP response exceeds the allowed size.");

            var initialCapacity = advertisedLength > 0 ? (int)advertisedLength : 8192;
            var buffer = new byte[8192];
            var totalBytes = 0;
            using (var memory = new MemoryStream(initialCapacity))
            {
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    if (totalBytes > MaxResponseBytes - read)
                        throw new InvalidOperationException("Local MCP response exceeds the allowed size.");
                    memory.Write(buffer, 0, read);
                    totalBytes += read;
                }

                try
                {
                    return StrictUtf8.GetString(memory.GetBuffer(), 0, totalBytes);
                }
                catch (DecoderFallbackException)
                {
                    throw new InvalidOperationException("Local MCP response is not valid UTF-8.");
                }
            }
        }

        private static string NormalizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || body.IndexOf("data:", StringComparison.OrdinalIgnoreCase) < 0)
                return body == null ? string.Empty : body.Trim();

            var builder = new StringBuilder();
            using (var reader = new StringReader(body))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        builder.Append(line.Substring(5).Trim());
                }
            }
            return builder.Length == 0 ? body.Trim() : builder.ToString();
        }

        private sealed class LocalHttpResult
        {
            public LocalHttpResult(int statusCode, string? sessionId, string body)
            {
                StatusCode = statusCode;
                SessionId = sessionId;
                Body = body ?? string.Empty;
            }

            public int StatusCode { get; private set; }
            public string? SessionId { get; private set; }
            public string Body { get; private set; }
        }
    }
}
