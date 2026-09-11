using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbInvalidQuantityNameBackupFallbackSmoke
    {
        public static void Run()
        {
            InvalidQuantityNameRecoversValidatedBackup();
            QuantityRuleExpressionPreservesXmlValidControlWhitespace();
        }

        private static void InvalidQuantityNameRecoversValidatedBackup()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "qs3d-invalid-quantity-name-fallback-" + Guid.NewGuid().ToString("N") + ".qsdb");

            try
            {
                var project = new ProjectState("P1", "Invalid quantity name fallback");
                var element = new ProjectElement(
                    "E1",
                    ElementCategory.ArchitecturalWall,
                    string.Empty,
                    string.Empty,
                    string.Empty);
                element.SetQuantity("AreaM2", 0d);
                project.Elements.Add(element);

                var store = new QsdbProjectStore();
                store.Save(project, path);

                element.SetQuantity("AreaM2", 2d);
                store.Save(project, path);
                if (!File.Exists(path + ".bak"))
                    throw new Exception("QSDB backup fixture was not created before invalid quantity-name tampering.");

                var positive = store.Load(path);
                if (positive.Elements.Count != 1 || positive.Elements[0].Quantities["AreaM2"] != 2d)
                    throw new Exception("Positive quantity did not roundtrip before invalid quantity-name tampering.");

                var document = XDocument.Load(path, LoadOptions.None);
                var persistedQuantity = document.Root?.Element("elements")?.Element("element")?.Element("quantities")?.Element("q")
                    ?? throw new Exception("Serialized QSDB quantity fixture was not found for invalid-name tampering.");
                persistedQuantity.SetAttributeValue("name", "Area\tM2");
                document.Save(path, SaveOptions.DisableFormatting);

                var tamperedName = XDocument.Load(path, LoadOptions.None)
                    .Root?.Element("elements")?.Element("element")?.Element("quantities")?.Element("q")?.Attribute("name")?.Value;
                if (!string.Equals(tamperedName, "Area\tM2", StringComparison.Ordinal))
                    throw new Exception("Invalid quantity-name fixture did not preserve the internal XML-permitted control character.");

                var normalizedAtSchemaBoundary = false;
                try
                {
                    store.Load(path);
                }
                catch (InvalidDataException ex)
                {
                    normalizedAtSchemaBoundary =
                        ex.Message.IndexOf("quantity name", StringComparison.OrdinalIgnoreCase) >= 0
                        && ex.Message.IndexOf("control characters", StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (!normalizedAtSchemaBoundary)
                    throw new Exception("Invalid persisted quantity name was not rejected as InvalidDataException at the QSDB schema boundary.");

                var recovered = store.LoadWithBackupFallback(path);
                if (!recovered.RecoveredFromBackup)
                    throw new Exception("Invalid persisted quantity name bypassed QSDB backup recovery.");
                if (!string.Equals(recovered.SourcePath, Path.GetFullPath(path + ".bak"), StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Invalid persisted quantity name recovered from an unexpected QSDB source.");
                if (recovered.Project.Elements.Count != 1 || recovered.Project.Elements[0].Quantities["AreaM2"] != 0d)
                    throw new Exception("Validated QSDB backup quantity did not roundtrip during invalid-name recovery.");
                if (string.IsNullOrWhiteSpace(recovered.PrimaryFailureMessage))
                    throw new Exception("QSDB backup recovery did not retain the primary invalid quantity-name validation failure.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void QuantityRuleExpressionPreservesXmlValidControlWhitespace()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "qs3d-quantity-rule-expression-tab-" + Guid.NewGuid().ToString("N") + ".qsdb");
            const string expression = "LengthM\t+ 1";

            try
            {
                var project = new ProjectState("P2", "Expression compatibility");
                project.QuantityRules.Add(new QuantityRule(
                    "R1",
                    ElementCategory.ArchitecturalWall,
                    "AreaM2",
                    expression,
                    "1"));

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);
                if (loaded.QuantityRules.Count != 1 ||
                    !string.Equals(loaded.QuantityRules[0].Expression, expression, StringComparison.Ordinal))
                {
                    throw new Exception("QSDB identity hardening changed the XML-valid quantity-rule expression contract.");
                }
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }
    }
}
