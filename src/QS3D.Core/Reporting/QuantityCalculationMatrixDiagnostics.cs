using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Reporting
{
    public sealed class QuantityCalculationMatrixPair
    {
        public QuantityCalculationMatrixPair(int sourceCode, int targetCode)
        {
            SourceCode = sourceCode;
            TargetCode = targetCode;
        }

        public int SourceCode { get; }
        public int TargetCode { get; }
    }

    public sealed class QuantityCalculationMatrixDiagnosticResult
    {
        internal QuantityCalculationMatrixDiagnosticResult(
            IReadOnlyList<int> observedCategoryCodes,
            IReadOnlyList<int> intersectionOnlyCategoryCodes,
            IReadOnlyList<int> unreferencedCategoryRuleCodes,
            IReadOnlyList<QuantityCalculationMatrixPair> missingDirectedPairs,
            int existingDirectedRuleCount)
        {
            ObservedCategoryCodes = observedCategoryCodes;
            IntersectionOnlyCategoryCodes = intersectionOnlyCategoryCodes;
            UnreferencedCategoryRuleCodes = unreferencedCategoryRuleCodes;
            MissingDirectedPairs = missingDirectedPairs;
            ExistingDirectedRuleCount = existingDirectedRuleCount;
        }

        public IReadOnlyList<int> ObservedCategoryCodes { get; }
        public IReadOnlyList<int> IntersectionOnlyCategoryCodes { get; }
        public IReadOnlyList<int> UnreferencedCategoryRuleCodes { get; }
        public IReadOnlyList<QuantityCalculationMatrixPair> MissingDirectedPairs { get; }
        public int ExistingDirectedRuleCount { get; }
        public long ExpectedDirectedRuleCount => (long)ObservedCategoryCodes.Count * ObservedCategoryCodes.Count;
        public bool IsCompleteDirectedMatrix => MissingDirectedPairs.Count == 0;
    }

    public static class QuantityCalculationMatrixDiagnostics
    {
        public static QuantityCalculationMatrixDiagnosticResult Analyze(QuantityCalculationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var snapshot = settings.Clone();
            snapshot.NormalizeAndValidate();

            var categoryCodes = new HashSet<int>(snapshot.CategoryRules.Select(x => x.Category));
            var intersectionCodes = new HashSet<int>();
            var existingPairs = new HashSet<long>();
            foreach (var rule in snapshot.IntersectionRules)
            {
                intersectionCodes.Add(rule.Source);
                intersectionCodes.Add(rule.Target);
                existingPairs.Add(PairKey(rule.Source, rule.Target));
            }

            var observedCodes = categoryCodes
                .Concat(intersectionCodes)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            var intersectionOnlyCodes = intersectionCodes
                .Where(x => !categoryCodes.Contains(x))
                .OrderBy(x => x)
                .ToList();
            var unreferencedCategoryCodes = categoryCodes
                .Where(x => !intersectionCodes.Contains(x))
                .OrderBy(x => x)
                .ToList();

            var missingPairs = new List<QuantityCalculationMatrixPair>();
            foreach (var sourceCode in observedCodes)
            foreach (var targetCode in observedCodes)
                if (!existingPairs.Contains(PairKey(sourceCode, targetCode)))
                    missingPairs.Add(new QuantityCalculationMatrixPair(sourceCode, targetCode));

            return new QuantityCalculationMatrixDiagnosticResult(
                observedCodes.AsReadOnly(),
                intersectionOnlyCodes.AsReadOnly(),
                unreferencedCategoryCodes.AsReadOnly(),
                missingPairs.AsReadOnly(),
                snapshot.IntersectionRules.Count);
        }

        private static long PairKey(int sourceCode, int targetCode)
        {
            return ((long)(uint)sourceCode << 32) | (uint)targetCode;
        }
    }
}
