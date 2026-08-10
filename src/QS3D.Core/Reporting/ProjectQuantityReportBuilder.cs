using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public static class ProjectQuantityReportBuilder
    {
        public static IReadOnlyList<QuantityReportRow> Group(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements)
            {
                var floor = floors.TryGetValue(element.FloorId, out var floorName) ? floorName : element.FloorId;
                var familyName = families.TryGetValue(element.FamilyId, out var family) ? family.Name : element.FamilyId;
                var category = element.Category.ToString();
                var key = element.FloorId + "\u001f" + category + "\u001f" + element.FamilyId;
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow { Floor = floor, Category = category, FamilyName = familyName };
                    rows[key] = row;
                    order.Add(key);
                }

                row.Count++;
                row.ElementIds.Add(element.Id);
                var gross = Q(element, "GrossConcreteM3", Q(element, "GrossVolumeM3"));
                var net = Q(element, "NetConcreteM3", Q(element, "NetVolumeM3", gross));
                row.GrossConcreteM3 += gross;
                row.NetConcreteM3 += net;
                row.DeductionM3 += Q(element, "DeductionM3", Math.Max(0d, gross - net));
                row.FormworkM2 += Q(element, "FormworkM2");
                row.LengthM += Q(element, "LengthM");
                row.OuterPerimeterM += Q(element, "OuterPerimeterM", element.Category == ElementCategory.Room ? Q(element, "PerimeterM") : 0d);
                row.InnerPerimeterM += Q(element, "InnerPerimeterM", element.Category == ElementCategory.Skirting ? Q(element, "PerimeterM") : 0d);
                row.DoorAreaM2 += element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening ? Q(element, "OpeningAreaM2") : Q(element, "DoorAreaM2");
                row.SideAreaM2 += Q(element, "SideAreaM2", element.Category == ElementCategory.WallFinish ? Q(element, "NetFinishAreaM2") : 0d);
                row.BottomAreaM2 += Q(element, "BottomAreaM2", element.Category == ElementCategory.FloorFinish || element.Category == ElementCategory.Waterproofing ? Q(element, "AreaM2") : 0d);
                row.TopAreaM2 += Q(element, "TopAreaM2", element.Category == ElementCategory.CeilingFinish ? Q(element, "AreaM2") : 0d);
                row.OtherAreaM2 += Q(element, "OtherAreaM2");
            }

            return order.Select(x => rows[x]).ToList();
        }

        private static double Q(ProjectElement element, string name, double fallback = 0d) => element.Quantities.TryGetValue(name, out var value) ? value : fallback;
    }
}
