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
            foreach (var element in elements)
            {
                if (element == null) continue;
                var key = element.Floor + "\u001f" + element.Family.Category + "\u001f" + element.Family.Name;
                if (!grouped.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow { Floor = element.Floor, Category = element.Family.Category.ToString(), FamilyName = element.Family.Name };
                    grouped.Add(key, row); order.Add(key);
                }
                row.Count++;
                row.ElementIds.Add(element.Id);
                row.GrossConcreteM3 += element.GrossConcreteM3;
                row.DeductionM3 += element.DeductionM3;
                row.NetConcreteM3 += element.NetConcreteM3;
                row.FormworkM2 += element.FormworkM2;
                row.LengthM += element.LengthM;
                row.OuterPerimeterM += element.OuterPerimeterM;
                row.InnerPerimeterM += element.InnerPerimeterM;
                row.DoorAreaM2 += element.DoorAreaM2;
                row.SideAreaM2 += element.SideAreaM2;
                row.BottomAreaM2 += element.BottomAreaM2;
                row.TopAreaM2 += element.TopAreaM2;
                row.OtherAreaM2 += element.OtherAreaM2;
            }
            var result = new List<QuantityReportRow>(order.Count);
            foreach (var key in order) result.Add(grouped[key]);
            return result;
        }
    }
}
