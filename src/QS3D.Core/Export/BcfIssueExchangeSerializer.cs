using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                var root = document.Root;
                if (root == null || !string.Equals(root.Name.LocalName, "BcfIssueExchange", StringComparison.Ordinal))
                    throw new InvalidDataException("BCF payload root is invalid.");
                if (!string.Equals(RequiredAttribute(root, "schemaVersion"), BcfIssueExchange.SchemaVersion, StringComparison.Ordinal))
                    throw new InvalidDataException("Unsupported BCF schema version.");

                var topics = new List<BcfTopic>();
                foreach (var topicElement in root.Elements("Topic"))
                {
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
            var container = topicElement.Element("Viewpoints");
            if (container == null) throw new InvalidDataException("BCF topic is missing Viewpoints.");
            var viewpoints = new List<BcfViewpoint>();
            foreach (var viewpointElement in container.Elements("Viewpoint"))
            {
                var cameraElement = viewpointElement.Element("OrthogonalCamera") ?? throw new InvalidDataException("BCF viewpoint is missing OrthogonalCamera.");
                var camera = new BcfOrthogonalCamera(
                    ReadPoint(cameraElement, "ViewPoint"),
                    ReadPoint(cameraElement, "Direction"),
                    ReadPoint(cameraElement, "UpVector"),
                    ReadDouble(cameraElement, "ViewToWorldScale"),
                    ReadDouble(cameraElement, "AspectRatio"));
                var components = new List<BcfComponentReference>();
                foreach (var componentElement in viewpointElement.Elements("Component"))
                {
                    components.Add(new BcfComponentReference(RequiredAttribute(componentElement, "qs3dElementId"), RequiredAttribute(componentElement, "ifcGlobalId")));
                }
                viewpoints.Add(new BcfViewpoint(RequiredAttribute(viewpointElement, "id"), camera, components));
            }
            return viewpoints;
        }

        private static IReadOnlyList<BcfComment> ReadComments(XElement topicElement)
        {
            var container = topicElement.Element("Comments");
            if (container == null) throw new InvalidDataException("BCF topic is missing Comments.");
            var comments = new List<BcfComment>();
            foreach (var commentElement in container.Elements("Comment"))
            {
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
            var point = parent.Element(name) ?? throw new InvalidDataException("Missing BCF camera point: " + name);
            return new BcfPoint3(ParseNumber(RequiredAttribute(point, "x")), ParseNumber(RequiredAttribute(point, "y")), ParseNumber(RequiredAttribute(point, "z")));
        }

        private static double ReadDouble(XElement parent, string name)
        {
            return ParseNumber(RequiredElementValue(parent, name));
        }

        private static double ParseNumber(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || double.IsNaN(parsed) || double.IsInfinity(parsed))
                throw new InvalidDataException("BCF numeric value is invalid.");
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
            var element = parent.Element(name);
            if (element == null) throw new InvalidDataException("Missing required BCF element: " + name);
            return element.Value;
        }
    }
}
