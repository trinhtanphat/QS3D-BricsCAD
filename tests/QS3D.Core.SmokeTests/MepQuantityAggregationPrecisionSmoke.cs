using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepQuantityAggregationPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CollectivelySignificantSmallMetricsArePreserved();
            OrdinaryAggregationRemainsStable();
            NonFiniteAggregationStillFailsClosed();
        }

        private static void CollectivelySignificantSmallMetricsArePreserved()
        {
            const double expected = 10000000000000002d;
            var groups = Aggregate(
                Element("large", 2, 1e16d, 1e16d, 1e16d),
                Element("small-a", 3, 1d, 1d, 1d),
                Element("small-b", 4, 1d, 1d, 1d));

            Assert(groups.Count == 1, "Precision regression must remain in one MEP aggregate group.");
            var group = groups[0];
            Assert(group.ElementCount == 3, "MEP aggregate element count changed unexpectedly.");
            Assert(group.QuantityCount == 9, "MEP aggregate quantity count changed unexpectedly.");
            Assert(group.LengthM.Equals(expected), "MEP aggregate length lost collectively significant small contributions.");
            Assert(group.AreaM2.Equals(expected), "MEP aggregate area lost collectively significant small contributions.");
            Assert(group.VolumeM3.Equals(expected), "MEP aggregate volume lost collectively significant small contributions.");
        }

        private static void OrdinaryAggregationRemainsStable()
        {
            var groups = Aggregate(
                Element("ordinary-a", 1, 10d, 20d, 30d),
                Element("ordinary-b", 1, 2d, 3d, 4d),
                Element("ordinary-c", 1, 1d, 2d, 3d));

            Assert(groups.Count == 1, "Ordinary MEP aggregation must remain in one group.");
            Assert(groups[0].LengthM.Equals(13d), "Ordinary exact MEP length aggregation changed unexpectedly.");
            Assert(groups[0].AreaM2.Equals(25d), "Ordinary exact MEP area aggregation changed unexpectedly.");
            Assert(groups[0].VolumeM3.Equals(37d), "Ordinary exact MEP volume aggregation changed unexpectedly.");
        }

        private static void NonFiniteAggregationStillFailsClosed()
        {
            Capture<OverflowException>(() => Aggregate(
                Element("overflow-a", 1, double.MaxValue, 0d, 0d),
                Element("overflow-b", 1, double.MaxValue, 0d, 0d)));
        }

        private static IReadOnlyList<MepQuantityGroup> Aggregate(params MepElement[] elements) =>
            new MepQuantityService().Aggregate(elements);

        private static MepElement Element(
            string id,
            int count,
            double lengthM,
            double areaM2,
            double volumeM3) =>
            new MepElement(
                id,
                MepElementKind.Pipe,
                "CHW",
                "DN100",
                "L1",
                count,
                lengthM,
                areaM2,
                volumeM3);

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
