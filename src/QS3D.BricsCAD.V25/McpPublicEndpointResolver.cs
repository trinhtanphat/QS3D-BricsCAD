using System;
using System.Net;
using System.Net.Sockets;

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

        // Keep the operator-supplied fallback distinct from the process variable that Publish()
        // updates for connector_info/status. Without this snapshot, a provider URL published by
        // QS3D could later be mistaken for a user fallback after a Quick Tunnel exits.
        private static readonly string ConfiguredEnvironmentFallback = ReadConfiguredEnvironmentFallback();

        public static string Resolve()
        {
            string value;
            try
            {
                value = NormalizeCandidate(McpCloudflareAccountTunnelManager.PublicMcpUrl);
                if (!string.IsNullOrWhiteSpace(value)) return Publish(value);
            }
            catch { }

            try
            {
                value = NormalizeCandidate(McpCloudflareTunnelManager.PublicMcpUrl);
                if (!string.IsNullOrWhiteSpace(value)) return Publish(value);
            }
            catch { }

            try
            {
                value = NormalizeCandidate(ConfiguredEnvironmentFallback);
                if (!string.IsNullOrWhiteSpace(value)) return Publish(value);
            }
            catch { }

            // Also clear a previously published provider URL from the process-visible status
            // surface when no live/configured public endpoint remains.
            return Publish(string.Empty);
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

            IPAddress literalAddress;
            if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out literalAddress) && IsPrivateOrLocalAddress(literalAddress))
                return string.Empty;

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

        private static bool IsPrivateOrLocalAddress(IPAddress address)
        {
            if (address == null || IPAddress.IsLoopback(address)) return true;
            var bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                if (bytes.Length != 4) return true;
                var first = bytes[0];
                var second = bytes[1];
                if (first == 0 || first == 10 || first == 127 || first >= 224) return true;
                if (first == 100 && second >= 64 && second <= 127) return true;
                if (first == 169 && second == 254) return true;
                if (first == 172 && second >= 16 && second <= 31) return true;
                if (first == 192 && second == 168) return true;
                if (first == 192 && second == 0 && (bytes[2] == 0 || bytes[2] == 2)) return true;
                if (first == 198 && (second == 18 || second == 19)) return true;
                if (first == 198 && second == 51 && bytes[2] == 100) return true;
                if (first == 203 && second == 0 && bytes[2] == 113) return true;
                return false;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (bytes.Length != 16) return true;
                if (address.Equals(IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal)
                    return true;
                if ((bytes[0] & 0xFE) == 0xFC) return true; // RFC 4193 unique-local fc00::/7
                if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
                    return true; // RFC 3849 documentation range

                var mappedV4 = true;
                for (var i = 0; i < 10; i++)
                {
                    if (bytes[i] == 0) continue;
                    mappedV4 = false;
                    break;
                }
                if (mappedV4 && bytes[10] == 0xFF && bytes[11] == 0xFF)
                    return IsPrivateOrLocalAddress(new IPAddress(new[] { bytes[12], bytes[13], bytes[14], bytes[15] }));
                return false;
            }

            return true;
        }

        private static string ReadConfiguredEnvironmentFallback()
        {
            try { return (Environment.GetEnvironmentVariable(PublicUrlEnvironment) ?? string.Empty).Trim(); }
            catch { return string.Empty; }
        }

        private static string Publish(string value)
        {
            var resolved = value ?? string.Empty;
            try
            {
                var current = Environment.GetEnvironmentVariable(PublicUrlEnvironment) ?? string.Empty;
                if (!string.Equals(current, resolved, StringComparison.Ordinal))
                    Environment.SetEnvironmentVariable(PublicUrlEnvironment, resolved, EnvironmentVariableTarget.Process);
            }
            catch { }
            return resolved;
        }
    }
}
