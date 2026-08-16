using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QS3D.Core.Export
{
    public static class BcfZipPackage
    {
        public const int MaxArchiveBytes = 16 * 1024 * 1024;
        public const int MaxEntryBytes = 2 * 1024 * 1024;
        public const int MaxEntries = 2048;
        public const int MaxTotalUncompressedBytes = 32 * 1024 * 1024;

        private const string VersionFileName = "bcf.version";
        private const string ExtensionsFileName = "extensions.xml";
        private const string MarkupFileName = "markup.bcf";
        private const string OriginatingSystem = "QS3D";
        private static readonly DateTimeOffset DeterministicEntryTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static byte[] Write(BcfIssueExchange exchange)
        {
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            var entryCount = 2;
            foreach (var topic in exchange.Topics)
            {
                entryCount += 1 + topic.Viewpoints.Count;
                if (entryCount > MaxEntries) throw new InvalidDataException("BCF package entry count exceeds the bounded contract.");
            }

            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                WriteTextEntry(archive, VersionFileName, BuildVersion());
                WriteTextEntry(archive, ExtensionsFileName, BuildExtensions(exchange));
                foreach (var topic in exchange.Topics)
                {
                    WriteTextEntry(archive, topic.Id + "/" + MarkupFileName, BuildMarkup(topic));
                    foreach (var viewpoint in topic.Viewpoints)
                        WriteTextEntry(archive, topic.Id + "/" + viewpoint.Id + ".bcfv", BuildViewpoint(viewpoint));
                }
            }
            var bytes = stream.ToArray();
            if (bytes.Length > MaxArchiveBytes) throw new InvalidDataException("BCF package exceeds the bounded archive size.");
            return bytes;
        }

        public static BcfIssueExchange Read(byte[] package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (package.Length == 0 || package.Length > MaxArchiveBytes) throw new InvalidDataException("BCF package size is invalid.");
            try
            {
                using var stream = new MemoryStream(package, false);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
                var entries = ValidateEntries(archive);
                ValidateVersion(ReadRequired(entries, VersionFileName));
                var vocabularies = ReadExtensions(ReadRequired(entries, ExtensionsFileName));

                var topicFolders = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var path in entries.Keys)
                {
                    if (path == VersionFileName || path == ExtensionsFileName) continue;
                    var parts = path.Split('/');
                    if (parts.Length != 2) throw new InvalidDataException("Unsupported BCF package entry path: " + path);
                    var folder = BcfIssueExchangeContract.RequireBcfGuid(parts[0], "topicFolder");
                    if (!string.Equals(folder, parts[0], StringComparison.Ordinal)) throw new InvalidDataException("BCF topic folder is not canonical.");
                    if (!string.Equals(parts[1], MarkupFileName, StringComparison.Ordinal) && !parts[1].EndsWith(".bcfv", StringComparison.Ordinal))
                        throw new InvalidDataException("Unsupported BCF topic entry: " + path);
                    topicFolders.Add(folder);
                }
                if (topicFolders.Count == 0) throw new InvalidDataException("BCF package contains no topics.");
                if (topicFolders.Count > BcfIssueExchangeContract.MaxTopics) throw new InvalidDataException("BCF topic count exceeds the bounded package contract.");

                var topics = new List<BcfTopic>();
                foreach (var folder in topicFolders)
                {
                    var markup = ReadRequired(entries, folder + "/" + MarkupFileName);
                    var data = ReadMarkup(markup, folder);
                    if (!vocabularies.TopicTypes.Contains(data.Type)) throw new InvalidDataException("BCF topic type is not declared in extensions.xml: " + data.Type);
                    if (!vocabularies.TopicStatuses.Contains(data.Status)) throw new InvalidDataException("BCF topic status is not declared in extensions.xml: " + data.Status);

                    var viewpoints = new List<BcfViewpoint>();
                    foreach (var reference in data.Viewpoints)
                    {
                        var expectedFileName = reference.Id + ".bcfv";
                        if (!string.Equals(reference.FileName, expectedFileName, StringComparison.Ordinal)) throw new InvalidDataException("BCF viewpoint filename must match its GUID.");
                        viewpoints.Add(ReadViewpoint(ReadRequired(entries, folder + "/" + expectedFileName), reference.Id));
                    }

                    var referencedFiles = new HashSet<string>(data.Viewpoints.Select(x => x.Id + ".bcfv"), StringComparer.Ordinal);
                    foreach (var path in entries.Keys)
                    {
                        if (!path.StartsWith(folder + "/", StringComparison.Ordinal) || !path.EndsWith(".bcfv", StringComparison.Ordinal)) continue;
                        var fileName = path.Substring(folder.Length + 1);
                        if (!referencedFiles.Contains(fileName)) throw new InvalidDataException("BCF package contains an unreferenced viewpoint file: " + path);
                    }
                    topics.Add(new BcfTopic(data.Id, data.Title, data.Status, data.Type, data.Description, data.CreationAuthor, data.CreationDateUtc, data.Comments, viewpoints));
                }
                return BcfIssueExchange.Create(topics);
            }
            catch (InvalidDataException) { throw; }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is XmlException || exception is IOException || exception is NotSupportedException)
            {
                throw new InvalidDataException("BCF package failed validation.", exception);
            }
        }

        private static Dictionary<string, ZipArchiveEntry> ValidateEntries(ZipArchive archive)
        {
            if (archive.Entries.Count == 0 || archive.Entries.Count > MaxEntries) throw new InvalidDataException("BCF package entry count is invalid.");
            var result = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                ValidateSafeEntryPath(entry.FullName);
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) throw new InvalidDataException("Directory-only BCF entries are not supported.");
                if (result.ContainsKey(entry.FullName)) throw new InvalidDataException("Duplicate BCF package entry: " + entry.FullName);
                result.Add(entry.FullName, entry);
                if (entry.Length < 0 || entry.Length > MaxEntryBytes) throw new InvalidDataException("BCF package entry exceeds the bounded size: " + entry.FullName);
                total += entry.Length;
                if (total > MaxTotalUncompressedBytes) throw new InvalidDataException("BCF package uncompressed size exceeds the bounded contract.");
            }
            return result;
        }

        private static void ValidateSafeEntryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("/", StringComparison.Ordinal) || path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0)
                throw new InvalidDataException("Unsafe BCF package entry path.");
            var parts = path.Split('/');
            for (var index = 0; index < parts.Length; index++)
            {
                if (parts[index].Length == 0 || parts[index] == "." || parts[index] == "..") throw new InvalidDataException("Unsafe BCF package entry path.");
            }
        }

        private static ZipArchiveEntry ReadRequired(Dictionary<string, ZipArchiveEntry> entries, string path)
        {
            if (!entries.TryGetValue(path, out var entry)) throw new InvalidDataException("Missing required BCF package entry: " + path);
            return entry;
        }

        private static string ReadText(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true);
            return reader.ReadToEnd();
        }

        private static void WriteTextEntry(ZipArchive archive, string path, string text)
        {
            ValidateSafeEntryPath(path);
            var bytes = new UTF8Encoding(false, true).GetBytes(text);
            if (bytes.Length > MaxEntryBytes) throw new InvalidDataException("BCF package entry exceeds the bounded size: " + path);
            var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
            entry.LastWriteTime = DeterministicEntryTime;
            using var stream = entry.Open();
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string BuildVersion()
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            return Xml(new XElement("Version", new XAttribute("VersionId", BcfIssueExchange.SchemaVersion), new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName), new XAttribute(xsi + "noNamespaceSchemaLocation", "https://raw.githubusercontent.com/buildingSMART/BCF-XML/release_3_0/Schemas/version.xsd")));
        }

        private static void ValidateVersion(ZipArchiveEntry entry)
        {
            var root = ParseRoot(ReadText(entry), "Version");
            EnsureAllowedAttributes(root, "VersionId");
            EnsureNoChildElements(root);
            var version = RequiredAttribute(root, "VersionId");
            if (!string.Equals(version, BcfIssueExchange.SchemaVersion, StringComparison.Ordinal)) throw new InvalidDataException("Unsupported BCF package version: " + version);
        }

        private static string BuildExtensions(BcfIssueExchange exchange)
        {
            var topicTypes = exchange.Topics.Select(x => x.Type).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
            var statuses = exchange.Topics.Select(x => x.Status).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
            return Xml(new XElement("Extensions", new XElement("TopicTypes", topicTypes.Select(x => new XElement("TopicType", x))), new XElement("TopicStatuses", statuses.Select(x => new XElement("TopicStatus", x)))));
        }

        private static ExtensionVocabularies ReadExtensions(ZipArchiveEntry entry)
        {
            var root = ParseRoot(ReadText(entry), "Extensions");
            EnsureAllowedAttributes(root);
            EnsureAllowedChildren(root, "TopicTypes", "TopicStatuses");
            var types = ReadUniqueTokens(RequiredSingle(root, "TopicTypes"), "TopicType");
            var statuses = ReadUniqueTokens(RequiredSingle(root, "TopicStatuses"), "TopicStatus");
            if (types.Count == 0 || statuses.Count == 0) throw new InvalidDataException("BCF extensions.xml must declare topic types and statuses for this subset.");
            return new ExtensionVocabularies(types, statuses);
        }

        private static HashSet<string> ReadUniqueTokens(XElement container, string itemName)
        {
            EnsureAllowedAttributes(container);
            EnsureAllowedChildren(container, itemName);
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in container.Elements())
            {
                if (!string.Equals(child.Name.LocalName, itemName, StringComparison.Ordinal) || child.Name.NamespaceName.Length != 0) throw new InvalidDataException("Unsupported BCF extensions.xml element: " + child.Name.LocalName);
                var token = IfcRoundTripProjectionContract.RequireCanonicalToken(ReadLeafValue(child), itemName);
                if (!result.Add(token)) throw new InvalidDataException("Duplicate BCF extension token: " + token);
            }
            return result;
        }

        private static string BuildMarkup(BcfTopic topic)
        {
            var topicElement = new XElement("Topic", new XAttribute("Guid", topic.Id), new XAttribute("TopicType", topic.Type), new XAttribute("TopicStatus", topic.Status), new XElement("Title", topic.Title), new XElement("CreationDate", Date(topic.CreationDateUtc)), new XElement("CreationAuthor", topic.CreationAuthor));
            if (topic.Description.Length > 0) topicElement.Add(new XElement("Description", topic.Description));
            if (topic.Comments.Count > 0)
            {
                topicElement.Add(new XElement("Comments", topic.Comments.Select(comment =>
                {
                    var element = new XElement("Comment", new XAttribute("Guid", comment.Id), new XElement("Date", Date(comment.CreatedUtc)), new XElement("Author", comment.Author), new XElement("Comment", comment.Text));
                    if (comment.ViewpointId != null) element.Add(new XElement("Viewpoint", new XAttribute("Guid", comment.ViewpointId)));
                    return element;
                })));
            }
            if (topic.Viewpoints.Count > 0)
            {
                topicElement.Add(new XElement("Viewpoints", topic.Viewpoints.Select(viewpoint => new XElement("ViewPoint", new XAttribute("Guid", viewpoint.Id), new XElement("Viewpoint", viewpoint.Id + ".bcfv")))));
            }
            return Xml(new XElement("Markup", topicElement));
        }

        private static MarkupTopicData ReadMarkup(ZipArchiveEntry entry, string folder)
        {
            var root = ParseRoot(ReadText(entry), "Markup");
            EnsureAllowedAttributes(root);
            EnsureAllowedChildren(root, "Topic");
            var topic = RequiredSingle(root, "Topic");
            EnsureAllowedAttributes(topic, "Guid", "TopicType", "TopicStatus");
            EnsureAllowedChildren(topic, "Title", "CreationDate", "CreationAuthor", "Description", "Comments", "Viewpoints");
            var id = BcfIssueExchangeContract.RequireBcfGuid(RequiredAttribute(topic, "Guid"), "Guid");
            if (!string.Equals(id, folder, StringComparison.Ordinal)) throw new InvalidDataException("BCF topic folder and markup GUID do not match.");
            var comments = ReadComments(OptionalSingle(topic, "Comments"));
            var viewpoints = ReadViewpointReferences(OptionalSingle(topic, "Viewpoints"));
            return new MarkupTopicData(id, RequiredSingleValue(topic, "Title"), RequiredAttribute(topic, "TopicStatus"), RequiredAttribute(topic, "TopicType"), OptionalSingleValue(topic, "Description") ?? string.Empty, RequiredSingleValue(topic, "CreationAuthor"), ParseUtc(RequiredSingleValue(topic, "CreationDate")), comments, viewpoints);
        }

        private static List<BcfComment> ReadComments(XElement? container)
        {
            var comments = new List<BcfComment>();
            if (container == null) return comments;
            EnsureAllowedAttributes(container);
            EnsureAllowedChildren(container, "Comment");
            foreach (var element in container.Elements("Comment"))
            {
                EnsureAllowedAttributes(element, "Guid");
                EnsureAllowedChildren(element, "Date", "Author", "Comment", "Viewpoint");
                var viewpointElement = OptionalSingle(element, "Viewpoint");
                string? viewpointId = null;
                if (viewpointElement != null)
                {
                    EnsureAllowedAttributes(viewpointElement, "Guid");
                    EnsureNoChildElements(viewpointElement);
                    viewpointId = RequiredAttribute(viewpointElement, "Guid");
                }
                comments.Add(new BcfComment(RequiredAttribute(element, "Guid"), RequiredSingleValue(element, "Author"), ParseUtc(RequiredSingleValue(element, "Date")), RequiredSingleValue(element, "Comment"), viewpointId));
            }
            return comments;
        }

        private static List<ViewpointReference> ReadViewpointReferences(XElement? container)
        {
            var result = new List<ViewpointReference>();
            if (container == null) return result;
            EnsureAllowedAttributes(container);
            EnsureAllowedChildren(container, "ViewPoint");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in container.Elements("ViewPoint"))
            {
                EnsureAllowedAttributes(element, "Guid");
                EnsureAllowedChildren(element, "Viewpoint");
                var id = BcfIssueExchangeContract.RequireBcfGuid(RequiredAttribute(element, "Guid"), "Guid");
                if (!ids.Add(id)) throw new InvalidDataException("Duplicate BCF viewpoint reference: " + id);
                result.Add(new ViewpointReference(id, RequiredSingleValue(element, "Viewpoint")));
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return result;
        }

        private static string BuildViewpoint(BcfViewpoint viewpoint)
        {
            var selection = new XElement("Selection", viewpoint.Components.Select(component => new XElement("Component", new XAttribute("IfcGuid", component.IfcGlobalId), new XElement("OriginatingSystem", OriginatingSystem), new XElement("AuthoringToolId", component.Qs3dElementId))));
            var camera = viewpoint.Camera;
            return Xml(new XElement("VisualizationInfo", new XAttribute("Guid", viewpoint.Id), new XElement("Components", selection), new XElement("OrthogonalCamera", Point("CameraViewPoint", camera.ViewPoint), Point("CameraDirection", camera.Direction), Point("CameraUpVector", camera.UpVector), new XElement("ViewToWorldScale", Number(camera.ViewToWorldScale)), new XElement("AspectRatio", Number(camera.AspectRatio)))));
        }

        private static BcfViewpoint ReadViewpoint(ZipArchiveEntry entry, string expectedId)
        {
            var root = ParseRoot(ReadText(entry), "VisualizationInfo");
            EnsureAllowedAttributes(root, "Guid");
            EnsureAllowedChildren(root, "Components", "OrthogonalCamera");
            var id = BcfIssueExchangeContract.RequireBcfGuid(RequiredAttribute(root, "Guid"), "Guid");
            if (!string.Equals(id, expectedId, StringComparison.Ordinal)) throw new InvalidDataException("BCF viewpoint GUID does not match markup reference.");
            var componentsElement = RequiredSingle(root, "Components");
            EnsureAllowedAttributes(componentsElement);
            EnsureAllowedChildren(componentsElement, "Selection");
            var selection = RequiredSingle(componentsElement, "Selection");
            EnsureAllowedAttributes(selection);
            EnsureAllowedChildren(selection, "Component");
            var components = new List<BcfComponentReference>();
            foreach (var component in selection.Elements("Component"))
            {
                EnsureAllowedAttributes(component, "IfcGuid");
                EnsureAllowedChildren(component, "OriginatingSystem", "AuthoringToolId");
                var origin = RequiredSingleValue(component, "OriginatingSystem");
                if (!string.Equals(origin, OriginatingSystem, StringComparison.Ordinal)) throw new InvalidDataException("Unsupported BCF component originating system: " + origin);
                components.Add(new BcfComponentReference(RequiredSingleValue(component, "AuthoringToolId"), RequiredAttribute(component, "IfcGuid")));
            }
            var cameraElement = RequiredSingle(root, "OrthogonalCamera");
            EnsureAllowedAttributes(cameraElement);
            EnsureAllowedChildren(cameraElement, "CameraViewPoint", "CameraDirection", "CameraUpVector", "ViewToWorldScale", "AspectRatio");
            var camera = new BcfOrthogonalCamera(ReadPoint(cameraElement, "CameraViewPoint"), ReadPoint(cameraElement, "CameraDirection"), ReadPoint(cameraElement, "CameraUpVector"), ReadNumber(cameraElement, "ViewToWorldScale"), ReadNumber(cameraElement, "AspectRatio"));
            return new BcfViewpoint(id, camera, components);
        }

        private static XElement Point(string name, BcfPoint3 point) => new XElement(name, new XElement("X", Number(point.X)), new XElement("Y", Number(point.Y)), new XElement("Z", Number(point.Z)));

        private static BcfPoint3 ReadPoint(XElement parent, string name)
        {
            var element = RequiredSingle(parent, name);
            EnsureAllowedAttributes(element);
            EnsureAllowedChildren(element, "X", "Y", "Z");
            return new BcfPoint3(ReadNumber(element, "X"), ReadNumber(element, "Y"), ReadNumber(element, "Z"));
        }

        private static double ReadNumber(XElement parent, string name)
        {
            var value = RequiredSingleValue(parent, name);
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result)) throw new InvalidDataException("BCF numeric value is invalid: " + name);
            return result;
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Date(DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);
        private static DateTime ParseUtc(string value)
        {
            if (!DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ||
                parsed.Kind != DateTimeKind.Utc ||
                !string.Equals(value, parsed.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new InvalidDataException("BCF timestamp must use canonical UTC round-trip format.");
            return parsed;
        }
        private static string Xml(XElement root) => new XDocument(new XDeclaration("1.0", "UTF-8", null), root).ToString(SaveOptions.DisableFormatting);

        private static XElement ParseRoot(string text, string expectedName)
        {
            var document = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
            EnsureDocumentContent(document);
            var root = document.Root;
            if (root == null || root.Name.NamespaceName.Length != 0 || !string.Equals(root.Name.LocalName, expectedName, StringComparison.Ordinal)) throw new InvalidDataException("Invalid BCF XML root; expected " + expectedName + ".");
            return root;
        }

        private static void EnsureDocumentContent(XDocument document)
        {
            foreach (var node in document.Nodes())
            {
                if (node is XElement) continue;
                if (node is XText text && !(node is XCData) && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Unsupported BCF XML document content.");
            }
        }

        private static void EnsureAllowedAttributes(XElement element, params string[] names)
        {
            var allowed = new HashSet<string>(names, StringComparer.Ordinal);
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration) continue;
                if (attribute.Name.NamespaceName == "http://www.w3.org/2001/XMLSchema-instance")
                {
                    if (string.Equals(attribute.Name.LocalName, "noNamespaceSchemaLocation", StringComparison.Ordinal)) continue;
                    throw new InvalidDataException("Unsupported BCF XML schema-instance attribute: " + attribute.Name.LocalName);
                }
                if (attribute.Name.NamespaceName.Length != 0 || !allowed.Contains(attribute.Name.LocalName)) throw new InvalidDataException("Unsupported BCF XML attribute: " + attribute.Name.LocalName);
            }
        }

        private static void EnsureAllowedChildren(XElement element, params string[] names)
        {
            EnsureElementOnlyContent(element);
            var allowed = new HashSet<string>(names, StringComparer.Ordinal);
            foreach (var child in element.Elements())
            {
                if (child.Name.NamespaceName.Length != 0 || !allowed.Contains(child.Name.LocalName)) throw new InvalidDataException("Unsupported BCF XML element: " + child.Name.LocalName);
            }
        }

        private static void EnsureElementOnlyContent(XElement element)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XElement) continue;
                if (node is XText text && !(node is XCData) && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Unexpected BCF XML container content: " + element.Name.LocalName);
            }
        }

        private static void EnsureNoChildElements(XElement element)
        {
            if (element.Nodes().Any()) throw new InvalidDataException("BCF XML element must not contain content: " + element.Name.LocalName);
        }

        private static XElement RequiredSingle(XElement parent, string name)
        {
            var elements = parent.Elements(name).ToList();
            if (elements.Count != 1) throw new InvalidDataException("Expected exactly one BCF XML element: " + name);
            return elements[0];
        }

        private static XElement? OptionalSingle(XElement parent, string name)
        {
            var elements = parent.Elements(name).ToList();
            if (elements.Count > 1) throw new InvalidDataException("Duplicate BCF XML element: " + name);
            return elements.Count == 0 ? null : elements[0];
        }

        private static string RequiredSingleValue(XElement parent, string name) => ReadLeafValue(RequiredSingle(parent, name));

        private static string? OptionalSingleValue(XElement parent, string name)
        {
            var element = OptionalSingle(parent, name);
            return element == null ? null : ReadLeafValue(element);
        }

        private static string ReadLeafValue(XElement element)
        {
            if (element.HasAttributes)
                throw new InvalidDataException("BCF XML value element must not contain attributes: " + element.Name.LocalName);
            EnsureScalarContent(element);
            return element.Value;
        }

        private static void EnsureScalarContent(XElement element)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XText && !(node is XCData)) continue;
                throw new InvalidDataException("BCF XML value element must contain plain text only: " + element.Name.LocalName);
            }
        }

        private static string RequiredAttribute(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            if (attribute == null) throw new InvalidDataException("Missing required BCF XML attribute: " + name);
            return attribute.Value;
        }

        private sealed class ExtensionVocabularies
        {
            internal ExtensionVocabularies(HashSet<string> topicTypes, HashSet<string> topicStatuses) { TopicTypes = topicTypes; TopicStatuses = topicStatuses; }
            internal HashSet<string> TopicTypes { get; }
            internal HashSet<string> TopicStatuses { get; }
        }

        private sealed class ViewpointReference
        {
            internal ViewpointReference(string id, string fileName) { Id = id; FileName = fileName; }
            internal string Id { get; }
            internal string FileName { get; }
        }

        private sealed class MarkupTopicData
        {
            internal MarkupTopicData(string id, string title, string status, string type, string description, string creationAuthor, DateTime creationDateUtc, List<BcfComment> comments, List<ViewpointReference> viewpoints)
            {
                Id = id; Title = title; Status = status; Type = type; Description = description; CreationAuthor = creationAuthor; CreationDateUtc = creationDateUtc; Comments = comments; Viewpoints = viewpoints;
            }
            internal string Id { get; }
            internal string Title { get; }
            internal string Status { get; }
            internal string Type { get; }
            internal string Description { get; }
            internal string CreationAuthor { get; }
            internal DateTime CreationDateUtc { get; }
            internal List<BcfComment> Comments { get; }
            internal List<ViewpointReference> Viewpoints { get; }
        }
    }
}