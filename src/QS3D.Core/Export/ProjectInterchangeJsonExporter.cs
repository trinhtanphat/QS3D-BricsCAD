using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Export
{
    public static class ProjectInterchangeJsonExporter
    {
        public const string FormatName = "QS3D.SemanticSnapshot";
        public const int FormatVersion = 1;

        public static string Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateProjectIdentity(project);
            ProjectInterchangeSemanticReferenceValidator.Validate(project);
            ValidateSemanticCollections(project);

            var json = new StringBuilder(32768);
            json.Append("{\n");
            Property(json, 1, "format", FormatName, true);
            NumberProperty(json, 1, "formatVersion", FormatVersion, true);
            json.Append("  \"units\": {\"length\":\"m\",\"area\":\"m2\",\"volume\":\"m3\",\"mass\":\"kg\"},\n");
            json.Append("  \"project\": {\n");
            Property(json, 2, "id", project.ProjectId, true);
            Property(json, 2, "name", project.Name, true);
            NumberProperty(json, 2, "schemaVersion", project.SchemaVersion, true);
            Property(json, 2, "drawingFingerprint", project.DrawingFingerprint ?? string.Empty, true);
            Property(json, 2, "updatedUtc", Utc(project.UpdatedUtc), false);
            json.Append("  },\n");

            json.Append("  \"zones\": [");
            var zones = project.Zones.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            if (zones.Count > 0) json.Append('\n');
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                json.Append("    {\"id\":\"").Append(Escape(zone.Id)).Append("\",\"name\":\"").Append(Escape(zone.Name)).Append("\"}");
                json.Append(i + 1 < zones.Count ? ",\n" : "\n");
            }
            json.Append("  ],\n");

            json.Append("  \"floors\": [");
            var floors = project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            if (floors.Count > 0) json.Append('\n');
            for (var i = 0; i < floors.Count; i++)
            {
                var floor = floors[i];
                json.Append("    {\"id\":\"").Append(Escape(floor.Id)).Append("\",\"name\":\"").Append(Escape(floor.Name))
                    .Append("\",\"elevationM\":").Append(Number(floor.ElevationM)).Append('}');
                json.Append(i + 1 < floors.Count ? ",\n" : "\n");
            }
            json.Append("  ],\n");

            json.Append("  \"families\": [");
            var families = project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            if (families.Count > 0) json.Append('\n');
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                json.Append("    {\"id\":\"").Append(Escape(family.Id)).Append("\",\"name\":\"").Append(Escape(family.Name))
                    .Append("\",\"category\":\"").Append(Escape(family.Category.ToString())).Append("\",\"properties\":");
                AppendStringMap(json, family.Properties.Where(x => IsInterchangeProperty(x.Key)), 2);
                json.Append('}');
                json.Append(i + 1 < families.Count ? ",\n" : "\n");
            }
            json.Append("  ],\n");

            json.Append("  \"elements\": [");
            var elements = project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            if (elements.Count > 0) json.Append('\n');
            for (var i = 0; i < elements.Count; i++)
            {
                AppendElement(json, elements[i]);
                json.Append(i + 1 < elements.Count ? ",\n" : "\n");
            }
            json.Append("  ]\n");
            json.Append("}\n");
            return json.ToString();
        }

        public static void Export(string path, ProjectState project)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Interchange export path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = AtomicFileCommit.CreateTempPath(fullPath);
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(Build(project));
                    writer.Flush();
                    stream.Flush(true);
                }
                AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);
            }
            finally
            {
                AtomicFileCommit.TryDelete(tempPath);
            }
        }

        private static void AppendElement(StringBuilder json, ProjectElement element)
        {
            json.Append("    {\n");
            Property(json, 3, "id", element.Id, true);
            Property(json, 3, "category", element.Category.ToString(), true);
            Property(json, 3, "familyId", element.FamilyId ?? string.Empty, true);
            Property(json, 3, "floorId", element.FloorId ?? string.Empty, true);
            Property(json, 3, "zoneId", element.ZoneId ?? string.Empty, true);
            Property(json, 3, "drawingFingerprint", element.DrawingFingerprint ?? string.Empty, true);
            Property(json, 3, "updatedUtc", Utc(element.UpdatedUtc), true);
            Property(json, 3, "sourceRefScope", "drawing-local", true);

            json.Append("      \"sourceHandles\": ");
            AppendStringArray(json, element.SourceHandles, "sourceHandles");
            json.Append(",\n");
            json.Append("      \"dependencies\": ");
            AppendStringArray(json, element.DependsOn, "dependencies");
            json.Append(",\n");
            json.Append("      \"properties\": ");
            AppendStringMap(json, element.Properties.Where(x => ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key)), 3);
            json.Append(",\n");
            json.Append("      \"quantities\": ");
            AppendNumberMap(json, element.Quantities);
            json.Append("\n    }");
        }

        private static bool IsInterchangeProperty(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)) return false;
            if (normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static void AppendStringMap(StringBuilder json, IEnumerable<KeyValuePair<string, string>> source, int indent)
        {
            var items = source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
            if (items.Count == 0) { json.Append("{}"); return; }
            json.Append("{\n");
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                json.Append(new string(' ', (indent + 1) * 2)).Append('"').Append(Escape(item.Key)).Append("\":\"")
                    .Append(Escape(item.Value ?? string.Empty)).Append('"');
                json.Append(i + 1 < items.Count ? ",\n" : "\n");
            }
            json.Append(new string(' ', indent * 2)).Append('}');
        }

        private static void AppendNumberMap(StringBuilder json, IDictionary<string, double> source)
        {
            var items = source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
            json.Append('{');
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append('"').Append(Escape(items[i].Key)).Append("\":").Append(Number(items[i].Value));
            }
            json.Append('}');
        }

        private static void AppendStringArray(StringBuilder json, IEnumerable<string> values, string label)
        {
            if (values == null) throw new InvalidDataException("Interchange export requires " + label + ".");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = new List<string>();
            var index = 0;
            foreach (var value in values)
            {
                var raw = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidDataException("Interchange export " + label + " contains an empty value at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new InvalidDataException("Interchange export " + label + " contains a non-canonical padded value at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
                if (!seen.Add(raw))
                    throw new InvalidDataException("Interchange export " + label + " contains a duplicate value: " + raw + ".");
                items.Add(raw);
                index++;
            }
            items.Sort(StringComparer.OrdinalIgnoreCase);

            json.Append('[');
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append('"').Append(Escape(items[i])).Append('"');
            }
            json.Append(']');
        }

        private static void ValidateProjectIdentity(ProjectState project)
        {
            if (string.IsNullOrWhiteSpace(project.ProjectId)) throw new InvalidDataException("Interchange export requires a project id.");
            if (project.SchemaVersion <= 0) throw new InvalidDataException("Interchange export requires a positive project schema version.");
        }

        private static void ValidateSemanticCollections(ProjectState project)
        {
            ValidateUniqueIds(project.Zones, x => x.Id, "Zone");
            ValidateUniqueIds(project.Floors, x => x.Id, "Floor");
            ValidateUniqueIds(project.Families, x => x.Id, "Family");
            ValidateUniqueIds(project.Elements, x => x.Id, "element");
        }

        private static void ValidateUniqueIds<T>(IEnumerable<T> source, Func<T, string> idSelector, string label) where T : class
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (item == null)
                    throw new InvalidDataException("Interchange export " + label + " collection contains a null entry.");
                var id = (idSelector(item) ?? string.Empty).Trim();
                if (id.Length == 0)
                    throw new InvalidDataException("Interchange export " + label + " collection contains an empty id.");
                if (!seen.Add(id))
                    throw new InvalidDataException("Interchange export contains duplicate " + label + " id: " + id + ".");
            }
        }

        private static void Property(StringBuilder json, int indent, string name, string value, bool comma)
        {
            json.Append(new string(' ', indent * 2)).Append('"').Append(Escape(name)).Append("\":\"").Append(Escape(value ?? string.Empty)).Append('"');
            json.Append(comma ? ",\n" : "\n");
        }

        private static void NumberProperty(StringBuilder json, int indent, string name, int value, bool comma)
        {
            json.Append(new string(' ', indent * 2)).Append('"').Append(Escape(name)).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
            json.Append(comma ? ",\n" : "\n");
        }

        private static string Number(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidDataException("Interchange export cannot encode a non-finite number.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Utc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("Interchange export timestamps must have DateTimeKind.Utc for deterministic output.");
            return value.ToString("O", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            var input = value ?? string.Empty;
            var result = new StringBuilder(input.Length + 8);
            foreach (var ch in input)
            {
                switch (ch)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (ch < 0x20) result.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else result.Append(ch);
                        break;
                }
            }
            return result.ToString();
        }
    }
}
