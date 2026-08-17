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
            Categories = SnapshotBounded(
                categories ?? Array.Empty<ElementCategory>(),
                SemanticScheduleCatalog.MaxIds,
                "Semantic schedule category list exceeds 5000 entries.");
            FloorId = floorId ?? string.Empty;
            ZoneId = zoneId ?? string.Empty;
            IncludeElementIds = SnapshotBounded(
                includeElementIds ?? Array.Empty<string>(),
                SemanticScheduleCatalog.MaxIds,
                "Semantic schedule include list exceeds 5000 ids.");
            ExcludeElementIds = SnapshotBounded(
                excludeElementIds ?? Array.Empty<string>(),
                SemanticScheduleCatalog.MaxIds,
                "Semantic schedule exclude list exceeds 5000 ids.");
            Columns = SnapshotBounded(
                columns ?? throw new ArgumentNullException(nameof(columns)),
                SemanticScheduleCatalog.MaxColumns,
                "Semantic schedule requires 1..32 columns.");
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

        private static IReadOnlyList<T> SnapshotBounded<T>(IEnumerable<T> values, int maxCount, string capacityError)
        {
            var result = new List<T>(Math.Min(maxCount, 256));
            using (var enumerator = values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (result.Count >= maxCount) throw new InvalidOperationException(capacityError);
                    result.Add(enumerator.Current);
                }
            }
            return result.AsReadOnly();
        }
    }

    public static class SemanticScheduleCatalog
    {
        public const string MetadataKey = "QS3D.Documentation.SemanticSchedules.v1";
        private const int MaxSchedules = 128;
        internal const int MaxIds = 5000;
        internal const int MaxColumns = 32;
        private const int MaxRows = 5000;
        private const int MaxPayloadChars = 1024 * 1024;

        public static IReadOnlyList<SemanticScheduleDefinition> Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(MetadataKey, out var payload) || string.IsNullOrEmpty(payload))
                return Array.Empty<SemanticScheduleDefinition>();
            if (payload.Length > MaxPayloadChars) throw new InvalidDataException("Semantic schedule catalog exceeds the 1 MiB metadata limit.");

            var root = Parse(payload);
            if (root.Name.NamespaceName.Length != 0 || !string.Equals(root.Name.LocalName, "semanticSchedules", StringComparison.Ordinal) || (string)root.Attribute("version") != "1")
                throw new InvalidDataException("Semantic schedule catalog format/version is invalid.");
            var scheduleNodes = MaterializeScheduleNodesBounded(root);
            ValidateSchema(root, scheduleNodes);
            var definitions = scheduleNodes.Select(ReadDefinition).ToList();
            ValidateCatalog(definitions);
            return definitions.AsReadOnly();
        }

        private static IReadOnlyList<XElement> MaterializeScheduleNodesBounded(XElement root)
        {
            var result = new List<XElement>(MaxSchedules);
            foreach (var schedule in root.Elements("schedule"))
            {
                if (result.Count >= MaxSchedules)
                    throw new InvalidOperationException("Semantic schedule catalog exceeds the supported 128 definitions.");
                result.Add(schedule);
            }
            return result.AsReadOnly();
        }

        public static void Save(ProjectState project, IEnumerable<SemanticScheduleDefinition> definitions)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var list = new List<SemanticScheduleDefinition>(MaxSchedules);
            foreach (var definition in definitions)
            {
                if (list.Count >= MaxSchedules)
                    throw new InvalidOperationException("Semantic schedule catalog exceeds the supported 128 definitions.");
                list.Add(definition);
            }
            ValidateCatalog(list);
            if (list.Count == 0)
            {
                if (!project.Metadata.ContainsKey(MetadataKey)) return;
                project.Metadata.Remove(MetadataKey);
                return;
            }

            var payload = Serialize(list);
            if (payload.Length > MaxPayloadChars) throw new InvalidOperationException("Semantic schedule catalog exceeds the 1 MiB metadata limit.");
            if (project.Metadata.TryGetValue(MetadataKey, out var current) && string.Equals(current, payload, StringComparison.Ordinal)) return;
            project.Metadata[MetadataKey] = payload;
        }

        public static void Upsert(ProjectState project, SemanticScheduleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var list = Load(project).ToList();
            var normalizedId = Required(definition.Id, "schedule id", 80);
            var index = list.FindIndex(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
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

            var categorySet = new HashSet<ElementCategory>(normalized.Categories);
            var matches = new List<ProjectElement>(Math.Min(project.Elements.Count, MaxRows));
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element.");
                if (categorySet.Count > 0 && !categorySet.Contains(element.Category)) continue;
                if (normalized.FloorId.Length > 0 && !string.Equals((element.FloorId ?? string.Empty).Trim(), normalized.FloorId, StringComparison.OrdinalIgnoreCase)) continue;
                if (normalized.ZoneId.Length > 0 && !string.Equals((element.ZoneId ?? string.Empty).Trim(), normalized.ZoneId, StringComparison.OrdinalIgnoreCase)) continue;
                if (include.Count > 0 && !include.Contains(element.Id)) continue;
                if (exclude.Contains(element.Id)) continue;
                if (matches.Count >= MaxRows)
                    throw new InvalidOperationException("Semantic schedule supports at most 5000 matching elements.");
                matches.Add(element);
            }

            var ids = matches
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
            var categories = raw.Categories.ToArray();
            var uniqueCategories = new HashSet<ElementCategory>();
            foreach (var category in categories)
            {
                if (!Enum.IsDefined(typeof(ElementCategory), category)) throw new InvalidOperationException("Semantic schedule contains unsupported category " + category + ".");
                if (!uniqueCategories.Add(category)) throw new InvalidOperationException("Semantic schedule contains duplicate category " + category + ".");
            }
            categories = uniqueCategories.OrderBy(x => x.ToString(), StringComparer.Ordinal).ToArray();
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
            var root = new XElement("semanticSchedules", new XAttribute("version", "1"), definitions.Select(Normalize)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => new XElement("schedule",
                    new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("title", x.Title),
                    new XAttribute("floorId", x.FloorId), new XAttribute("zoneId", x.ZoneId),
                    new XElement("categories", x.Categories.Select(c => new XElement("category", new XAttribute("value", c)))),
                    new XElement("include", x.IncludeElementIds.Select(id => new XElement("id", new XAttribute("value", id)))),
                    new XElement("exclude", x.ExcludeElementIds.Select(id => new XElement("id", new XAttribute("value", id)))),
                    new XElement("columns", x.Columns.Select(c => new XElement("column", new XAttribute("header", c.Header), new XAttribute("template", c.Template)))))));
            return root.ToString(SaveOptions.DisableFormatting);
        }

        private static SemanticScheduleDefinition ReadDefinition(XElement node)
        {
            try
            {
                var categories = node.Element("categories").Elements("category").Select(ParseCategory);
                var include = node.Element("include").Elements("id").Select(x => (string)x.Attribute("value"));
                var exclude = node.Element("exclude").Elements("id").Select(x => (string)x.Attribute("value"));
                var columns = node.Element("columns").Elements("column").Select(x => new SemanticDocumentationColumn((string)x.Attribute("header"), (string)x.Attribute("template")));
                return Normalize(new SemanticScheduleDefinition((string)node.Attribute("id"), (string)node.Attribute("name"), (string)node.Attribute("title"), categories, (string)node.Attribute("floorId"), (string)node.Attribute("zoneId"), include, exclude, columns));
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException("Semantic schedule definition is malformed.", ex);
            }
        }

        private static ElementCategory ParseCategory(XElement node)
        {
            var stored = (string)node.Attribute("value");
            var raw = Required(stored, "category", 64);
            if (!string.Equals(stored, raw, StringComparison.Ordinal)
                || !Enum.TryParse(raw, false, out ElementCategory category)
                || !Enum.IsDefined(typeof(ElementCategory), category)
                || !string.Equals(raw, category.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Semantic schedule category must use a canonical ElementCategory name.");
            return category;
        }

        private static void ValidateSchema(XElement root, IReadOnlyList<XElement> schedules)
        {
            ValidateElement(root, "semanticSchedules", new[] { "version" }, new[] { "schedule" });
            EnsureRequiredAttributes(root, "version");
            foreach (var schedule in schedules)
            {
                ValidateElement(schedule, "schedule", new[] { "id", "name", "title", "floorId", "zoneId" }, new[] { "categories", "include", "exclude", "columns" });
                EnsureRequiredAttributes(schedule, "id", "name", "title", "floorId", "zoneId");

                var categories = RequireExactlyOneChild(schedule, "categories");
                var include = RequireExactlyOneChild(schedule, "include");
                var exclude = RequireExactlyOneChild(schedule, "exclude");
                var columns = RequireExactlyOneChild(schedule, "columns");

                ValidateElement(categories, "categories", Array.Empty<string>(), new[] { "category" });
                foreach (var category in categories.Elements("category"))
                {
                    ValidateElement(category, "category", new[] { "value" }, Array.Empty<string>());
                    EnsureRequiredAttributes(category, "value");
                }

                ValidateElement(include, "include", Array.Empty<string>(), new[] { "id" });
                foreach (var id in include.Elements("id"))
                {
                    ValidateElement(id, "id", new[] { "value" }, Array.Empty<string>());
                    EnsureRequiredAttributes(id, "value");
                }

                ValidateElement(exclude, "exclude", Array.Empty<string>(), new[] { "id" });
                foreach (var id in exclude.Elements("id"))
                {
                    ValidateElement(id, "id", new[] { "value" }, Array.Empty<string>());
                    EnsureRequiredAttributes(id, "value");
                }

                ValidateElement(columns, "columns", Array.Empty<string>(), new[] { "column" });
                foreach (var column in columns.Elements("column"))
                {
                    ValidateElement(column, "column", new[] { "header", "template" }, Array.Empty<string>());
                    EnsureRequiredAttributes(column, "header", "template");
                }
            }
        }

        private static void ValidateElement(XElement element, string expectedName, IReadOnlyCollection<string> allowedAttributes, IReadOnlyCollection<string> allowedChildren)
        {
            if (element.Name.NamespaceName.Length != 0 || !string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                throw new InvalidDataException("Semantic schedule catalog contains an unsupported XML element: " + element.Name + ".");

            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.NamespaceName.Length != 0 || !allowedAttributes.Contains(attribute.Name.LocalName))
                    throw new InvalidDataException("Semantic schedule catalog contains an unsupported attribute on " + expectedName + ": " + attribute.Name + ".");
            }

            foreach (var node in element.Nodes())
            {
                var child = node as XElement;
                if (child != null)
                {
                    if (child.Name.NamespaceName.Length != 0 || !allowedChildren.Contains(child.Name.LocalName))
                        throw new InvalidDataException("Semantic schedule catalog contains an unsupported child of " + expectedName + ": " + child.Name + ".");
                    continue;
                }

                if (node is XCData)
                    throw new InvalidDataException("Semantic schedule catalog contains unsupported CDATA content in " + expectedName + ".");
                var text = node as XText;
                if (text != null && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Semantic schedule catalog contains unsupported XML content in " + expectedName + ".");
            }
        }

        private static void EnsureRequiredAttributes(XElement element, params string[] attributeNames)
        {
            foreach (var attributeName in attributeNames)
                if (element.Attribute(attributeName) == null)
                    throw new InvalidDataException("Semantic schedule catalog is missing required attribute " + attributeName + " on " + element.Name.LocalName + ".");
        }

        private static XElement RequireExactlyOneChild(XElement parent, string childName)
        {
            var children = parent.Elements(childName).ToArray();
            if (children.Length != 1)
                throw new InvalidDataException("Semantic schedule catalog requires exactly one " + childName + " container per schedule.");
            return children[0];
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
            return RequireXmlText(normalized, label);
        }

        private static string Optional(string value, int maxLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length > maxLength || normalized.Any(char.IsControl)) throw new ArgumentException("Semantic schedule optional id is invalid.");
            return RequireXmlText(normalized, "optional id");
        }

        private static string RequireXmlText(string value, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
                return value;
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Semantic schedule " + label + " contains characters that are invalid in XML.", ex);
            }
        }
    }
}
