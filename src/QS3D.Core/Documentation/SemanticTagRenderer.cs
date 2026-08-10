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
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            EnsureUniqueProjectElement(project, element);
            var source = (template ?? string.Empty).Trim();
            if (source.Length == 0) throw new ArgumentException("Semantic tag template is required.", nameof(template));
            if (source.Length > MaxTemplateLength) throw new ArgumentException("Semantic tag template exceeds " + MaxTemplateLength + " characters.", nameof(template));

            var output = new StringBuilder(Math.Min(source.Length + 64, MaxRenderedLength));
            var tokenCount = 0;
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
                if (close < 0) throw new FormatException("Semantic tag template has an unclosed token at character " + open + ".");
                if (source.IndexOf('{', open + 1, close - open - 1) >= 0)
                    throw new FormatException("Semantic tag tokens cannot be nested.");

                var token = source.Substring(open + 1, close - open - 1).Trim();
                if (token.Length == 0) throw new FormatException("Semantic tag token cannot be empty.");
                tokenCount++;
                if (tokenCount > MaxTokens) throw new FormatException("Semantic tag template exceeds the supported " + MaxTokens + " token limit.");
                AppendBounded(output, Resolve(project, element, token));
                index = close + 1;
            }

            if (output.Length == 0 && !allowEmpty) throw new InvalidOperationException("Semantic tag rendered to an empty label.");
            return output.ToString();
        }

        private static string Resolve(ProjectState project, ProjectElement element, string token)
        {
            if (string.Equals(token, "Id", StringComparison.OrdinalIgnoreCase)) return element.Id;
            if (string.Equals(token, "Category", StringComparison.OrdinalIgnoreCase)) return element.Category.ToString();
            if (string.Equals(token, "Family", StringComparison.OrdinalIgnoreCase)) return ResolveFamily(project, element);
            if (string.Equals(token, "Floor", StringComparison.OrdinalIgnoreCase)) return ResolveFloor(project, element);
            if (string.Equals(token, "Zone", StringComparison.OrdinalIgnoreCase)) return ResolveZone(project, element);

            if (token.StartsWith("P:", StringComparison.OrdinalIgnoreCase))
            {
                var key = token.Substring(2).Trim();
                if (key.Length == 0) throw new FormatException("P: semantic tag token requires a property name.");
                if (!IsDocumentableProperty(key))
                    throw new InvalidOperationException("Semantic tag cannot expose generated/native runtime property: " + key + ".");
                return element.Properties.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
            }

            if (token.StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
            {
                var key = token.Substring(2).Trim();
                if (key.Length == 0) throw new FormatException("Q: semantic tag token requires a quantity name.");
                if (!element.Quantities.TryGetValue(key, out var value)) return string.Empty;
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new InvalidOperationException("Semantic tag quantity is not finite: " + key + ".");
                return value.ToString("R", CultureInfo.InvariantCulture);
            }

            throw new FormatException("Unsupported semantic tag token: {" + token + "}.");
        }

        private static void EnsureUniqueProjectElement(ProjectState project, ProjectElement element)
        {
            ProjectElement? match = null;
            foreach (var candidate in project.Elements)
            {
                if (!string.Equals(candidate.Id, element.Id, StringComparison.OrdinalIgnoreCase)) continue;
                if (match != null)
                    throw new InvalidOperationException("Semantic tag element id is ambiguous in project: " + element.Id + ".");
                match = candidate;
            }

            if (match == null || !ReferenceEquals(match, element))
                throw new InvalidOperationException("Semantic tag element is not part of the supplied project: " + element.Id + ".");
        }

        private static string ResolveFamily(ProjectState project, ProjectElement element)
        {
            if (string.IsNullOrWhiteSpace(element.FamilyId)) return string.Empty;
            ProjectFamily? match = null;
            foreach (var family in project.Families)
            {
                if (!string.Equals(family.Id, element.FamilyId, StringComparison.OrdinalIgnoreCase)) continue;
                if (match != null)
                    throw new InvalidOperationException("Semantic tag references ambiguous Family " + element.FamilyId + " on element " + element.Id + ".");
                match = family;
            }
            if (match == null) throw new InvalidOperationException("Semantic tag references missing Family " + element.FamilyId + " on element " + element.Id + ".");
            return match.Name;
        }

        private static string ResolveFloor(ProjectState project, ProjectElement element)
        {
            if (string.IsNullOrWhiteSpace(element.FloorId)) return string.Empty;
            FloorDefinition? match = null;
            foreach (var floor in project.Floors)
            {
                if (!string.Equals(floor.Id, element.FloorId, StringComparison.OrdinalIgnoreCase)) continue;
                if (match != null)
                    throw new InvalidOperationException("Semantic tag references ambiguous Floor " + element.FloorId + " on element " + element.Id + ".");
                match = floor;
            }
            if (match == null) throw new InvalidOperationException("Semantic tag references missing Floor " + element.FloorId + " on element " + element.Id + ".");
            return match.Name;
        }

        private static string ResolveZone(ProjectState project, ProjectElement element)
        {
            if (string.IsNullOrWhiteSpace(element.ZoneId)) return string.Empty;
            ZoneDefinition? match = null;
            foreach (var zone in project.Zones)
            {
                if (!string.Equals(zone.Id, element.ZoneId, StringComparison.OrdinalIgnoreCase)) continue;
                if (match != null)
                    throw new InvalidOperationException("Semantic tag references ambiguous Zone " + element.ZoneId + " on element " + element.Id + ".");
                match = zone;
            }
            if (match == null) throw new InvalidOperationException("Semantic tag references missing Zone " + element.ZoneId + " on element " + element.Id + ".");
            return match.Name;
        }

        private static bool IsDocumentableProperty(string key)
        {
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)) return false;
            if (key.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
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
