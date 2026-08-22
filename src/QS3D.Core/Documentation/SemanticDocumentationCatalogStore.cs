using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticDocumentationCatalog
    {
        internal SemanticDocumentationCatalog(
            IReadOnlyList<SemanticViewDefinition> views,
            IReadOnlyList<SemanticSheetDefinition> sheets)
        {
            Views = new List<SemanticViewDefinition>(views).AsReadOnly();
            Sheets = new List<SemanticSheetDefinition>(sheets).AsReadOnly();
        }

        public IReadOnlyList<SemanticViewDefinition> Views { get; }
        public IReadOnlyList<SemanticSheetDefinition> Sheets { get; }
    }

    public sealed class SemanticDocumentationCatalogStore
    {
        public const string MetadataKey = "QS3D.Documentation.Catalog.v1";
        private const int FormatVersion = 1;
        private const int MaxCatalogChars = 1024 * 1024;

        public void Save(
            ProjectState project,
            IEnumerable<SemanticViewDefinition> views,
            IEnumerable<SemanticSheetDefinition> sheets)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (views == null) throw new ArgumentNullException(nameof(views));
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));

            var viewDefinitions = MaterializeViews(views);
            var sheetDefinitions = MaterializeSheets(sheets);
            var viewPlans = SemanticViewPlanner.BuildCatalog(project, viewDefinitions);
            SemanticSheetPlanner.BuildCatalog(sheetDefinitions, viewPlans);

            if (viewDefinitions.Count == 0 && sheetDefinitions.Count == 0)
            {
                if (!project.Metadata.ContainsKey(MetadataKey)) return;
                project.Touch();
                project.Metadata.Remove(MetadataKey);
                return;
            }

            var payload = Serialize(viewDefinitions, sheetDefinitions);
            if (payload.Length > MaxCatalogChars)
                throw new InvalidOperationException("Semantic documentation catalog exceeds the 1 MiB metadata limit.");
            if (project.Metadata.TryGetValue(MetadataKey, out var current) && string.Equals(current, payload, StringComparison.Ordinal)) return;

            project.Touch();
            project.Metadata[MetadataKey] = payload;
        }

        public SemanticDocumentationCatalog Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(MetadataKey, out var payload) || string.IsNullOrEmpty(payload))
                return new SemanticDocumentationCatalog(Array.Empty<SemanticViewDefinition>(), Array.Empty<SemanticSheetDefinition>());
            if (payload.Length > MaxCatalogChars)
                throw new InvalidDataException("Semantic documentation catalog exceeds the 1 MiB metadata limit.");

            var root = ParseRoot(payload);
            if (!string.Equals(root.Name.LocalName, "documentation", StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation catalog root is invalid.");
            if (Integer(root.Attribute("version")?.Value, "documentation version") != FormatVersion)
                throw new InvalidDataException("Unsupported semantic documentation catalog version.");

            var views = ReadViews(root.Element("views"));
            var sheets = ReadSheets(root.Element("sheets"));
            var viewPlans = SemanticViewPlanner.BuildCatalog(project, views);
            SemanticSheetPlanner.BuildCatalog(sheets, viewPlans);
            return new SemanticDocumentationCatalog(views, sheets);
        }

        private static IReadOnlyList<SemanticViewDefinition> MaterializeViews(IEnumerable<SemanticViewDefinition> values)
        {
            var result = new List<SemanticViewDefinition>();
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentException("Semantic documentation view cannot be null.", nameof(values));
                result.Add(value);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<SemanticSheetDefinition> MaterializeSheets(IEnumerable<SemanticSheetDefinition> values)
        {
            var result = new List<SemanticSheetDefinition>();
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentException("Semantic documentation sheet cannot be null.", nameof(values));
                result.Add(value);
            }
            return result.AsReadOnly();
        }

        private static string Serialize(
            IReadOnlyList<SemanticViewDefinition> views,
            IReadOnlyList<SemanticSheetDefinition> sheets)
        {
            var root = new XElement("documentation",
                new XAttribute("version", FormatVersion),
                new XElement("views",
                    views
                        .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(SerializeView)),
                new XElement("sheets",
                    sheets
                        .OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(SerializeSheet)));
            return root.ToString(SaveOptions.DisableFormatting);
        }

        private static XElement SerializeView(SemanticViewDefinition view)
        {
            return new XElement("view",
                new XAttribute("id", view.Id ?? string.Empty),
                new XAttribute("name", view.Name ?? string.Empty),
                new XAttribute("kind", view.Kind),
                new XAttribute("floorId", view.FloorId ?? string.Empty),
                new XAttribute("zoneId", view.ZoneId ?? string.Empty),
                new XElement("categories",
                    view.Categories
                        .Distinct()
                        .OrderBy(x => x.ToString(), StringComparer.OrdinalIgnoreCase)
                        .Select(x => new XElement("category", new XAttribute("value", x)))),
                new XElement("include",
                    view.IncludeElementIds
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x, StringComparer.Ordinal)
                        .Select(x => new XElement("id", new XAttribute("value", x ?? string.Empty)))),
                new XElement("exclude",
                    view.ExcludeElementIds
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => x, StringComparer.Ordinal)
                        .Select(x => new XElement("id", new XAttribute("value", x ?? string.Empty)))));
        }

        private static XElement SerializeSheet(SemanticSheetDefinition sheet)
        {
            return new XElement("sheet",
                new XAttribute("id", sheet.Id ?? string.Empty),
                new XAttribute("number", sheet.Number ?? string.Empty),
                new XAttribute("name", sheet.Name ?? string.Empty),
                new XAttribute("widthMm", Number(sheet.WidthMm)),
                new XAttribute("heightMm", Number(sheet.HeightMm)),
                new XAttribute("titleBlockName", sheet.TitleBlockName ?? string.Empty),
                new XElement("placements",
                    sheet.Placements
                        .OrderBy(x => x.Ymm)
                        .ThenBy(x => x.Xmm)
                        .ThenBy(x => x.ViewId, StringComparer.OrdinalIgnoreCase)
                        .Select(x => new XElement("placement",
                            new XAttribute("viewId", x.ViewId ?? string.Empty),
                            new XAttribute("xMm", Number(x.Xmm)),
                            new XAttribute("yMm", Number(x.Ymm)),
                            new XAttribute("widthMm", Number(x.WidthMm)),
                            new XAttribute("heightMm", Number(x.HeightMm))))));
        }

        private static XElement ParseRoot(string payload)
        {
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = MaxCatalogChars
                };
                using (var text = new StringReader(payload))
                using (var reader = XmlReader.Create(text, settings))
                {
                    var document = XDocument.Load(reader, LoadOptions.None);
                    return document.Root ?? throw new InvalidDataException("Semantic documentation catalog has no root element.");
                }
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException("Semantic documentation catalog XML is invalid.", ex);
            }
        }

        private static IReadOnlyList<SemanticViewDefinition> ReadViews(XElement? container)
        {
            if (container == null) return Array.Empty<SemanticViewDefinition>();
            var result = new List<SemanticViewDefinition>();
            foreach (var item in container.Elements("view"))
            {
                var kindText = Required(item, "kind");
                if (!Enum.TryParse(kindText, true, out SemanticViewKind kind) || !Enum.IsDefined(typeof(SemanticViewKind), kind))
                    throw new InvalidDataException("Semantic documentation view kind is invalid: " + kindText + ".");

                var categories = new List<ElementCategory>();
                foreach (var categoryElement in item.Element("categories")?.Elements("category") ?? Enumerable.Empty<XElement>())
                {
                    var categoryText = Required(categoryElement, "value");
                    if (!Enum.TryParse(categoryText, true, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category))
                        throw new InvalidDataException("Semantic documentation view category is invalid: " + categoryText + ".");
                    categories.Add(category);
                }

                result.Add(new SemanticViewDefinition(
                    Required(item, "id"),
                    Required(item, "name"),
                    kind,
                    Optional(item, "floorId"),
                    Optional(item, "zoneId"),
                    categories,
                    ReadIds(item.Element("include")),
                    ReadIds(item.Element("exclude"))));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<SemanticSheetDefinition> ReadSheets(XElement? container)
        {
            if (container == null) return Array.Empty<SemanticSheetDefinition>();
            var result = new List<SemanticSheetDefinition>();
            foreach (var item in container.Elements("sheet"))
            {
                var placements = new List<SemanticSheetPlacementDefinition>();
                foreach (var placement in item.Element("placements")?.Elements("placement") ?? Enumerable.Empty<XElement>())
                {
                    placements.Add(new SemanticSheetPlacementDefinition(
                        Required(placement, "viewId"),
                        Double(Required(placement, "xMm"), "placement xMm"),
                        Double(Required(placement, "yMm"), "placement yMm"),
                        Double(Required(placement, "widthMm"), "placement widthMm"),
                        Double(Required(placement, "heightMm"), "placement heightMm")));
                }

                result.Add(new SemanticSheetDefinition(
                    Required(item, "id"),
                    Required(item, "number"),
                    Required(item, "name"),
                    Double(Required(item, "widthMm"), "sheet widthMm"),
                    Double(Required(item, "heightMm"), "sheet heightMm"),
                    placements,
                    Optional(item, "titleBlockName")));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> ReadIds(XElement? container)
        {
            if (container == null) return Array.Empty<string>();
            return container.Elements("id").Select(x => Required(x, "value")).ToArray();
        }

        private static string Required(XElement element, string attribute)
        {
            var value = element.Attribute(attribute)?.Value;
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Semantic documentation catalog is missing attribute: " + attribute + ".");
            return value.Trim();
        }

        private static string? Optional(XElement element, string attribute)
        {
            var value = element.Attribute(attribute)?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int Integer(string? value, string label)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                throw new InvalidDataException("Semantic documentation " + label + " is invalid.");
            return result;
        }

        private static double Double(string value, string label)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidDataException("Semantic documentation " + label + " is invalid.");
            return result;
        }

        private static string Number(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Semantic documentation numeric values must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
