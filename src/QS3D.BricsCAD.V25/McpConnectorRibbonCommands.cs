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
        private const int TimeoutMilliseconds = 4000;

        [CommandMethod("QS3DMCPSETTINGSHTTP", CommandFlags.Modal)]
        public void ShowConnectorSettings()
        {
            Run(document =>
            {
                McpEmbeddedServer.EnsureStarted();
                var token = McpEmbeddedServer.GetBearerToken();
                var publicUrl = McpEmbeddedServer.PublicUrl;
                var text =
                    "QS3D MCP đã được nhúng trong plugin.\n\n"
                    + "Local MCP URL: " + McpEmbeddedServer.Endpoint + "\n"
                    + "Bearer token: " + token + "\n"
                    + "Token source: " + McpEmbeddedServer.TokenSource + "\n"
                    + "Token file: " + McpEmbeddedServer.TokenFilePath + "\n"
                    + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "Public URL: " + publicUrl + "\n")
                    + "\nKhông cần clone/cài QS3D-CAD-MCP riêng.\n"
                    + "Nếu dùng Cloudflare Tunnel, expose local port 8765 và dùng URL public + /mcp trong ChatGPT custom MCP.";
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
                var publicUrl = McpEmbeddedServer.PublicUrl;
                var text = "QS3D AI / MCP\n\n"
                           + "Server: embedded trong QS3D plugin\n"
                           + "Local endpoint: " + McpEmbeddedServer.Endpoint + "\n"
                           + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "Public endpoint: " + publicUrl + "\n")
                           + "Auth: Bearer (" + McpEmbeddedServer.TokenSource + ")\n"
                           + "MCP protocol: " + (result.Ready ? "READY" : "NOT READY") + "\n"
                           + "Chi tiết: " + result.Message + "\n\n"
                           + "Cài đặt: QS3DMCPSETTINGSHTTP\n"
                           + "Kiểm tra: QS3DMCPCHECKHTTP\n"
                           + "Tài liệu: QS3DMCPDOCSHTTP";
                MessageBox.Show(text, "QS3D AI Dashboard", MessageBoxButton.OK,
                    result.Ready ? MessageBoxImage.Information : MessageBoxImage.Warning);
                Report(document, result.Ready ? "MCP protocol READY." : "MCP protocol chưa READY: " + result.Message);
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
        private const string PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL";

        public static string WriteGuide()
        {
            var publicUrl = (Environment.GetEnvironmentVariable(PublicUrlEnvironment) ?? string.Empty).Trim();
            var path = Path.Combine(Path.GetTempPath(), "QS3D-CHATGPT-MCP.txt");
            var text =
                "QS3D ChatGPT / custom MCP\r\n"
                + "===========================\r\n\r\n"
                + "MCP server is EMBEDDED in QS3D-BricsCAD. No second QS3D-CAD-MCP clone/install is required.\r\n\r\n"
                + "Local endpoint: " + McpEmbeddedServer.Endpoint + "\r\n"
                + "Health endpoint: " + McpEmbeddedServer.HealthEndpoint + "\r\n"
                + "Bearer token file: " + McpEmbeddedServer.TokenFilePath + "\r\n"
                + "Bearer token source: " + McpEmbeddedServer.TokenSource + "\r\n"
                + (string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "Configured public URL: " + publicUrl + "\r\n")
                + "\r\n"
                + "MCP protocol flow:\r\n"
                + "initialize -> notifications/initialized -> tools/list -> tools/call\r\n\r\n"
                + "Built-in tools:\r\n"
                + "- connector_info\r\n"
                + "- qs3d_status\r\n"
                + "- cad_active_document\r\n"
                + "- cad_selection\r\n"
                + "- cad_database_snapshot\r\n"
                + "- qs3d_run_command (QS3D* only; confirmMutation=true required)\r\n"
                + "- cad_cancel_command\r\n\r\n"
                + "Cloudflare quick tunnel example:\r\n"
                + "cloudflared tunnel --url http://127.0.0.1:8765 --http-host-header 127.0.0.1:8765\r\n\r\n"
                + "Then configure ChatGPT custom MCP with:\r\n"
                + "https://<generated-host>/mcp\r\n"
                + "Authorization: Bearer <contents of the token file above>\r\n\r\n"
                + "Optional QS3D_MCP_PUBLIC_URL stores only the public connector URL for the local dashboard.\r\n"
                + "Optional QS3D_MCP_BEARER_TOKEN overrides the generated token; use a strong secret.\r\n"
                + "The listener binds only to 127.0.0.1 and never opens a public interface directly.\r\n";
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

            try
            {
                var initialize = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
                                 + ProtocolVersion
                                 + "\",\"capabilities\":{},\"clientInfo\":{\"name\":\"QS3D-BricsCAD\",\"version\":\"embedded-1\"}}}";
                var init = Post(endpoint, initialize, timeoutMilliseconds, null);
                if (!HasJsonProperty(init.Body, "result") || !HasJsonProperty(init.Body, "serverInfo"))
                    return new McpProtocolProbeResult(false, "initialize không trả MCP result/serverInfo: HTTP " + init.StatusCode + ".");

                var sessionId = init.SessionId;
                if (string.IsNullOrWhiteSpace(sessionId))
                    return new McpProtocolProbeResult(false, "initialize không trả Mcp-Session-Id.");

                var initialized = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}";
                Post(endpoint, initialized, timeoutMilliseconds, sessionId);

                var list = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}";
                var tools = Post(endpoint, list, timeoutMilliseconds, sessionId);
                if (!HasJsonProperty(tools.Body, "result") || !HasJsonProperty(tools.Body, "tools"))
                    return new McpProtocolProbeResult(false, "tools/list không trả danh sách MCP tools: HTTP " + tools.StatusCode + ".");

                var serverName = ExtractServerName(init.Body);
                var toolCount = Regex.Matches(tools.Body ?? string.Empty, "\\\"name\\\"\\s*:").Count;
                return new McpProtocolProbeResult(true,
                    "READY; protocol=" + ProtocolVersion
                    + "; server=" + (string.IsNullOrWhiteSpace(serverName) ? "unknown" : serverName)
                    + "; tools=" + toolCount + ".");
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
        }

        private static HttpResult Post(Uri endpoint, string json, int timeoutMilliseconds, string? sessionId)
        {
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
#pragma warning restore SYSLIB0014
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json, text/event-stream";
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            if (!string.IsNullOrWhiteSpace(sessionId)) request.Headers["Mcp-Session-Id"] = sessionId;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + McpEmbeddedServer.GetBearerToken();

            var payload = Encoding.UTF8.GetBytes(json);
            request.ContentLength = payload.Length;
            using (var stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                string body;
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream!, Encoding.UTF8)) body = NormalizeBody(reader.ReadToEnd());
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
