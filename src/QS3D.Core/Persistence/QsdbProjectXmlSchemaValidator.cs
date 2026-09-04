using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;

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
                false);

            ValidateRequiredCanonicalAttribute(root, "projectId", "project id");
            ValidateRequiredCanonicalAttribute(root, "name", "project name");
            ValidateOptionalCanonicalAttribute(root, "drawingFingerprint", "drawing fingerprint");
            ValidateOptionalCanonicalAttribute(root, "activeZoneId", "active zone id");
            ValidateOptionalCanonicalAttribute(root, "activeFloorId", "active floor id");

            foreach (var section in RootSections) RequireExactlyOne(root, section);

            ValidateMap(root.Element("metadata"), "project metadata");
            ValidateZones(root.Element("zones"));
            ValidateFloors(root.Element("floors"));
            ValidateFamilies(root.Element("families"));
            ValidateRules(root.Element("rules"));
            ValidateElements(root.Element("elements"));
            ValidateAudit(root.Element("audit"));
            ValidateElementReferences(root);
        }

        private static void ValidateMap(XElement container, string owner)
        {
            ValidateElement(container, container.Name.LocalName, Array.Empty<string>(), new[] { "p" });
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in container.Elements("p"))
            {
                ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>());
                ValidateCanonicalMapKey(property, owner);
                var key = property.Attribute("name")?.Value ?? string.Empty;
                if (!seenKeys.Add(key))
                    throw new InvalidDataException("QSDB " + owner + " contains duplicate map key: " + key + ".");
            }
        }

        private static void ValidateCanonicalMapKey(XElement property, string owner)
        {
            var key = property.Attribute("name")?.Value;
            if (key == null || string.IsNullOrWhiteSpace(key))
                throw new InvalidDataException("QSDB " + owner + " key must not be empty.");
            if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + owner + " key must not contain leading/trailing whitespace.");
        }

        private static void ValidateZones(XElement zones)
        {
            ValidateElement(zones, "zones", Array.Empty<string>(), new[] { "zone" });
            foreach (var zone in zones.Elements("zone"))
            {
                ValidateElement(zone, "zone", new[] { "id", "name" }, Array.Empty<string>());
                ValidateRequiredCanonicalAttribute(zone, "id", "zone id");
                ValidateRequiredCanonicalAttribute(zone, "name", "zone name");
            }
        }

        private static void ValidateFloors(XElement floors)
        {
            ValidateElement(floors, "floors", Array.Empty<string>(), new[] { "floor" });
            foreach (var floor in floors.Elements("floor"))
            {
                ValidateElement(floor, "floor", new[] { "id", "name", "elevationM" }, Array.Empty<string>());
                ValidateRequiredCanonicalAttribute(floor, "id", "floor id");
                ValidateRequiredCanonicalAttribute(floor, "name", "floor name");
            }
        }

        private static void ValidateFamilies(XElement families)
        {
            ValidateElement(families, "families", Array.Empty<string>(), new[] { "family" });
            foreach (var family in families.Elements("family"))
            {
                ValidateElement(family, "family", new[] { "id", "name", "category" }, new[] { "properties" });
                ValidateRequiredCanonicalAttribute(family, "id", "family id");
                ValidateRequiredCanonicalAttribute(family, "name", "family name");
                ValidateNamedCategoryAttribute(family, "family category");
                RequireAtMostOne(family, "properties");
                foreach (var properties in family.Elements("properties")) ValidateMap(properties, "family properties");
            }
        }

        private static void ValidateRules(XElement rules)
        {
            ValidateElement(rules, "rules", Array.Empty<string>(), new[] { "rule" });
            foreach (var rule in rules.Elements("rule"))
            {
                ValidateElement(rule, "rule", new[] { "id", "category", "output", "expression", "version" }, Array.Empty<string>());
                ValidateRequiredCanonicalAttribute(rule, "id", "quantity rule id");
                ValidateNamedCategoryAttribute(rule, "quantity rule category");
                ValidateRequiredCanonicalAttribute(rule, "output", "quantity rule output");
                ValidateRequiredCanonicalAttribute(rule, "expression", "quantity rule expression");
                ValidateRequiredCanonicalAttribute(rule, "version", "quantity rule version");
            }
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

                ValidateRequiredCanonicalAttribute(element, "id", "element id");
                ValidateNamedCategoryAttribute(element, "element category");
                ValidateOptionalCanonicalAttribute(element, "familyId", "element family id");
                ValidateOptionalCanonicalAttribute(element, "floorId", "element floor id");
                ValidateOptionalCanonicalAttribute(element, "zoneId", "element zone id");
                ValidateOptionalCanonicalAttribute(element, "drawingFingerprint", "element drawing fingerprint");

                RequireAtMostOne(element, "handles");
                RequireAtMostOne(element, "dependencies");
                RequireAtMostOne(element, "properties");
                RequireAtMostOne(element, "quantities");

                foreach (var handles in element.Elements("handles"))
                {
                    ValidateElement(handles, "handles", Array.Empty<string>(), new[] { "h" });
                    var seenHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var handle in handles.Elements("h"))
                    {
                        ValidateElement(handle, "h", Array.Empty<string>(), Array.Empty<string>(), true);
                        ValidateCanonicalText(handle, "source handle");
                        if (!seenHandles.Add(handle.Value))
                            throw new InvalidDataException("QSDB element contains duplicate source handle: " + handle.Value + ".");
                    }
                }

                foreach (var dependencies in element.Elements("dependencies"))
                {
                    ValidateElement(dependencies, "dependencies", Array.Empty<string>(), new[] { "d" });
                    var seenDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var dependency in dependencies.Elements("d"))
                    {
                        ValidateElement(dependency, "d", Array.Empty<string>(), Array.Empty<string>(), true);
                        ValidateCanonicalText(dependency, "dependency id");
                        if (!seenDependencies.Add(dependency.Value))
                            throw new InvalidDataException("QSDB element contains duplicate dependency id: " + dependency.Value + ".");
                    }
                }

                foreach (var properties in element.Elements("properties")) ValidateMap(properties, "element properties");

                foreach (var quantities in element.Elements("quantities"))
                {
                    ValidateElement(quantities, "quantities", Array.Empty<string>(), new[] { "q" });
                    foreach (var quantity in quantities.Elements("q"))
                    {
                        ValidateElement(quantity, "q", new[] { "name", "value" }, Array.Empty<string>());
                        ValidateRequiredCanonicalAttribute(quantity, "name", "quantity name");
                        ValidateNonNegativeQuantityValue(quantity, element.Attribute("id")?.Value ?? string.Empty);
                    }
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
                ValidateRequiredCanonicalAttribute(item, "action", "audit action");
                ValidateOptionalCanonicalIdentityAttribute(item, "elementId", "audit element id");
                ValidateOptionalCanonicalIdentityAttribute(item, "correlationId", "audit correlation id");
            }
        }

        private static void ValidateElementReferences(XElement root)
        {
            var families = root.Element("families") ?? throw new InvalidDataException("QSDB is missing the family catalog.");
            var familyIds = ReadCatalogIds(families, "family");
            var familyCategories = ReadFamilyCategories(families);
            var floorIds = ReadCatalogIds(root.Element("floors"), "floor");
            var zoneIds = ReadCatalogIds(root.Element("zones"), "zone");
            var elements = root.Element("elements") ?? throw new InvalidDataException("QSDB is missing the elements section.");

            foreach (var element in elements.Elements("element"))
            {
                var elementId = element.Attribute("id")?.Value ?? string.Empty;
                ValidateOptionalReference(element, "familyId", familyIds, "family", elementId);
                ValidateFamilyCategoryReference(element, familyCategories, elementId);
                ValidateOptionalReference(element, "floorId", floorIds, "floor", elementId);
                ValidateOptionalReference(element, "zoneId", zoneIds, "zone", elementId);
            }
        }

        private static Dictionary<string, ElementCategory> ReadFamilyCategories(XElement families)
        {
            var result = new Dictionary<string, ElementCategory>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in families.Elements("family"))
            {
                var id = family.Attribute("id")?.Value ?? string.Empty;
                if (result.ContainsKey(id)) continue;
                result.Add(id, ReadValidatedCategory(family, "family category"));
            }
            return result;
        }

        private static void ValidateFamilyCategoryReference(
            XElement element,
            IReadOnlyDictionary<string, ElementCategory> familyCategories,
            string elementId)
        {
            var familyId = element.Attribute("familyId")?.Value;
            if (familyId == null || familyId.Length == 0) return;
            if (!familyCategories.TryGetValue(familyId, out var familyCategory)) return;

            var elementCategory = ReadValidatedCategory(element, "element category");
            if (familyCategory != elementCategory)
                throw new InvalidDataException(
                    "QSDB element " + elementId + " references Family " + familyId + " category " + familyCategory +
                    " but the element category is " + elementCategory + ".");
        }

        private static ElementCategory ReadValidatedCategory(XElement element, string owner)
        {
            var token = element.Attribute("category")?.Value ?? string.Empty;
            if (!Enum.TryParse(token, true, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category))
                throw new InvalidDataException("QSDB " + owner + " is invalid: " + token + ".");
            return category;
        }

        private static HashSet<string> ReadCatalogIds(XElement container, string itemName)
        {
            if (container == null) throw new InvalidDataException("QSDB is missing the " + itemName + " catalog.");
            return new HashSet<string>(
                container.Elements(itemName).Select(x => x.Attribute("id")?.Value ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void ValidateOptionalReference(
            XElement element,
            string attributeName,
            ISet<string> validIds,
            string targetName,
            string elementId)
        {
            var reference = element.Attribute(attributeName)?.Value;
            if (reference == null || reference.Length == 0) return;
            if (!validIds.Contains(reference))
                throw new InvalidDataException(
                    "QSDB element " + elementId + " " + attributeName + " does not reference an existing " + targetName + ": " + reference + ".");
        }

        private static void ValidateNamedCategoryAttribute(XElement element, string owner)
        {
            ValidateRequiredCanonicalAttribute(element, "category", owner);
            var token = element.Attribute("category")?.Value ?? string.Empty;
            if (!Enum.TryParse(token, true, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category))
                throw new InvalidDataException("QSDB " + owner + " is invalid: " + token + ".");
            var name = Enum.GetName(typeof(ElementCategory), category);
            if (name == null || !string.Equals(token, name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("QSDB " + owner + " must use a named ElementCategory token.");
        }

        private static void ValidateRequiredCanonicalAttribute(XElement element, string attributeName, string owner)
        {
            var value = element.Attribute(attributeName)?.Value;
            if (value == null || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("QSDB " + owner + " must not be empty.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + owner + " must not contain leading/trailing whitespace.");
        }

        private static void ValidateOptionalCanonicalAttribute(XElement element, string attributeName, string owner)
        {
            var value = element.Attribute(attributeName)?.Value;
            if (value == null || value.Length == 0) return;
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + owner + " must not contain leading/trailing whitespace.");
        }

        private static void ValidateOptionalCanonicalIdentityAttribute(XElement element, string attributeName, string owner)
        {
            var value = element.Attribute(attributeName)?.Value;
            if (value == null || value.Length == 0) return;
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
                value.Any(char.IsControl))
            {
                throw new InvalidDataException(
                    "QSDB " + owner + " must be empty or canonical without surrounding whitespace or control characters.");
            }
        }

        private static void ValidateCanonicalText(XElement element, string owner)
        {
            var value = element.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("QSDB " + owner + " must not be empty.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("QSDB " + owner + " must not contain leading/trailing whitespace.");
        }

        private static void ValidateNonNegativeQuantityValue(XElement quantity, string elementId)
        {
            var raw = quantity.Attribute("value")?.Value;
            if (raw == null || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return;
            if (value < 0d)
            {
                var quantityName = quantity.Attribute("name")?.Value ?? string.Empty;
                throw new InvalidDataException("QSDB element quantity must not be negative: " + elementId + "/" + quantityName + ".");
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
                if (node is XCData)
                    throw new InvalidDataException("Unsupported QSDB CDATA content in " + element.Name.LocalName + ".");

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
