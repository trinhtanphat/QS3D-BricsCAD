using System;
using System.Collections.Generic;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Small JSON member scanner used for security-sensitive MCP routing. It intentionally
    /// scopes lookups to one object level so nested values or string contents cannot impersonate
    /// a caller-controlled top-level confirmation/property.
    /// </summary>
    internal static class McpTopLevelJson
    {
        internal static bool TryFindPropertyValue(
            string json,
            string property,
            out string rawValue,
            out bool found,
            out string error)
        {
            rawValue = string.Empty;
            found = false;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(property))
            {
                error = "JSON property name is required.";
                return false;
            }

            var source = (json ?? string.Empty).Trim();
            if (source.Length < 2 || source[0] != '{')
            {
                error = "JSON value must be an object.";
                return false;
            }

            var index = 1;
            while (true)
            {
                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "JSON object ended unexpectedly.";
                    return false;
                }
                if (source[index] == '}')
                {
                    index++;
                    SkipWhitespace(source, ref index);
                    if (index != source.Length)
                    {
                        error = "Unexpected content after JSON object.";
                        return false;
                    }
                    return true;
                }
                if (source[index] != '"')
                {
                    error = "JSON object property name must be a string.";
                    return false;
                }

                string name;
                if (!TryReadString(source, ref index, out name, out error)) return false;
                SkipWhitespace(source, ref index);
                if (index >= source.Length || source[index] != ':')
                {
                    error = "JSON object property is missing ':'.";
                    return false;
                }
                index++;
                SkipWhitespace(source, ref index);
                var valueStart = index;
                if (!TrySkipValue(source, ref index, out error)) return false;
                var candidate = source.Substring(valueStart, index - valueStart).Trim();

                if (string.Equals(name, property, StringComparison.OrdinalIgnoreCase))
                {
                    if (found)
                    {
                        error = "duplicate top-level JSON property: " + property;
                        return false;
                    }
                    found = true;
                    rawValue = candidate;
                }

                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "JSON object ended unexpectedly.";
                    return false;
                }
                if (source[index] == ',')
                {
                    index++;
                    continue;
                }
                if (source[index] != '}')
                {
                    error = "JSON object requires ',' or '}' after a property value.";
                    return false;
                }
            }
        }

        internal static string ExtractString(string json, string property)
        {
            string raw;
            bool found;
            string error;
            if (!TryFindPropertyValue(json, property, out raw, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return string.Empty;
            var index = 0;
            string value;
            if (!TryReadString(raw, ref index, out value, out error)) return string.Empty;
            SkipWhitespace(raw, ref index);
            if (index != raw.Length) return string.Empty;
            return value;
        }

        internal static bool ExtractBoolean(string json, string property)
        {
            string raw;
            bool found;
            string error;
            if (!TryFindPropertyValue(json, property, out raw, out found, out error))
                throw new InvalidOperationException(error);
            return found && string.Equals(raw, "true", StringComparison.Ordinal);
        }

        internal static bool HasProperty(string json, string property)
        {
            string raw;
            bool found;
            string error;
            if (!TryFindPropertyValue(json, property, out raw, out found, out error))
                throw new InvalidOperationException(error);
            return found;
        }

        private static void SkipWhitespace(string source, ref int index)
        {
            while (index < source.Length && char.IsWhiteSpace(source[index])) index++;
        }

        private static bool TryReadString(string source, ref int index, out string value, out string error)
        {
            value = string.Empty;
            error = string.Empty;
            if (index >= source.Length || source[index] != '"')
            {
                error = "JSON string value is required.";
                return false;
            }

            index++;
            var output = new System.Text.StringBuilder();
            while (index < source.Length)
            {
                var ch = source[index++];
                if (ch == '"')
                {
                    value = output.ToString();
                    return true;
                }
                if (ch < 32)
                {
                    error = "JSON string contains an unescaped control character.";
                    return false;
                }
                if (ch != '\\')
                {
                    output.Append(ch);
                    continue;
                }
                if (index >= source.Length)
                {
                    error = "JSON string escape is incomplete.";
                    return false;
                }

                ch = source[index++];
                switch (ch)
                {
                    case '"': output.Append('"'); break;
                    case '\\': output.Append('\\'); break;
                    case '/': output.Append('/'); break;
                    case 'b': output.Append('\b'); break;
                    case 'f': output.Append('\f'); break;
                    case 'n': output.Append('\n'); break;
                    case 'r': output.Append('\r'); break;
                    case 't': output.Append('\t'); break;
                    case 'u':
                        if (index + 4 > source.Length)
                        {
                            error = "JSON unicode escape is incomplete.";
                            return false;
                        }
                        int code;
                        if (!int.TryParse(source.Substring(index, 4), System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out code))
                        {
                            error = "JSON unicode escape is invalid.";
                            return false;
                        }
                        output.Append((char)code);
                        index += 4;
                        break;
                    default:
                        error = "JSON string escape is invalid.";
                        return false;
                }
            }
            error = "JSON string is unterminated.";
            return false;
        }

        private static bool TrySkipValue(string source, ref int index, out string error)
        {
            error = string.Empty;
            if (index >= source.Length)
            {
                error = "JSON property value is missing.";
                return false;
            }

            if (source[index] == '"')
            {
                string ignored;
                return TryReadString(source, ref index, out ignored, out error);
            }

            if (source[index] == '{' || source[index] == '[')
            {
                var closers = new Stack<char>();
                closers.Push(source[index] == '{' ? '}' : ']');
                index++;
                while (index < source.Length && closers.Count > 0)
                {
                    var ch = source[index];
                    if (ch == '"')
                    {
                        string ignored;
                        if (!TryReadString(source, ref index, out ignored, out error)) return false;
                        continue;
                    }
                    if (ch == '{') { closers.Push('}'); index++; continue; }
                    if (ch == '[') { closers.Push(']'); index++; continue; }
                    if (ch == '}' || ch == ']')
                    {
                        if (closers.Peek() != ch)
                        {
                            error = "JSON nested value has mismatched delimiters.";
                            return false;
                        }
                        closers.Pop();
                        index++;
                        continue;
                    }
                    index++;
                }
                if (closers.Count != 0)
                {
                    error = "JSON nested value is unterminated.";
                    return false;
                }
                return true;
            }

            var start = index;
            while (index < source.Length && source[index] != ',' && source[index] != '}') index++;
            if (source.Substring(start, index - start).Trim().Length == 0)
            {
                error = "JSON property value is missing.";
                return false;
            }
            return true;
        }
    }
}
