using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Formulas;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaReferenceGrammarParitySmoke
    {
        public static void Run()
        {
            ReferenceDiscoveryRejectsIncompleteExpression();
            RuleSchedulerDoesNotMaskIncompleteSyntaxAsCycle();
            ReferenceDiscoveryDoesNotExecuteRuntimeArithmetic();
            ReferenceDiscoveryValidatesFunctionShape();
        }

        private static void ReferenceDiscoveryRejectsIncompleteExpression()
        {
            var evaluator = new ExpressionEvaluator();
            var error = Throws<InvalidOperationException>(() => evaluator.GetReferencedVariables("A +"));
            if (error.Message.IndexOf("Expected a number, variable, function, or parenthesized expression", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Formula reference discovery did not surface the evaluator grammar error for an incomplete expression.");
        }

        private static void RuleSchedulerDoesNotMaskIncompleteSyntaxAsCycle()
        {
            var project = new ProjectState("P-FORMULA-GRAMMAR", "Formula grammar parity");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "A", "B +", "1"));
            project.QuantityRules.Add(new QuantityRule("R2", ElementCategory.Beam, "B", "A +", "1"));

            var error = Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));
            if (error.Message.IndexOf("Expected a number, variable, function, or parenthesized expression", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Malformed quantity-rule syntax was not surfaced before dependency scheduling.");
            if (error.Message.IndexOf("Circular quantity rule dependency", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Malformed quantity-rule syntax was incorrectly reported as a circular dependency.");
            if (element.Quantities.ContainsKey("A") || element.Quantities.ContainsKey("B"))
                throw new InvalidOperationException("Malformed formula scheduling mutated managed output quantities.");
            if (element.Properties.ContainsKey("Rule:A") || element.Properties.ContainsKey("Rule:B"))
                throw new InvalidOperationException("Malformed formula scheduling mutated managed output provenance.");
        }

        private static void ReferenceDiscoveryDoesNotExecuteRuntimeArithmetic()
        {
            var evaluator = new ExpressionEvaluator();
            var references = evaluator.GetReferencedVariables("A / 0");
            if (references.Count != 1 || !string.Equals(references.Single(), "A", StringComparison.Ordinal))
                throw new InvalidOperationException("Reference discovery changed dependency extraction while skipping runtime arithmetic.");

            var error = Throws<InvalidOperationException>(() => evaluator.Evaluate("A / 0", new System.Collections.Generic.Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1d }));
            if (error.Message.IndexOf("Division by zero", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Formula evaluation no longer enforces runtime division-by-zero safety.");
        }

        private static void ReferenceDiscoveryValidatesFunctionShape()
        {
            var evaluator = new ExpressionEvaluator();
            var error = Throws<InvalidOperationException>(() => evaluator.GetReferencedVariables("abs(A, B)"));
            if (error.Message.IndexOf("abs expects 1 argument", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Reference discovery did not validate the evaluator function contract.");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class FormulaReferenceGrammarParitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FormulaReferenceGrammarParitySmoke.Run();
        }
    }
}
