using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaAdditivePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CollectivelySignificantPositiveTermsArePreserved();
            CollectivelySignificantNegativeTermsArePreserved();
            OpposingTinyTermsCancel();
            MultiplicationPrecedenceRemainsStable();
            AdditiveOverflowStillFailsClosed();
            ReferenceDiscoveryStillParsesAdditiveChains();
        }

        private static void CollectivelySignificantPositiveTermsArePreserved()
        {
            var actual = new ExpressionEvaluator().Evaluate("1e16 + 1 + 1");
            Assert(actual.Equals(10000000000000002d), "Formula addition lost collectively significant positive contributions.");
        }

        private static void CollectivelySignificantNegativeTermsArePreserved()
        {
            var actual = new ExpressionEvaluator().Evaluate("1e16 - 1 - 1");
            Assert(actual.Equals(9999999999999998d), "Formula subtraction lost collectively significant negative contributions.");
        }

        private static void OpposingTinyTermsCancel()
        {
            var actual = new ExpressionEvaluator().Evaluate("1e16 + 1 - 1");
            Assert(actual.Equals(1e16d), "Opposing sub-ULP formula contributions no longer cancel to the original value.");
        }

        private static void MultiplicationPrecedenceRemainsStable()
        {
            var actual = new ExpressionEvaluator().Evaluate("2 + 3 * 4");
            Assert(actual.Equals(14d), "Compensated additive parsing changed multiplication precedence.");
        }

        private static void AdditiveOverflowStillFailsClosed()
        {
            Capture<InvalidOperationException>(() => new ExpressionEvaluator().Evaluate("1e308 + 1e308"));
        }

        private static void ReferenceDiscoveryStillParsesAdditiveChains()
        {
            var references = new ExpressionEvaluator().GetReferencedVariables("large + small + small - correction").ToArray();
            Assert(references.Length == 3, "Formula reference discovery changed its distinct additive-chain reference count.");
            Assert(references[0] == "large" && references[1] == "small" && references[2] == "correction",
                "Formula reference discovery changed first-seen additive-chain ordering or de-duplication.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
