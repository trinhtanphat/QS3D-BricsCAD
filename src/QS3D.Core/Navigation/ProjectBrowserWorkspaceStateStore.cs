using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Navigation
{
    public sealed class ProjectBrowserWorkspaceState
    {
        public ProjectBrowserWorkspaceState(
            ProjectBrowserGrouping grouping = ProjectBrowserGrouping.FloorThenCategory,
            string? query = null,
            bool dirtyOnly = false,
            IEnumerable<ElementCategory>? categories = null,
            IEnumerable<string>? floorIds = null,
            IEnumerable<string>? zoneIds = null,
            IEnumerable<string>? expandedPaths = null,
            IEnumerable<string>? selectedElementIds = null,
            string? primaryElementId = null)
        {
            if (!Enum.IsDefined(typeof(ProjectBrowserGrouping), grouping)) throw new ArgumentOutOfRangeException(nameof(grouping));
            Grouping = grouping;
            Query = NormalizeQuery(query);
            DirtyOnly = dirtyOnly;
            Categories = NormalizeCategories(categories);
            FloorIds = NormalizeIds(floorIds, "project browser workspace floor filter", 10000);
            ZoneIds = NormalizeIds(zoneIds, "project browser workspace zone filter", 10000);
            ExpandedPaths = NormalizePaths(expandedPaths);
            SelectedElementIds = NormalizeIds(selectedElementIds, "project browser workspace selected element", 10000);
            PrimaryElementId = NormalizePrimary(primaryElementId, SelectedElementIds);
        }

        public ProjectBrowserGrouping Grouping { get; }
        public string Query { get; }
        public bool DirtyOnly { get; }
        public IReadOnlyList<ElementCategory> Categories { get; }
        public IReadOnlyList<string> FloorIds { get; }
        public IReadOnlyList<string> ZoneIds { get; }
        public IReadOnlyList<string> ExpandedPaths { get; }
        public IReadOnlyList<string> SelectedElementIds { get; }
        public string PrimaryElementId { get; }

        internal ProjectBrowserQueryOptions ToQueryOptions() =>
            new ProjectBrowserQueryOptions(Query, DirtyOnly, Categories, FloorIds, ZoneIds);

        private static string NormalizeQuery(string? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value)) return string.Empty;
            var normalized = value.Trim();
            if (normalized.Length > 160) throw new ArgumentException("Project browser workspace query exceeds 160 characters.", nameof(value));
            try
            {
                XmlConvert.VerifyXmlChars(normalized);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Project browser workspace query contains characters that cannot be persisted as XML.", nameof(value), ex);
            }
            return normalized;
        }

        private static IReadOnlyList<ElementCategory> NormalizeCategories(IEnumerable<ElementCategory>? values)
        {
            var result = new SortedSet<ElementCategory>();
            var count = 0;
            foreach (var value in values ?? Enumerable.Empty<ElementCategory>())
            {
                if (count >= ProjectBrowserQueryPlanner.MaxFilterIds)
                    throw new InvalidOperationException(
                        "Project browser workspace category filter exceeds " + ProjectBrowserQueryPlanner.MaxFilterIds + " entries.");
                count++;
                if (!Enum.IsDefined(typeof(ElementCategory), value))
                    throw new ArgumentOutOfRangeException(nameof(values), "Project browser workspace contains an undefined category.");
                result.Add(value);
            }
            return result.ToList().AsReadOnly();
        }

        private static IReadOnlyList<string> NormalizeIds(IEnumerable<string>? values, string label, int maxCount)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in values ?? Enumerable.Empty<string>())
            {
                if (result.Count >= maxCount) throw new InvalidOperationException(label + " list exceeds " + maxCount + " entries.");
                var value = RequiredCanonical(raw, label);
                if (!seen.Add(value)) throw new InvalidOperationException("Duplicate " + label + ": " + value + ".");
                result.Add(value);
            }
            result.Sort(CompareCanonical);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> NormalizePaths(IEnumerable<string>? values)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var raw in values ?? Enumerable.Empty<string>())
            {
                if (result.Count >= 50000) throw new InvalidOperationException("Project browser workspace expanded path list exceeds 50000 entries.");
                var value = RequiredCanonical(raw, "project browser workspace expanded path");
                if (!seen.Add(value)) throw new InvalidOperationException("Duplicate project browser workspace expanded path: " + value + ".");
                result.Add(value);
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static string NormalizePrimary(string? value, IReadOnlyList<string> selected)
        {
            if (value == null || string.IsNullOrWhiteSpace(value)) return selected.Count == 0 ? string.Empty : selected[0];
            var primary = RequiredCanonical(value, "project browser workspace primary element id");
            var match = selected.FirstOrDefault(x => string.Equals(x, primary, StringComparison.OrdinalIgnoreCase));
            if (match == null) throw new InvalidOperationException("Project browser workspace primary element must belong to the selected element set: " + primary + ".");
            return match;
        }

        private static string RequiredCanonical(string value, string label)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException(label + " is required.");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException(label + " must not contain surrounding whitespace: " + raw + ".");
            return raw;
        }

        private static int CompareCanonical(string left, string right)
        {
            var insensitive = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            return insensitive != 0 ? insensitive : StringComparer.Ordinal.Compare(left, right);
        }
    }

    public sealed class ProjectBrowserWorkspaceStateStore
    {
        public const string MetadataKey = "QS3D.ProjectBrowser.WorkspaceState";
        public const string FormatName = "QS3D.ProjectBrowserWorkspaceState";
        public const int FormatVersion = 1;
        private const int MaxSerializedChars = 262144;

        public ProjectBrowserWorkspaceState Load(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue(MetadataKey, out var serialized))
                return new ProjectBrowserWorkspaceState();

            var state = Deserialize(serialized);
            ValidateAgainstProject(project, state);
            return state;
        }

        public bool Save(ProjectState project, ProjectBrowserWorkspaceState state)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (state == null) throw new ArgumentNullException(nameof(state));
            ValidateAgainstProject(project, state);
            var serialized = Serialize(state);
            if (serialized.Length > MaxSerializedChars)
                throw new InvalidOperationException("Project browser workspace state exceeds the maximum persisted size.");
            if (project.Metadata.TryGetValue(MetadataKey, out var existing) && string.Equals(existing, serialized, StringComparison.Ordinal))
                return false;
            project.Metadata[MetadataKey] = serialized;
            return true;
        }

        public bool Clear(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return project.Metadata.Remove(MetadataKey);
        }

        public string Serialize(ProjectBrowserWorkspaceState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var root = new XElement("ProjectBrowserWorkspaceState",
                new XAttribute("format", FormatName),
                new XAttribute("version", FormatVersion),
                new XAttribute("grouping", state.Grouping.ToString()),
                new XAttribute("dirtyOnly", state.DirtyOnly ? "true" : "false"),
                new XAttribute("query", state.Query),
                new XAttribute("primaryElementId", state.PrimaryElementId),
                Collection("Categories", "Category", state.Categories.Select(x => x.ToString())),
                Collection("FloorIds", "Id", state.FloorIds),
                Collection("ZoneIds", "Id", state.ZoneIds),
                Collection("ExpandedPaths", "Path", state.ExpandedPaths),
                Collection("SelectedElementIds", "Id", state.SelectedElementIds));
            return new XDocument(root).ToString(SaveOptions.DisableFormatting);
        }

        public ProjectBrowserWorkspaceState Deserialize(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized)) throw new InvalidDataException("Project browser workspace state is empty.");
            if (serialized.Length > MaxSerializedChars) throw new InvalidDataException("Project browser workspace state exceeds the maximum persisted size.");

            XDocument document;
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = MaxSerializedChars
                };
                using (var reader = XmlReader.Create(new StringReader(serialized), settings)) document = XDocument.Load(reader, LoadOptions.None);
            }
            catch (Exception ex) when (ex is XmlException || ex is InvalidOperationException)
            {
                throw new InvalidDataException("Project browser workspace state XML is invalid.", ex);
            }

            var root = document.Root;
            if (root == null || root.Name != "ProjectBrowserWorkspaceState") throw new InvalidDataException("Project browser workspace state root is invalid.");
            ValidateDocumentShape(document, root);
            ValidateRootShape(root);
            RequireAttribute(root, "format", FormatName);
            RequireAttribute(root, "version", FormatVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var groupingRaw = (string)root.Attribute("grouping");
            if (!Enum.TryParse(groupingRaw, false, out ProjectBrowserGrouping grouping) ||
                !Enum.IsDefined(typeof(ProjectBrowserGrouping), grouping) ||
                !string.Equals(groupingRaw, grouping.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Project browser workspace grouping is invalid.");
            var dirtyRaw = (string)root.Attribute("dirtyOnly");
            if (!bool.TryParse(dirtyRaw, out var dirtyOnly) ||
                !string.Equals(dirtyRaw, dirtyOnly ? "true" : "false", StringComparison.Ordinal))
                throw new InvalidDataException("Project browser workspace dirtyOnly is invalid.");

            var expectedChildren = new HashSet<XName>(new[]
            {
                XName.Get("Categories"), XName.Get("FloorIds"), XName.Get("ZoneIds"),
                XName.Get("ExpandedPaths"), XName.Get("SelectedElementIds")
            });
            foreach (var child in root.Elements())
                if (!expectedChildren.Contains(child.Name))
                    throw new InvalidDataException("Project browser workspace contains an unsupported element: " + child.Name + ".");
            foreach (var name in expectedChildren)
                if (root.Elements(name).Count() != 1)
                    throw new InvalidDataException("Project browser workspace requires exactly one " + name.LocalName + " element.");
            var expectedChildOrder = new[]
            {
                XName.Get("Categories"), XName.Get("FloorIds"), XName.Get("ZoneIds"),
                XName.Get("ExpandedPaths"), XName.Get("SelectedElementIds")
            };
            if (!root.Elements().Select(x => x.Name).SequenceEqual(expectedChildOrder))
                throw new InvalidDataException("Project browser workspace collection containers are not in canonical order.");

            var categories = ReadCategories(root.Element("Categories"));
            var floorIds = ReadValues(root.Element("FloorIds"), "Id");
            var zoneIds = ReadValues(root.Element("ZoneIds"), "Id");
            var expanded = ReadValues(root.Element("ExpandedPaths"), "Path");
            var selected = ReadValues(root.Element("SelectedElementIds"), "Id");
            var queryRaw = (string)root.Attribute("query");
            var primaryRaw = (string)root.Attribute("primaryElementId");

            try
            {
                var state = new ProjectBrowserWorkspaceState(
                    grouping,
                    queryRaw,
                    dirtyOnly,
                    categories,
                    floorIds,
                    zoneIds,
                    expanded,
                    selected,
                    primaryRaw);
                if (!string.Equals(queryRaw, state.Query, StringComparison.Ordinal))
                    throw new InvalidDataException("Project browser workspace query is non-canonical.");
                if (!string.Equals(primaryRaw, state.PrimaryElementId, StringComparison.Ordinal))
                    throw new InvalidDataException("Project browser workspace primary element id is non-canonical.");
                if (!categories.SequenceEqual(state.Categories) ||
                    !floorIds.SequenceEqual(state.FloorIds, StringComparer.Ordinal) ||
                    !zoneIds.SequenceEqual(state.ZoneIds, StringComparer.Ordinal) ||
                    !expanded.SequenceEqual(state.ExpandedPaths, StringComparer.Ordinal) ||
                    !selected.SequenceEqual(state.SelectedElementIds, StringComparer.Ordinal))
                    throw new InvalidDataException("Project browser workspace collections are non-canonical.");
                return state;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                throw new InvalidDataException("Project browser workspace state violates canonical constraints.", ex);
            }
        }

        public void ValidateAgainstProject(ProjectState project, ProjectBrowserWorkspaceState state)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (state == null) throw new ArgumentNullException(nameof(state));
            var query = ProjectBrowserQueryPlanner.Build(project, state.Grouping, state.ToQueryOptions());
            ProjectBrowserVirtualizationPlanner.BuildViewport(query.Root, state.ExpandedPaths, 0, 1);
            ProjectBrowserSelectionPlanner.PlanReveal(query.Root, state.SelectedElementIds, state.PrimaryElementId);
        }

        private static XElement Collection(string containerName, string itemName, IEnumerable<string> values) =>
            new XElement(containerName, (values ?? Enumerable.Empty<string>()).Select(x => new XElement(itemName, x)));

        private static IReadOnlyList<ElementCategory> ReadCategories(XElement? container)
        {
            if (container == null) throw new InvalidDataException("Project browser workspace Categories element is missing.");
            ValidateCollectionShape(container, "Category");
            var result = new List<ElementCategory>();
            foreach (var element in container.Elements("Category"))
            {
                ValidateItemShape(element, "Category");
                if (!Enum.TryParse(element.Value, false, out ElementCategory value) ||
                    !Enum.IsDefined(typeof(ElementCategory), value) ||
                    !string.Equals(element.Value, value.ToString(), StringComparison.Ordinal))
                    throw new InvalidDataException("Project browser workspace category is invalid: " + element.Value + ".");
                result.Add(value);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> ReadValues(XElement? container, string itemName)
        {
            if (container == null) throw new InvalidDataException("Project browser workspace collection element is missing.");
            ValidateCollectionShape(container, itemName);
            var result = new List<string>();
            foreach (var element in container.Elements(itemName))
            {
                ValidateItemShape(element, itemName);
                result.Add(element.Value);
            }
            return result.AsReadOnly();
        }

        private static void ValidateDocumentShape(XDocument document, XElement root)
        {
            foreach (var node in document.Nodes())
            {
                if (ReferenceEquals(node, root)) continue;
                throw new InvalidDataException("Project browser workspace document contains unsupported node content.");
            }
        }

        private static void ValidateRootShape(XElement root)
        {
            var expectedAttributes = new HashSet<XName>(new[]
            {
                XName.Get("format"),
                XName.Get("version"),
                XName.Get("grouping"),
                XName.Get("dirtyOnly"),
                XName.Get("query"),
                XName.Get("primaryElementId")
            });
            foreach (var attribute in root.Attributes())
                if (!expectedAttributes.Contains(attribute.Name))
                    throw new InvalidDataException("Project browser workspace root contains unsupported attribute: " + attribute.Name + ".");
            foreach (var name in expectedAttributes)
                if (root.Attribute(name) == null)
                    throw new InvalidDataException("Project browser workspace root is missing required attribute: " + name + ".");
            ValidateContainerNodes(root, "root");
        }

        private static void ValidateCollectionShape(XElement container, string itemName)
        {
            if (container.HasAttributes)
                throw new InvalidDataException("Project browser workspace collection contains unsupported attributes: " + container.Name + ".");
            foreach (var child in container.Elements())
                if (child.Name != itemName)
                    throw new InvalidDataException("Project browser workspace collection contains unsupported element: " + child.Name + ".");
            ValidateContainerNodes(container, container.Name.LocalName);
        }

        private static void ValidateItemShape(XElement element, string itemName)
        {
            if (element.HasAttributes)
                throw new InvalidDataException("Project browser workspace " + itemName + " item contains unsupported attributes.");
            foreach (var node in element.Nodes())
            {
                if (node is XCData)
                    throw new InvalidDataException("Project browser workspace " + itemName + " item must not contain CDATA.");
                if (!(node is XText))
                    throw new InvalidDataException("Project browser workspace " + itemName + " item must contain text only.");
            }
        }

        private static void ValidateContainerNodes(XElement element, string label)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XElement) continue;
                if (node is XCData)
                    throw new InvalidDataException("Project browser workspace " + label + " must not contain CDATA.");
                var text = node as XText;
                if (text != null && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Project browser workspace " + label + " contains unsupported node content.");
            }
        }

        private static void RequireAttribute(XElement element, string name, string expected)
        {
            var actual = (string)element.Attribute(name);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException("Project browser workspace " + name + " is unsupported: " + (actual ?? string.Empty) + ".");
        }
    }
}