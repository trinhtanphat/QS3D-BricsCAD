using System;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Resolves the single public MCP endpoint shown to users and MCP clients.
    /// Provider-managed tunnel state wins over the optional environment fallback.
    /// Only non-loopback HTTPS endpoints with the canonical /mcp path are exposed.
    /// </summary>
    internal static class McpPublicEndpointResolver
    {
        private const string PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL";

        public static string Resolve()
        {
            string value;
            try
            {
                value = NormalizeCandidate(McpCloudflareAccountTunnelManager.PublicMcpUrl);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            catch { }

            try
            {
                value = NormalizeCandidate(McpCloudflareTunnelManager.PublicMcpUrl);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            catch { }

            try
            {
                value = NormalizeCandidate(Environment.GetEnvironmentVariable(PublicUrlEnvironment) ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            catch { }

            return string.Empty;
        }

        internal static string NormalizeCandidate(string value)
        {
            var raw = (value ?? string.Empty).Trim();
            if (raw.Length == 0) return string.Empty;

            Uri uri;
            if (!Uri.TryCreate(raw, UriKind.Absolute, out uri)) return string.Empty;
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return string.Empty;
            if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return string.Empty;

            var path = uri.AbsolutePath ?? string.Empty;
            if (path.Length == 0 || path == "/") path = "/mcp";
            else if (!string.Equals(path.TrimEnd('/'), "/mcp", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            else path = "/mcp";

            var builder = new UriBuilder(uri)
            {
                Scheme = Uri.UriSchemeHttps,
                Path = path,
                Query = string.Empty,
                Fragment = string.Empty
            };
            return builder.Uri.AbsoluteUri.TrimEnd('/');
        }
    }
}
