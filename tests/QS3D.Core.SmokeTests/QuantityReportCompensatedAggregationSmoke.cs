using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportCompensatedAggregationSmoke
    {
        internal static void Run()
        {
            PreservesRepresentableLowOrderContributions();
            PreservesOrdinaryAggregation();
            AccumulatorStillFailsClosedOnOverflow();
        }

        private static void PreservesRepresentableLowOrderContributions()
        {
            var family = new FamilyDefinition("W1", ElementCategory.StructuralWall, "Concrete");
            var first = Element("E1", family, 1e16);
            var second = Element("E2", family, 1d);
            var third = Element("E3", family, 1d);

            var rows = QuantityReportBuilder.Group(new[] { first, second, third });
            if (rows.Count != 1) throw new InvalidOperationException("Expected one grouped quantity-report row.");
            var row = rows[0];
            const double expected = 10000000000000002d;
            if (row.Count != 3) throw new InvalidOperationException("Quantity-report count changed during compensated aggregation.");
            if (row.GrossConcreteM3 != expected) throw new InvalidOperationException("Compensated gross-concrete aggregation lost representable low-order contributions.");
            if (row.NetConcreteM3 != expected) throw new InvalidOperationException("Compensated net-concrete aggregation lost representable low-order contributions.");
            if (row.FormworkM2 != expected) throw new InvalidOperationException("Compensated formwork aggregation lost representable low-order contributions.");
            if (row.LengthM != expected) throw new InvalidOperationException("Compensated length aggregation lost representable low-order contributions.");
            if (row.ElementIds.Count != 3) throw new InvalidOperationException("Quantity-report provenance changed during compensated aggregation.");
        }

        private static void PreservesOrdinaryAggregation()
        {
            var family = new FamilyDefinition("C1", ElementCategory.Column, "Concrete");
            var first = Element("O1", family, 1.25d);
            var second = Element("O2", family, 2.5d);
            var third = Element("O3", family, 0.25d);

            var row = QuantityReportBuilder.Group(new[] { first, second, third })[0];
            if (row.GrossConcreteM3 != 4d || row.FormworkM2 != 4d || row.LengthM != 4d)
                throw new InvalidOperationException("Ordinary quantity-report aggregation changed under compensated summation.");
        }

        private static void AccumulatorStillFailsClosedOnOverflow()
        {
            var accumulator = new QuantityReportMath.FiniteAccumulator();
            accumulator.Add(double.MaxValue, "overflow");
            try
            {
                accumulator.Add(double.MaxValue, "overflow");
                throw new InvalidOperationException("Expected compensated quantity-report accumulation to fail closed on overflow.");
            }
            catch (OverflowException)
            {
            }
        }

        private static ElementInstance Element(string id, FamilyDefinition family, double value)
        {
            return new ElementInstance(id, family, "L1")
            {
                GrossConcreteM3 = value,
                FormworkM2 = value,
                LengthM = value
            };
        }
    }
}
