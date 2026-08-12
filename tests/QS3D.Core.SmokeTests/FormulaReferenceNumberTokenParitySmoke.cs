using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Formulas;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaReferenceNumberTokenParitySmoke
    {
        public static void Run()
        {
            ReferenceScannerRejectsMalformedExponent();
            RuleSchedulerDoesNotMaskMalformedExponentAsCycle();
            ValidExponentStillFindsAndEvaluatesVariables();
        }

        private static void ReferenceScannerRejectsMalformedExponent()
        {
            var evaluator = new ExpressionEvaluator();
            var error = Throws<InvalidOperationException>(() => evaluator.GetReferencedVariables("1eFoo"));
            if (error.Message.IndexOf("Invalid number '1e'", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Formula reference scanner did not report the malformed exponent token.");
        }

        private static void RuleSchedulerDoesNotMaskMalformedExponentAsCycle()
        {
            var project = new ProjectState("P-FORMULA-TOKEN", "Formula token parity");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "Foo", "1eFoo", "1"));

            var error = Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));
            if (error.Message.IndexOf("Invalid number '1e'", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Malformed exponent syntax was not surfaced through quantity-rule scheduling.");
            if (error.Message.IndexOf("Circular quantity rule dependency", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Malformed exponent syntax was incorrectly reported as a circular quantity-rule dependency.");
            if (element.Quantities.ContainsKey("Foo"))
                throw new InvalidOperationException("Malformed formula scheduling mutated the managed output quantity.");
            if (element.Properties.ContainsKey("Rule:Foo"))
                throw new InvalidOperationException("Malformed formula scheduling mutated managed output provenance.");
        }

        private static void ValidExponentStillFindsAndEvaluatesVariables()
        {
            var evaluator = new ExpressionEvaluator();
            var references = evaluator.GetReferencedVariables("1e2 + Foo");
            if (references.Count != 1 || !string.Equals(references.Single(), "Foo", StringComparison.Ordinal))
                throw new InvalidOperationException("Valid exponent notation changed formula variable extraction.");

            var value = evaluator.Evaluate(
                "1e2 + Foo",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["Foo"] = 3d });
            if (Math.Abs(value - 103d) > 1e-12d)
                throw new InvalidOperationException("Valid exponent notation changed formula evaluation.");
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

    internal static class FormulaReferenceNumberTokenParitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FormulaReferenceNumberTokenParitySmoke.Run();
        }
    }
}
