using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbRuleTextCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesCanonicalRoundTrip();
            RejectsPaddedExpression();
            RejectsPaddedVersion();
        }

        private static void PreservesCanonicalRoundTrip()
        {
            WithFixture(path =>
            {
                var loaded = new QsdbProjectStore().Load(path);
                if (loaded.QuantityRules.Count != 1 ||
                    !string.Equals(loaded.QuantityRules[0].Expression, "LengthM * 2", StringComparison.Ordinal) ||
                    !string.Equals(loaded.QuantityRules[0].Version, "v1", StringComparison.Ordinal))
                    throw new InvalidOperationException("Canonical QSDB QuantityRule text must continue to round-trip unchanged.");
            });
        }

        private static void RejectsPaddedExpression() => RejectPadded("expression");
        private static void RejectsPaddedVersion() => RejectPadded("version");

        private static void RejectPadded(string attributeName)
        {
            WithFixture(path =>
            {
                var document = XDocument.Load(path, LoadOptions.None);
                var attribute = document.Root?.Element("rules")?.Element("rule")?.Attribute(attributeName)
                    ?? throw new InvalidOperationException("Rule text smoke fixture is missing " + attributeName + ".");
                attribute.Value = " " + attribute.Value + " ";
                document.Save(path, SaveOptions.DisableFormatting);
                ExpectInvalidData(path, attributeName);
            });
        }

        private static void WithFixture(Action<string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-rule-text-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("RULE-TEXT-CANON", "Rule text canonicality");
                project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "DoubleLength", "LengthM * 2", "v1"));
                new QsdbProjectStore().SaveNew(project, path);
                assertion(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void ExpectInvalidData(string path, string surface)
        {
            try
            {
                new QsdbProjectStore().Load(path);
                throw new InvalidOperationException("QSDB load must reject a padded QuantityRule " + surface + ".");
            }
            catch (InvalidDataException)
            {
            }
        }
    }
}
