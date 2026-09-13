using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageProfileRoutingSmoke
    {
        internal static void Run()
        {
            RoutesSelectedProfilesIntoFormulaAndRateEvaluation();
            PreservesFailClosedEvaluationContract();
        }

        private static void RoutesSelectedProfilesIntoFormulaAndRateEvaluation()
        {
            var formulaCalls = 0;
            var rateCalls = 0;
            var package = new TakeoffPackageDefinition("PKG-PROFILE", "Profile routing", "R1", "Uniclass-2025", "Net-Concrete-v2");

            var result = new AutodeskTakeoffPackageCoordinator().BuildProfileAware(
                package,
                Enumerable.Empty<DrawingSheet2D>(),
                Enumerable.Empty<TakeoffQuantityEvidence2D>(),
                new[] { new IfcQtoItem("GUID-P", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 5d, "m3") },
                (classificationProfile, formulaProfile, classification, quantity) =>
                {
                    Equal("Uniclass-2025", classificationProfile, "Formula classification profile.");
                    Equal("Net-Concrete-v2", formulaProfile, "Formula profile.");
                    Equal("03-CONCRETE", classification, "Formula classification.");
                    formulaCalls++;
                    return quantity;
                },
                (classificationProfile, formulaProfile, classification, unit) =>
                {
                    Equal("Uniclass-2025", classificationProfile, "Rate classification profile.");
                    Equal("Net-Concrete-v2", formulaProfile, "Rate formula profile.");
                    Equal("03-CONCRETE", classification, "Rate classification.");
                    Equal("m3", unit, "Rate unit.");
                    rateCalls++;
                    return 2d;
                });

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Profile-aware package should remain publishable.");
            Equal(1, formulaCalls, "Formula callback count.");
            Equal(1, rateCalls, "Rate callback count.");
            Equal(1, result.Inventory.Count, "Profile-aware evaluation should publish one inventory row.");
            Equal(10d, result.EstimatedCost, "Profile-aware evaluation should retain estimate behavior.");
        }

        private static void PreservesFailClosedEvaluationContract()
        {
            var package = new TakeoffPackageDefinition("PKG-PROFILE-FAIL", "Profile routing", "R1", "Uniclass-2025", "Broken-profile");
            var rateCalled = false;

            var result = new AutodeskTakeoffPackageCoordinator().BuildProfileAware(
                package,
                Enumerable.Empty<DrawingSheet2D>(),
                Enumerable.Empty<TakeoffQuantityEvidence2D>(),
                new[] { new IfcQtoItem("GUID-F", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 1d, "m3") },
                (classificationProfile, formulaProfile, classification, quantity) => throw new InvalidOperationException("profile unavailable"),
                (classificationProfile, formulaProfile, classification, unit) => { rateCalled = true; return 1d; });

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "Profile evaluation failure must block publication.");
            True(result.Issues.Any(x => x.Code == "PKG.EVALUATION_FAILED"), "Profile evaluation failure must use the established package issue contract.");
            Equal(0, result.Inventory.Count, "Failed profile evaluation must not publish inventory.");
            True(!result.CanEstimate, "Failed profile evaluation must not be estimatable.");
            True(!rateCalled, "Rate evaluation must not continue after formula failure.");
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

    internal static class QsTakeoffPackageProfileRoutingRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QsTakeoffPackageProfileRoutingSmoke.Run();
        }
    }
}
