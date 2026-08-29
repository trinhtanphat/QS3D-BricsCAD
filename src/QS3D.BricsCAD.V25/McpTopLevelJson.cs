using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Small JSON member scanner used for security-sensitive MCP routing. It intentionally
    /// scopes lookups to one object level so nested values or string contents cannot impersonate
    /// a caller-controlled top-level confirmation/property.
    /// </summary>
    internal static class McpTopLevelJson
    {
        private const int MaxJsonDepth = 64;

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

            var source = TrimJsonWhitespace(json ?? string.Empty);
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
                var candidate = TrimJsonWhitespace(source.Substring(valueStart, index - valueStart));

                if (string.Equals(name, property, StringComparison.OrdinalIgnoreCase))
                {
                    if (found)
                    {
                        error = "duplicate top-level JSON property: " + property;
                        return false;
                    }
                    if (string.Equals(property, "arguments", StringComparison.OrdinalIgnoreCase)
                        && candidate.Length > 0 && candidate[0] == '{')
                    {
                        string canonicalArguments;
                        if (!TryCanonicalizeFlatObject(candidate, out canonicalArguments, out error))
                            return false;
                        candidate = canonicalArguments;
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
                    SkipWhitespace(source, ref index);
                    if (index >= source.Length || source[index] == '}')
                    {
                        error = "JSON object cannot end with a trailing comma.";
                        return false;
                    }
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
            if (!TryFindPropertyValue(json, property, out raw, out found, out error)) return string.Empty;
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

        internal static bool TryExtractDouble(
            string json,
            string property,
            out double value,
            out bool found,
            out string error)
        {
            value = 0d;
            string raw;
            if (!TryFindPropertyValue(json, property, out raw, out found, out error)) return false;
            if (!found) return true;
            if (!IsJsonNumberToken(raw)
                || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || double.IsNaN(value)
                || double.IsInfinity(value))
            {
                error = property + " must be a finite JSON number.";
                return false;
            }
            return true;
        }

        internal static bool TryExtractInteger(
            string json,
            string property,
            out int value,
            out bool found,
            out string error)
        {
            value = 0;
            string raw;
            if (!TryFindPropertyValue(json, property, out raw, out found, out error)) return false;
            if (!found) return true;
            if (!IsJsonIntegerToken(raw)
                || !int.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
            {
                error = property + " must be a JSON integer.";
                return false;
            }
            return true;
        }

        internal static string ExtractId(string json)
        {
            string raw;
            bool found;
            string error;
            if (!TryFindPropertyValue(json, "id", out raw, out found, out error))
                throw new InvalidOperationException(error);
            if (!found || string.Equals(raw, "null", StringComparison.Ordinal)) return "null";
            if (IsJsonNumberToken(raw)) return raw;
            if (raw.Length >= 2 && raw[0] == '"')
            {
                var index = 0;
                string value;
                if (!TryReadString(raw, ref index, out value, out error)) return "null";
                SkipWhitespace(raw, ref index);
                if (index != raw.Length) return "null";
                return QuoteJsonString(value, false);
            }
            throw new InvalidOperationException("JSON-RPC id must be a string, number, or null.");
        }

        private static bool TryCanonicalizeFlatObject(string json, out string canonical, out string error)
        {
            canonical = string.Empty;
            error = string.Empty;
            var source = TrimJsonWhitespace(json ?? string.Empty);
            if (source.Length < 2 || source[0] != '{')
            {
                error = "MCP arguments must be a JSON object.";
                return false;
            }

            var index = 1;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var output = new StringBuilder("{");
            var first = true;
            while (true)
            {
                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "MCP arguments object ended unexpectedly.";
                    return false;
                }
                if (source[index] == '}')
                {
                    index++;
                    SkipWhitespace(source, ref index);
                    if (index != source.Length)
                    {
                        error = "Unexpected content after MCP arguments object.";
                        return false;
                    }
                    canonical = output.Append('}').ToString();
                    return true;
                }
                if (source[index] != '"')
                {
                    error = "MCP argument property name must be a string.";
                    return false;
                }

                string name;
                if (!TryReadString(source, ref index, out name, out error)) return false;
                if (!names.Add(name))
                {
                    error = "duplicate top-level JSON property: " + name;
                    return false;
                }
                SkipWhitespace(source, ref index);
                if (index >= source.Length || source[index] != ':')
                {
                    error = "MCP argument property is missing ':'.";
                    return false;
                }
                index++;
                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "MCP argument value is missing.";
                    return false;
                }
                if (source[index] == '{' || source[index] == '[')
                {
                    error = "MCP argument values must be flat JSON scalars.";
                    return false;
                }

                string canonicalValue;
                if (source[index] == '"')
                {
                    string value;
                    if (!TryReadString(source, ref index, out value, out error)) return false;
                    canonicalValue = QuoteJsonString(value, true);
                }
                else
                {
                    var start = index;
                    while (index < source.Length && source[index] != ',' && source[index] != '}') index++;
                    var token = TrimJsonWhitespace(source.Substring(start, index - start));
                    if (!IsJsonPrimitiveToken(token))
                    {
                        error = "MCP argument value must be a JSON string, boolean, null, or number.";
                        return false;
                    }
                    canonicalValue = token;
                }

                if (!first) output.Append(',');
                first = false;
                output.Append(QuoteJsonString(name, true)).Append(':').Append(canonicalValue);

                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "MCP arguments object ended unexpectedly.";
                    return false;
                }
                if (source[index] == ',')
                {
                    index++;
                    SkipWhitespace(source, ref index);
                    if (index >= source.Length || source[index] == '}')
                    {
                        error = "MCP arguments object cannot end with a trailing comma.";
                        return false;
                    }
                    continue;
                }
                if (source[index] != '}')
                {
                    error = "MCP arguments object requires ',' or '}' after a property value.";
                    return false;
                }
            }
        }

        private static string QuoteJsonString(string value, bool encodeQuotesAsUnicode)
        {
            var output = new StringBuilder((value ?? string.Empty).Length + 8);
            output.Append('"');
            foreach (var ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '"': output.Append(encodeQuotesAsUnicode ? "\\u0022" : "\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\b': output.Append("\\b"); break;
                    case '\f': output.Append("\\f"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (ch < 32) output.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else output.Append(ch);
                        break;
                }
            }
            return output.Append('"').ToString();
        }

        private static bool IsJsonPrimitiveToken(string token)
        {
            return string.Equals(token, "true", StringComparison.Ordinal)
                   || string.Equals(token, "false", StringComparison.Ordinal)
                   || string.Equals(token, "null", StringComparison.Ordinal)
                   || IsJsonNumberToken(token);
        }

        private static bool IsJsonIntegerToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            var index = 0;
            if (token[index] == '-')
            {
                index++;
                if (index == token.Length) return false;
            }
            if (token[index] == '0')
                return index + 1 == token.Length;
            if (token[index] < '1' || token[index] > '9') return false;
            for (index++; index < token.Length; index++)
                if (token[index] < '0' || token[index] > '9') return false;
            return true;
        }

        private static bool IsJsonNumberToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            var index = 0;
            if (token[index] == '-')
            {
                index++;
                if (index == token.Length) return false;
            }

            if (token[index] == '0') index++;
            else
            {
                if (token[index] < '1' || token[index] > '9') return false;
                while (++index < token.Length && token[index] >= '0' && token[index] <= '9') { }
            }

            if (index < token.Length && token[index] == '.')
            {
                index++;
                var fractionStart = index;
                while (index < token.Length && token[index] >= '0' && token[index] <= '9') index++;
                if (index == fractionStart) return false;
            }

            if (index < token.Length && (token[index] == 'e' || token[index] == 'E'))
            {
                index++;
                if (index < token.Length && (token[index] == '+' || token[index] == '-')) index++;
                var exponentStart = index;
                while (index < token.Length && token[index] >= '0' && token[index] <= '9') index++;
                if (index == exponentStart) return false;
            }
            return index == token.Length;
        }

        private static string TrimJsonWhitespace(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var start = 0;
            while (start < value.Length && IsJsonWhitespace(value[start])) start++;
            if (start == value.Length) return string.Empty;
            var end = value.Length - 1;
            while (end >= start && IsJsonWhitespace(value[end])) end--;
            return start == 0 && end == value.Length - 1
                ? value
                : value.Substring(start, end - start + 1);
        }

        private static bool IsJsonWhitespace(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
        }

        private static bool IsJsonValueDelimiter(char ch)
        {
            return IsJsonWhitespace(ch) || ch == ',' || ch == '}' || ch == ']';
        }

        private static void SkipWhitespace(string source, ref int index)
        {
            while (index < source.Length && IsJsonWhitespace(source[index])) index++;
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
            var output = new StringBuilder();
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
                        if (!int.TryParse(source.Substring(index, 4), NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture, out code))
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
            return TrySkipJsonValue(source, ref index, 0, out error);
        }

        private static bool TrySkipJsonValue(string source, ref int index, int depth, out string error)
        {
            error = string.Empty;
            if (index >= source.Length)
            {
                error = "JSON property value is missing.";
                return false;
            }
            if (depth > MaxJsonDepth)
            {
                error = "JSON nesting exceeds the supported depth.";
                return false;
            }

            if (source[index] == '"')
            {
                string ignored;
                return TryReadString(source, ref index, out ignored, out error);
            }
            if (source[index] == '{') return TrySkipJsonObject(source, ref index, depth + 1, out error);
            if (source[index] == '[') return TrySkipJsonArray(source, ref index, depth + 1, out error);
            return TrySkipJsonPrimitive(source, ref index, out error);
        }

        private static bool TrySkipJsonObject(string source, ref int index, int depth, out string error)
        {
            error = string.Empty;
            if (depth > MaxJsonDepth)
            {
                error = "JSON nesting exceeds the supported depth.";
                return false;
            }
            index++;
            SkipWhitespace(source, ref index);
            if (index >= source.Length)
            {
                error = "JSON object ended unexpectedly.";
                return false;
            }
            if (source[index] == '}')
            {
                index++;
                return true;
            }

            while (true)
            {
                if (index >= source.Length || source[index] != '"')
                {
                    error = "JSON object property name must be a string.";
                    return false;
                }
                string ignoredName;
                if (!TryReadString(source, ref index, out ignoredName, out error)) return false;
                SkipWhitespace(source, ref index);
                if (index >= source.Length || source[index] != ':')
                {
                    error = "JSON object property is missing ':'.";
                    return false;
                }
                index++;
                SkipWhitespace(source, ref index);
                if (!TrySkipJsonValue(source, ref index, depth, out error)) return false;
                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "JSON object ended unexpectedly.";
                    return false;
                }
                if (source[index] == '}')
                {
                    index++;
                    return true;
                }
                if (source[index] != ',')
                {
                    error = "JSON object requires ',' or '}' after a property value.";
                    return false;
                }
                index++;
                SkipWhitespace(source, ref index);
                if (index >= source.Length || source[index] == '}')
                {
                    error = "JSON object cannot end with a trailing comma.";
                    return false;
                }
            }
        }

        private static bool TrySkipJsonArray(string source, ref int index, int depth, out string error)
        {
            error = string.Empty;
            if (depth > MaxJsonDepth)
            {
                error = "JSON nesting exceeds the supported depth.";
                return false;
            }
            index++;
            SkipWhitespace(source, ref index);
            if (index >= source.Length)
            {
                error = "JSON array ended unexpectedly.";
                return false;
            }
            if (source[index] == ']')
            {
                index++;
                return true;
            }

            while (true)
            {
                if (!TrySkipJsonValue(source, ref index, depth, out error)) return false;
                SkipWhitespace(source, ref index);
                if (index >= source.Length)
                {
                    error = "JSON array ended unexpectedly.";
                    return false;
                }
                if (source[index] == ']')
                {
                    index++;
                    return true;
                }
                if (source[index] != ',')
                {
                    error = "JSON array requires ',' or ']' after a value.";
                    return false;
                }
                index++;
                SkipWhitespace(source, ref index);
                if (index >= source.Length || source[index] == ']')
                {
                    error = "JSON array cannot end with a trailing comma.";
                    return false;
                }
            }
        }

        private static bool TrySkipJsonPrimitive(string source, ref int index, out string error)
        {
            error = string.Empty;
            var start = index;
            while (index < source.Length && !IsJsonValueDelimiter(source[index])) index++;
            var token = source.Substring(start, index - start);
            if (!IsJsonPrimitiveToken(token))
            {
                error = "JSON primitive value is invalid.";
                return false;
            }
            return true;
        }
    }
}
