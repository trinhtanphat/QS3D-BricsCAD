using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsTakeoffPackageNegativeBimQuantitySmoke
    {
        internal static void Run()
        {
            BlocksNegativeBimQuantityBeforeEvaluation();
            PreservesZeroAndPositiveBimQuantities();
        }

        private static void BlocksNegativeBimQuantityBeforeEvaluation()
        {
            var formulaCalled = false;
            var rateCalled = false;
            var result = new AutodeskTakeoffPackageCoordinator().Build(
                new TakeoffPackageDefinition("PKG-NEG", "Takeoff", "R1", "Uniclass", "Default"),
                Enumerable.Empty<DrawingSheet2D>(),
                Enumerable.Empty<TakeoffQuantityEvidence2D>(),
                new[] { new IfcQtoItem("GUID-N", "IfcWall", "L01", "03-CONCRETE", "NetVolume", -1d, "m3") },
                (classification, quantity) => { formulaCalled = true; return quantity; },
                (classification, unit) => { rateCalled = true; return 1d; });

            Equal(TakeoffPackageReadiness.Blocked, result.Readiness, "Negative BIM source quantity must block package publication.");
            var issue = result.Issues.Single(x => x.Code == "PKG.NEGATIVE_BIM_QUANTITY");
            Equal(TakeoffPackageValidationSeverity.Error, issue.Severity, "Negative BIM quantity issue severity.");
            Equal("GUID-N", issue.SourceId, "Negative BIM quantity issue source identity.");
            Equal(0, result.Inventory.Count, "Blocked package must not publish inventory.");
            Equal(0d, result.EstimatedCost, "Blocked package must not publish estimate cost.");
            True(!result.CanEstimate, "Blocked package must not be estimatable.");
            True(!formulaCalled && !rateCalled, "Negative BIM evidence must be rejected before formula/rate evaluation.");
        }

        private static void PreservesZeroAndPositiveBimQuantities()
        {
            var result = new AutodeskTakeoffPackageCoordinator().Build(
                new TakeoffPackageDefinition("PKG-OK", "Takeoff", "R1", "Uniclass", "Default"),
                Enumerable.Empty<DrawingSheet2D>(),
                Enumerable.Empty<TakeoffQuantityEvidence2D>(),
                new[]
                {
                    new IfcQtoItem("GUID-Z", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 0d, "m3"),
                    new IfcQtoItem("GUID-P", "IfcWall", "L01", "03-CONCRETE", "NetVolume", 5d, "m3")
                },
                (classification, quantity) => quantity,
                (classification, unit) => 2d);

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Zero and positive BIM quantities must remain publishable.");
            Equal(1, result.Inventory.Count, "Compatible BIM quantities should aggregate into one inventory row.");
            Equal(5d, result.Inventory[0].MeasuredQuantity, "Zero must not alter positive measured quantity.");
            Equal(2, result.Inventory[0].EvidenceCount, "Zero evidence remains part of source provenance.");
            Equal(10d, result.EstimatedCost, "Valid BIM quantities must still flow through formula/rate estimation.");
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

    internal static class QsTakeoffPackageNegativeBimQuantityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QsTakeoffPackageNegativeBimQuantitySmoke.Run();
        }
    }
}
