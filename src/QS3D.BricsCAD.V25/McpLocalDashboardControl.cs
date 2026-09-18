using System;
using System.Globalization;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    internal sealed class McpDashboardHttpResponse
    {
        public McpDashboardHttpResponse(int statusCode, string reason, string body)
        {
            StatusCode = statusCode;
            Reason = reason ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public int StatusCode { get; }
        public string Reason { get; }
        public string Body { get; }
    }

    /// <summary>
    /// Loopback-only control surface consumed by QS3D.McpDashboard. Authentication is enforced by
    /// McpEmbeddedServer before this class is invoked. Runtime API keys are handed only to the
    /// existing Credential Manager-backed OpenAI manager and are never serialized into responses.
    /// </summary>
    internal static class McpLocalDashboardControl
    {
        private const string Prefix = "/qs3d/dashboard/";

        internal static bool TryHandle(string method, string path, string body, out McpDashboardHttpResponse response)
        {
            response = new McpDashboardHttpResponse(404, "Not Found", Error("not found"));
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var verb = (method ?? string.Empty).Trim().ToUpperInvariant();
            var action = path.Substring(Prefix.Length).Trim('/').ToLowerInvariant();
            try
            {
                if (verb == "GET" && action == "status")
                {
                    response = Ok(BuildStatus());
                    return true;
                }

                if (verb != "POST")
                {
                    response = new McpDashboardHttpResponse(405, "Method Not Allowed", Error("POST required"));
                    return true;
                }

                string message;
                bool success;
                switch (action)
                {
                    case "openai/start":
                    {
                        var tunnelId = ReadString(body, "tunnelId");
                        if (string.IsNullOrWhiteSpace(tunnelId)) tunnelId = McpOpenAiSecureTunnelManager.SavedTunnelId;
                        var runtimeApiKey = ReadString(body, "runtimeApiKey");
                        success = McpOpenAiSecureTunnelManager.Start(tunnelId, runtimeApiKey, out message);
                        break;
                    }
                    case "openai/stop":
                        McpOpenAiSecureTunnelManager.Stop();
                        success = true;
                        message = "OpenAI Secure Tunnel stopped; autostart disabled.";
                        break;
                    case "openai/restart":
                    {
                        McpOpenAiSecureTunnelManager.StopForHostShutdown();
                        success = McpOpenAiSecureTunnelManager.Start(
                            McpOpenAiSecureTunnelManager.SavedTunnelId, string.Empty, out message);
                        break;
                    }
                    case "openai/autostart":
                        McpOpenAiSecureTunnelManager.SetAutoStart(ReadEnabled(body));
                        success = true;
                        message = "OpenAI autostart updated.";
                        break;
                    case "cloudflare/start":
                        success = McpCloudflareAccountTunnelManager.StartSaved(out message);
                        if (success) McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.CloudflareNamedTunnel);
                        break;
                    case "cloudflare/stop":
                        McpCloudflareAccountTunnelManager.Stop();
                        success = true;
                        message = "Cloudflare Named Tunnel stopped; autostart disabled.";
                        break;
                    case "cloudflare/restart":
                        McpCloudflareAccountTunnelManager.StopForHostShutdown();
                        success = McpCloudflareAccountTunnelManager.StartSaved(out message);
                        break;
                    case "cloudflare/autostart":
                        McpCloudflareAccountTunnelManager.SetAutoStart(ReadEnabled(body));
                        success = true;
                        message = "Cloudflare autostart updated.";
                        break;
                    default:
                        response = new McpDashboardHttpResponse(404, "Not Found", Error("unknown dashboard action"));
                        return true;
                }

                var safeMessage = Safe(message);
                response = new McpDashboardHttpResponse(success ? 200 : 409, success ? "OK" : "Conflict",
                    "{\"ok\":" + (success ? "true" : "false")
                    + ",\"message\":\"" + Json(safeMessage) + "\",\"status\":" + BuildStatus() + "}");
                return true;
            }
            catch (Exception ex)
            {
                response = new McpDashboardHttpResponse(500, "Internal Server Error",
                    Error(Safe(ex.Message)));
                return true;
            }
        }

        private static string BuildStatus()
        {
            var selected = McpTransportCoordinator.SelectedProvider.ToString();
            return "{"
                   + "\"mcpEndpoint\":\"" + Json(McpEmbeddedServer.Endpoint.ToString()) + "\","
                   + "\"selectedProvider\":\"" + Json(selected) + "\","
                   + "\"openai\":{"
                   + "\"configured\":" + Bool(McpOpenAiSecureTunnelManager.IsConfigured) + ","
                   + "\"running\":" + Bool(McpOpenAiSecureTunnelManager.IsRunning) + ","
                   + "\"ready\":" + Bool(McpOpenAiSecureTunnelManager.IsReady) + ","
                   + "\"autostart\":" + Bool(McpOpenAiSecureTunnelManager.AutoStartEnabled) + ","
                   + "\"credentialStored\":" + Bool(McpPersistentUserSettings.HasSavedOpenAiRuntimeApiKey) + ","
                   + "\"tunnelId\":\"" + Json(McpOpenAiSecureTunnelManager.SavedTunnelId) + "\","
                   + "\"healthUrl\":\"" + Json(McpOpenAiSecureTunnelManager.HealthBaseUrl) + "\","
                   + "\"trust\":\"" + Json(Safe(McpOpenAiSecureTunnelManager.ClientTrustSummary)) + "\","
                   + "\"error\":\"" + Json(Safe(McpOpenAiSecureTunnelManager.LastError)) + "\"},"
                   + "\"cloudflare\":{"
                   + "\"configured\":" + Bool(McpCloudflareAccountTunnelManager.IsConfigured) + ","
                   + "\"running\":" + Bool(McpCloudflareAccountTunnelManager.IsRunning) + ","
                   + "\"ready\":" + Bool(McpCloudflareAccountTunnelManager.IsPublicReady) + ","
                   + "\"autostart\":" + Bool(McpCloudflareAccountTunnelManager.AutoStartEnabled) + ","
                   + "\"hostname\":\"" + Json(McpCloudflareAccountTunnelManager.SavedHostname) + "\","
                   + "\"publicMcpUrl\":\"" + Json(McpCloudflareAccountTunnelManager.PublicMcpUrl) + "\","
                   + "\"error\":\"" + Json(Safe(McpCloudflareAccountTunnelManager.LastError)) + "\"}"
                   + "}";
        }

        private static string ReadString(string body, string property)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;
            try { return (McpTopLevelJson.ExtractString(body, property) ?? string.Empty).Trim(); }
            catch { return string.Empty; }
        }

        private static bool ReadEnabled(string body)
        {
            return string.Equals(ReadString(body, "enabled"), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static McpDashboardHttpResponse Ok(string json) => new McpDashboardHttpResponse(200, "OK", json);
        private static string Error(string message) => "{\"ok\":false,\"error\":\"" + Json(Safe(message)) + "\"}";
        private static string Bool(bool value) => value ? "true" : "false";
        private static string Safe(string value)
        {
            var safe = McpPublicTextSanitizer.Sanitize(value ?? string.Empty);
            return safe.Length <= 512 ? safe : safe.Substring(0, 512);
        }

        private static string Json(string value)
        {
            var s = value ?? string.Empty;
            var b = new StringBuilder(s.Length + 16);
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '\\': b.Append("\\\\"); break;
                    case '"': b.Append("\\\""); break;
                    case '\r': b.Append("\\r"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (ch < 32) b.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else b.Append(ch);
                        break;
                }
            }
            return b.ToString();
        }
    }
}
