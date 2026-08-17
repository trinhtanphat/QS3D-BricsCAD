using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Recognition;
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

        private sealed class BoundedMemoryStream : MemoryStream
        {
            private readonly long _maxBytes;

            public BoundedMemoryStream(long maxBytes)
            {
                if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
                _maxBytes = maxBytes;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                EnsureWritable(count);
                base.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                EnsureWritable(1);
                base.WriteByte(value);
            }

            private void EnsureWritable(int count)
            {
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (Position > _maxBytes - count)
                    throw new InvalidDataException("QS3D template exceeds 8 MiB.");
            }
        }

        private sealed class FamilyApplyPlan
        {
            public FamilyApplyPlan(ProjectFamily source, ProjectFamily? existing)
            {
                Source = source;
                Existing = existing;
                PreviousProperties = existing == null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(existing.Properties, StringComparer.OrdinalIgnoreCase);
                Changed = existing == null ||
                          !string.Equals(existing.Name, source.Name, StringComparison.Ordinal) ||
                          existing.Category != source.Category ||
                          !SameMap(existing.Properties, source.Properties);
            }

            public ProjectFamily Source { get; }
            public ProjectFamily? Existing { get; }
            public IDictionary<string, string> PreviousProperties { get; }
            public bool Changed { get; }
        }

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
            var layerMappings = project.Metadata
                .Where(x => x.Key.StartsWith(LayerMappingPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => new KeyValuePair<string, string>(x.Key.Substring(LayerMappingPrefix.Length), x.Value))
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ProjectRecognitionService.ValidateLayerMappings(layerMappings, "Project recognition mappings");
            foreach (var item in layerMappings)
            {
                var pattern = item.Key.Trim();
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
            var familyPlans = ValidateApply(project, profile);
            var rollback = ProjectStateSnapshot.Capture(project);

            try
            {
                var result = new TemplateApplyResult();
                var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var changedCategories = new HashSet<ElementCategory>();

                foreach (var plan in familyPlans)
                {
                    if (!plan.Changed) continue;
                    var target = plan.Existing;
                    if (target == null)
                    {
                        target = new ProjectFamily(plan.Source.Id, plan.Source.Name, plan.Source.Category);
                        foreach (var property in plan.Source.Properties) target.Properties[property.Key] = property.Value;
                        project.Families.Add(target);
                        result.FamiliesAdded++;
                    }
                    else
                    {
                        target.Name = plan.Source.Name;
                        target.Category = plan.Source.Category;
                        target.Properties.Clear();
                        foreach (var property in plan.Source.Properties) target.Properties[property.Key] = property.Value;
                        result.FamiliesUpdated++;
                    }

                    PropagateFamilyDefaults(project, plan.Source, plan.PreviousProperties, affected);
                }

                foreach (var source in profile.QuantityRules)
                {
                    var existing = project.FindQuantityRule(source.Id);
                    var same = existing != null && existing.Category == source.Category &&
                               string.Equals(existing.OutputName, source.OutputName, StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(existing.Expression, source.Expression, StringComparison.Ordinal) &&
                               string.Equals(existing.Version, source.Version, StringComparison.Ordinal);
                    if (same) continue;

                    if (existing != null)
                    {
                        changedCategories.Add(existing.Category);
                        project.QuantityRules.Remove(existing);
                        result.RulesUpdated++;
                    }
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
                else project.Metadata.Remove(VisibleBqColumnsKey);

                foreach (var element in project.Elements)
                {
                    if (!changedCategories.Contains(element.Category)) continue;
                    element.MarkDirty(ElementDirtyFlags.Quantity);
                    affected.Add(element.Id);
                }

                result.AffectedElements = affected.Count;
                AuditTrail.ForProject(project).Record("template.apply", string.Empty, profile.Id + " • families +" + result.FamiliesAdded + "/~" + result.FamiliesUpdated + " • rules +" + result.RulesAdded + "/~" + result.RulesUpdated + " • mappings " + result.LayerMappingsApplied);
                return result;
            }
            catch (Exception applyError)
            {
                try
                {
                    rollback.Restore(project);
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException("Template apply failed and project rollback also failed.", applyError, rollbackError);
                }
                throw;
            }
        }

        public void Save(TemplateProfile profile, string path)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Template path is required.", nameof(path));
            Validate(profile);
            EnsureSerializedLowerBoundWithinLimit(profile);
            var full = Path.GetFullPath(path);
            var payload = SerializeBounded(profile);
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = AtomicFileCommit.CreateTempPath(full);
            var backup = full + ".bak";
            try
            {
                File.WriteAllBytes(temp, payload);
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
            TemplateProfileXmlSchemaValidator.Validate(root);
            if (!string.Equals(root.Name.LocalName, "qs3dTemplate", StringComparison.Ordinal)) throw new InvalidDataException("Invalid QS3D template root.");
            var schema = Required(root, "schema");
            if (!string.Equals(schema, "1", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported QS3D template schema: " + schema);
            var profile = new TemplateProfile(Required(root, "id"), Required(root, "name"));

            foreach (var item in root.Element("families")?.Elements("family") ?? Enumerable.Empty<XElement>())
            {
                var category = RequiredCanonicalCategory(item, "family");
                var family = new ProjectFamily(Required(item, "id"), Required(item, "name"), category);
                var propertyNames = new List<string>();
                foreach (var property in item.Element("properties")?.Elements("p") ?? Enumerable.Empty<XElement>())
                {
                    var propertyName = Required(property, "name");
                    if (family.Properties.ContainsKey(propertyName)) throw new InvalidDataException("Duplicate template family property: " + family.Id + "/" + propertyName);
                    family.Properties[propertyName] = Value(property, "value");
                    propertyNames.Add(propertyName);
                }
                RequireCanonicalOrder(propertyNames, "family properties for " + family.Id);
                profile.Families.Add(family);
            }
            RequireCanonicalOrder(profile.Families.Select(x => x.Id), "families");

            foreach (var item in root.Element("rules")?.Elements("rule") ?? Enumerable.Empty<XElement>())
            {
                var category = RequiredCanonicalCategory(item, "rule");
                profile.QuantityRules.Add(new QuantityRule(Required(item, "id"), category, Required(item, "output"), Required(item, "expression"), Required(item, "version")));
            }
            RequireCanonicalOrder(profile.QuantityRules.Select(x => x.Id), "quantity rules");

            var mappingPatterns = new List<string>();
            foreach (var item in root.Element("layerMappings")?.Elements("map") ?? Enumerable.Empty<XElement>())
            {
                var pattern = RequiredCanonicalLayerMappingPattern(item);
                if (profile.LayerMappings.ContainsKey(pattern)) throw new InvalidDataException("Duplicate template layer mapping: " + pattern);
                profile.LayerMappings.Add(pattern, RequiredCanonicalLayerMappingCategory(item));
                mappingPatterns.Add(pattern);
            }
            RequireCanonicalOrder(mappingPatterns, "layer mappings");

            foreach (var column in ReadCanonicalBqColumns(root.Element("bqColumns"))) profile.VisibleBqColumns.Add(column);
            Validate(profile);
            return profile;
        }

        private static IReadOnlyList<FamilyApplyPlan> ValidateApply(ProjectState project, TemplateProfile profile)
        {
            var duplicateProjectFamily = project.Families.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateProjectFamily != null) throw new InvalidOperationException("Project contains duplicate family id: " + duplicateProjectFamily.Key);
            var duplicateProjectRule = project.QuantityRules.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateProjectRule != null) throw new InvalidOperationException("Project contains duplicate quantity rule id: " + duplicateProjectRule.Key);

            var plans = new List<FamilyApplyPlan>(profile.Families.Count);
            foreach (var source in profile.Families)
            {
                var existing = project.FindFamily(source.Id);
                if (existing != null && existing.Category != source.Category && project.Elements.Any(x => string.Equals(x.FamilyId, existing.Id, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Template cannot change category of in-use family " + existing.Id + ".");
                plans.Add(new FamilyApplyPlan(source, existing));
            }

            var projectedRules = new Dictionary<string, QuantityRule>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in project.QuantityRules) projectedRules.Add(rule.Id, rule);
            foreach (var source in profile.QuantityRules) projectedRules[source.Id] = source;
            var duplicateOutput = projectedRules.Values.GroupBy(x => x.Category + "\u001f" + x.OutputName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateOutput != null) throw new InvalidOperationException("Template would create multiple project rules for the same category/output: " + duplicateOutput.Key);

            var projectMappings = project.Metadata
                .Where(x => x.Key.StartsWith(LayerMappingPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => new KeyValuePair<string, string>(x.Key.Substring(LayerMappingPrefix.Length), x.Value))
                .ToList();
            ProjectRecognitionService.ValidateLayerMappings(projectMappings, "Project recognition mappings");

            var projectedMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in projectMappings)
            {
                var pattern = item.Key.Trim();
                if (pattern.Length > 0) projectedMappings[pattern] = item.Value;
            }
            foreach (var mapping in profile.LayerMappings) projectedMappings[mapping.Key.Trim()] = mapping.Value;
            ProjectRecognitionService.ValidateLayerMappings(projectedMappings, "Projected project recognition mappings");
            return plans;
        }

        private static void PropagateFamilyDefaults(ProjectState project, ProjectFamily source, IDictionary<string, string> previousProperties, ISet<string> affected)
        {
            foreach (var element in project.Elements.Where(x => string.Equals(x.FamilyId, source.Id, StringComparison.OrdinalIgnoreCase)))
            {
                var changed = false;
                var geometryChanged = false;
                foreach (var oldProperty in previousProperties)
                {
                    if (source.Properties.ContainsKey(oldProperty.Key)) continue;
                    if (!element.Properties.TryGetValue(oldProperty.Key, out var current) || !string.Equals(current, oldProperty.Value ?? string.Empty, StringComparison.Ordinal)) continue;
                    element.Properties.Remove(oldProperty.Key);
                    changed = true;
                    if (ElementGeometryPolicy.AffectsGeneratedGeometry(element.Category, oldProperty.Key)) geometryChanged = true;
                }

                foreach (var property in source.Properties)
                {
                    var hasCurrent = element.Properties.TryGetValue(property.Key, out var current);
                    var inherited = previousProperties.TryGetValue(property.Key, out var previous) && hasCurrent && string.Equals(current, previous ?? string.Empty, StringComparison.Ordinal);
                    if (hasCurrent && !inherited) continue;
                    var next = property.Value ?? string.Empty;
                    if (hasCurrent && string.Equals(current, next, StringComparison.Ordinal)) continue;
                    element.Properties[property.Key] = next;
                    changed = true;
                    if (ElementGeometryPolicy.AffectsGeneratedGeometry(element.Category, property.Key)) geometryChanged = true;
                }

                if (!changed) continue;
                var dirty = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
                if (geometryChanged) dirty |= ElementDirtyFlags.Geometry;
                element.MarkDirty(dirty);
                affected.Add(element.Id);
            }
        }

        private static byte[] SerializeBounded(TemplateProfile profile)
        {
            try
            {
                using (var stream = new BoundedMemoryStream(MaxTemplateFileBytes))
                {
                    Serialize(profile).Save(stream, SaveOptions.DisableFormatting);
                    return stream.ToArray();
                }
            }
            catch (InvalidDataException) { throw; }
            catch (XmlException ex)
            {
                throw new InvalidDataException("Template contains characters that are invalid in XML.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException("Template contains data that cannot be represented as XML.", ex);
            }
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

        private static void EnsureSerializedLowerBoundWithinLimit(TemplateProfile profile)
        {
            long estimate = 128;
            AddEstimatedBytes(ref estimate, profile.Id, 8);
            AddEstimatedBytes(ref estimate, profile.Name, 8);
            foreach (var family in profile.Families)
            {
                AddEstimatedBytes(ref estimate, family.Id, 32);
                AddEstimatedBytes(ref estimate, family.Name, 16);
                AddEstimatedBytes(ref estimate, family.Category.ToString(), 16);
                foreach (var property in family.Properties)
                {
                    AddEstimatedBytes(ref estimate, property.Key, 16);
                    AddEstimatedBytes(ref estimate, property.Value ?? string.Empty, 16);
                }
            }
            foreach (var rule in profile.QuantityRules)
            {
                AddEstimatedBytes(ref estimate, rule.Id, 32);
                AddEstimatedBytes(ref estimate, rule.Category.ToString(), 16);
                AddEstimatedBytes(ref estimate, rule.OutputName, 16);
                AddEstimatedBytes(ref estimate, rule.Expression, 16);
                AddEstimatedBytes(ref estimate, rule.Version, 16);
            }
            foreach (var mapping in profile.LayerMappings)
            {
                AddEstimatedBytes(ref estimate, mapping.Key, 24);
                AddEstimatedBytes(ref estimate, mapping.Value, 16);
            }
            foreach (var column in profile.VisibleBqColumns.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
                AddEstimatedBytes(ref estimate, column, 16);
        }

        private static void AddEstimatedBytes(ref long estimate, string value, int markupBytes)
        {
            if (estimate > MaxTemplateFileBytes - markupBytes)
                throw new InvalidDataException("QS3D template exceeds 8 MiB.");
            estimate += markupBytes;
            var textBytes = Encoding.UTF8.GetByteCount(value ?? string.Empty);
            if (textBytes > MaxTemplateFileBytes - estimate)
                throw new InvalidDataException("QS3D template exceeds 8 MiB.");
            estimate += textBytes;
        }

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
            if (profile.Families.Any(x => x == null)) throw new InvalidDataException("Template family list cannot contain null entries.");
            if (profile.QuantityRules.Any(x => x == null)) throw new InvalidDataException("Template rule list cannot contain null entries.");
            var duplicateFamily = profile.Families.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateFamily != null) throw new InvalidDataException("Duplicate template family id: " + duplicateFamily.Key);
            foreach (var family in profile.Families)
            {
                foreach (var property in family.Properties)
                {
                    var key = property.Key;
                    if (string.IsNullOrWhiteSpace(key))
                        throw new InvalidDataException("Template family property key cannot be empty: " + family.Id);
                    if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                        throw new InvalidDataException("Template family property key must not contain leading/trailing whitespace: " + family.Id + "/" + key);
                }
            }
            var duplicateRule = profile.QuantityRules.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateRule != null) throw new InvalidDataException("Duplicate template rule id: " + duplicateRule.Key);
            var duplicateOutput = profile.QuantityRules.GroupBy(x => x.Category + "\u001f" + x.OutputName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicateOutput != null) throw new InvalidDataException("Template contains multiple rules for the same category/output.");
            foreach (var mapping in profile.LayerMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Key)) throw new InvalidDataException("Template layer mapping pattern is empty.");
                if (string.IsNullOrWhiteSpace(mapping.Value) ||
                    !Enum.TryParse(mapping.Value, false, out ElementCategory category) ||
                    !Enum.IsDefined(typeof(ElementCategory), category) ||
                    !string.Equals(mapping.Value, category.ToString(), StringComparison.Ordinal))
                    throw new InvalidDataException("Invalid or non-canonical template layer mapping category: " + mapping.Value);
            }
            try { ProjectRecognitionService.ValidateLayerMappings(profile.LayerMappings, "Template layer mappings"); }
            catch (InvalidOperationException ex) { throw new InvalidDataException(ex.Message, ex); }
            ValidateSerializedXmlText(profile);
        }

        private static void ValidateSerializedXmlText(TemplateProfile profile)
        {
            try
            {
                VerifyXmlText(profile.Id);
                VerifyXmlText(profile.Name);
                foreach (var family in profile.Families)
                {
                    VerifyXmlText(family.Id);
                    VerifyXmlText(family.Name);
                    foreach (var property in family.Properties)
                    {
                        VerifyXmlText(property.Key);
                        VerifyXmlText(property.Value ?? string.Empty);
                    }
                }
                foreach (var rule in profile.QuantityRules)
                {
                    VerifyXmlText(rule.Id);
                    VerifyXmlText(rule.OutputName);
                    VerifyXmlText(rule.Expression);
                    VerifyXmlText(rule.Version);
                }
                foreach (var mapping in profile.LayerMappings)
                {
                    VerifyXmlText(mapping.Key);
                    VerifyXmlText(mapping.Value);
                }
                foreach (var column in profile.VisibleBqColumns) VerifyXmlText(column ?? string.Empty);
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException("Template contains characters that are invalid in XML.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException("Template contains data that cannot be represented as XML.", ex);
            }
        }

        private static void VerifyXmlText(string value) => XmlConvert.VerifyXmlChars(value ?? string.Empty);

        private static ElementCategory RequiredCanonicalCategory(XElement element, string label)
        {
            var raw = element.Attribute("category")?.Value;
            if (string.IsNullOrWhiteSpace(raw) ||
                !Enum.TryParse(raw, false, out ElementCategory category) ||
                !Enum.IsDefined(typeof(ElementCategory), category) ||
                !string.Equals(raw, category.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Invalid or non-canonical template " + label + " category.");
            return category;
        }

        private static string RequiredCanonicalLayerMappingPattern(XElement element)
        {
            var raw = element.Attribute("pattern")?.Value;
            if (raw == null || string.IsNullOrWhiteSpace(raw) || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Template layer mapping pattern is empty or non-canonical.");
            return raw;
        }

        private static string RequiredCanonicalLayerMappingCategory(XElement element)
        {
            var raw = element.Attribute("category")?.Value;
            if (string.IsNullOrWhiteSpace(raw) ||
                !Enum.TryParse(raw, false, out ElementCategory category) ||
                !Enum.IsDefined(typeof(ElementCategory), category) ||
                !string.Equals(raw, category.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Invalid or non-canonical template layer mapping category.");
            return category.ToString();
        }

        private static IReadOnlyList<string> ReadCanonicalBqColumns(XElement? container)
        {
            if (container == null) return Array.Empty<string>();
            var values = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in container.Elements("column"))
            {
                var raw = column.Attribute("name")?.Value;
                if (raw == null || string.IsNullOrWhiteSpace(raw) || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new InvalidDataException("Template BQ column name is empty or non-canonical.");
                if (!seen.Add(raw)) throw new InvalidDataException("Duplicate template BQ column: " + raw);
                values.Add(raw);
            }
            var canonical = values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (!values.SequenceEqual(canonical, StringComparer.Ordinal))
                throw new InvalidDataException("Template BQ columns are not in canonical order.");
            return values.AsReadOnly();
        }

        private static void RequireCanonicalOrder(IEnumerable<string> values, string label)
        {
            var actual = values.ToList();
            var canonical = actual.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (!actual.SequenceEqual(canonical, StringComparer.Ordinal))
                throw new InvalidDataException("Template " + label + " are not in canonical order.");
        }

        private static IEnumerable<string> SplitColumns(string value) => (value ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);
        private static bool SameMap(IDictionary<string, string> left, IDictionary<string, string> right) => left.Count == right.Count && left.All(x => right.TryGetValue(x.Key, out var value) && string.Equals(x.Value ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal));
        private static string Required(XElement element, string name)
        {
            var raw = element.Attribute(name)?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidDataException("Missing attribute: " + name);
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Non-canonical attribute with leading or trailing whitespace: " + name);
            return raw;
        }
        private static string Value(XElement element, string name) => element.Attribute(name)?.Value ?? string.Empty;
    }
}
