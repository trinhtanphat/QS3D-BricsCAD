using System.Security;
using System.Text;

namespace QS3D.Core.Export
{
    internal static class XlsxXmlText
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sanitized = new StringBuilder(value.Length);
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (char.IsHighSurrogate(current))
                {
                    if (index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                    {
                        sanitized.Append(current);
                        sanitized.Append(value[++index]);
                    }
                    else
                    {
                        sanitized.Append('\uFFFD');
                    }
                    continue;
                }

                if (char.IsLowSurrogate(current))
                {
                    sanitized.Append('\uFFFD');
                    continue;
                }

                if (current == '\t' || current == '\n' || current == '\r' ||
                    (current >= '\u0020' && current <= '\uD7FF') ||
                    (current >= '\uE000' && current <= '\uFFFD'))
                {
                    sanitized.Append(current);
                }
                else
                {
                    sanitized.Append('\uFFFD');
                }
            }

            return SecurityElement.Escape(sanitized.ToString()) ?? string.Empty;
        }
    }
}
