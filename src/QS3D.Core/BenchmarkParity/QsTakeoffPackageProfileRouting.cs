using System;
using System.Collections.Generic;

namespace QS3D.Core.BenchmarkParity
{
    public delegate double TakeoffPackageFormulaEvaluator(
        string classificationProfile,
        string formulaProfile,
        string classification,
        double quantity);

    public delegate double TakeoffPackageRateEvaluator(
        string classificationProfile,
        string formulaProfile,
        string classification,
        string unit);

    public static class AutodeskTakeoffPackageProfileRouting
    {
        public static TakeoffPackageBuildResult BuildProfileAware(
            this AutodeskTakeoffPackageCoordinator coordinator,
            TakeoffPackageDefinition package,
            IEnumerable<DrawingSheet2D> drawingSheets,
            IEnumerable<TakeoffQuantityEvidence2D> drawingEvidence,
            IEnumerable<IfcQtoItem> bimQuantities,
            TakeoffPackageFormulaEvaluator formula,
            TakeoffPackageRateEvaluator rateProvider)
        {
            if (coordinator == null) throw new ArgumentNullException("coordinator");
            if (package == null) throw new ArgumentNullException("package");
            if (formula == null) throw new ArgumentNullException("formula");
            if (rateProvider == null) throw new ArgumentNullException("rateProvider");

            return coordinator.Build(
                package,
                drawingSheets,
                drawingEvidence,
                bimQuantities,
                (classification, quantity) => formula(
                    package.ClassificationProfile,
                    package.FormulaProfile,
                    classification,
                    quantity),
                (classification, unit) => rateProvider(
                    package.ClassificationProfile,
                    package.FormulaProfile,
                    classification,
                    unit));
        }
    }
}
