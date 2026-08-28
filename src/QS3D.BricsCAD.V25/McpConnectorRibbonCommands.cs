using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Functional MCP controls used by TOOL > MCP (AI). The MCP server is embedded in the
    /// QS3D plugin; no second repository, probe DLL or Node process is required.
    /// </summary>
    public sealed class McpConnectorRibbonCommands
    {
        private const int TimeoutMilliseconds = 5000;

        [CommandMethod("QS3DMCPSETTINGSHTTP", CommandFlags.Modal)]
        public void ShowConnectorSettings()
        {
            Run(document =>
            {
                McpEmbeddedServer.EnsureStarted();
                var publicUrl = FirstPublicUrl();
                var text =
                    "QS3D MCP đã được nhúng trong plugin.\n\n"
                    + "Local MCP URL: " + McpEmbeddedServer.Endpoint + "\n"
                    + "Bearer token: " + McpEmbeddedServer.GetBearerToken() + "\n"
                    + "Token source: " + McpEmbeddedServer.TokenSource + "\n"
                    + "Token file: " + McpEmbeddedServer.TokenFilePath + "\n"
                    + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "Public MCP URL: " + publicUrl + "\n")
                    + "\nKhông cần clone/cài QS3D-CAD-MCP riêng.\n"
                    + "Luồng khuyến nghị: QS3DMCPACCOUNTSETUP -> đăng nhập Cloudflare trong browser -> tạo/reuse Named Tunnel -> thêm URL /mcp + Bearer token vào ChatGPT custom MCP.";
                MessageBox.Show(text, "QS3D MCP Settings", MessageBoxButton.OK, MessageBoxImage.Information);
                Report(document, "MCP settings: " + McpEmbeddedServer.Describe());
            });
        }

        [CommandMethod("QS3DMCPDOCSHTTP", CommandFlags.Modal)]
        public void OpenConnectorDocs()
        {
            Run(document =>
            {
                McpEmbeddedServer.EnsureStarted();
                var path = McpConnectorEndpoint.WriteGuide();
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    Report(document, "Đã mở hướng dẫn ChatGPT/MCP: " + path);
                }
                catch (Exception ex)
                {
                    Report(document, "Đã tạo hướng dẫn MCP tại " + path + " nhưng không mở được: " + ex.Message);
                }
            });
        }

        [CommandMethod("QS3DMCPCHECKHTTP", CommandFlags.Modal)]
        public void CheckConnector()
        {
            Run(document =>
            {
                McpEmbeddedServer.EnsureStarted();
                var result = McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, TimeoutMilliseconds);
                Report(document, "MCP " + McpEmbeddedServer.Endpoint + ": " + result.Message);
            });
        }

        [CommandMethod("QS3DAIDASHBOARDHTTP", CommandFlags.Modal)]
        public void ShowConnectorDashboard()
        {
            Run(document =>
            {
                McpEmbeddedServer.EnsureStarted();
                var result = McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, TimeoutMilliseconds);
                var publicUrl = FirstPublicUrl();
                var text = "QS3D AI / MCP\n\n"
                           + "Server: embedded trong QS3D plugin\n"
                           + "Local endpoint: " + McpEmbeddedServer.Endpoint + "\n"
                           + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "Public endpoint: " + publicUrl + "\n")
                           + "Auth: Bearer (" + McpEmbeddedServer.TokenSource + ")\n"
                           + "MCP protocol + tool call: " + (result.Ready ? "READY" : "NOT READY") + "\n"
                           + "Automation: " + McpEmbeddedServer.Describe() + "\n"
                           + "Chi tiết: " + result.Message + "\n\n"
                           + "Setup Cloudflare: QS3DMCPACCOUNTSETUP\n"
                           + "Advanced/Quick Tunnel: QS3DMCPSETUP\n"
                           + "Kiểm tra: QS3DMCPCHECKHTTP\n"
                           + "Tài liệu: QS3DMCPDOCSHTTP";
                MessageBox.Show(text, "QS3D AI Dashboard", MessageBoxButton.OK,
                    result.Ready ? MessageBoxImage.Information : MessageBoxImage.Warning);
                Report(document, result.Ready ? "MCP protocol/tool-call READY." : "MCP protocol chưa READY: " + result.Message);
            });
        }

        [CommandMethod("QS3DMCPSTART", CommandFlags.Modal)]
        public void StartConnector()
        {
            Run(document =>
            {
                McpEmbeddedServer.EnsureStarted();
                Report(document, "Embedded MCP started: " + McpEmbeddedServer.Describe());
            });
        }

        [CommandMethod("QS3DMCPSTOP", CommandFlags.Modal)]
        public void StopConnector()
        {
            Run(document =>
            {
                McpEmbeddedServer.Stop();
                Report(document, "Embedded MCP stopped.");
            });
        }

        private static string FirstPublicUrl()
        {
            if (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.PublicMcpUrl)) return McpCloudflareAccountTunnelManager.PublicMcpUrl;
            if (!string.IsNullOrWhiteSpace(McpCloudflareTunnelManager.PublicMcpUrl)) return McpCloudflareTunnelManager.PublicMcpUrl;
            return McpEmbeddedServer.PublicUrl;
        }

        private static void Run(Action<Document> action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try { action(document); }
            catch (Exception ex) { Report(document, "MCP lỗi: " + ex.Message); }
        }

        private static void Report(Document document, string message) =>
            document.Editor.WriteMessage("\nQS3D " + message);
    }

    internal static class McpConnectorEndpoint
    {
        public static string WriteGuide()
        {
            var publicUrl = !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.PublicMcpUrl)
                ? McpCloudflareAccountTunnelManager.PublicMcpUrl
                : (!string.IsNullOrWhiteSpace(McpCloudflareTunnelManager.PublicMcpUrl)
                    ? McpCloudflareTunnelManager.PublicMcpUrl
                    : McpEmbeddedServer.PublicUrl);
            var path = Path.Combine(Path.GetTempPath(), "QS3D-CHATGPT-MCP.txt");
            var text =
                "QS3D ChatGPT / custom MCP\r\n"
                + "===========================\r\n\r\n"
                + "MCP server is EMBEDDED in QS3D-BricsCAD. No second repository, Node runtime or probe process is required.\r\n\r\n"
                + "Local endpoint: " + McpEmbeddedServer.Endpoint + "\r\n"
                + "Health endpoint: " + McpEmbeddedServer.HealthEndpoint + "\r\n"
                + "Bearer token file: " + McpEmbeddedServer.TokenFilePath + "\r\n"
                + "Bearer token source: " + McpEmbeddedServer.TokenSource + "\r\n"
                + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "Public MCP URL: " + publicUrl + "\r\n")
                + "\r\n"
                + "Recommended setup:\r\n"
                + "1. Run QS3DMCPACCOUNTSETUP.\r\n"
                + "2. Login to Cloudflare in the provider-owned browser page. QS3D never asks for the Cloudflare password.\r\n"
                + "3. Enter a hostname and let QS3D create/reuse the named tunnel + DNS route.\r\n"
                + "4. Configure ChatGPT custom MCP with the public /mcp URL and Authorization: Bearer <token>.\r\n"
                + "5. Run QS3DMCPCHECKHTTP for initialize -> initialized -> tools/list -> tools/call -> session DELETE verification.\r\n\r\n"
                + "Read-only / observation tools:\r\n"
                + "- connector_info\r\n"
                + "- qs3d_status\r\n"
                + "- cad_active_document\r\n"
                + "- cad_selection\r\n"
                + "- cad_database_snapshot\r\n"
                + "- cad_view_state\r\n"
                + "- cad_wait_idle\r\n"
                + "- cad_command_catalog\r\n"
                + "- cad_audit_tail\r\n\r\n"
                + "Direct native CAD mutation tools (confirmMutation=true):\r\n"
                + "- cad_create_line\r\n"
                + "- cad_create_circle\r\n"
                + "- cad_create_polyline\r\n"
                + "- cad_create_text\r\n"
                + "- cad_entity_transform\r\n"
                + "- cad_entity_delete\r\n"
                + "- cad_layer\r\n"
                + "- cad_command_sequence (allowlisted command + bounded prompt inputs)\r\n"
                + "- qs3d_run_command (QS3D* command names only)\r\n"
                + "- cad_ui_click / cad_ui_type / cad_ui_key (foreground BricsCAD-process window only)\r\n"
                + "- cad_agent_resume\r\n\r\n"
                + "Safety / recovery tools:\r\n"
                + "- cad_agent_stop (emergency stop, no confirmation required)\r\n"
                + "- cad_cancel_command (ESC x2, no confirmation required)\r\n\r\n"
                + "Cloudflare Quick Tunnel is available only as a test fallback from QS3DMCPSETUP.\r\n"
                + "The MCP listener binds only to 127.0.0.1:8765; remote access must arrive through the configured tunnel.\r\n"
                + "Ordinary MCP mutation tools require confirmMutation=true, are blocked while the emergency stop is active, and write a bounded local audit log.\r\n"
                + "No arbitrary PowerShell/cmd/shell/process execution is exposed by the network MCP server.\r\n";
            File.WriteAllText(path, text, new UTF8Encoding(false));
            return path;
        }
    }

    internal sealed class McpProtocolProbeResult
    {
        public McpProtocolProbeResult(bool ready, string message)
        {
            Ready = ready;
            Message = message;
        }

        public bool Ready { get; }
        public string Message { get; }
    }

    internal static class McpProtocolProbe
    {
        private const string ProtocolVersion = "2025-06-18";

        public static McpProtocolProbeResult Check(Uri endpoint, int timeoutMilliseconds)
        {
            if (endpoint == null) return new McpProtocolProbeResult(false, "endpoint null.");
            if (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return new McpProtocolProbeResult(false, "ChatGPT/custom MCP cần endpoint http/https Streamable HTTP.");

            string? sessionId = null;
            try
            {
                var initialize = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
                                 + ProtocolVersion
                                 + "\",\"capabilities\":{},\"clientInfo\":{\"name\":\"QS3D-BricsCAD\",\"version\":\"embedded-3\"}}}";
                var init = Send(endpoint, "POST", initialize, timeoutMilliseconds, null);
                if (!HasJsonProperty(init.Body, "result") || !HasJsonProperty(init.Body, "serverInfo"))
                    return new McpProtocolProbeResult(false, "initialize không trả MCP result/serverInfo: HTTP " + init.StatusCode + ".");

                sessionId = init.SessionId;
                if (string.IsNullOrWhiteSpace(sessionId))
                    return new McpProtocolProbeResult(false, "initialize không trả Mcp-Session-Id.");

                var initialized = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}";
                Send(endpoint, "POST", initialized, timeoutMilliseconds, sessionId);

                var list = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}";
                var tools = Send(endpoint, "POST", list, timeoutMilliseconds, sessionId);
                if (!HasJsonProperty(tools.Body, "result") || !HasJsonProperty(tools.Body, "tools"))
                    return new McpProtocolProbeResult(false, "tools/list không trả danh sách MCP tools: HTTP " + tools.StatusCode + ".");

                var call = "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"connector_info\",\"arguments\":{}}}";
                var called = Send(endpoint, "POST", call, timeoutMilliseconds, sessionId);
                if (!HasJsonProperty(called.Body, "result") || !called.Body.Contains("connector_info") && !called.Body.Contains("singleRepository"))
                    return new McpProtocolProbeResult(false, "tools/call connector_info không trả MCP tool result hợp lệ: HTTP " + called.StatusCode + ".");

                var ping = "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"ping\",\"params\":{}}";
                var pong = Send(endpoint, "POST", ping, timeoutMilliseconds, sessionId);
                if (!HasJsonProperty(pong.Body, "result"))
                    return new McpProtocolProbeResult(false, "ping không trả result.");

                var serverName = ExtractServerName(init.Body);
                var toolCount = Regex.Matches(tools.Body ?? string.Empty, "\\\"name\\\"\\s*:").Count;
                var deleted = Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, sessionId);
                sessionId = null;
                if (deleted.StatusCode != 204)
                    return new McpProtocolProbeResult(false, "session DELETE không trả HTTP 204.");

                return new McpProtocolProbeResult(true,
                    "READY; protocol=" + ProtocolVersion
                    + "; server=" + (string.IsNullOrWhiteSpace(serverName) ? "unknown" : serverName)
                    + "; toolDescriptors~=" + toolCount
                    + "; connector_info=OK; ping=OK; sessionDelete=OK.");
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                var status = response == null ? string.Empty : " HTTP " + (int)response.StatusCode + " " + response.StatusDescription + ".";
                return new McpProtocolProbeResult(false, "MCP HTTP unavailable:" + status + " " + ex.Message);
            }
            catch (Exception ex)
            {
                return new McpProtocolProbeResult(false, "MCP protocol error: " + ex.Message);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    try { Send(endpoint, "DELETE", string.Empty, Math.Min(timeoutMilliseconds, 1500), sessionId); } catch { }
                }
            }
        }

        private static HttpResult Send(Uri endpoint, string method, string json, int timeoutMilliseconds, string? sessionId)
        {
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
#pragma warning restore SYSLIB0014
            request.Method = method;
            request.Accept = "application/json, text/event-stream";
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            if (!string.IsNullOrWhiteSpace(sessionId)) request.Headers["Mcp-Session-Id"] = sessionId;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + McpEmbeddedServer.GetBearerToken();

            if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                request.ContentType = "application/json";
                var payload = Encoding.UTF8.GetBytes(json ?? string.Empty);
                request.ContentLength = payload.Length;
                using (var stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
            }
            else
            {
                request.ContentLength = 0;
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                string body = string.Empty;
                var responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    using (responseStream)
                    using (var reader = new StreamReader(responseStream, Encoding.UTF8)) body = NormalizeBody(reader.ReadToEnd());
                }
                return new HttpResult((int)response.StatusCode, response.Headers["Mcp-Session-Id"], body);
            }
        }

        private static string NormalizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;
            if (!body.Contains("data:")) return body.Trim();
            var builder = new StringBuilder();
            using (var reader = new StringReader(body))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                    if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) builder.Append(line.Substring(5).Trim());
            }
            return builder.Length == 0 ? body.Trim() : builder.ToString();
        }

        private static bool HasJsonProperty(string body, string property) =>
            !string.IsNullOrWhiteSpace(body)
            && Regex.IsMatch(body, "\\\"" + Regex.Escape(property) + "\\\"\\s*:", RegexOptions.IgnoreCase);

        private static string ExtractServerName(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;
            var match = Regex.Match(
                body,
                "\\\"serverInfo\\\"\\s*:\\s*\\{[^}]*\\\"name\\\"\\s*:\\s*\\\"(?<name>[^\\\"]+)\\\"",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["name"].Value : string.Empty;
        }

        private sealed class HttpResult
        {
            public HttpResult(int statusCode, string? sessionId, string body)
            {
                StatusCode = statusCode;
                SessionId = sessionId;
                Body = body ?? string.Empty;
            }

            public int StatusCode { get; }
            public string? SessionId { get; }
            public string Body { get; }
        }
    }
}
