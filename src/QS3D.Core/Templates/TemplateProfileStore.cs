using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.Templates
{
    public sealed class TemplateApplyResult
    {
        public int FamiliesAdded { get; set; }
        public int FamiliesUpdated { get; set; }
        public int RulesAdded { get; set; }
        public int RulesUpdated { get; set; }
        public int LayerMappingsApplied { get; set; }
        public int AffectedElements { get; set; }
    }

    public sealed class TemplateProfileStore
    {
        public const string LayerMappingPrefix = "QS3D.LayerMapping:";
        public const string VisibleBqColumnsKey = "QS3D.BqVisibleColumns";
        private const long MaxTemplateFileBytes = 8L * 1024L * 1024L;

        public TemplateProfile ExportProject(ProjectState project, string id, string name)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var profile = new TemplateProfile(id, name);
            foreach (var family in project.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var copy = new ProjectFamily(family.Id, family.Name, family.Category);
                foreach (var property in family.Properties) copy.Properties[property.Key] = property.Value;
                profile.Families.Add(copy);
            }
            foreach (var rule in project.QuantityRules.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                profile.QuantityRules.Add(new QuantityRule(rule.Id, rule.Category, rule.OutputName, rule.Expression, rule.Version));
            foreach (var item in project.Metadata.Where(x => x.Key.StartsWith(LayerMappingPrefix, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var pattern = item.Key.Substring(LayerMappingPrefix.Length).Trim();
                if (pattern.Length > 0) profile.LayerMappings[pattern] = item.Value;
            }
            if (project.Metadata.TryGetValue(VisibleBqColumnsKey, out var columns))
                foreach (var column in SplitColumns(columns)) profile.VisibleBqColumns.Add(column);
            return profile;
        }

        public TemplateApplyResult Apply(ProjectState project, TemplateProfile profile)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            Validate(profile);

            var result = new TemplateApplyResult();
            var changedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changedCategories = new HashSet<ElementCategory>();

            foreach (var source in profile.Families)
            {
                var existing = project.FindFamily(source.Id);
                if (existing == null)
                {
                    existing = new ProjectFamily(source.Id, source.Name, source.Category);
                    foreach (var property in source.Properties) existing.Properties[property.Key] = property.Value;
                    project.Families.Add(existing);
                    result.FamiliesAdded++;
                    changedFamilies.Add(existing.Id);
                    continue;
                }

                if (existing.Category != source.Category && project.Elements.Any(x => string.Equals(x.FamilyId, existing.Id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Template cannot change category of in-use family " + existing.Id + ".");

                var changed = !string.Equals(existing.Name, source.Name, StringComparison.Ordinal) || existing.Category != source.Category || !SameMap(existing.Properties, source.Properties);
                if (!changed) continue;
                existing.Name = source.Name;
                existing.Category = source.Category;
                existing.Properties.Clear();
                foreach (var property in source.Properties) existing.Properties[property.Key] = property.Value;
                result.FamiliesUpdated++;
                changedFamilies.Add(existing.Id);
            }

            foreach (var source in profile.QuantityRules)
            {
                var existing = project.FindQuantityRule(source.Id);
                var same = existing != null && existing.Category == source.Category && string.Equals(existing.OutputName, source.OutputName, StringComparison.OrdinalIgnoreCase) && string.Equals(existing.Expression, source.Expression, StringComparison.Ordinal) && string.Equals(existing.Version, source.Version, StringComparison.Ordinal);
                if (same) continue;
                var collision = project.QuantityRules.FirstOrDefault(x => !string.Equals(x.Id, source.Id, StringComparison.OrdinalIgnoreCase) && x.Category == source.Category && string.Equals(x.OutputName, source.OutputName, StringComparison.OrdinalIgnoreCase));
                if (collision != null) throw new InvalidOperationException("Template rule output conflicts with project rule " + collision.Id + ".");
                if (existing != null) { project.QuantityRules.Remove(existing); result.RulesUpdated++; }
                else result.RulesAdded++;
                project.QuantityRules.Add(new QuantityRule(source.Id, source.Category, source.OutputName, source.Expression, source.Version));
                changedCategories.Add(source.Category);
            }

            foreach (var mapping in profile.LayerMappings)
            {
                if (!Enum.TryParse(mapping.Value, true, out ElementCategory category)) throw new InvalidDataException("Invalid template layer mapping category: " + mapping.Value);
                project.Metadata[LayerMappingPrefix + mapping.Key.Trim()] = category.ToString();
                result.LayerMappingsApplied++;
            }

            var visibleColumns = profile.VisibleBqColumns.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (visibleColumns.Length > 0) project.Metadata[VisibleBqColumnsKey] = string.Join("|", visibleColumns);

            var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (!changedFamilies.Contains(element.FamilyId) && !changedCategories.Contains(element.Category)) continue;
                element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
                affected.Add(element.Id);
            }
            result.AffectedElements = affected.Count;
            project.Touch();
            AuditTrail.ForProject(project).Record("template.apply", string.Empty, profile.Id + " • families +" + result.FamiliesAdded + "/~" + result.FamiliesUpdated + " • rules +" + result.RulesAdded + "/~" + result.RulesUpdated + " • mappings " + result.LayerMappingsApplied);
            return result;
        }

        public void Save(TemplateProfile profile, string path)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Template path is required.", nameof(path));
            Validate(profile);
            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = AtomicFileCommit.CreateTempPath(full);
            var backup = full + ".bak";
            try
            {
                Serialize(profile).Save(temp, SaveOptions.DisableFormatting);
                Load(temp);
                AtomicFileCommit.ReplaceWithBackup(temp, full, backup);
            }
            finally { AtomicFileCommit.TryDelete(temp); }
        }

        public TemplateProfile Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Template path is required.", nameof(path));
            var document = LoadDocument(path);
            var root = document.Root ?? throw new InvalidDataException("Template has no root element.");
            if (!string.Equals(root.Name.LocalName, "qs3dTemplate", StringComparison.Ordinal)) throw new InvalidDataException("Invalid QS3D template root.");
            var schema = Required(root, "schema");
            if (!string.Equals(schema, "1", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported QS3D template schema: " + schema);
            var profile = new TemplateProfile(Required(root, "id"), Required(root, "name"));

            foreach (var item in root.Element("families")?.Elements("family") ?? Enumerable.Empty<XElement>())
            {
                if (!Enum.TryParse(Required(item, "category"), true, out ElementCategory category)) throw new InvalidDataException("Invalid template family category.");
                var family = new ProjectFamily(Required(item, "id"), Required(item, "name"), category);
                foreach (var property in item.Element("properties")?.Elements("p") ?? Enumerable.Empty<XElement>()) family.Properties[Required(property, "name")] = Value(property, "value");
                profile.Families.Add(family);
            }
            foreach (var item in root.Element("rules")?.Elements("rule") ?? Enumerable.Empty<XElement>())
            {
                if (!Enum.TryParse(Required(item, "category"), true, out ElementCategory category)) throw new InvalidDataException("Invalid template rule category.");
                profile.QuantityRules.Add(new QuantityRule(Required(item, "id"), category, Required(item, "output"), Required(item, "expression"), Required(item, "version")));
            }
            foreach (var item in root.Element("layerMappings")?.Elements("map") ?? Enumerable.Empty<XElement>()) profile.LayerMappings[Required(item, "pattern")] = Required(item, "category");
            foreach (var item in root.Element("bqColumns")?.Elements("column") ?? Enumerable.Empty<XElement>()) profile.VisibleBqColumns.Add(Required(item, "name"));
            Validate(profile);
            return profile;
        }

        private static XDocument Serialize(TemplateProfile profile) => new XDocument(
            new XElement("qs3dTemplate",
                new XAttribute("schema", "1"),
                new XAttribute("id", profile.Id),
                new XAttribute("name", profile.Name),
                new XElement("families", profile.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("family",
                    new XAttribute("id", x.Id), new XAttribute("name", x.Name), new XAttribute("category", x.Category),
                    new XElement("properties", x.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => new XElement("p", new XAttribute("name", p.Key), new XAttribute("value", p.Value ?? string.Empty))))))),
                new XElement("rules", profile.QuantityRules.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("rule",
                    new XAttribute("id", x.Id), new XAttribute("category", x.Category), new XAttribute("output", x.OutputName), new XAttribute("expression", x.Expression), new XAttribute("version", x.Version)))),
                new XElement("layerMappings", profile.LayerMappings.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("map", new XAttribute("pattern", x.Key), new XAttribute("category", x.Value)))),
                new XElement("bqColumns", profile.VisibleBqColumns.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("column", new XAttribute("name", x))))));

        private static XDocument LoadDocument(string path)
        {
            var full = Path.GetFullPath(path);
            var info = new FileInfo(full);
            if (info.Length > MaxTemplateFileBytes) throw new InvalidDataException("QS3D template exceeds 8 MiB.");
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxTemplateFileBytes };
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = XmlReader.Create(stream, settings)) return XDocument.Load(reader, LoadOptions.None);
        }

        private static void Validate(TemplateProfile profile)
        {
            var duplicateFamily = profile.Families.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateFamily != null) throw new InvalidDataException("Duplicate template family id: " + duplicateFamily.Key);
            var duplicateRule = profile.QuantityRules.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateRule != null) throw new InvalidDataException("Duplicate template rule id: " + duplicateRule.Key);
            var duplicateOutput = profile.QuantityRules.GroupBy(x => x.Category + "\u001f" + x.OutputName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateOutput != null) throw new InvalidDataException("Template contains multiple rules for the same category/output.");
            foreach (var mapping in profile.LayerMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Key)) throw new InvalidDataException("Template layer mapping pattern is empty.");
                if (!Enum.TryParse(mapping.Value, true, out ElementCategory _)) throw new InvalidDataException("Invalid template layer mapping category: " + mapping.Value);
            }
        }

        private static IEnumerable<string> SplitColumns(string value) => (value ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);
        private static bool SameMap(IDictionary<string, string> left, IDictionary<string, string> right) => left.Count == right.Count && left.All(x => right.TryGetValue(x.Key, out var value) && string.Equals(x.Value ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal));
        private static string Required(XElement element, string name) => !string.IsNullOrWhiteSpace(element.Attribute(name)?.Value) ? element.Attribute(name)!.Value.Trim() : throw new InvalidDataException("Missing attribute: " + name);
        private static string Value(XElement element, string name) => element.Attribute(name)?.Value ?? string.Empty;
    }
}
