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
        private const int MaxCatalogViews = 10000;
        private const int MaxCatalogSheets = 10000;

        public void Save(
            ProjectState project,
            IEnumerable<SemanticViewDefinition> views,
            IEnumerable<SemanticSheetDefinition> sheets)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (views == null) throw new ArgumentNullException(nameof(views));
            if (sheets == null) throw new ArgumentNullException(nameof(sheets));

            var projectSnapshot = CaptureProjectStructure(project);
            var viewDefinitions = MaterializeViews(views);
            EnsureProjectStructureUnchanged(project, projectSnapshot);
            var sheetDefinitions = MaterializeSheets(sheets);
            EnsureProjectStructureUnchanged(project, projectSnapshot);
            var viewPlans = SemanticViewPlanner.BuildCatalog(project, viewDefinitions);
            SemanticSheetPlanner.BuildCatalog(sheetDefinitions, viewPlans);
            EnsureProjectStructureUnchanged(project, projectSnapshot);

            if (viewDefinitions.Count == 0 && sheetDefinitions.Count == 0)
            {
                if (!project.Metadata.ContainsKey(MetadataKey)) return;
                EnsureProjectStructureUnchanged(project, projectSnapshot);
                project.Metadata.Remove(MetadataKey);
                return;
            }

            var payload = Serialize(viewDefinitions, sheetDefinitions);
            if (payload.Length > MaxCatalogChars)
                throw new InvalidOperationException("Semantic documentation catalog exceeds the 1 MiB metadata limit.");
            if (project.Metadata.TryGetValue(MetadataKey, out var current) && string.Equals(current, payload, StringComparison.Ordinal)) return;

            EnsureProjectStructureUnchanged(project, projectSnapshot);
            project.Metadata[MetadataKey] = payload;
        }

        private static ProjectStructureSnapshot CaptureProjectStructure(ProjectState project) =>
            new ProjectStructureSnapshot(
                project.ChangeVersion,
                project.Elements.ToArray(),
                project.Floors.ToArray(),
                project.Zones.ToArray());

        private static void EnsureProjectStructureUnchanged(ProjectState project, ProjectStructureSnapshot snapshot)
        {
            if (project.ChangeVersion != snapshot.ChangeVersion)
                throw new InvalidOperationException("Project changed while the semantic documentation catalog was being saved.");
            EnsureSameReferences(project.Elements, snapshot.Elements);
            EnsureSameElementPlanningValues(project.Elements, snapshot.ElementPlanningValues);
            EnsureSameReferences(project.Floors, snapshot.Floors);
            EnsureSameReferences(project.Zones, snapshot.Zones);
        }

        private static void EnsureSameReferences<T>(IList<T> current, IReadOnlyList<T> expected) where T : class
        {
            if (current.Count != expected.Count)
                throw new InvalidOperationException("Project structure changed while the semantic documentation catalog was being saved.");
            for (var i = 0; i < expected.Count; i++)
                if (!ReferenceEquals(current[i], expected[i]))
                    throw new InvalidOperationException("Project structure changed while the semantic documentation catalog was being saved.");
        }

        private static void EnsureSameElementPlanningValues(
            IList<ProjectElement> current,
            IReadOnlyList<ProjectElementPlanningValues> expected)
        {
            if (current.Count != expected.Count)
                throw new InvalidOperationException("Project structure changed while the semantic documentation catalog was being saved.");
            for (var i = 0; i < expected.Count; i++)
            {
                var element = current[i];
                var values = expected[i];
                if (element == null)
                {
                    if (!values.IsNull)
                        throw new InvalidOperationException("Project structure changed while the semantic documentation catalog was being saved.");
                    continue;
                }

                if (values.IsNull ||
                    !string.Equals(element.Id, values.Id, StringComparison.Ordinal) ||
                    element.Category != values.Category ||
                    !string.Equals(element.FloorId, values.FloorId, StringComparison.Ordinal) ||
                    !string.Equals(element.ZoneId, values.ZoneId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Project structure changed while the semantic documentation catalog was being saved.");
            }
        }

        private sealed class ProjectStructureSnapshot
        {
            public ProjectStructureSnapshot(
                long changeVersion,
                IReadOnlyList<ProjectElement> elements,
                IReadOnlyList<FloorDefinition> floors,
                IReadOnlyList<ZoneDefinition> zones)
            {
                ChangeVersion = changeVersion;
                Elements = elements;
                ElementPlanningValues = elements.Select(x => new ProjectElementPlanningValues(x)).ToArray();
                Floors = floors;
                Zones = zones;
            }

            public long ChangeVersion { get; }
            public IReadOnlyList<ProjectElement> Elements { get; }
            public IReadOnlyList<ProjectElementPlanningValues> ElementPlanningValues { get; }
            public IReadOnlyList<FloorDefinition> Floors { get; }
            public IReadOnlyList<ZoneDefinition> Zones { get; }
        }

        private sealed class ProjectElementPlanningValues
        {
            public ProjectElementPlanningValues(ProjectElement? element)
            {
                IsNull = element == null;
                Id = element?.Id;
                Category = element?.Category ?? default;
                FloorId = element?.FloorId;
                ZoneId = element?.ZoneId;
            }

            public bool IsNull { get; }
            public string? Id { get; }
            public ElementCategory Category { get; }
            public string? FloorId { get; }
            public string? ZoneId { get; }
        }

        public SemanticDocumentationCatalog Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(MetadataKey, out var payload))
                return new SemanticDocumentationCatalog(Array.Empty<SemanticViewDefinition>(), Array.Empty<SemanticSheetDefinition>());
            if (string.IsNullOrEmpty(payload))
                throw new InvalidDataException("Semantic documentation catalog payload is empty.");
            if (payload.Length > MaxCatalogChars)
                throw new InvalidDataException("Semantic documentation catalog exceeds the 1 MiB metadata limit.");

            var root = ParseRoot(payload);
            if (root.Name.NamespaceName.Length != 0 || !string.Equals(root.Name.LocalName, "documentation", StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation catalog root is invalid.");
            if (Integer(root.Attribute("version")?.Value, "documentation version") != FormatVersion)
                throw new InvalidDataException("Unsupported semantic documentation catalog version.");
            ValidateSchema(root);

            var views = ReadViews(root.Element("views"));
            var sheets = ReadSheets(root.Element("sheets"));
            var viewPlans = SemanticViewPlanner.BuildCatalog(project, views);
            SemanticSheetPlanner.BuildCatalog(sheets, viewPlans);
            return new SemanticDocumentationCatalog(views, sheets);
        }

        private static IReadOnlyList<SemanticViewDefinition> MaterializeViews(IEnumerable<SemanticViewDefinition> values)
        {
            var result = new List<SemanticViewDefinition>(Math.Min(MaxCatalogViews, 256));
            foreach (var value in values)
            {
                if (result.Count >= MaxCatalogViews)
                    throw new InvalidOperationException("Semantic view catalog supports at most " + MaxCatalogViews + " views.");
                if (value == null) throw new ArgumentException("Semantic documentation view cannot be null.", nameof(values));
                result.Add(value);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<SemanticSheetDefinition> MaterializeSheets(IEnumerable<SemanticSheetDefinition> values)
        {
            var result = new List<SemanticSheetDefinition>(Math.Min(MaxCatalogSheets, 256));
            foreach (var value in values)
            {
                if (result.Count >= MaxCatalogSheets)
                    throw new InvalidOperationException("Semantic sheet catalog supports at most " + MaxCatalogSheets + " sheets.");
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
                        .OrderBy(x => CanonicalRequiredText(x.Name), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => CanonicalRequiredText(x.Id), StringComparer.OrdinalIgnoreCase)
                        .Select(SerializeView)),
                new XElement("sheets",
                    sheets
                        .OrderBy(x => CanonicalRequiredText(x.Number), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => CanonicalRequiredText(x.Id), StringComparer.OrdinalIgnoreCase)
                        .Select(SerializeSheet)));
            return root.ToString(SaveOptions.DisableFormatting);
        }

        private static XElement SerializeView(SemanticViewDefinition view)
        {
            return new XElement("view",
                new XAttribute("id", CanonicalRequiredText(view.Id)),
                new XAttribute("name", CanonicalRequiredText(view.Name)),
                new XAttribute("kind", view.Kind),
                new XAttribute("floorId", CanonicalOptionalText(view.FloorId)),
                new XAttribute("zoneId", CanonicalOptionalText(view.ZoneId)),
                new XElement("categories",
                    view.Categories
                        .Distinct()
                        .OrderBy(x => x.ToString(), StringComparer.OrdinalIgnoreCase)
                        .Select(x => new XElement("category", new XAttribute("value", x)))),
                new XElement("include",
                    view.IncludeElementIds
                        .OrderBy(x => CanonicalRequiredText(x), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => CanonicalRequiredText(x), StringComparer.Ordinal)
                        .Select(x => new XElement("id", new XAttribute("value", CanonicalRequiredText(x))))),
                new XElement("exclude",
                    view.ExcludeElementIds
                        .OrderBy(x => CanonicalRequiredText(x), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(x => CanonicalRequiredText(x), StringComparer.Ordinal)
                        .Select(x => new XElement("id", new XAttribute("value", CanonicalRequiredText(x))))));
        }

        private static XElement SerializeSheet(SemanticSheetDefinition sheet)
        {
            return new XElement("sheet",
                new XAttribute("id", CanonicalRequiredText(sheet.Id)),
                new XAttribute("number", CanonicalRequiredText(sheet.Number)),
                new XAttribute("name", CanonicalRequiredText(sheet.Name)),
                new XAttribute("widthMm", Number(sheet.WidthMm)),
                new XAttribute("heightMm", Number(sheet.HeightMm)),
                new XAttribute("titleBlockName", CanonicalOptionalText(sheet.TitleBlockName)),
                new XElement("placements",
                    sheet.Placements
                        .OrderBy(x => x.Ymm)
                        .ThenBy(x => x.Xmm)
                        .ThenBy(x => CanonicalRequiredText(x.ViewId), StringComparer.OrdinalIgnoreCase)
                        .Select(x => new XElement("placement",
                            new XAttribute("viewId", CanonicalRequiredText(x.ViewId)),
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

        private static void ValidateSchema(XElement root)
        {
            ValidateElement(root, "documentation", new[] { "version" }, new[] { "views", "sheets" });
            RequireExactlyOneChild(root, "views");
            RequireExactlyOneChild(root, "sheets");

            var views = root.Element("views");
            if (views != null)
            {
                ValidateElement(views, "views", Array.Empty<string>(), new[] { "view" });
                foreach (var view in views.Elements("view"))
                {
                    ValidateElement(view, "view", new[] { "id", "name", "kind", "floorId", "zoneId" }, new[] { "categories", "include", "exclude" });
                    EnsureAtMostOneChild(view, "categories");
                    EnsureAtMostOneChild(view, "include");
                    EnsureAtMostOneChild(view, "exclude");

                    var categories = view.Element("categories");
                    if (categories != null)
                    {
                        ValidateElement(categories, "categories", Array.Empty<string>(), new[] { "category" });
                        foreach (var category in categories.Elements("category"))
                            ValidateElement(category, "category", new[] { "value" }, Array.Empty<string>());
                    }
                    ValidateIdContainer(view.Element("include"), "include");
                    ValidateIdContainer(view.Element("exclude"), "exclude");
                }
            }

            var sheets = root.Element("sheets");
            if (sheets != null)
            {
                ValidateElement(sheets, "sheets", Array.Empty<string>(), new[] { "sheet" });
                foreach (var sheet in sheets.Elements("sheet"))
                {
                    ValidateElement(sheet, "sheet", new[] { "id", "number", "name", "widthMm", "heightMm", "titleBlockName" }, new[] { "placements" });
                    EnsureAtMostOneChild(sheet, "placements");
                    var placements = sheet.Element("placements");
                    if (placements == null) continue;
                    ValidateElement(placements, "placements", Array.Empty<string>(), new[] { "placement" });
                    foreach (var placement in placements.Elements("placement"))
                        ValidateElement(placement, "placement", new[] { "viewId", "xMm", "yMm", "widthMm", "heightMm" }, Array.Empty<string>());
                }
            }
        }

        private static void ValidateIdContainer(XElement? container, string expectedName)
        {
            if (container == null) return;
            ValidateElement(container, expectedName, Array.Empty<string>(), new[] { "id" });
            foreach (var id in container.Elements("id"))
                ValidateElement(id, "id", new[] { "value" }, Array.Empty<string>());
        }

        private static void ValidateElement(XElement element, string expectedName, IReadOnlyCollection<string> allowedAttributes, IReadOnlyCollection<string> allowedChildren)
        {
            if (element.Name.NamespaceName.Length != 0 || !string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation catalog contains an unsupported XML element: " + element.Name + ".");

            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.NamespaceName.Length != 0 || !allowedAttributes.Contains(attribute.Name.LocalName))
                    throw new InvalidDataException("Semantic documentation catalog contains an unsupported attribute on " + expectedName + ": " + attribute.Name + ".");
            }

            foreach (var node in element.Nodes())
            {
                var child = node as XElement;
                if (child != null)
                {
                    if (child.Name.NamespaceName.Length != 0 || !allowedChildren.Contains(child.Name.LocalName))
                        throw new InvalidDataException("Semantic documentation catalog contains an unsupported child of " + expectedName + ": " + child.Name + ".");
                    continue;
                }

                if (node is XCData)
                    throw new InvalidDataException("Semantic documentation catalog contains unsupported CDATA content in " + expectedName + ".");
                var text = node as XText;
                if (text != null && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Semantic documentation catalog contains unsupported XML content in " + expectedName + ".");
            }
        }

        private static void RequireExactlyOneChild(XElement parent, string childName)
        {
            if (parent.Elements(childName).Take(2).Count() != 1)
                throw new InvalidDataException("Semantic documentation catalog requires exactly one " + childName + " container.");
        }

        private static void EnsureAtMostOneChild(XElement parent, string childName)
        {
            if (parent.Elements(childName).Skip(1).Any())
                throw new InvalidDataException("Semantic documentation catalog contains duplicate " + childName + " containers.");
        }

        private static IReadOnlyList<SemanticViewDefinition> ReadViews(XElement? container)
        {
            if (container == null) return Array.Empty<SemanticViewDefinition>();
            var result = new List<SemanticViewDefinition>(Math.Min(MaxCatalogViews, 256));
            foreach (var item in container.Elements("view"))
            {
                if (result.Count >= MaxCatalogViews)
                    throw new InvalidDataException("Semantic documentation catalog supports at most " + MaxCatalogViews + " views.");
                var kind = NamedEnum<SemanticViewKind>(Required(item, "kind"), "view kind");

                var categories = new List<ElementCategory>();
                foreach (var categoryElement in item.Element("categories")?.Elements("category") ?? Enumerable.Empty<XElement>())
                {
                    var category = NamedEnum<ElementCategory>(Required(categoryElement, "value"), "view category");
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
            var result = new List<SemanticSheetDefinition>(Math.Min(MaxCatalogSheets, 256));
            foreach (var item in container.Elements("sheet"))
            {
                if (result.Count >= MaxCatalogSheets)
                    throw new InvalidDataException("Semantic documentation catalog supports at most " + MaxCatalogSheets + " sheets.");
                var placements = new List<SemanticSheetPlacementDefinition>();
                foreach (var placement in item.Element("placements")?.Elements("placement") ?? Enumerable.Empty<XElement>())
                {
                    placements.Add(new SemanticSheetPlacementDefinition(
                        Required(placement, "viewId"),
                        RequiredDouble(placement, "xMm", "placement xMm"),
                        RequiredDouble(placement, "yMm", "placement yMm"),
                        RequiredDouble(placement, "widthMm", "placement widthMm"),
                        RequiredDouble(placement, "heightMm", "placement heightMm")));
                }

                result.Add(new SemanticSheetDefinition(
                    Required(item, "id"),
                    Required(item, "number"),
                    Required(item, "name"),
                    RequiredDouble(item, "widthMm", "sheet widthMm"),
                    RequiredDouble(item, "heightMm", "sheet heightMm"),
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

        private static TEnum NamedEnum<TEnum>(string token, string label) where TEnum : struct
        {
            if (!Enum.TryParse(token, true, out TEnum value) || !Enum.IsDefined(typeof(TEnum), value))
                throw new InvalidDataException("Semantic documentation " + label + " is invalid: " + token + ".");
            var name = Enum.GetName(typeof(TEnum), value);
            if (name == null || !string.Equals(token, name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Semantic documentation " + label + " must use a named enum token.");
            return value;
        }

        private static string Required(XElement element, string attribute)
        {
            var value = element.Attribute(attribute)?.Value;
            if (value == null || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Semantic documentation catalog is missing attribute: " + attribute + ".");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation catalog attribute must use canonical text: " + attribute + ".");
            return value;
        }

        private static string? Optional(XElement element, string attribute)
        {
            var value = element.Attribute(attribute)?.Value;
            if (value == null || value.Length == 0) return null;
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation catalog attribute must use canonical text: " + attribute + ".");
            return value;
        }

        private static int Integer(string? value, string label)
        {
            if (string.IsNullOrEmpty(value) ||
                !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) ||
                !string.Equals(value, result.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation " + label + " is invalid.");
            return result;
        }

        private static double RequiredDouble(XElement element, string attribute, string label)
        {
            var value = element.Attribute(attribute)?.Value;
            if (value == null || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Semantic documentation catalog is missing attribute: " + attribute + ".");
            return Double(value, label);
        }

        private static double Double(string value, string label)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ||
                double.IsNaN(result) ||
                double.IsInfinity(result) ||
                !string.Equals(value, Number(result), StringComparison.Ordinal))
                throw new InvalidDataException("Semantic documentation " + label + " is invalid.");
            return result;
        }

        private static string Number(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Semantic documentation numeric values must be finite.");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string CanonicalRequiredText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Semantic documentation required text must not be blank.");
            return RequireXmlText(value!.Trim());
        }

        private static string CanonicalOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : RequireXmlText(value!.Trim());
        }

        private static string RequireXmlText(string value)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
                return value;
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Semantic documentation text contains characters that are invalid in XML.", ex);
            }
        }
    }
}
