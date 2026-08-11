using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QS3D.Core.Persistence
{
    internal static class QsdbProjectXmlSchemaValidator
    {
        private static readonly string[] RootSections =
        {
            "metadata", "zones", "floors", "families", "rules", "elements", "audit"
        };

        internal static void ValidateCurrent(XElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            ValidateElement(
                root,
                "qs3d",
                new[]
                {
                    "schema", "projectId", "name", "updatedUtc", "changeVersion",
                    "drawingPath", "drawingFingerprint", "activeZoneId", "activeFloorId"
                },
                RootSections,
                false,
                true);

            foreach (var section in RootSections) RequireExactlyOne(root, section);

            ValidateMap(root.Element("metadata"), "project metadata");
            ValidateZones(root.Element("zones"));
            ValidateFloors(root.Element("floors"));
            ValidateFamilies(root.Element("families"));
            ValidateRules(root.Element("rules"));
            ValidateElements(root.Element("elements"));
            ValidateAudit(root.Element("audit"));
        }

        private static void ValidateMap(XElement container, string owner)
        {
            ValidateElement(container, container.Name.LocalName, Array.Empty<string>(), new[] { "p" });
            foreach (var property in container.Elements("p"))
            {
                ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>());
                ValidateCanonicalMapKey(property, owner);
            }
        }

        private static void ValidateCanonicalMapKey(XElement property, string owner)
        {
            var key = property.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidDataException("QSDB " + owner + " key must not be empty.");
            if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + owner + " key must not contain leading/trailing whitespace.");
        }

        private static void ValidateZones(XElement zones)
        {
            ValidateElement(zones, "zones", Array.Empty<string>(), new[] { "zone" });
            foreach (var zone in zones.Elements("zone"))
                ValidateElement(zone, "zone", new[] { "id", "name" }, Array.Empty<string>());
        }

        private static void ValidateFloors(XElement floors)
        {
            ValidateElement(floors, "floors", Array.Empty<string>(), new[] { "floor" });
            foreach (var floor in floors.Elements("floor"))
                ValidateElement(floor, "floor", new[] { "id", "name", "elevationM" }, Array.Empty<string>());
        }

        private static void ValidateFamilies(XElement families)
        {
            ValidateElement(families, "families", Array.Empty<string>(), new[] { "family" });
            foreach (var family in families.Elements("family"))
            {
                ValidateElement(family, "family", new[] { "id", "name", "category" }, new[] { "properties" });
                RequireAtMostOne(family, "properties");
                foreach (var properties in family.Elements("properties")) ValidateMap(properties, "family properties");
            }
        }

        private static void ValidateRules(XElement rules)
        {
            ValidateElement(rules, "rules", Array.Empty<string>(), new[] { "rule" });
            foreach (var rule in rules.Elements("rule"))
                ValidateElement(rule, "rule", new[] { "id", "category", "output", "expression", "version" }, Array.Empty<string>());
        }

        private static void ValidateElements(XElement elements)
        {
            ValidateElement(elements, "elements", Array.Empty<string>(), new[] { "element" });
            foreach (var element in elements.Elements("element"))
            {
                ValidateElement(
                    element,
                    "element",
                    new[]
                    {
                        "id", "category", "familyId", "floorId", "zoneId", "drawingFingerprint",
                        "dirty", "updatedUtc"
                    },
                    new[] { "handles", "dependencies", "properties", "quantities" });

                RequireAtMostOne(element, "handles");
                RequireAtMostOne(element, "dependencies");
                RequireAtMostOne(element, "properties");
                RequireAtMostOne(element, "quantities");

                foreach (var handles in element.Elements("handles"))
                {
                    ValidateElement(handles, "handles", Array.Empty<string>(), new[] { "h" });
                    foreach (var handle in handles.Elements("h"))
                        ValidateElement(handle, "h", Array.Empty<string>(), Array.Empty<string>(), true);
                }

                foreach (var dependencies in element.Elements("dependencies"))
                {
                    ValidateElement(dependencies, "dependencies", Array.Empty<string>(), new[] { "d" });
                    foreach (var dependency in dependencies.Elements("d"))
                        ValidateElement(dependency, "d", Array.Empty<string>(), Array.Empty<string>(), true);
                }

                foreach (var properties in element.Elements("properties")) ValidateMap(properties, "element properties");

                foreach (var quantities in element.Elements("quantities"))
                {
                    ValidateElement(quantities, "quantities", Array.Empty<string>(), new[] { "q" });
                    foreach (var quantity in quantities.Elements("q"))
                        ValidateElement(quantity, "q", new[] { "name", "value" }, Array.Empty<string>());
                }
            }
        }

        private static void ValidateAudit(XElement audit)
        {
            ValidateElement(audit, "audit", Array.Empty<string>(), new[] { "event" });
            foreach (var item in audit.Elements("event"))
            {
                ValidateElement(
                    item,
                    "event",
                    new[] { "utc", "action", "elementId", "detail", "actor", "correlationId" },
                    Array.Empty<string>());
            }
        }

        private static void ValidateElement(
            XElement element,
            string expectedName,
            IEnumerable<string> allowedAttributes,
            IEnumerable<string> allowedChildren,
            bool allowText = false,
            bool ignoreRootNameCase = false)
        {
            if (element == null) throw new InvalidDataException("QSDB is missing a required XML section.");
            if (element.Name.Namespace != XNamespace.None ||
                !(ignoreRootNameCase
                    ? string.Equals(element.Name.LocalName, expectedName, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal)))
            {
                throw new InvalidDataException("Unsupported QSDB element or namespace: " + element.Name);
            }

            var attributes = new HashSet<XName>(allowedAttributes.Select(XName.Get));
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name))
                    throw new InvalidDataException("Unsupported QSDB attribute: " + element.Name.LocalName + "/" + attribute.Name);
            }

            var children = new HashSet<XName>(allowedChildren.Select(XName.Get));
            foreach (var node in element.Nodes())
            {
                if (node is XText text)
                {
                    if (!allowText && !string.IsNullOrWhiteSpace(text.Value))
                        throw new InvalidDataException("Unsupported QSDB text content in " + element.Name.LocalName + ".");
                    continue;
                }

                if (node is XElement child)
                {
                    if (child.Name.Namespace != XNamespace.None || !children.Contains(child.Name))
                        throw new InvalidDataException("Unsupported QSDB child element: " + element.Name.LocalName + "/" + child.Name);
                    continue;
                }

                throw new InvalidDataException("Unsupported QSDB XML content in " + element.Name.LocalName + ".");
            }
        }

        private static void RequireExactlyOne(XElement parent, string childName)
        {
            var count = parent.Elements(XName.Get(childName)).Take(2).Count();
            if (count != 1)
                throw new InvalidDataException("QSDB requires exactly one " + childName + " section.");
        }

        private static void RequireAtMostOne(XElement parent, string childName)
        {
            if (parent.Elements(XName.Get(childName)).Skip(1).Any())
                throw new InvalidDataException("Duplicate QSDB singleton element: " + parent.Name.LocalName + "/" + childName);
        }
    }
}
