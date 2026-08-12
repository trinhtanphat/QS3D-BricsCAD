using System;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationMatrixDiagnosticsSmoke
    {
        public static void Run()
        {
            CompleteMatrixReportsNoGaps();
            MissingReversePairRemainsDirected();
            ReportsIntersectionOnlyAndUnreferencedCodes();
            UnknownImportedCodesStayExactAndSorted();
            AnalysisDoesNotMutateCallerOrdering();
        }

        private static void CompleteMatrixReportsNoGaps()
        {
            var settings = Settings(10, 20);
            AddPair(settings, 10, 10);
            AddPair(settings, 10, 20);
            AddPair(settings, 20, 10);
            AddPair(settings, 20, 20);

            var result = QuantityCalculationMatrixDiagnostics.Analyze(settings);

            True(result.IsCompleteDirectedMatrix);
            Equal(4L, result.ExpectedDirectedRuleCount);
            Equal(4, result.ExistingDirectedRuleCount);
            Equal(0, result.MissingDirectedPairs.Count);
            Equal(0, result.IntersectionOnlyCategoryCodes.Count);
            Equal(0, result.UnreferencedCategoryRuleCodes.Count);
            Sequence(result.ObservedCategoryCodes, 10, 20);
        }

        private static void MissingReversePairRemainsDirected()
        {
            var settings = Settings(10, 20);
            AddPair(settings, 10, 10);
            AddPair(settings, 10, 20);
            AddPair(settings, 20, 20);

            var result = QuantityCalculationMatrixDiagnostics.Analyze(settings);

            False(result.IsCompleteDirectedMatrix);
            Equal(1, result.MissingDirectedPairs.Count);
            Pair(result.MissingDirectedPairs[0], 20, 10);
        }

        private static void ReportsIntersectionOnlyAndUnreferencedCodes()
        {
            var settings = Settings(10, 40);
            AddPair(settings, 10, 10);
            AddPair(settings, 10, 30);
            AddPair(settings, 30, 10);
            AddPair(settings, 30, 30);

            var result = QuantityCalculationMatrixDiagnostics.Analyze(settings);

            Sequence(result.ObservedCategoryCodes, 10, 30, 40);
            Sequence(result.IntersectionOnlyCategoryCodes, 30);
            Sequence(result.UnreferencedCategoryRuleCodes, 40);
            True(result.MissingDirectedPairs.Count > 0);
        }

        private static void UnknownImportedCodesStayExactAndSorted()
        {
            var settings = Settings(1302, 1301);
            AddPair(settings, 1302, 1301);

            var result = QuantityCalculationMatrixDiagnostics.Analyze(settings);

            Sequence(result.ObservedCategoryCodes, 1301, 1302);
            Equal(3, result.MissingDirectedPairs.Count);
            Pair(result.MissingDirectedPairs[0], 1301, 1301);
            Pair(result.MissingDirectedPairs[1], 1301, 1302);
            Pair(result.MissingDirectedPairs[2], 1302, 1302);
        }

        private static void AnalysisDoesNotMutateCallerOrdering()
        {
            var settings = Settings(20, 10);
            AddPair(settings, 20, 10);
            AddPair(settings, 10, 20);

            QuantityCalculationMatrixDiagnostics.Analyze(settings);

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

        private static void AddPair(QuantityCalculationSettings settings, int source, int target)
        {
            settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting { Source = source, Target = target });
        }

        private static void Pair(QuantityCalculationMatrixPair pair, int source, int target)
        {
            Equal(source, pair.SourceCode);
            Equal(target, pair.TargetCode);
        }

        private static void Sequence(System.Collections.Generic.IReadOnlyList<int> actual, params int[] expected)
        {
            Equal(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++) Equal(expected[i], actual[i]);
        }

        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void False(bool value) { if (value) throw new Exception("Expected false."); }
    }
}
