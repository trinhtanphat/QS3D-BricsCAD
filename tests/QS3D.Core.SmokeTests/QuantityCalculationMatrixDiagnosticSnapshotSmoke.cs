using System;
using System.IO;
using System.Text;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationMatrixDiagnosticSnapshotSmoke
    {
        public static void Run()
        {
            SnapshotPreservesExactDirectedDiagnostics();
            JsonExportIsPortableAndSanitized();
            SnapshotCreationDoesNotMutateCaller();
        }

        private static void SnapshotPreservesExactDirectedDiagnostics()
        {
            var settings = Settings(1302, 1301);
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = 1302, Target = 1301 });

            var snapshot = QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);

            Equal(QuantityCalculationSettings.CurrentSchemaVersion, snapshot.SchemaVersion);
            Sequence(snapshot.ObservedCategoryCodes, 1301, 1302);
            Equal(1, snapshot.ExistingDirectedRuleCount);
            Equal(4L, snapshot.ExpectedDirectedRuleCount);
            False(snapshot.IsCompleteDirectedMatrix);
            Equal(3, snapshot.MissingDirectedPairs.Count);
            Pair(snapshot.MissingDirectedPairs[0], 1301, 1301);
            Pair(snapshot.MissingDirectedPairs[1], 1301, 1302);
            Pair(snapshot.MissingDirectedPairs[2], 1302, 1302);
        }

        private static void JsonExportIsPortableAndSanitized()
        {
            var settings = Settings(10, 30, 40);
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = 10, Target = 30 });
            var snapshot = QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);

            using (var stream = new MemoryStream())
            {
                QuantityCalculationMatrixDiagnosticSnapshotExporter.Write(stream, snapshot);
                var json = Encoding.UTF8.GetString(stream.ToArray());

                Contains(json, "\"schemaVersion\"");
                Contains(json, "\"observedCategoryCodes\"");
                Contains(json, "\"intersectionOnlyCategoryCodes\"");
                Contains(json, "\"unreferencedCategoryRuleCodes\"");
                Contains(json, "\"missingDirectedPairs\"");
                Contains(json, "\"sourceCode\"");
                Contains(json, "\"targetCode\"");
                NotContains(json, "SettingsPath");
                NotContains(json, "ProjectId");
                NotContains(json, "Drawing");
                NotContains(json, "Handle");
                NotContains(json, "GeneratedUtc");
                NotContains(json, "User");
            }
        }

        private static void SnapshotCreationDoesNotMutateCaller()
        {
            var settings = Settings(20, 10);
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = 20, Target = 10 });
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = 10, Target = 20 });

            QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);

            Equal(20, settings.CategoryRules[0].Category);
            Equal(10, settings.CategoryRules[1].Category);
            Equal(20, settings.IntersectionRules[0].Source);
            Equal(10, settings.IntersectionRules[0].Target);
            Equal(10, settings.IntersectionRules[1].Source);
            Equal(20, settings.IntersectionRules[1].Target);
        }

        private static QuantityCalculationSettings Settings(params int[] categoryCodes)
        {
            var settings = QuantityCalculationSettings.CreateDefault();
            settings.CategoryRules.Clear();
            settings.IntersectionRules.Clear();
            foreach (var code in categoryCodes)
                settings.CategoryRules.Add(new QuantityCategoryRuleSetting { Category = code, FaceAngleThresholdDeg = 30d });
            return settings;
        }

        private static void Pair(QuantityCalculationMatrixDiagnosticPairSnapshot pair, int source, int target)
        {
            Equal(source, pair.SourceCode);
            Equal(target, pair.TargetCode);
        }

        private static void Sequence(System.Collections.Generic.IReadOnlyList<int> actual, params int[] expected)
        {
            Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++) Equal(expected[i], actual[i]);
        }

        private static void Contains(string text, string value) { if (text.IndexOf(value, StringComparison.Ordinal) < 0) throw new Exception("Expected JSON token " + value + "."); }
        private static void NotContains(string text, string value) { if (text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) throw new Exception("Unexpected JSON token " + value + "."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
    }
}
