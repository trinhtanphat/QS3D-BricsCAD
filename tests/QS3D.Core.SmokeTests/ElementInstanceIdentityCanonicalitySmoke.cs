using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceIdentityCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var family = new FamilyDefinition("Identity Smoke", ElementCategory.Beam);
            var canonical = new ElementInstance("E1", family, "Tầng 1");
            Equal("E1", canonical.Id, "canonical id");
            Equal("Tầng 1", canonical.Floor, "unrelated floor behavior");

            ExpectArgument(() => new ElementInstance(" E1", family, "Tầng 1"), "leading space");
            ExpectArgument(() => new ElementInstance("E1 ", family, "Tầng 1"), "trailing space");
            ExpectArgument(() => new ElementInstance("\tE1", family, "Tầng 1"), "leading tab");
            ExpectArgument(() => new ElementInstance("E1\r", family, "Tầng 1"), "trailing carriage return");
            ExpectArgument(() => new ElementInstance("E1\n", family, "Tầng 1"), "trailing newline");
            ExpectArgument(() => new ElementInstance("E\t1", family, "Tầng 1"), "embedded tab");
            ExpectArgument(() => new ElementInstance("E\n1", family, "Tầng 1"), "embedded newline");
            ExpectArgument(() => new ElementInstance("E\u00011", family, "Tầng 1"), "embedded control character");
        }

        private static void ExpectArgument(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected ArgumentException.");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    label + ": expected '" + expected + "', actual '" + actual + "'.");
        }
    }
}
