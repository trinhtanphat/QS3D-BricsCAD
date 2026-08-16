using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QS3D.Core.Templates
{
    internal static class TemplateProfileXmlSchemaValidator
    {
        internal static void Validate(XElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            ValidateDocumentShape(root);
            ValidateElement(root, "qs3dTemplate", new[] { "schema", "id", "name" }, new[] { "families", "rules", "layerMappings", "bqColumns" });
            RequireExactlyOne(root, "families");
            RequireExactlyOne(root, "rules");
            RequireExactlyOne(root, "layerMappings");
            RequireExactlyOne(root, "bqColumns");
            var expectedRootOrder = new[]
            {
                XName.Get("families"), XName.Get("rules"), XName.Get("layerMappings"), XName.Get("bqColumns")
            };
            if (!root.Elements().Select(x => x.Name).SequenceEqual(expectedRootOrder))
                throw new InvalidDataException("QS3D template root sections are not in canonical order.");

            foreach (var families in root.Elements("families"))
            {
                ValidateElement(families, "families", Array.Empty<string>(), new[] { "family" });
                foreach (var family in families.Elements("family"))
                {
                    ValidateElement(family, "family", new[] { "id", "name", "category" }, new[] { "properties" });
                    RequireExactlyOne(family, "properties");
                    foreach (var properties in family.Elements("properties"))
                    {
                        ValidateElement(properties, "properties", Array.Empty<string>(), new[] { "p" });
                        foreach (var property in properties.Elements("p"))
                            ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>());
                    }
                }
            }

            foreach (var rules in root.Elements("rules"))
            {
                ValidateElement(rules, "rules", Array.Empty<string>(), new[] { "rule" });
                foreach (var rule in rules.Elements("rule"))
                    ValidateElement(rule, "rule", new[] { "id", "category", "output", "expression", "version" }, Array.Empty<string>());
            }

            foreach (var mappings in root.Elements("layerMappings"))
            {
                ValidateElement(mappings, "layerMappings", Array.Empty<string>(), new[] { "map" });
                foreach (var map in mappings.Elements("map"))
                    ValidateElement(map, "map", new[] { "pattern", "category" }, Array.Empty<string>());
            }

            foreach (var columns in root.Elements("bqColumns"))
            {
                ValidateElement(columns, "bqColumns", Array.Empty<string>(), new[] { "column" });
                foreach (var column in columns.Elements("column"))
                    ValidateElement(column, "column", new[] { "name" }, Array.Empty<string>());
            }
        }

        private static void ValidateDocumentShape(XElement root)
        {
            var document = root.Document;
            if (document == null) return;
            foreach (var node in document.Nodes())
            {
                if (ReferenceEquals(node, root)) continue;
                throw new InvalidDataException("Unsupported QS3D template document-level XML content.");
            }
        }

        private static void ValidateElement(XElement element, string expectedName, IEnumerable<string> allowedAttributes, IEnumerable<string> allowedChildren)
        {
            var expected = XName.Get(expectedName);
            if (element.Name != expected)
                throw new InvalidDataException("Unsupported QS3D template element or namespace: " + element.Name);

            var attributes = new HashSet<XName>(allowedAttributes.Select(XName.Get));
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name))
                    throw new InvalidDataException("Unsupported QS3D template attribute: " + element.Name.LocalName + "/" + attribute.Name);
                if (!string.Equals(attribute.Name.LocalName, "value", StringComparison.Ordinal) &&
                    (string.IsNullOrWhiteSpace(attribute.Value) || !string.Equals(attribute.Value, attribute.Value.Trim(), StringComparison.Ordinal)))
                    throw new InvalidDataException("QS3D template attribute is empty or non-canonical: " + element.Name.LocalName + "/" + attribute.Name.LocalName);
            }

            var children = new HashSet<XName>(allowedChildren.Select(XName.Get));
            foreach (var node in element.Nodes())
            {
                if (node is XCData)
                    throw new InvalidDataException("Unsupported QS3D template CDATA content in " + element.Name.LocalName + ".");
                if (node is XText text)
                {
                    if (!string.IsNullOrWhiteSpace(text.Value))
                        throw new InvalidDataException("Unsupported QS3D template text content in " + element.Name.LocalName + ".");
                    continue;
                }
                if (node is XElement child)
                {
                    if (child.Name.Namespace != XNamespace.None || !children.Contains(child.Name))
                        throw new InvalidDataException("Unsupported QS3D template child element: " + element.Name.LocalName + "/" + child.Name);
                    continue;
                }
                throw new InvalidDataException("Unsupported QS3D template XML content in " + element.Name.LocalName + ".");
            }
        }

        private static void RequireExactlyOne(XElement parent, string childName)
        {
            var count = parent.Elements(XName.Get(childName)).Take(2).Count();
            if (count != 1)
                throw new InvalidDataException("QS3D template requires exactly one singleton element: " + parent.Name.LocalName + "/" + childName);
        }
    }
}
