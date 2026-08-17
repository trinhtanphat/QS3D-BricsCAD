using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using QS3D.Core.Domain;
using QS3D.Core.Formulas;

namespace QS3D.Core.Rules
{
    public sealed class QuantityRule
    {
        public QuantityRule(string id, ElementCategory category, string outputName, string expression, string version)
        {
            Id = RequiredToken(id, nameof(id));
            if (!Enum.IsDefined(typeof(ElementCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Quantity rule category must be a defined ElementCategory.");
            Category = category;
            OutputName = RequiredToken(outputName, nameof(outputName));
            Expression = RequiredXmlText(expression, nameof(expression));
            Version = RequiredToken(version, nameof(version));
        }

        public string Id { get; }
        public ElementCategory Category { get; }
        public string OutputName { get; }
        public string Expression { get; }
        public string Version { get; }

        private static string RequiredToken(string value, string name)
        {
            var normalized = Required(value, name);
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Value cannot contain control characters.", name);
            try
            {
                XmlConvert.VerifyXmlChars(normalized);
                return normalized;
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Value contains characters that are invalid in XML.", name, ex);
            }
        }

        private static string RequiredXmlText(string value, string name)
        {
            var normalized = Required(value, name);
            try
            {
                XmlConvert.VerifyXmlChars(normalized);
                return normalized;
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Value contains characters that are invalid in XML.", name, ex);
            }
        }

        private static string Required(string value, string name) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
    }

    public sealed class QuantityRuleEngine
    {
        private const string ProvenancePrefix = "Rule:";
        private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();

        public void Apply(ProjectElement element, QuantityRule rule, IReadOnlyDictionary<string, double> variables)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (element.Category != rule.Category) return;

            var result = _evaluator.Evaluate(rule.Expression, variables);
            element.SetQuantity(rule.OutputName, result);
            SetProvenance(element, rule.OutputName, rule.Id + "@" + rule.Version);
        }

        public int ApplyMatching(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!ReferenceEquals(project.FindElement(element.Id), element))
                throw new InvalidOperationException("Quantity rule matching requires the canonical project-owned element instance.");
            ValidateRuleIdentities(project.QuantityRules);
            ValidateFamilyIdentities(project.Families);
            var family = ResolveFamily(project, element);

            var rules = project.QuantityRules
                .Where(x => x.Category == element.Category)
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ValidateOutputs(rules, element.Category);

            var activeOutputs = new HashSet<string>(rules.Select(x => x.OutputName), StringComparer.OrdinalIgnoreCase);
            var staleOutputs = GetStaleManagedOutputs(element, activeOutputs);
            if (rules.Count == 0)
            {
                CleanupStaleOutputs(element, staleOutputs);
                return staleOutputs.Count;
            }

            var variables = BuildVariables(element, family);
            foreach (var output in activeOutputs) variables.Remove(output);
            foreach (var stale in staleOutputs) variables.Remove(stale);

            var references = new Dictionary<QuantityRule, IReadOnlyCollection<string>>();
            foreach (var rule in rules) references[rule] = _evaluator.GetReferencedVariables(rule.Expression);

            var pending = new List<QuantityRule>(rules);
            var resolvedOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var staged = new List<KeyValuePair<QuantityRule, double>>(rules.Count);
            while (pending.Count > 0)
            {
                var progressed = false;
                for (var index = 0; index < pending.Count;)
                {
                    var rule = pending[index];
                    if (WaitsForManagedOutput(rule, references[rule], activeOutputs, resolvedOutputs))
                    {
                        index++;
                        continue;
                    }

                    var value = _evaluator.Evaluate(rule.Expression, variables);
                    staged.Add(new KeyValuePair<QuantityRule, double>(rule, value));
                    variables[rule.OutputName] = value;
                    resolvedOutputs.Add(rule.OutputName);
                    pending.RemoveAt(index);
                    progressed = true;
                }

                if (progressed) continue;
                var unresolved = string.Join(", ", pending.Select(x => x.OutputName).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                throw new InvalidOperationException("Circular quantity rule dependency for " + element.Category + ": " + unresolved + ".");
            }

            CleanupStaleOutputs(element, staleOutputs);
            foreach (var item in staged)
            {
                element.SetQuantity(item.Key.OutputName, item.Value);
                SetProvenance(element, item.Key.OutputName, item.Key.Id + "@" + item.Key.Version);
            }
            return rules.Count + staleOutputs.Count;
        }

        private static void ValidateRuleIdentities(IEnumerable<QuantityRule> rules)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in rules)
            {
                if (rule == null)
                    throw new InvalidOperationException("Project quantity rule collection contains a null rule.");
                if (!seenIds.Add(rule.Id))
                    throw new InvalidOperationException("Project contains duplicate quantity rule id: " + rule.Id);
            }
        }

