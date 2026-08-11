using System;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    /// <summary>
    /// Applies only the persisted boolean deduction flags and their directly named
    /// minimum thresholds to already-measured candidate quantities.
    ///
    /// Geometry discovery remains outside this type: callers must provide measured
    /// concrete volume (m3) or formwork/contact area (mm2). Missing directed rules
    /// are reported as missing rather than mirrored or synthesized.
    /// </summary>
    public sealed class QuantityCalculationDeductionGate
    {
        private readonly QuantityCalculationRuleSet _rules;
        private readonly QuantityCalculationSettings _settings;

        public QuantityCalculationDeductionGate(QuantityCalculationRuleSet rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _settings = rules.Snapshot;
            _settings.NormalizeAndValidate();
        }

        public bool AllowsFormworkArea(double candidateAreaMm2)
        {
            RequireCandidate(candidateAreaMm2, nameof(candidateAreaMm2));
            return candidateAreaMm2 >= _settings.MinFormworkAreaMm2;
        }

        public bool TryAllowConcreteDeduction(int sourceCode, int targetCode, double candidateVolumeM3, out bool allowed)
        {
            RequireCandidate(candidateVolumeM3, nameof(candidateVolumeM3));
            if (!_rules.TryGetIntersectionRule(sourceCode, targetCode, out var rule))
            {
                allowed = false;
                return false;
            }

            allowed = rule.SubtractConcrete && candidateVolumeM3 >= _settings.MinConcreteVolumeM3;
            return true;
        }

        public bool TryAllowConcreteDeduction(ElementCategory source, ElementCategory target, double candidateVolumeM3, out bool allowed)
        {
            RequireCandidate(candidateVolumeM3, nameof(candidateVolumeM3));
            if (!_rules.TryGetIntersectionRule(source, target, out var rule))
            {
                allowed = false;
                return false;
            }

            allowed = rule.SubtractConcrete && candidateVolumeM3 >= _settings.MinConcreteVolumeM3;
            return true;
        }

        public bool TryAllowSideFormworkByConcreteDeduction(int sourceCode, int targetCode, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(sourceCode, targetCode, candidateAreaMm2, x => x.SubtractSideFormworkByConcrete, out allowed);

        public bool TryAllowBottomFormworkByConcreteDeduction(int sourceCode, int targetCode, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(sourceCode, targetCode, candidateAreaMm2, x => x.SubtractBottomFormworkByConcrete, out allowed);

        public bool TryAllowSideFormworkBySideFormworkDeduction(int sourceCode, int targetCode, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(sourceCode, targetCode, candidateAreaMm2, x => x.SubtractSideFormworkBySideFormwork, out allowed);

        public bool TryAllowBottomFormworkByBottomFormworkDeduction(int sourceCode, int targetCode, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(sourceCode, targetCode, candidateAreaMm2, x => x.SubtractBottomFormworkByBottomFormwork, out allowed);

        public bool TryAllowSideFormworkByConcreteDeduction(ElementCategory source, ElementCategory target, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(source, target, candidateAreaMm2, x => x.SubtractSideFormworkByConcrete, out allowed);

        public bool TryAllowBottomFormworkByConcreteDeduction(ElementCategory source, ElementCategory target, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(source, target, candidateAreaMm2, x => x.SubtractBottomFormworkByConcrete, out allowed);

        public bool TryAllowSideFormworkBySideFormworkDeduction(ElementCategory source, ElementCategory target, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(source, target, candidateAreaMm2, x => x.SubtractSideFormworkBySideFormwork, out allowed);

        public bool TryAllowBottomFormworkByBottomFormworkDeduction(ElementCategory source, ElementCategory target, double candidateAreaMm2, out bool allowed) =>
            TryAllowAreaDeduction(source, target, candidateAreaMm2, x => x.SubtractBottomFormworkByBottomFormwork, out allowed);

        private bool TryAllowAreaDeduction(
            int sourceCode,
            int targetCode,
            double candidateAreaMm2,
            Func<QuantityIntersectionRuleSetting, bool> flag,
            out bool allowed)
        {
            RequireCandidate(candidateAreaMm2, nameof(candidateAreaMm2));
            if (!_rules.TryGetIntersectionRule(sourceCode, targetCode, out var rule))
            {
                allowed = false;
                return false;
            }

            allowed = flag(rule) && candidateAreaMm2 >= _settings.MinSubtractAreaMm2;
            return true;
        }

        private bool TryAllowAreaDeduction(
            ElementCategory source,
            ElementCategory target,
            double candidateAreaMm2,
            Func<QuantityIntersectionRuleSetting, bool> flag,
            out bool allowed)
        {
            RequireCandidate(candidateAreaMm2, nameof(candidateAreaMm2));
            if (!_rules.TryGetIntersectionRule(source, target, out var rule))
            {
                allowed = false;
                return false;
            }

            allowed = flag(rule) && candidateAreaMm2 >= _settings.MinSubtractAreaMm2;
            return true;
        }

        private static void RequireCandidate(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Candidate quantity must be a finite non-negative number.");
        }
    }
}
