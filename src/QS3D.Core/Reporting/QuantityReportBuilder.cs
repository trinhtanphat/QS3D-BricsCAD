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
            foreach (var element in elements)
            {
                if (element == null) continue;
                if (!seenElementIds.Add(element.Id))
                    throw new InvalidOperationException("Quantity report contains duplicate element id: " + element.Id + ".");
                var key = element.Floor + "\u001f" + element.Family.Category + "\u001f" + element.Family.Name;
                if (!grouped.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow { Floor = element.Floor, Category = element.Family.Category.ToString(), FamilyName = element.Family.Name };
                    grouped.Add(key, row); order.Add(key);
                }
                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3, element.GrossConcreteM3, element.Id + "/GrossConcreteM3");
                row.DeductionM3 = QuantityReportMath.Add(row.DeductionM3, element.DeductionM3, element.Id + "/DeductionM3");
                row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3, element.NetConcreteM3, element.Id + "/NetConcreteM3");
                row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2, element.FormworkM2, element.Id + "/FormworkM2");
                row.LengthM = QuantityReportMath.Add(row.LengthM, element.LengthM, element.Id + "/LengthM");
                row.OuterPerimeterM = QuantityReportMath.Add(row.OuterPerimeterM, element.OuterPerimeterM, element.Id + "/OuterPerimeterM");
                row.InnerPerimeterM = QuantityReportMath.Add(row.InnerPerimeterM, element.InnerPerimeterM, element.Id + "/InnerPerimeterM");
                row.DoorAreaM2 = QuantityReportMath.Add(row.DoorAreaM2, element.DoorAreaM2, element.Id + "/DoorAreaM2");
                row.SideAreaM2 = QuantityReportMath.Add(row.SideAreaM2, element.SideAreaM2, element.Id + "/SideAreaM2");
                row.BottomAreaM2 = QuantityReportMath.Add(row.BottomAreaM2, element.BottomAreaM2, element.Id + "/BottomAreaM2");
                row.TopAreaM2 = QuantityReportMath.Add(row.TopAreaM2, element.TopAreaM2, element.Id + "/TopAreaM2");
                row.OtherAreaM2 = QuantityReportMath.Add(row.OtherAreaM2, element.OtherAreaM2, element.Id + "/OtherAreaM2");
            }
            var result = new List<QuantityReportRow>(order.Count);
            foreach (var key in order) result.Add(grouped[key]);
            return result;
        }
    }
}
