using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportCompensatedAggregationSmoke
    {
        internal static void Run()
        {
            PreservesRepresentableSmallContributions();
            PreservesOrdinaryAggregation();
            RejectsNonFiniteContribution();
        }

        private static void PreservesRepresentableSmallContributions()
        {
            var family = new FamilyDefinition("Precision Beam", ElementCategory.Beam, "Concrete");
            var elements = new[]
            {
                Element("precision-1", family, 10000000000000000d),
                Element("precision-2", family, 1d),
                Element("precision-3", family, 1d)
            };

            var rows = QuantityReportBuilder.Group(elements);
            Equal(1, rows.Count, "Precision elements must remain in one report group.");
            Equal(3, rows[0].Count, "Precision group count must remain unchanged.");
            Equal(10000000000000002d, rows[0].GrossConcreteM3, "Grouped gross concrete must retain representable small contributions.");
            Equal(10000000000000002d, rows[0].NetConcreteM3, "Grouped net concrete must retain representable small contributions.");
            Equal(10000000000000002d, rows[0].LengthM, "Grouped length must retain representable small contributions.");
        }

        private static void PreservesOrdinaryAggregation()
        {
            var family = new FamilyDefinition("Ordinary Beam", ElementCategory.Beam, "Concrete");
            var rows = QuantityReportBuilder.Group(new[]
            {
                Element("ordinary-1", family, 10d),
                Element("ordinary-2", family, 20d),
                Element("ordinary-3", family, 30d)
            });
            Equal(60d, rows[0].GrossConcreteM3, "Ordinary grouped gross concrete must remain unchanged.");
            Equal(60d, rows[0].LengthM, "Ordinary grouped length must remain unchanged.");
        }

        private static void RejectsNonFiniteContribution()
        {
            var family = new FamilyDefinition("Invalid Beam", ElementCategory.Beam, "Concrete");
            try
            {
                QuantityReportBuilder.Group(new[] { Element("invalid-1", family, double.PositiveInfinity) });
                throw new InvalidOperationException("Non-finite grouped quantity must fail closed.");
            }
            catch (InvalidOperationException ex) when (ex.Message.IndexOf("not finite", StringComparison.OrdinalIgnoreCase) >= 0)
            {
            }
        }

        private static ElementInstance Element(string id, FamilyDefinition family, double value)
        {
            return new ElementInstance(id, family, "L1")
            {
                GrossConcreteM3 = value,
                LengthM = value
            };
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QuantityReportCompensatedAggregationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityReportCompensatedAggregationSmoke.Run();
        }
    }
}
