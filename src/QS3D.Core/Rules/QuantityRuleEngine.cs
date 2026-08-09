using System;
using System.Collections.Generic;
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
    }
}
