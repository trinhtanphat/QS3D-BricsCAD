using System;
using System.Text;
using System.Text.RegularExpressions;

namespace QS3D.BricsCAD.V25
{
    internal static class McpPublicTextSanitizer
    {
        internal const int MaxPublicTextCharacters = 512;

        internal static string Sanitize(string message)
        {
            var value = message ?? string.Empty;
            value = Regex.Replace(value, @"(?i)(Authorization\s*:\s*(?:Bearer|Basic)\s+)[^\s,;]+", "$1[REDACTED]");
            value = Regex.Replace(value, @"(?i)\b(api[-_]?key|access[-_]?token|auth[-_]?token|secret|password)\b\s*[:=]\s*[^\s,;]+", "$1=[REDACTED]");
            value = Regex.Replace(value, @"(?i)\bBearer\s+[A-Za-z0-9._~+/-]+=*", "Bearer [REDACTED]");
            value = Regex.Replace(value, @"(?i)(?:[A-Z]:\\|\\\\)[^\r\n\t]+", "[PATH]");
            value = Regex.Replace(value, @"(?i)(?:/home/|/Users/|/tmp/|/var/tmp/)[^\r\n\t]+", "[PATH]");

            var clean = new StringBuilder(Math.Min(value.Length, MaxPublicTextCharacters));
            foreach (var ch in value)
            {
                if (clean.Length >= MaxPublicTextCharacters) break;
                if (char.IsControl(ch))
                {
                    if (ch == '\r' || ch == '\n' || ch == '\t') clean.Append(' ');
                    continue;
                }
                clean.Append(ch);
            }
            return clean.ToString().Trim();
        }
    }
}
