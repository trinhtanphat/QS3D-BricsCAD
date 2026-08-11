using System;

namespace QS3D.Core.Reporting
{
    public sealed class QuantityIntersectionCandidateMeasurement
    {
        public QuantityIntersectionCandidateMeasurement(
            int sourceCode,
            int targetCode,
            double concreteVolumeM3,
            double sideFormworkByConcreteAreaMm2,
            double bottomFormworkByConcreteAreaMm2,
            double sideFormworkBySideFormworkAreaMm2,
            double bottomFormworkByBottomFormworkAreaMm2)
        {
            if (sourceCode < 0) throw new ArgumentOutOfRangeException(nameof(sourceCode));
            if (targetCode < 0) throw new ArgumentOutOfRangeException(nameof(targetCode));
            RequireMeasurement(concreteVolumeM3, nameof(concreteVolumeM3));
            RequireMeasurement(sideFormworkByConcreteAreaMm2, nameof(sideFormworkByConcreteAreaMm2));
            RequireMeasurement(bottomFormworkByConcreteAreaMm2, nameof(bottomFormworkByConcreteAreaMm2));
            RequireMeasurement(sideFormworkBySideFormworkAreaMm2, nameof(sideFormworkBySideFormworkAreaMm2));
            RequireMeasurement(bottomFormworkByBottomFormworkAreaMm2, nameof(bottomFormworkByBottomFormworkAreaMm2));

            SourceCode = sourceCode;
            TargetCode = targetCode;
            ConcreteVolumeM3 = concreteVolumeM3;
            SideFormworkByConcreteAreaMm2 = sideFormworkByConcreteAreaMm2;
            BottomFormworkByConcreteAreaMm2 = bottomFormworkByConcreteAreaMm2;
            SideFormworkBySideFormworkAreaMm2 = sideFormworkBySideFormworkAreaMm2;
            BottomFormworkByBottomFormworkAreaMm2 = bottomFormworkByBottomFormworkAreaMm2;
        }

        public int SourceCode { get; }
        public int TargetCode { get; }
        public double ConcreteVolumeM3 { get; }
        public double SideFormworkByConcreteAreaMm2 { get; }
        public double BottomFormworkByConcreteAreaMm2 { get; }
        public double SideFormworkBySideFormworkAreaMm2 { get; }
        public double BottomFormworkByBottomFormworkAreaMm2 { get; }

        private static void RequireMeasurement(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Candidate measurement must be a finite non-negative number.");
        }
    }

    public sealed class QuantityIntersectionDeductionPlan
    {
        internal QuantityIntersectionDeductionPlan(
            int sourceCode,
            int targetCode,
            bool ruleFound,
            double concreteVolumeM3,
            double sideFormworkByConcreteAreaMm2,
            double bottomFormworkByConcreteAreaMm2,
            double sideFormworkBySideFormworkAreaMm2,
            double bottomFormworkByBottomFormworkAreaMm2)
        {
            SourceCode = sourceCode;
            TargetCode = targetCode;
            RuleFound = ruleFound;
            ConcreteVolumeM3 = concreteVolumeM3;
            SideFormworkByConcreteAreaMm2 = sideFormworkByConcreteAreaMm2;
            BottomFormworkByConcreteAreaMm2 = bottomFormworkByConcreteAreaMm2;
            SideFormworkBySideFormworkAreaMm2 = sideFormworkBySideFormworkAreaMm2;
            BottomFormworkByBottomFormworkAreaMm2 = bottomFormworkByBottomFormworkAreaMm2;
        }

        public int SourceCode { get; }
        public int TargetCode { get; }
        public bool RuleFound { get; }
        public double ConcreteVolumeM3 { get; }
        public double SideFormworkByConcreteAreaMm2 { get; }
        public double BottomFormworkByConcreteAreaMm2 { get; }
        public double SideFormworkBySideFormworkAreaMm2 { get; }
        public double BottomFormworkByBottomFormworkAreaMm2 { get; }

        public bool HasAnyDeduction =>
            ConcreteVolumeM3 > 0d ||
            SideFormworkByConcreteAreaMm2 > 0d ||
            BottomFormworkByConcreteAreaMm2 > 0d ||
            SideFormworkBySideFormworkAreaMm2 > 0d ||
            BottomFormworkByBottomFormworkAreaMm2 > 0d;
    }

    public sealed class QuantityIntersectionDeductionPlanner
    {
        private readonly QuantityCalculationDeductionGate _gate;

        public QuantityIntersectionDeductionPlanner(QuantityCalculationDeductionGate gate)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }

        public QuantityIntersectionDeductionPlan Plan(QuantityIntersectionCandidateMeasurement candidate)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));

            var found = _gate.TryAllowConcreteDeduction(
                candidate.SourceCode,
                candidate.TargetCode,
                candidate.ConcreteVolumeM3,
                out var subtractConcrete);
            if (!found)
                return Empty(candidate.SourceCode, candidate.TargetCode);

            var sideConcreteFound = _gate.TryAllowSideFormworkByConcreteDeduction(
                candidate.SourceCode,
                candidate.TargetCode,
                candidate.SideFormworkByConcreteAreaMm2,
                out var subtractSideByConcrete);
            var bottomConcreteFound = _gate.TryAllowBottomFormworkByConcreteDeduction(
                candidate.SourceCode,
                candidate.TargetCode,
                candidate.BottomFormworkByConcreteAreaMm2,
                out var subtractBottomByConcrete);
            var sideSideFound = _gate.TryAllowSideFormworkBySideFormworkDeduction(
                candidate.SourceCode,
                candidate.TargetCode,
                candidate.SideFormworkBySideFormworkAreaMm2,
                out var subtractSideBySide);
            var bottomBottomFound = _gate.TryAllowBottomFormworkByBottomFormworkDeduction(
                candidate.SourceCode,
                candidate.TargetCode,
                candidate.BottomFormworkByBottomFormworkAreaMm2,
                out var subtractBottomByBottom);

            if (!sideConcreteFound || !bottomConcreteFound || !sideSideFound || !bottomBottomFound)
                throw new InvalidOperationException("Quantity deduction gate returned inconsistent directed rule availability.");

            return new QuantityIntersectionDeductionPlan(
                candidate.SourceCode,
                candidate.TargetCode,
                true,
                subtractConcrete ? candidate.ConcreteVolumeM3 : 0d,
                subtractSideByConcrete ? candidate.SideFormworkByConcreteAreaMm2 : 0d,
                subtractBottomByConcrete ? candidate.BottomFormworkByConcreteAreaMm2 : 0d,
                subtractSideBySide ? candidate.SideFormworkBySideFormworkAreaMm2 : 0d,
                subtractBottomByBottom ? candidate.BottomFormworkByBottomFormworkAreaMm2 : 0d);
        }

        private static QuantityIntersectionDeductionPlan Empty(int sourceCode, int targetCode) =>
            new QuantityIntersectionDeductionPlan(sourceCode, targetCode, false, 0d, 0d, 0d, 0d, 0d);
    }
}
