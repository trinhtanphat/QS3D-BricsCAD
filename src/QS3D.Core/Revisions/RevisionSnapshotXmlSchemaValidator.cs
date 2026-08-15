using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QS3D.Core.Revisions
{
    internal static class RevisionSnapshotXmlSchemaValidator
    {
        internal static void Validate(XElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var document = root.Document;
            if (document != null)
            {
                foreach (var node in document.Nodes())
                {
                    if (ReferenceEquals(node, root)) continue;
                    throw new InvalidDataException("Unsupported QS3D revision document-level XML content.");
                }
            }

            ValidateElement(root, "qs3dRevision", new[] { "id", "createdUtc" }, new[] { "elements" });
            RequireExactlyOne(root, "elements");

            foreach (var elements in root.Elements("elements"))
            {
                ValidateElement(elements, "elements", Array.Empty<string>(), new[] { "element" });
                foreach (var element in elements.Elements("element"))
                {
                    ValidateElement(element, "element", new[] { "id", "category", "familyId", "floorId", "zoneId" }, new[] { "properties", "quantities", "sourceHandles", "dependencies" });
                    RequireExactlyOne(element, "properties");
                    RequireExactlyOne(element, "quantities");
                    RequireExactlyOne(element, "sourceHandles");
                    RequireExactlyOne(element, "dependencies");

                    foreach (var properties in element.Elements("properties"))
                    {
                        ValidateElement(properties, "properties", Array.Empty<string>(), new[] { "p" });
                        foreach (var property in properties.Elements("p"))
                            ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>());
                    }

                    foreach (var quantities in element.Elements("quantities"))
                    {
                        ValidateElement(quantities, "quantities", Array.Empty<string>(), new[] { "q" });
                        foreach (var quantity in quantities.Elements("q"))
                            ValidateElement(quantity, "q", new[] { "name", "value" }, Array.Empty<string>());
                    }

                    foreach (var handles in element.Elements("sourceHandles"))
                    {
                        ValidateElement(handles, "sourceHandles", Array.Empty<string>(), new[] { "h" });
                        foreach (var handle in handles.Elements("h"))
                            ValidateElement(handle, "h", new[] { "value" }, Array.Empty<string>());
                    }

                    foreach (var dependencies in element.Elements("dependencies"))
                    {
                        ValidateElement(dependencies, "dependencies", Array.Empty<string>(), new[] { "d" });
                        foreach (var dependency in dependencies.Elements("d"))
                            ValidateElement(dependency, "d", new[] { "value" }, Array.Empty<string>());
                    }
                }
            }
        }

        private static void ValidateElement(XElement element, string expectedName, IEnumerable<string> allowedAttributes, IEnumerable<string> allowedChildren)
        {
            var expected = XName.Get(expectedName);
            if (element.Name != expected)
                throw new InvalidDataException("Unsupported QS3D revision element or namespace: " + element.Name);

            var attributes = new HashSet<XName>(allowedAttributes.Select(XName.Get));
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name))
                    throw new InvalidDataException("Unsupported QS3D revision attribute: " + element.Name.LocalName + "/" + attribute.Name);
            }

            var children = new HashSet<XName>(allowedChildren.Select(XName.Get));
            foreach (var node in element.Nodes())
            {
                if (node is XCData)
                    throw new InvalidDataException("Unsupported QS3D revision CDATA content in " + element.Name.LocalName + ".");
                if (node is XText text)
                {
                    if (!string.IsNullOrWhiteSpace(text.Value))
                        throw new InvalidDataException("Unsupported QS3D revision text content in " + element.Name.LocalName + ".");
                    continue;
                }
                if (node is XElement child)
                {
                    if (child.Name.Namespace != XNamespace.None || !children.Contains(child.Name))
                        throw new InvalidDataException("Unsupported QS3D revision child element: " + element.Name.LocalName + "/" + child.Name);
                    continue;
                }
                throw new InvalidDataException("Unsupported QS3D revision XML content in " + element.Name.LocalName + ".");
            }
        }

        private static void RequireExactlyOne(XElement parent, string childName)
        {
            var name = XName.Get(childName);
            if (parent.Elements(name).Take(2).Count() != 1)
                throw new InvalidDataException("QS3D revision requires exactly one " + childName + " section.");
        }
    }
}