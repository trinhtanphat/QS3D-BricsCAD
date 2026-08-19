using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Progress;

namespace QS3D.Core.SmokeTests
{
    internal static class ClaimSnapshotAdditivePrecisionSmoke
    {
        private const decimal Tiny = 0.0000000000000000000000000001m;
        private const decimal Huge = 10000000000000000000000000000m;

        private static readonly MethodInfo AddPreservingContribution =
            typeof(ClaimSnapshot).GetMethod(
                "AddPreservingContribution",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "ClaimSnapshot.AddPreservingContribution is unavailable.");

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            Equal(3.75m, Add(1.25m, 2.5m), "honest addition");
            Equal(2m, Add(0m, 2m), "zero accumulated value");
            Equal(2m, Add(2m, 0m), "zero incoming contribution");

            ExpectOverflow(
                () => Add(Huge, Tiny),
                "lost a non-zero contribution",
                "incoming contribution precision loss");
            ExpectOverflow(
                () => Add(Tiny, Huge),
                "lost a non-zero accumulated contribution",
                "accumulated contribution precision loss");
            ExpectOverflow(
                () => Add(decimal.MaxValue, 1m),
                "overflowed decimal arithmetic",
                "decimal overflow");
        }

        private static decimal Add(decimal left, decimal right)
        {
            try
            {
                var result = AddPreservingContribution.Invoke(
                    null,
                    new object[] { left, right, "claim snapshot gross value" });
                if (result is decimal value)
                    return value;
                throw new InvalidOperationException(
                    "ClaimSnapshot.AddPreservingContribution returned an unexpected value.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is OverflowException overflow)
            {
                throw overflow;
            }
        }

        private static void ExpectOverflow(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(
                        label + ": unexpected error message: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ": expected OverflowException.");
        }

        private static void Equal(decimal expected, decimal actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
