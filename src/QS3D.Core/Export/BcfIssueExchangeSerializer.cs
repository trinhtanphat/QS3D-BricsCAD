using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace QS3D.Core.Export
{
    public static class BcfIssueExchangeSerializer
    {
        public static string Serialize(BcfIssueExchange exchange)
        {
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            var root = new XElement("BcfIssueExchange", new XAttribute("schemaVersion", BcfIssueExchange.SchemaVersion));
            foreach (var topic in exchange.Topics)
            {
                var topicElement = new XElement(
                    "Topic",
                    new XAttribute("id", topic.Id),
                    new XAttribute("status", topic.Status),
                    new XAttribute("type", topic.Type),
                    new XAttribute("creationAuthor", topic.CreationAuthor),
                    new XAttribute("creationDateUtc", topic.CreationDateUtc.ToString("O", CultureInfo.InvariantCulture)),
                    new XElement("Title", topic.Title),
                    new XElement("Description", topic.Description));

                var viewpointsElement = new XElement("Viewpoints");
                foreach (var viewpoint in topic.Viewpoints)
                {
                    var camera = viewpoint.Camera;
                    var viewpointElement = new XElement(
                        "Viewpoint",
                        new XAttribute("id", viewpoint.Id),
                        new XElement(
                            "OrthogonalCamera",
                            Point("ViewPoint", camera.ViewPoint),
                            Point("Direction", camera.Direction),
                            Point("UpVector", camera.UpVector),
                            new XElement("ViewToWorldScale", Number(camera.ViewToWorldScale)),
                            new XElement("AspectRatio", Number(camera.AspectRatio))));
                    foreach (var component in viewpoint.Components)
                    {
                        viewpointElement.Add(
                            new XElement(
                                "Component",
                                new XAttribute("qs3dElementId", component.Qs3dElementId),
                                new XAttribute("ifcGlobalId", component.IfcGlobalId)));
                    }
                    viewpointsElement.Add(viewpointElement);
                }
                topicElement.Add(viewpointsElement);

                var commentsElement = new XElement("Comments");
                foreach (var comment in topic.Comments)
                {
                    var commentElement = new XElement(
                        "Comment",
                        new XAttribute("id", comment.Id),
                        new XAttribute("author", comment.Author),
                        new XAttribute("createdUtc", comment.CreatedUtc.ToString("O", CultureInfo.InvariantCulture)));
                    if (comment.ViewpointId != null) commentElement.Add(new XAttribute("viewpointId", comment.ViewpointId));
                    commentElement.Add(new XElement("Text", comment.Text));
                    commentsElement.Add(commentElement);
                }
                topicElement.Add(commentsElement);
                root.Add(topicElement);
            }
            return new XDocument(root).ToString(SaveOptions.DisableFormatting);
        }

        public static BcfIssueExchange Deserialize(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) throw new InvalidDataException("BCF payload is empty.");
            try
            {
                var document = XDocument.Parse(payload, LoadOptions.None);
                EnsureDocumentContent(document);
                var root = document.Root;
                if (root == null || root.Name.NamespaceName.Length != 0 || !string.Equals(root.Name.LocalName, "BcfIssueExchange", StringComparison.Ordinal))
                    throw new InvalidDataException("BCF payload root is invalid.");
                EnsureAllowedAttributes(root, "schemaVersion");
                EnsureAllowedChildren(root, "Topic");
                if (!string.Equals(RequiredAttribute(root, "schemaVersion"), BcfIssueExchange.SchemaVersion, StringComparison.Ordinal))
                    throw new InvalidDataException("Unsupported BCF schema version.");

                var topics = new List<BcfTopic>();
                foreach (var topicElement in root.Elements("Topic"))
                {
                    EnsureAllowedAttributes(topicElement, "id", "status", "type", "creationAuthor", "creationDateUtc");
                    EnsureAllowedChildren(topicElement, "Title", "Description", "Viewpoints", "Comments");
                    var viewpoints = ReadViewpoints(topicElement);
                    var comments = ReadComments(topicElement);
                    topics.Add(
                        new BcfTopic(
                            RequiredAttribute(topicElement, "id"),
                            RequiredElementValue(topicElement, "Title"),
                            RequiredAttribute(topicElement, "status"),
                            RequiredAttribute(topicElement, "type"),
                            RequiredElementValue(topicElement, "Description"),
                            RequiredAttribute(topicElement, "creationAuthor"),
                            ParseUtc(RequiredAttribute(topicElement, "creationDateUtc")),
                            comments,
                            viewpoints));
                }
                return BcfIssueExchange.Create(topics);
            }
            catch (InvalidDataException) { throw; }
            catch (Exception exception) when (exception is XmlException || exception is FormatException || exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new InvalidDataException("BCF payload failed validation.", exception);
            }
        }

        private static IReadOnlyList<BcfViewpoint> ReadViewpoints(XElement topicElement)
        {
            var container = RequiredSingle(topicElement, "Viewpoints");
            EnsureAllowedAttributes(container);
            EnsureAllowedChildren(container, "Viewpoint");
            var viewpoints = new List<BcfViewpoint>();
            foreach (var viewpointElement in container.Elements("Viewpoint"))
            {
                EnsureAllowedAttributes(viewpointElement, "id");
                EnsureAllowedChildren(viewpointElement, "OrthogonalCamera", "Component");
                var cameraElement = RequiredSingle(viewpointElement, "OrthogonalCamera");
                EnsureAllowedAttributes(cameraElement);
                EnsureAllowedChildren(cameraElement, "ViewPoint", "Direction", "UpVector", "ViewToWorldScale", "AspectRatio");
                var camera = new BcfOrthogonalCamera(
                    ReadPoint(cameraElement, "ViewPoint"),
                    ReadPoint(cameraElement, "Direction"),
                    ReadPoint(cameraElement, "UpVector"),
                    ReadDouble(cameraElement, "ViewToWorldScale"),
                    ReadDouble(cameraElement, "AspectRatio"));
                var components = new List<BcfComponentReference>();
                foreach (var componentElement in viewpointElement.Elements("Component"))
                {
                    EnsureAllowedAttributes(componentElement, "qs3dElementId", "ifcGlobalId");
                    EnsureNoContent(componentElement);
                    components.Add(new BcfComponentReference(RequiredAttribute(componentElement, "qs3dElementId"), RequiredAttribute(componentElement, "ifcGlobalId")));
                }
                viewpoints.Add(new BcfViewpoint(RequiredAttribute(viewpointElement, "id"), camera, components));
            }
            return viewpoints;
        }

        private static IReadOnlyList<BcfComment> ReadComments(XElement topicElement)
        {
            var container = RequiredSingle(topicElement, "Comments");
            EnsureAllowedAttributes(container);
            EnsureAllowedChildren(container, "Comment");
            var comments = new List<BcfComment>();
            foreach (var commentElement in container.Elements("Comment"))
            {
                EnsureAllowedAttributes(commentElement, "id", "author", "createdUtc", "viewpointId");
                EnsureAllowedChildren(commentElement, "Text");
                comments.Add(
                    new BcfComment(
                        RequiredAttribute(commentElement, "id"),
                        RequiredAttribute(commentElement, "author"),
                        ParseUtc(RequiredAttribute(commentElement, "createdUtc")),
                        RequiredElementValue(commentElement, "Text"),
                        OptionalAttribute(commentElement, "viewpointId")));
            }
            return comments;
        }

        private static XElement Point(string name, BcfPoint3 point) =>
            new XElement(name, new XAttribute("x", Number(point.X)), new XAttribute("y", Number(point.Y)), new XAttribute("z", Number(point.Z)));

        private static BcfPoint3 ReadPoint(XElement parent, string name)
        {
            var point = RequiredSingle(parent, name);
            EnsureAllowedAttributes(point, "x", "y", "z");
            EnsureNoContent(point);
            return new BcfPoint3(ParseNumber(RequiredAttribute(point, "x")), ParseNumber(RequiredAttribute(point, "y")), ParseNumber(RequiredAttribute(point, "z")));
        }

        private static double ReadDouble(XElement parent, string name)
        {
            return ParseNumber(RequiredElementValue(parent, name));
        }

        private static double ParseNumber(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
                double.IsNaN(parsed) ||
                double.IsInfinity(parsed) ||
                !string.Equals(value, Number(parsed), StringComparison.Ordinal))
                throw new InvalidDataException("BCF numeric value must use canonical invariant round-trip format.");
            return parsed;
        }

        private static DateTime ParseUtc(string value)
        {
            if (!DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ||
                parsed.Kind != DateTimeKind.Utc ||
                !string.Equals(value, parsed.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new InvalidDataException("BCF timestamp must use canonical UTC round-trip format.");
            return parsed;
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static void EnsureDocumentContent(XDocument document)
        {
            foreach (var node in document.Nodes())
            {
                if (node is XElement) continue;
                if (node is XText text && !(node is XCData) && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Unsupported BCF document content.");
            }
        }

        private static void EnsureAllowedAttributes(XElement element, params string[] names)
        {
            var allowed = new HashSet<string>(names, StringComparer.Ordinal);
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name.NamespaceName.Length != 0 || !allowed.Contains(attribute.Name.LocalName))
                    throw new InvalidDataException("Unsupported BCF attribute: " + attribute.Name.LocalName);
            }
        }

        private static void EnsureAllowedChildren(XElement element, params string[] names)
        {
            EnsureElementOnlyContent(element);
            var allowed = new HashSet<string>(names, StringComparer.Ordinal);
            foreach (var child in element.Elements())
            {
                if (child.Name.NamespaceName.Length != 0 || !allowed.Contains(child.Name.LocalName))
                    throw new InvalidDataException("Unsupported BCF element: " + child.Name.LocalName);
            }
        }

        private static void EnsureElementOnlyContent(XElement element)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XElement) continue;
                if (node is XText text && !(node is XCData) && string.IsNullOrWhiteSpace(text.Value)) continue;
                throw new InvalidDataException("Unexpected BCF container content: " + element.Name.LocalName);
            }
        }

        private static void EnsureNoContent(XElement element)
        {
            if (element.Nodes().Any()) throw new InvalidDataException("BCF empty element must not contain content: " + element.Name.LocalName);
        }

        private static void EnsureScalarContent(XElement element)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XText && !(node is XCData)) continue;
                throw new InvalidDataException("BCF scalar element must contain plain text only: " + element.Name.LocalName);
            }
        }

        private static XElement RequiredSingle(XElement parent, string name)
        {
            var elements = parent.Elements(name).ToList();
            if (elements.Count != 1) throw new InvalidDataException("Expected exactly one BCF element: " + name);
            return elements[0];
        }

        private static string RequiredAttribute(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            if (attribute == null) throw new InvalidDataException("Missing required BCF attribute: " + name);
            return attribute.Value;
        }

        private static string? OptionalAttribute(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }

        private static string RequiredElementValue(XElement parent, string name)
        {
            var element = RequiredSingle(parent, name);
            EnsureAllowedAttributes(element);
            EnsureScalarContent(element);
            return element.Value;
        }
    }
}
