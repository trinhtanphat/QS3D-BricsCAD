using System;
using System.Globalization;
using System.Text;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public static class SemanticTagRenderer
    {
        private const int MaxTemplateLength = 512;
        private const int MaxRenderedLength = 2048;
        private const int MaxTokens = 64;

        public static string Render(ProjectState project, ProjectElement element, string template)
        {
            return Render(project, element, template, allowEmpty: false);
        }

        public static string Render(ProjectState project, ProjectElement element, string template, bool allowEmpty)
        {
            var context = new SemanticTagRenderContext(project);
            return Render(context, element, template, allowEmpty);
        }

        public static void ValidateTemplate(string template)
        {
            ValidateTemplateSource(NormalizeTemplate(template));
        }

        internal static string Render(SemanticTagRenderContext context, ProjectElement element, string template, bool allowEmpty)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (element == null) throw new ArgumentNullException(nameof(element));
            context.EnsureElement(element);
            var source = NormalizeTemplate(template);
            ValidateTemplateSource(source);

            var output = new StringBuilder(Math.Min(source.Length + 64, MaxRenderedLength));
            for (var index = 0; index < source.Length;)
            {
                var open = source.IndexOf('{', index);
                if (open < 0)
                {
                    AppendBounded(output, source.Substring(index));
                    break;
                }

                AppendBounded(output, source.Substring(index, open - index));
                var close = source.IndexOf('}', open + 1);
                var token = source.Substring(open + 1, close - open - 1).Trim();
                AppendBounded(output, Resolve(context, element, token));
                index = close + 1;
            }

            if (output.Length == 0 && !allowEmpty) throw new InvalidOperationException("Semantic tag rendered to an empty label.");
            return output.ToString();
        }

        private static string NormalizeTemplate(string? template)
        {
            var source = (template ?? string.Empty).Trim();
            if (source.Length == 0) throw new ArgumentException("Semantic tag template is required.", nameof(template));
            if (source.Length > MaxTemplateLength) throw new ArgumentException("Semantic tag template exceeds " + MaxTemplateLength + " characters.", nameof(template));
            return source;
        }

        private static void ValidateTemplateSource(string source)
        {
            var tokenCount = 0;
            for (var index = 0; index < source.Length;)
            {
                var open = source.IndexOf('{', index);
                var strayClose = source.IndexOf('}', index);
                if (strayClose >= 0 && (open < 0 || strayClose < open))
                    throw new FormatException("Semantic tag template has an unexpected closing brace at character " + strayClose + ".");
                if (open < 0) break;

                var close = source.IndexOf('}', open + 1);
                if (close < 0) throw new FormatException("Semantic tag template has an unclosed token at character " + open + ".");
                if (source.IndexOf('{', open + 1, close - open - 1) >= 0)
                    throw new FormatException("Semantic tag tokens cannot be nested.");

                var token = source.Substring(open + 1, close - open - 1).Trim();
                if (token.Length == 0) throw new FormatException("Semantic tag token cannot be empty.");
                tokenCount++;
                if (tokenCount > MaxTokens) throw new FormatException("Semantic tag template exceeds the supported " + MaxTokens + " token limit.");
                ValidateToken(token);
                index = close + 1;
            }
        }

        private static void ValidateToken(string token)
        {
            if (string.Equals(token, "Id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Category", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Family", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Floor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Zone", StringComparison.OrdinalIgnoreCase)) return;

            if (token.StartsWith("P:", StringComparison.OrdinalIgnoreCase))
            {
                var key = token.Substring(2).Trim();
                if (key.Length == 0) throw new FormatException("P: semantic tag token requires a property name.");
                if (!IsDocumentableProperty(key))
                    throw new InvalidOperationException("Semantic tag cannot expose generated/native runtime property: " + key + ".");
                return;
            }

            if (token.StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
            {
                var key = token.Substring(2).Trim();
                if (key.Length == 0) throw new FormatException("Q: semantic tag token requires a quantity name.");
                return;
            }

            throw new FormatException("Unsupported semantic tag token: {" + token + "}.");
        }

        private static string Resolve(SemanticTagRenderContext context, ProjectElement element, string token)
        {
            ValidateToken(token);
            if (string.Equals(token, "Id", StringComparison.OrdinalIgnoreCase)) return element.Id;
            if (string.Equals(token, "Category", StringComparison.OrdinalIgnoreCase)) return element.Category.ToString();
            if (string.Equals(token, "Family", StringComparison.OrdinalIgnoreCase)) return context.ResolveFamily(element);
            if (string.Equals(token, "Floor", StringComparison.OrdinalIgnoreCase)) return context.ResolveFloor(element);
            if (string.Equals(token, "Zone", StringComparison.OrdinalIgnoreCase)) return context.ResolveZone(element);

            if (token.StartsWith("P:", StringComparison.OrdinalIgnoreCase))
            {
                var key = token.Substring(2).Trim();
                return element.Properties.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
            }

            var quantityKey = token.Substring(2).Trim();
            if (!element.Quantities.TryGetValue(quantityKey, out var quantity)) return string.Empty;
            if (double.IsNaN(quantity) || double.IsInfinity(quantity))
                throw new InvalidOperationException("Semantic tag quantity is not finite: " + quantityKey + ".");
            return quantity.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool IsDocumentableProperty(string key)
        {
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)) return false;
            if (key.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
            if (key.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        private static void AppendBounded(StringBuilder output, string value)
        {
            var text = value ?? string.Empty;
            if (output.Length > MaxRenderedLength - text.Length)
                throw new InvalidOperationException("Semantic tag output exceeds " + MaxRenderedLength + " characters.");
            output.Append(text);
        }
    }
}
