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
            PreservesLowOrderContributionsAcrossAllFields();
            PreservesOrdinaryAggregation();
            AccumulatorStillFailsClosedOnOverflow();
            AccumulatorRejectsNonFiniteInputs();
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

        private static void PreservesLowOrderContributionsAcrossAllFields()
        {
            var family = new FamilyDefinition("W2", ElementCategory.StructuralWall, "Concrete");
            var first = ElementWithAllQuantities("A1", family, 1e16);
            var second = ElementWithAllQuantities("A2", family, 1d);
            var third = ElementWithAllQuantities("A3", family, 1d);

            var row = QuantityReportBuilder.Group(new[] { first, second, third })[0];
            const double expected = 10000000000000002d;
            Equal(expected, row.GrossConcreteM3, "GrossConcreteM3");
            Equal(expected, row.DeductionM3, "DeductionM3");
            Equal(expected, row.NetConcreteM3, "NetConcreteM3");
            Equal(expected, row.FormworkM2, "FormworkM2");
            Equal(expected, row.LengthM, "LengthM");
            Equal(expected, row.OuterPerimeterM, "OuterPerimeterM");
            Equal(expected, row.InnerPerimeterM, "InnerPerimeterM");
            Equal(expected, row.DoorAreaM2, "DoorAreaM2");
            Equal(expected, row.SideAreaM2, "SideAreaM2");
            Equal(expected, row.BottomAreaM2, "BottomAreaM2");
            Equal(expected, row.TopAreaM2, "TopAreaM2");
            Equal(expected, row.OtherAreaM2, "OtherAreaM2");
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

        private static void AccumulatorRejectsNonFiniteInputs()
        {
            ThrowsInvalidOperation(() =>
            {
                var accumulator = new QuantityReportMath.FiniteAccumulator();
                accumulator.Add(double.NaN, "nan");
            });
            ThrowsInvalidOperation(() =>
            {
                var accumulator = new QuantityReportMath.FiniteAccumulator();
                accumulator.Add(double.PositiveInfinity, "infinity");
            });
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

        private static ElementInstance ElementWithAllQuantities(string id, FamilyDefinition family, double value)
        {
            return new ElementInstance(id, family, "L1")
            {
                GrossConcreteM3 = value,
                DeductionM3 = value,
                NetConcreteM3 = value,
                FormworkM2 = value,
                LengthM = value,
                OuterPerimeterM = value,
                InnerPerimeterM = value,
                DoorAreaM2 = value,
                SideAreaM2 = value,
                BottomAreaM2 = value,
                TopAreaM2 = value,
                OtherAreaM2 = value
            };
        }

        private static void Equal(double expected, double actual, string field)
        {
            if (actual != expected)
                throw new InvalidOperationException("Compensated " + field + " aggregation lost representable low-order contributions.");
        }

        private static void ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Expected non-finite compensated quantity-report input to fail closed.");
        }
    }
}
