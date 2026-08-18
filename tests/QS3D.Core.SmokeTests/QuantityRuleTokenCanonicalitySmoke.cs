using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleTokenCanonicalitySmoke
    {
        internal static void Run()
        {
            var canonical = new QuantityRule("rule-1", ElementCategory.ArchitecturalWall, "NetVolumeM3", " 1 + 2 ", "v1");
            Equal("rule-1", canonical.Id, "Id");
            Equal("NetVolumeM3", canonical.OutputName, "OutputName");
            Equal("v1", canonical.Version, "Version");
            Equal("1 + 2", canonical.Expression, "Expression");

            ExpectArgument("id", () => new QuantityRule(" rule-1", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", "v1"));
            ExpectArgument("id", () => new QuantityRule("rule-1 ", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", "v1"));
            ExpectArgument("id", () => new QuantityRule("\trule-1", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", "v1"));
            ExpectArgument("outputName", () => new QuantityRule("rule-1", ElementCategory.ArchitecturalWall, "NetVolumeM3\n", "1", "v1"));
            ExpectArgument("version", () => new QuantityRule("rule-1", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", " v1 "));
        }

        private static void ExpectArgument(string parameterName, Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, parameterName, StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity-rule canonicality smoke received the wrong parameter name.");
                return;
            }

            throw new InvalidOperationException("Quantity-rule canonicality smoke expected ArgumentException for " + parameterName + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Quantity-rule canonicality smoke mismatch for " + label + ".");
        }
    }
}
