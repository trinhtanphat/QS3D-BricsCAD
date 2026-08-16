using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepQuantityCompensationSmoke
    {
        internal static void Run()
        {
            PreservesCollectivelySignificantSmallMetrics();
            PreservesSmallMetricsAcrossInputOrder();
            PreservesOrdinaryGroupingAndCounts();
            PreservesOverflowFailClosedBehavior();
        }

        private static void PreservesCollectivelySignificantSmallMetrics()
        {
            var service = new MepQuantityService();
            var groups = service.Aggregate(new[]
            {
                Element("MEP-1", 1, 1e16d, 1e16d, 1e16d),
                Element("MEP-2", 2, 1d, 1d, 1d),
                Element("MEP-3", 3, 1d, 1d, 1d)
            });

            Require(groups.Count == 1, "MEP compensation repro must remain in one aggregate group.");
            var group = groups[0];
            const double expected = 10000000000000002d;
            Require(group.LengthM == expected, "MEP length aggregation lost collectively significant small contributions.");
            Require(group.AreaM2 == expected, "MEP area aggregation lost collectively significant small contributions.");
            Require(group.VolumeM3 == expected, "MEP volume aggregation lost collectively significant small contributions.");
            Require(group.ElementCount == 3, "MEP compensated aggregation changed element count semantics.");
            Require(group.QuantityCount == 6, "MEP compensated aggregation changed quantity count semantics.");
        }

        private static void PreservesSmallMetricsAcrossInputOrder()
        {
            var service = new MepQuantityService();
            var group = service.Aggregate(new[]
            {
                Element("ORDER-1", 1, 1d, 1d, 1d),
                Element("ORDER-2", 1, 1e16d, 1e16d, 1e16d),
                Element("ORDER-3", 1, 1d, 1d, 1d)
            })[0];

            const double expected = 10000000000000002d;
            Require(group.LengthM == expected, "MEP length compensation depends on large-term input order.");
            Require(group.AreaM2 == expected, "MEP area compensation depends on large-term input order.");
            Require(group.VolumeM3 == expected, "MEP volume compensation depends on large-term input order.");
        }

        private static void PreservesOrdinaryGroupingAndCounts()
        {
            var service = new MepQuantityService();
            var groups = service.Aggregate(new[]
            {
                new MepElement("ORD-1", MepElementKind.Pipe, "CHW", "DN100", "Level 01", 2, 2d, 3d, 4d),
                new MepElement("ORD-2", MepElementKind.Pipe, "chw", "dn100", "level 01", 5, 4d, 6d, 8d),
                new MepElement("ORD-3", MepElementKind.Duct, "SA", "600x300", "Level 02", 7, 10d, 20d, 30d)
            });

            Require(groups.Count == 2, "MEP compensated aggregation changed grouping semantics.");
            var pipe = Find(groups, MepElementKind.Pipe);
            Require(pipe.ElementCount == 2, "MEP ordinary pipe element count changed.");
            Require(pipe.QuantityCount == 7, "MEP ordinary pipe quantity count changed.");
            Require(pipe.LengthM == 6d && pipe.AreaM2 == 9d && pipe.VolumeM3 == 12d,
                "MEP ordinary exact metric totals changed.");

            var duct = Find(groups, MepElementKind.Duct);
            Require(duct.ElementCount == 1 && duct.QuantityCount == 7,
                "MEP separate-group count semantics changed.");
            Require(duct.LengthM == 10d && duct.AreaM2 == 20d && duct.VolumeM3 == 30d,
                "MEP separate-group metric totals changed.");
        }

        private static void PreservesOverflowFailClosedBehavior()
        {
            var service = new MepQuantityService();
            Throws<OverflowException>(() => service.Aggregate(new[]
            {
                Element("MAX-1", 1, double.MaxValue, 0d, 0d),
                Element("MAX-2", 1, double.MaxValue, 0d, 0d)
            }));
        }

        private static MepElement Element(string id, int count, double lengthM, double areaM2, double volumeM3) =>
            new MepElement(id, MepElementKind.Pipe, "CHW", "DN100", "Level 01", count, lengthM, areaM2, volumeM3);

        private static MepQuantityGroup Find(System.Collections.Generic.IReadOnlyList<MepQuantityGroup> groups, MepElementKind kind)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].Kind == kind) return groups[i];
            }
            throw new InvalidOperationException("Expected MEP aggregate group for " + kind + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }

    internal static class MepQuantityCompensationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MepQuantityCompensationSmoke.Run();
    }
}
