using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DDiscreteCountSmoke
    {
        internal static void Run()
        {
            RejectsFractionalCountMarkup();
            PreservesFractionalLengthMarkup();
            PublishesWholeCountThroughPackageEstimate();
        }

        private static void RejectsFractionalCountMarkup()
        {
            Throws<ArgumentOutOfRangeException>(() => new TakeoffMarkup2D(
                "COUNT-1", "S1", TakeoffMeasurementKind.Count, 1.5d,
                "Doors", "L01", "A-DOOR", "H-1"),
                "Fractional count markup must fail closed before evidence extraction.");
        }

        private static void PreservesFractionalLengthMarkup()
        {
            var markup = new TakeoffMarkup2D(
                "LEN-1", "S1", TakeoffMeasurementKind.Length, 1.5d,
                "Partitions", "L01", "A-WALL", "H-2");

            Equal(1.5d, markup.RawValue, "Fractional geometric measurements remain valid.");
        }

        private static void PublishesWholeCountThroughPackageEstimate()
        {
            var sheet = new DrawingSheet2D(
                "S1", "Ground floor", DrawingSheetSourceKind.Pdf,
                "drawings/A101.pdf#page=1", "R1", new DrawingCalibration(10d, 100d, "mm"));
            var markup = new TakeoffMarkup2D(
                "COUNT-3", "S1", TakeoffMeasurementKind.Count, 3d,
                "Doors", "L01", "A-DOOR", "H-3");

            var takeoff = new CalibratedTakeoffEngine2D().Extract(sheet, new[] { markup });
            Equal(1, takeoff.Evidence.Count, "One count evidence item expected.");
            Equal(3d, takeoff.Evidence[0].Quantity, "Count quantity must remain an exact discrete item count.");
            Equal("ea", takeoff.Evidence[0].Unit, "Count unit must remain ea.");

            var package = new TakeoffPackageDefinition("PKG-COUNT", "Count package", "R1", "Uniclass", "Identity");
            var result = new AutodeskTakeoffPackageCoordinator().Build(
                package,
                new[] { sheet },
                takeoff.Evidence,
                Enumerable.Empty<IfcQtoItem>(),
                (classification, quantity) => quantity,
                (classification, unit) => 25d);

            Equal(TakeoffPackageReadiness.Ready, result.Readiness, "Valid whole counts should remain package-ready.");
            Equal(1, result.Inventory.Count, "Whole count should produce one inventory row.");
            Equal(3d, result.Inventory[0].MeasuredQuantity, "Inventory must preserve discrete count quantity.");
            Equal(75d, result.EstimatedCost, "Whole count must flow through formula/rate estimation unchanged.");
        }

        private static void Throws<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class Qs2DDiscreteCountRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Qs2DDiscreteCountSmoke.Run();
        }
    }
}
