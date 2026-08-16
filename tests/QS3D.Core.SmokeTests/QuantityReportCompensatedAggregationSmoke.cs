using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportCompensatedAggregationSmoke
    {
        internal static void Run()
        {
            PreservesRepresentableSmallContributions();
            PreservesOrderSensitiveContributions();
            PreservesAllQuantityFields();
            PreservesOrdinaryAggregation();
            RejectsNonFiniteAndOverflowingTotals();
            Ed2ParityUsesCompensatedQuantityAndMassTotals();
            Ed2ParityStillRejectsWrongSummary();
        }

        private static void PreservesRepresentableSmallContributions()
        {
            var family = new FamilyDefinition("Precision Beam", ElementCategory.Beam, "Concrete");
            var rows = QuantityReportBuilder.Group(new[]
            {
                Element("precision-1", family, 10000000000000000d),
                Element("precision-2", family, 1d),
                Element("precision-3", family, 1d)
            });

            Equal(1, rows.Count, "Precision elements must remain in one report group.");
            Equal(3, rows[0].Count, "Precision group count must remain unchanged.");
            Equal(10000000000000002d, rows[0].GrossConcreteM3, "Grouped gross concrete must retain both small contributions.");
            Equal(10000000000000002d, rows[0].NetConcreteM3, "Grouped net concrete must retain both small contributions.");
            Equal(10000000000000002d, rows[0].LengthM, "Grouped length must retain both small contributions.");
        }

        private static void PreservesOrderSensitiveContributions()
        {
            var family = new FamilyDefinition("Order Beam", ElementCategory.Beam, "Concrete");
            var rows = QuantityReportBuilder.Group(new[]
            {
                Element("order-1", family, 1d),
                Element("order-2", family, 10000000000000000d),
                Element("order-3", family, 1d)
            });

            Equal(10000000000000002d, rows[0].GrossConcreteM3, "Small/huge/small ordering must preserve the representable total.");
            Equal(10000000000000002d, rows[0].LengthM, "Length aggregation must use the same compensated contract.");
        }

        private static void PreservesAllQuantityFields()
        {
            var family = new FamilyDefinition("All Fields", ElementCategory.Beam, "Concrete");
            var first = Element("all-1", family, 10000000000000000d);
            var second = Element("all-2", family, 1d);
            var third = Element("all-3", family, 1d);
            PopulateAll(first, 10000000000000000d);
            PopulateAll(second, 1d);
            PopulateAll(third, 1d);

            var row = QuantityReportBuilder.Group(new[] { first, second, third })[0];
            const double expected = 10000000000000002d;
            Equal(expected, row.GrossConcreteM3, "GrossConcreteM3 compensation drifted.");
            Equal(expected, row.DeductionM3, "DeductionM3 compensation drifted.");
            Equal(expected, row.NetConcreteM3, "NetConcreteM3 compensation drifted.");
            Equal(expected, row.FormworkM2, "FormworkM2 compensation drifted.");
            Equal(expected, row.LengthM, "LengthM compensation drifted.");
            Equal(expected, row.OuterPerimeterM, "OuterPerimeterM compensation drifted.");
            Equal(expected, row.InnerPerimeterM, "InnerPerimeterM compensation drifted.");
            Equal(expected, row.DoorAreaM2, "DoorAreaM2 compensation drifted.");
            Equal(expected, row.SideAreaM2, "SideAreaM2 compensation drifted.");
            Equal(expected, row.BottomAreaM2, "BottomAreaM2 compensation drifted.");
            Equal(expected, row.TopAreaM2, "TopAreaM2 compensation drifted.");
            Equal(expected, row.OtherAreaM2, "OtherAreaM2 compensation drifted.");
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
            Equal(60d, rows[0].NetConcreteM3, "Ordinary grouped net concrete must remain unchanged.");
            Equal(60d, rows[0].LengthM, "Ordinary grouped length must remain unchanged.");
        }

        private static void RejectsNonFiniteAndOverflowingTotals()
        {
            var family = new FamilyDefinition("Invalid Beam", ElementCategory.Beam, "Concrete");
            Throws<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { Element("nan", family, double.NaN) }));
            Throws<OverflowException>(() => QuantityReportBuilder.Group(new[]
            {
                Element("overflow-1", family, double.MaxValue),
                Element("overflow-2", family, double.MaxValue)
            }));
        }

        private static void Ed2ParityUsesCompensatedQuantityAndMassTotals()
        {
            var details = new[]
            {
                Ed2Detail("ed2-1", "A1", 10000000000000000d),
                Ed2Detail("ed2-2", "A2", 1d),
                Ed2Detail("ed2-3", "A3", 1d)
            };
            var summary = Ed2Summary(details, 10000000000000002d);
            var path = Path.Combine(Path.GetTempPath(), "qs3d-ed2-compensated-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                XlsxQuantityExporter.ExportEd2(path, details, new[] { summary });
                if (!File.Exists(path)) throw new InvalidOperationException("ED2 compensated parity export did not publish the workbook.");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void Ed2ParityStillRejectsWrongSummary()
        {
            var details = new[]
            {
                Ed2Detail("bad-1", "B1", 10000000000000000d),
                Ed2Detail("bad-2", "B2", 1d),
                Ed2Detail("bad-3", "B3", 1d)
            };
            var summary = Ed2Summary(details, 10000000000000000d);
            var path = Path.Combine(Path.GetTempPath(), "qs3d-ed2-wrong-summary-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(path, details, new[] { summary }));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static QuantityReportRow Ed2Detail(string id, string handle, double value)
        {
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Beam",
                FamilyId = "F1",
                FamilyName = "Precision Beam",
                ElementName = "Precision Beam",
                Material = "Concrete",
                DrawingFingerprint = "0123456789ABCDEF",
                Count = 1,
                GrossConcreteM3 = value,
                NetConcreteM3 = value,
                LengthM = value,
                MassKg = value
            };
            row.ElementIds.Add(id);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static QuantityReportRow Ed2Summary(IReadOnlyList<QuantityReportRow> details, double value)
        {
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Beam",
                FamilyId = "F1",
                FamilyName = "Precision Beam",
                ElementName = "Precision Beam",
                Material = "Concrete",
                DrawingFingerprint = "0123456789ABCDEF",
                Count = details.Count,
                GrossConcreteM3 = value,
                NetConcreteM3 = value,
                LengthM = value,
                MassKg = value
            };
            foreach (var detail in details)
            {
                row.ElementIds.Add(detail.ElementIds[0]);
                row.SourceHandles.Add(detail.SourceHandles[0]);
            }
            return row;
        }

        private static ElementInstance Element(string id, FamilyDefinition family, double value)
        {
            return new ElementInstance(id, family, "L1")
            {
                GrossConcreteM3 = value,
                NetConcreteM3 = value,
                LengthM = value
            };
        }

        private static void PopulateAll(ElementInstance element, double value)
        {
            element.GrossConcreteM3 = value;
            element.DeductionM3 = value;
            element.NetConcreteM3 = value;
            element.FormworkM2 = value;
            element.LengthM = value;
            element.OuterPerimeterM = value;
            element.InnerPerimeterM = value;
            element.DoorAreaM2 = value;
            element.SideAreaM2 = value;
            element.BottomAreaM2 = value;
            element.TopAreaM2 = value;
            element.OtherAreaM2 = value;
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
            }
            catch (TException)
            {
            }
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
