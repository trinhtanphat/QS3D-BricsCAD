using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticScheduleDefinition
    {
        public SemanticScheduleDefinition(
            string id,
            string name,
            string title,
            IEnumerable<ElementCategory> categories,
            string floorId,
            string zoneId,
            IEnumerable<string> includeElementIds,
            IEnumerable<string> excludeElementIds,
            IEnumerable<SemanticDocumentationColumn> columns)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Title = title ?? string.Empty;
            Categories = new List<ElementCategory>(categories ?? Array.Empty<ElementCategory>()).AsReadOnly();
            FloorId = floorId ?? string.Empty;
            ZoneId = zoneId ?? string.Empty;
            IncludeElementIds = new List<string>(includeElementIds ?? Array.Empty<string>()).AsReadOnly();
            ExcludeElementIds = new List<string>(excludeElementIds ?? Array.Empty<string>()).AsReadOnly();
            Columns = new List<SemanticDocumentationColumn>(columns ?? throw new ArgumentNullException(nameof(columns))).AsReadOnly();
        }

        public string Id { get; }
        public string Name { get; }
        public string Title { get; }
        public IReadOnlyList<ElementCategory> Categories { get; }
        public string FloorId { get; }
        public string ZoneId { get; }
        public IReadOnlyList<string> IncludeElementIds { get; }
        public IReadOnlyList<string> ExcludeElementIds { get; }
        public IReadOnlyList<SemanticDocumentationColumn> Columns { get; }
    }

    public static class SemanticScheduleCatalog
    {
        public const string MetadataKey = "QS3D.Documentation.SemanticSchedules.v1";
        private const int MaxSchedules = 128;
        private const int MaxIds = 5000;
        private const int MaxColumns = 32;
        private const int MaxPayloadChars = 1024 * 1024;

        public static IReadOnlyList<SemanticScheduleDefinition> Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(MetadataKey, out var payload) || string.IsNullOrWhiteSpace(payload))
                return Array.Empty<SemanticScheduleDefinition>();
            if (payload.Length > MaxPayloadChars) throw new InvalidDataException("Semantic schedule catalog exceeds the 1 MiB metadata limit.");

            var root = Parse(payload);
            if (!string.Equals(root.Name.LocalName, "semanticSchedules", StringComparison.Ordinal) || (string)root.Attribute("version") != "1")
                throw new InvalidDataException("Semantic schedule catalog format/version is invalid.");
            var definitions = root.Elements("schedule").Select(ReadDefinition).ToList();
            ValidateCatalog(definitions);
            return definitions.AsReadOnly();
        }

        public static void Save(ProjectState project, IEnumerable<SemanticScheduleDefinition> definitions)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var list = definitions.ToList();
            ValidateCatalog(list);
            if (list.Count == 0)
            {
                if (!project.Metadata.ContainsKey(MetadataKey)) return;
                project.Touch();
                project.Metadata.Remove(MetadataKey);
                return;
            }

            var payload = Serialize(list);
            if (payload.Length > MaxPayloadChars) throw new InvalidOperationException("Semantic schedule catalog exceeds the 1 MiB metadata limit.");
            if (project.Metadata.TryGetValue(MetadataKey, out var current) && string.Equals(current, payload, StringComparison.Ordinal)) return;
            project.Touch();
            project.Metadata[MetadataKey] = payload;
        }

        public static void Upsert(ProjectState project, SemanticScheduleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var list = Load(project).ToList();
            var index = list.FindIndex(x => string.Equals(x.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) list[index] = definition; else list.Add(definition);
            Save(project, list);
        }

        public static bool Remove(ProjectState project, string id)
        {
            var normalized = Required(id, "schedule id", 80);
            var list = Load(project).ToList();
            var removed = list.RemoveAll(x => string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed) Save(project, list);
            return removed;
        }

        public static SemanticDocumentationTable Build(ProjectState project, SemanticScheduleDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = Normalize(definition);
            if (normalized.FloorId.Length > 0 && project.FindFloor(normalized.FloorId) == null)
                throw new InvalidOperationException("Semantic schedule references missing Floor " + normalized.FloorId + ".");
            if (normalized.ZoneId.Length > 0 && project.FindZone(normalized.ZoneId) == null)
                throw new InvalidOperationException("Semantic schedule references missing Zone " + normalized.ZoneId + ".");

            var include = new HashSet<string>(normalized.IncludeElementIds, StringComparer.OrdinalIgnoreCase);
            var exclude = new HashSet<string>(normalized.ExcludeElementIds, StringComparer.OrdinalIgnoreCase);
            foreach (var id in include)
                if (project.FindElement(id) == null) throw new InvalidOperationException("Semantic schedule include list references missing Element " + id + ".");
            foreach (var id in exclude)
                if (project.FindElement(id) == null) throw new InvalidOperationException("Semantic schedule exclude list references missing Element " + id + ".");

            var candidates = project.Elements.ToArray();
            if (candidates.Any(x => x == null))
                throw new InvalidOperationException("Project contains a null semantic element.");

            var categorySet = new HashSet<ElementCategory>(normalized.Categories);
            var ids = candidates
                .Where(x => categorySet.Count == 0 || categorySet.Contains(x.Category))
                .Where(x => normalized.FloorId.Length == 0 || string.Equals(x.FloorId, normalized.FloorId, StringComparison.OrdinalIgnoreCase))
                .Where(x => normalized.ZoneId.Length == 0 || string.Equals(x.ZoneId, normalized.ZoneId, StringComparison.OrdinalIgnoreCase))
                .Where(x => include.Count == 0 || include.Contains(x.Id))
                .Where(x => !exclude.Contains(x.Id))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => x.Id)
                .ToArray();

            return SemanticDocumentationTableBuilder.Build(project, normalized.Title, ids, normalized.Columns, allowEmpty: true);
        }

        private static void ValidateCatalog(IReadOnlyList<SemanticScheduleDefinition> definitions)
        {
            if (definitions.Count > MaxSchedules) throw new InvalidOperationException("Semantic schedule catalog exceeds the supported 128 definitions.");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in definitions)
            {
                var definition = Normalize(raw);
                if (!ids.Add(definition.Id)) throw new InvalidOperationException("Duplicate semantic schedule id: " + definition.Id + ".");
                if (!names.Add(definition.Name)) throw new InvalidOperationException("Duplicate semantic schedule name: " + definition.Name + ".");
            }
        }

        private static SemanticScheduleDefinition Normalize(SemanticScheduleDefinition raw)
        {
            if (raw == null) throw new ArgumentException("Semantic schedule definition cannot be null.");
            var id = Required(raw.Id, "schedule id", 80);
            var name = Required(raw.Name, "schedule name", 160);
            var title = Required(raw.Title, "schedule title", 160);
            var categories = raw.Categories.Distinct().OrderBy(x => x.ToString(), StringComparer.Ordinal).ToArray();
            foreach (var category in categories)
                if (!Enum.IsDefined(typeof(ElementCategory), category)) throw new InvalidOperationException("Semantic schedule contains unsupported category " + category + ".");
            var include = NormalizeIds(raw.IncludeElementIds, "include");
            var exclude = NormalizeIds(raw.ExcludeElementIds, "exclude");
            if (include.Intersect(exclude, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidOperationException("Semantic schedule include/exclude lists overlap.");
            var columns = raw.Columns.ToArray();
            if (columns.Length == 0 || columns.Length > MaxColumns) throw new InvalidOperationException("Semantic schedule requires 1..32 columns.");
            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalizedColumns = new List<SemanticDocumentationColumn>();
            foreach (var column in columns)
            {
                if (column == null) throw new InvalidOperationException("Semantic schedule column cannot be null.");
                var header = Required(column.Header, "column header", 96);
                var template = Required(column.Template, "column template", 512);
                if (!headers.Add(header)) throw new InvalidOperationException("Semantic schedule contains duplicate column header: " + header + ".");
                normalizedColumns.Add(new SemanticDocumentationColumn(header, template));
            }
            return new SemanticScheduleDefinition(id, name, title, categories, Optional(raw.FloorId, 64), Optional(raw.ZoneId, 64), include, exclude, normalizedColumns);
        }

        private static string[] NormalizeIds(IEnumerable<string> values, string label)
        {
            var ids = (values ?? Array.Empty<string>()).Select(x => Required(x, label + " element id", 128)).ToArray();
            if (ids.Length > MaxIds) throw new InvalidOperationException("Semantic schedule " + label + " list exceeds 5000 ids.");
            if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Length) throw new InvalidOperationException("Semantic schedule " + label + " list contains duplicate ids.");
            return ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray();
        }

        private static string Serialize(IEnumerable<SemanticScheduleDefinition> definitions)
        {
            var schedules = definitions.Select(Normalize)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => new XElement("schedule",
                    new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("title", x.Title),
                    new XAttribute("floorId", x.FloorId), new XAttribute("zoneId", x.ZoneId),
                    new XElement("categories", x.Categories.Select(c => new XElement("category", new XAttribute("value", c)))),
                    new XElement("include", x.IncludeElementIds.Select(id => new XElement("id", new XAttribute("value", id)))),
                    new XElement("exclude", x.ExcludeElementIds.Select(id => new XElement("id", new XAttribute("value", id)))),
                    new XElement("columns", x.Columns.Select(c => new XElement("column", new XAttribute("header", c.Header), new XAttribute("template", c.Template))))));
            var root = new XElement("semanticSchedules", new XAttribute("version", "1"), schedules);
            return root.ToString(SaveOptions.DisableFormatting);
        }

        private static SemanticScheduleDefinition ReadDefinition(XElement node)
        {
            try
            {
                var categories = node.Element("categories")?.Elements("category").Select(x => (ElementCategory)Enum.Parse(typeof(ElementCategory), Required((string)x.Attribute("value"), "category", 64), true)) ?? Array.Empty<ElementCategory>();
                var include = node.Element("include")?.Elements("id").Select(x => (string)x.Attribute("value")) ?? Array.Empty<string>();
                var exclude = node.Element("exclude")?.Elements("id").Select(x => (string)x.Attribute("value")) ?? Array.Empty<string>();
                var columns = node.Element("columns")?.Elements("column").Select(x => new SemanticDocumentationColumn((string)x.Attribute("header"), (string)x.Attribute("template"))) ?? Array.Empty<SemanticDocumentationColumn>();
                return Normalize(new SemanticScheduleDefinition((string)node.Attribute("id"), (string)node.Attribute("name"), (string)node.Attribute("title"), categories, (string)node.Attribute("floorId"), (string)node.Attribute("zoneId"), include, exclude, columns));
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException("Semantic schedule definition is malformed.", ex);
            }
        }

        private static XElement Parse(string payload)
        {
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxPayloadChars };
                using (var text = new StringReader(payload))
                using (var reader = XmlReader.Create(text, settings))
                    return XDocument.Load(reader, LoadOptions.None).Root ?? throw new InvalidDataException("Semantic schedule catalog has no root element.");
            }
            catch (InvalidDataException) { throw; }
            catch (Exception ex) when (ex is XmlException || ex is ArgumentException) { throw new InvalidDataException("Semantic schedule catalog is malformed.", ex); }
        }

        private static string Required(string value, string label, int maxLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Semantic schedule " + label + " is required.");
            if (normalized.Length > maxLength) throw new ArgumentException("Semantic schedule " + label + " exceeds supported length.");
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Semantic schedule " + label + " contains control characters.");
            return normalized;
        }

        private static string Optional(string value, int maxLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length > maxLength || normalized.Any(char.IsControl)) throw new ArgumentException("Semantic schedule optional id is invalid.");
            return normalized;
        }
    }
}
