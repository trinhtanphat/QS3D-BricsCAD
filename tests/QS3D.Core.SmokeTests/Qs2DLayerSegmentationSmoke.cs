using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DLayerSegmentationSmoke
    {
        internal static void Run()
        {
            PreservesDistinctDrawingLayers();
            PreservesLegacyWorkflowLineConstructor();
            PreservesLayersThroughPackageEstimate();
        }

        private static void PreservesDistinctDrawingLayers()
        {
            var evidence = new[]
            {
                Evidence("M-1", "A-WALL", 4d),
                Evidence("M-2", "A-FINISH", 6d),
                Evidence("M-3", "A-WALL", 1d)
            };

            var lines = new AutodeskTakeoffWorkflow().BuildInventoryAndEstimate(
                evidence,
                Enumerable.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 10d);

            Equal(2, lines.Count, "Distinct drawing layers must remain separate inventory rows.");
            Equal("A-FINISH", lines[0].Layer, "Layer must participate in deterministic row ordering.");
            Equal(6d, lines[0].MeasuredQuantity, "Finish-layer quantity must not merge with wall-layer evidence.");
            Equal("A-WALL", lines[1].Layer, "Second layer should preserve its source layer identity.");
            Equal(5d, lines[1].MeasuredQuantity, "Evidence on the same layer should still aggregate.");
            Equal(2, lines[1].EvidenceCount, "Same-layer evidence count should aggregate independently.");
        }

        private static void PreservesLegacyWorkflowLineConstructor()
        {
            var line = new TakeoffWorkflowLine("Walls", "L01", "m2", 2d, 2d, 5d, 1);
            Equal(string.Empty, line.Layer, "Legacy constructor must remain source-compatible and default Layer to empty.");
            Equal(10d, line.EstimatedCost, "Legacy constructor estimate behavior must remain unchanged.");
        }

        private static void PreservesLayersThroughPackageEstimate()
        {
            var sheet = new DrawingSheet2D(
                "S1", "Ground floor", DrawingSheetSourceKind.Pdf,
                "drawings/A101.pdf#page=1", "R1", new DrawingCalibration(1d, 1d, "m"));
            var evidence = new[]
            {
                Evidence("M-10", "A-WALL", 2d),
                Evidence("M-11", "A-FINISH", 3d)
            };
            var package = new TakeoffPackageDefinition("PKG-LAYER", "Layer package", "R1", "Uniclass", "Identity");

            var result = new AutodeskTakeoffPackageCoordinator().Build(
                package,
                new[] { sheet },
                evidence,
                Enumerable.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 20d);

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Layer segmentation must not block a valid package.");
            Equal(2, result.Inventory.Count, "Package inventory must preserve distinct drawing layers.");
            Equal("A-FINISH", result.Inventory[0].Layer, "Package inventory must expose the layer dimension.");
            Equal("A-WALL", result.Inventory[1].Layer, "Package inventory must retain both source layers.");
            Equal(100d, result.EstimatedCost, "Layer segmentation must preserve total estimate arithmetic.");
        }

        private static TakeoffQuantityEvidence2D Evidence(string markupId, string layer, double quantity)
        {
            return new TakeoffQuantityEvidence2D(
                markupId,
                "S1",
                "R1",
                "drawings/A101.pdf#page=1",
                "H-" + markupId,
                "Walls",
                "L01",
                layer,
                quantity,
                "m2");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class Qs2DLayerSegmentationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Qs2DLayerSegmentationSmoke.Run();
        }
    }
}