        private static void ValidateFamilyIdentities(IEnumerable<ProjectFamily> families)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project family collection contains a null family.");
                var id = family.Id ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Project family collection contains a blank or non-canonical family id.");
                if (!seenIds.Add(id))
                    throw new InvalidOperationException("Project contains duplicate family id: " + id + ".");
            }
        }

        private static ProjectFamily? ResolveFamily(ProjectState project, ProjectElement element)
        {
            var rawFamilyId = element.FamilyId ?? string.Empty;
            var familyId = rawFamilyId.Trim();
            if (familyId.Length == 0) return null;
            if (!string.Equals(rawFamilyId, familyId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Quantity rule target element contains a non-canonical family id: " + element.Id + "/" + rawFamilyId + ".");
            var family = project.FindFamily(familyId)
                ?? throw new InvalidOperationException("Quantity rule target element references missing family id: " + element.Id + "/" + familyId + ".");
            if (family.Category != element.Category)
                throw new InvalidOperationException("Quantity rule target element/family category mismatch: " + element.Id + "/" + family.Id + ".");
            return family;
        }

        private static bool WaitsForManagedOutput(QuantityRule rule, IEnumerable<string> references, ISet<string> activeOutputs, ISet<string> resolvedOutputs)
        {
            foreach (var reference in references)
            {
                if (!activeOutputs.Contains(reference)) continue;
                if (string.Equals(reference, rule.OutputName, StringComparison.OrdinalIgnoreCase)) return true;
                if (!resolvedOutputs.Contains(reference)) return true;
            }
            return false;
        }

        private static void ValidateOutputs(IReadOnlyList<QuantityRule> rules, ElementCategory category)
        {
            var duplicate = rules.GroupBy(x => x.OutputName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null) throw new InvalidOperationException("Multiple quantity rules target " + category + "/" + duplicate.Key + ".");
        }

        private static List<string> GetStaleManagedOutputs(ProjectElement element, ISet<string> activeOutputs)
        {
            var result = new List<string>();
            foreach (var key in element.Properties.Keys.ToArray())
            {
                var canonicalKey = key.Trim();
                if (!canonicalKey.StartsWith(ProvenancePrefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(key, canonicalKey, StringComparison.Ordinal))
                    throw new InvalidOperationException("Element " + element.Id + " contains malformed quantity-rule provenance key: " + key + ".");
                var output = key.Substring(ProvenancePrefix.Length);
                if (string.IsNullOrWhiteSpace(output) || !string.Equals(output, output.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Element " + element.Id + " contains malformed quantity-rule provenance key: " + key + ".");
                if (activeOutputs.Contains(output)) continue;
                if (!result.Contains(output, StringComparer.OrdinalIgnoreCase)) result.Add(output);
            }
            return result;
        }

        private static void SetProvenance(ProjectElement element, string output, string provenance)
        {
            var key = ProvenancePrefix + output;
            if (element.Properties.TryGetValue(key, out var existing) && string.Equals(existing, provenance, StringComparison.Ordinal)) return;
            element.SetProperty(key, provenance);
        }

        private static void CleanupStaleOutputs(ProjectElement element, IEnumerable<string> staleOutputs)
        {
            foreach (var output in staleOutputs)
            {
                var quantityRemoved = element.Quantities.Remove(output);
                var provenanceRemoved = element.RemoveProperty(ProvenancePrefix + output);
                if (quantityRemoved && !provenanceRemoved)
                    element.MarkDirty(ElementDirtyFlags.Quantity);
            }
        }

        private static Dictionary<string, double> BuildVariables(ProjectElement element, ProjectFamily? family)
        {
            var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (family != null) AddNumeric(family.Properties, variables);
            AddNumeric(element.Properties, variables);
            foreach (var quantity in element.Quantities)
            {
                var quantityName = quantity.Key ?? string.Empty;
                if (string.IsNullOrWhiteSpace(quantityName) || !string.Equals(quantityName, quantityName.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Rule variable quantity name is blank or non-canonical: " + element.Id + "/" + quantityName);
                if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value)) throw new InvalidOperationException("Rule variable quantity is not finite: " + element.Id + "/" + quantityName);
                AddVariable(variables, quantityName, quantity.Value);
            }
            if (!variables.ContainsKey("Count")) variables["Count"] = 1d;
            return variables;
        }

        private static void AddNumeric(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, double> target)
        {
            var normalizedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;
                if (double.TryParse(item.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && !double.IsNaN(value) && !double.IsInfinity(value))
                {
                    var normalizedName = item.Key.Trim();
                    if (!string.Equals(item.Key, normalizedName, StringComparison.Ordinal))
                        throw new InvalidOperationException("Rule variable numeric property name is non-canonical: " + item.Key + ".");
                    if (!normalizedNames.Add(normalizedName))
                        throw new InvalidOperationException("Rule variable property name conflicts after normalization: " + item.Key + ".");
                    AddVariable(target, normalizedName, value);
                }
            }
        }

        private static void AddVariable(IDictionary<string, double> target, string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            target[name.Trim()] = value;
        }
    }
}
