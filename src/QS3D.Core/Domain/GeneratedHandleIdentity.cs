using System;
using System.Globalization;

namespace QS3D.Core.Domain
{
    internal static class GeneratedHandleIdentity
    {
        internal static string Normalize(string? handle)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return string.Empty;

            var hex = normalized;
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            if (hex.Length == 0) return normalized;

            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value == 0UL)
                return normalized;
            return value.ToString("X", CultureInfo.InvariantCulture);
        }
    }
}
