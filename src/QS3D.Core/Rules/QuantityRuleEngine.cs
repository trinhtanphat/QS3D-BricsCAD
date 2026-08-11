using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Formulas;

namespace QS3D.Core.Rules
{
    public sealed class QuantityRule
    {
        public QuantityRule(string id, ElementCategory category, string outputName, string expression, string version)
        {
            Id = Required(id, nameof(id));
            if (!Enum.IsDefined(typeof(ElementCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Quantity rule category must be a defined ElementCategory.");
            Category = category;
            OutputName = Required(outputName, nameof(outputName));
            Expression = Required(expression, nameof(expression));
            Version = Required(version, nameof(version));
        }

        public string Id { get; }
        public ElementCategory Category { get; }
        public string OutputName { get; }
        public string Expression { get; }
        public string Version { get; }

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
            element.Properties[ProvenancePrefix + rule.OutputName] = rule.Id + "@" + rule.Version;
        }

        public int ApplyMatching(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!ReferenceEquals(project.FindElement(element.Id), element))
                throw new InvalidOperationException("Quantity rule matching requires the canonical project-owned element instance.");

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

            var variables = BuildVariables(project, element);
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
                element.Properties[ProvenancePrefix + item.Key.OutputName] = item.Key.Id + "@" + item.Key.Version;
            }
            return rules.Count + staleOutputs.Count;
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
            foreach (var key in element.Properties.Keys.Where(x => x.StartsWith(ProvenancePrefix, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                var output = key.Substring(ProvenancePrefix.Length).Trim();
                if (output.Length == 0 || activeOutputs.Contains(output)) continue;
                if (!result.Contains(output, StringComparer.OrdinalIgnoreCase)) result.Add(output);
            }
            return result;
        }

        private static void CleanupStaleOutputs(ProjectElement element, IEnumerable<string> staleOutputs)
        {
            foreach (var output in staleOutputs)
            {
                element.Quantities.Remove(output);
                element.Properties.Remove(ProvenancePrefix + output);
            }
        }

        private static Dictionary<string, double> BuildVariables(ProjectState project, ProjectElement element)
        {
            var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var family = project.FindFamily(element.FamilyId);
            if (family != null) AddNumeric(family.Properties, variables);
            AddNumeric(element.Properties, variables);
            foreach (var quantity in element.Quantities)
            {
                if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value)) throw new InvalidOperationException("Rule variable quantity is not finite: " + element.Id + "/" + quantity.Key);
                AddVariable(variables, quantity.Key, quantity.Value);
            }
            if (!variables.ContainsKey("Count")) variables["Count"] = 1d;
            return variables;
        }

        private static void AddNumeric(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, double> target)
        {
            foreach (var item in source)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;
                if (double.TryParse(item.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && !double.IsNaN(value) && !double.IsInfinity(value))
                    AddVariable(target, item.Key, value);
            }
        }

        private static void AddVariable(IDictionary<string, double> target, string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            target[name.Trim()] = value;
        }
    }
}
