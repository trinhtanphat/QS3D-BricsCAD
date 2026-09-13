using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageEvaluationValidationSmoke
    {
        internal static void Run()
        {
            BlocksFormulaFailure();
            BlocksNonFiniteRate();
            PreservesSuccessfulEvaluation();
        }

        private static void BlocksFormulaFailure()
        {
            var result = Build(
                (classification, quantity) => throw new InvalidOperationException("formula profile failure"),
                (classification, unit) => 10d);

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "Formula evaluation failure must block package publication.");
            True(result.Issues.Any(x => x.Code == "PKG.EVALUATION_FAILED"), "Formula evaluation failure must surface as PKG.EVALUATION_FAILED.");
            Equal(0, result.Inventory.Count, "Blocked evaluation must not publish partial inventory.");
            True(!result.CanEstimate, "Blocked evaluation must not be estimatable.");
        }

        private static void BlocksNonFiniteRate()
        {
            var result = Build(
                (classification, quantity) => quantity,
                (classification, unit) => double.PositiveInfinity);

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "Non-finite rate must block package publication.");
            True(result.Issues.Any(x => x.Code == "PKG.EVALUATION_FAILED"), "Non-finite rate rejection must surface as package validation.");
            Equal(0, result.Inventory.Count, "Rejected rate must not publish partial inventory.");
        }

        private static void PreservesSuccessfulEvaluation()
        {
            var result = Build(
                (classification, quantity) => quantity * 2d,
                (classification, unit) => 3d);

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Valid formula/rate evaluation must remain publishable.");
            Equal(1, result.Inventory.Count, "Valid evaluation should produce one inventory row.");
            Equal(30d, result.EstimatedCost, "Formula and rate must still feed the estimate.");
        }

        private static TakeoffPackageBuildResult Build(Func<string, double, double> formula, Func<string, string, double> rateProvider)
        {
            return new AutodeskTakeoffPackageCoordinator().Build(
                new TakeoffPackageDefinition("PKG-1", "Takeoff", "R1", "Uniclass", "Default"),
                Enumerable.Empty<DrawingSheet2D>(),
                Enumerable.Empty<TakeoffQuantityEvidence2D>(),
                new[] { new IfcQtoItem("GUID-1", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 5d, "m3") },
                formula,
                rateProvider);
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QsTakeoffPackageEvaluationValidationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QsTakeoffPackageEvaluationValidationSmoke.Run();
        }
    }
}
