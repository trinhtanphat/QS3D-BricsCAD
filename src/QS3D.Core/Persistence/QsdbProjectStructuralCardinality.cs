using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QS3D.Core.Persistence
{
    internal static class QsdbProjectStructuralCardinality
    {
        internal const int MaxTopLevelEntries = 100000;
        internal const int MaxNestedEntries = 10000;

        internal static void Validate(XElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            ValidateDirectChildren(root, 7, "root sections");
            ValidateDirectChildren(root.Element("metadata"), MaxNestedEntries, "project metadata");

            ValidateDirectChildren(root.Element("zones"), MaxTopLevelEntries, "zones");
            ValidateDirectChildren(root.Element("floors"), MaxTopLevelEntries, "floors");

            var families = root.Element("families");
            ValidateDirectChildren(families, MaxTopLevelEntries, "families");
            if (families != null)
            {
                foreach (var family in families.Elements("family"))
                {
                    var familyId = family.Attribute("id")?.Value ?? string.Empty;
                    ValidateDirectChildren(family, 1, "family " + familyId + " child sections");
                    foreach (var properties in family.Elements("properties"))
                        ValidateDirectChildren(properties, MaxNestedEntries, "family " + familyId + " properties");
                }
            }

            ValidateDirectChildren(root.Element("rules"), MaxTopLevelEntries, "quantity rules");

            var elements = root.Element("elements");
            ValidateDirectChildren(elements, MaxTopLevelEntries, "elements");
            if (elements != null)
            {
                foreach (var element in elements.Elements("element"))
                {
                    var elementId = element.Attribute("id")?.Value ?? string.Empty;
                    ValidateDirectChildren(element, 4, "element " + elementId + " child sections");
                    foreach (var handles in element.Elements("handles"))
                        ValidateDirectChildren(handles, MaxNestedEntries, "element " + elementId + " handles");
                    foreach (var dependencies in element.Elements("dependencies"))
                        ValidateDirectChildren(dependencies, MaxNestedEntries, "element " + elementId + " dependencies");
                    foreach (var properties in element.Elements("properties"))
                        ValidateDirectChildren(properties, MaxNestedEntries, "element " + elementId + " properties");
                    foreach (var quantities in element.Elements("quantities"))
                        ValidateDirectChildren(quantities, MaxNestedEntries, "element " + elementId + " quantities");
                }
            }

            ValidateDirectChildren(root.Element("audit"), MaxTopLevelEntries, "audit events");
        }

        private static void ValidateDirectChildren(XElement? container, int maximum, string label)
        {
            if (container == null) return;
            if (container.Elements().Take(maximum + 1).Count() > maximum)
            {
                throw new InvalidDataException(
                    "QSDB " + label + " exceeds the maximum supported cardinality of " + maximum + ".");
            }
        }
    }
}
