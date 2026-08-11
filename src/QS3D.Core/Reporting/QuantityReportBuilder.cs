using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public static class QuantityReportBuilder
    {
        public static IReadOnlyList<QuantityReportRow> Group(IEnumerable<ElementInstance> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var order = new List<string>();
            var grouped = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var seenElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var elementIndex = 0;
            foreach (var element in elements)
            {
                if (element == null)
                    throw new ArgumentException("Quantity report elements cannot contain null entries. Invalid element index: " + elementIndex + ".", nameof(elements));
                if (!seenElementIds.Add(element.Id))
                    throw new InvalidOperationException("Quantity report contains duplicate element id: " + element.Id + ".");
                var material = NormalizeMaterial(element.Family.Material);
                var key = element.Floor + "\u001f" + element.Family.Category + "\u001f" + element.Family.Name + "\u001f" + material;
                if (!grouped.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow
                    {
                        Floor = element.Floor,
                        Category = element.Family.Category.ToString(),
                        FamilyName = element.Family.Name,
                        Material = material
                    };
                    grouped.Add(key, row); order.Add(key);
                }
                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3, NonNegative(element.GrossConcreteM3, element.Id, "GrossConcreteM3"), element.Id + "/GrossConcreteM3");
                row.DeductionM3 = QuantityReportMath.Add(row.DeductionM3, NonNegative(element.DeductionM3, element.Id, "DeductionM3"), element.Id + "/DeductionM3");
                row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3, NonNegative(element.NetConcreteM3, element.Id, "NetConcreteM3"), element.Id + "/NetConcreteM3");
                row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2, NonNegative(element.FormworkM2, element.Id, "FormworkM2"), element.Id + "/FormworkM2");
                row.LengthM = QuantityReportMath.Add(row.LengthM, NonNegative(element.LengthM, element.Id, "LengthM"), element.Id + "/LengthM");
                row.OuterPerimeterM = QuantityReportMath.Add(row.OuterPerimeterM, NonNegative(element.OuterPerimeterM, element.Id, "OuterPerimeterM"), element.Id + "/OuterPerimeterM");
                row.InnerPerimeterM = QuantityReportMath.Add(row.InnerPerimeterM, NonNegative(element.InnerPerimeterM, element.Id, "InnerPerimeterM"), element.Id + "/InnerPerimeterM");
                row.DoorAreaM2 = QuantityReportMath.Add(row.DoorAreaM2, NonNegative(element.DoorAreaM2, element.Id, "DoorAreaM2"), element.Id + "/DoorAreaM2");
                row.SideAreaM2 = QuantityReportMath.Add(row.SideAreaM2, NonNegative(element.SideAreaM2, element.Id, "SideAreaM2"), element.Id + "/SideAreaM2");
                row.BottomAreaM2 = QuantityReportMath.Add(row.BottomAreaM2, NonNegative(element.BottomAreaM2, element.Id, "BottomAreaM2"), element.Id + "/BottomAreaM2");
                row.TopAreaM2 = QuantityReportMath.Add(row.TopAreaM2, NonNegative(element.TopAreaM2, element.Id, "TopAreaM2"), element.Id + "/TopAreaM2");
                row.OtherAreaM2 = QuantityReportMath.Add(row.OtherAreaM2, NonNegative(element.OtherAreaM2, element.Id, "OtherAreaM2"), element.Id + "/OtherAreaM2");
                elementIndex++;
            }
            var result = new List<QuantityReportRow>(order.Count);
            foreach (var key in order) result.Add(grouped[key]);
            return result;
        }

        private static string NormalizeMaterial(string material) =>
            string.IsNullOrWhiteSpace(material) ? "Khác" : material.Trim();

        private static double NonNegative(double value, string elementId, string quantity) =>
            QuantityReportMath.NonNegative(value, elementId + "/" + quantity);
    }
}
