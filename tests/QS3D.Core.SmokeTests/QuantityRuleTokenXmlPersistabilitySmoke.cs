using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleTokenXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidTokens();
            PreservesSupplementaryUnicodeRoundTrip();
        }

        private static void RejectsXmlInvalidTokens()
        {
            const string invalid = "\uD800";
            ExpectArgument("id", () => new QuantityRule(invalid, ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", "v1"));
            ExpectArgument("outputName", () => new QuantityRule("rule-1", ElementCategory.ArchitecturalWall, invalid, "1", "v1"));
            ExpectArgument("version", () => new QuantityRule("rule-1", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", invalid));

            ExpectArgument("id", () => new QuantityRule("\u0001", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", "v1"));
        }

        private static void PreservesSupplementaryUnicodeRoundTrip()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-rule-token-xml-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                const string emoji = "\U0001F600";
                var project = new ProjectState("quantity-rule-token-xml", "Quantity Rule XML");
                var rule = new QuantityRule(
                    "rule-" + emoji,
                    ElementCategory.ArchitecturalWall,
                    "Output" + emoji,
                    "1",
                    "v" + emoji);
                project.QuantityRules.Add(rule);

                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);

                if (loaded.QuantityRules.Count != 1)
                    throw new InvalidOperationException("QuantityRule XML smoke expected one persisted rule.");
                var loadedRule = loaded.QuantityRules[0];
                Equal(rule.Id, loadedRule.Id, "Id");
                Equal(rule.OutputName, loadedRule.OutputName, "OutputName");
                Equal(rule.Version, loadedRule.Version, "Version");
            }
            finally
            {
                DeleteIfExists(path);
                DeleteIfExists(path + ".bak");
                DeleteIfExists(path + ".tmp");
            }
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
                    throw new InvalidOperationException(
                        "QuantityRule XML smoke expected parameter '" + parameterName + "' but got '" + ex.ParamName + "'.");
                return;
            }

            throw new InvalidOperationException("QuantityRule XML smoke expected ArgumentException for " + parameterName + ".");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "QuantityRule XML round-trip mismatch for " + label + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
