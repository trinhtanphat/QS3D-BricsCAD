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
        private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();

        public void Apply(ProjectElement element, QuantityRule rule, IReadOnlyDictionary<string, double> variables)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (variables == null) throw new ArgumentNullException(nameof(variables));
            if (element.Category != rule.Category) return;

            var result = _evaluator.Evaluate(rule.Expression, variables);
            element.SetQuantity(rule.OutputName, result);
            element.Properties["Rule:" + rule.OutputName] = rule.Id + "@" + rule.Version;
        }

        public int ApplyMatching(ProjectState project, ProjectElement element)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (element == null) throw new ArgumentNullException(nameof(element));

            var rules = project.QuantityRules
                .Where(x => x.Category == element.Category)
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (rules.Count == 0) return 0;

            var variables = BuildVariables(project, element);
            foreach (var rule in rules)
            {
                Apply(element, rule, variables);
                if (element.Quantities.TryGetValue(rule.OutputName, out var value)) variables[rule.OutputName] = value;
            }
            return rules.Count;
        }

        private static Dictionary<string, double> BuildVariables(ProjectState project, ProjectElement element)
        {
            var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var family = project.FindFamily(element.FamilyId);
            if (family != null) AddNumeric(family.Properties, variables);
            AddNumeric(element.Properties, variables);
            foreach (var quantity in element.Quantities) variables[quantity.Key] = quantity.Value;
            if (!variables.ContainsKey("Count")) variables["Count"] = 1d;
            return variables;
        }

        private static void AddNumeric(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, double> target)
        {
            foreach (var item in source)
            {
                if (double.TryParse(item.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && !double.IsNaN(value) && !double.IsInfinity(value))
                    target[item.Key] = value;
            }
        }
    }
}
