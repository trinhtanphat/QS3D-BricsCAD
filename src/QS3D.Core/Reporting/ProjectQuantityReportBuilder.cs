using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

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

                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(element.Id);
                AddHandles(row.SourceHandles, SourceHandleResolver.Resolve(project, new[] { element.Id }));
                var gross = Q(element, "GrossConcreteM3", Q(element, "GrossVolumeM3"));
                var net = Q(element, "NetConcreteM3", Q(element, "NetVolumeM3", gross));
                row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3, gross, element.Id + "/GrossConcreteM3");
                row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3, net, element.Id + "/NetConcreteM3");
                row.DeductionM3 = QuantityReportMath.Add(row.DeductionM3, Q(element, "DeductionM3", Math.Max(0d, gross - net)), element.Id + "/DeductionM3");
                row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2, Q(element, "FormworkM2"), element.Id + "/FormworkM2");
                row.LengthM = QuantityReportMath.Add(row.LengthM, Q(element, "LengthM"), element.Id + "/LengthM");
                row.OuterPerimeterM = QuantityReportMath.Add(row.OuterPerimeterM, Q(element, "OuterPerimeterM", element.Category == ElementCategory.Room ? Q(element, "PerimeterM") : 0d), element.Id + "/OuterPerimeterM");
                row.InnerPerimeterM = QuantityReportMath.Add(row.InnerPerimeterM, Q(element, "InnerPerimeterM", element.Category == ElementCategory.Skirting ? Q(element, "PerimeterM") : 0d), element.Id + "/InnerPerimeterM");
                row.DoorAreaM2 = QuantityReportMath.Add(row.DoorAreaM2, element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening ? Q(element, "OpeningAreaM2") : Q(element, "DoorAreaM2"), element.Id + "/DoorAreaM2");
                row.SideAreaM2 = QuantityReportMath.Add(row.SideAreaM2, Q(element, "SideAreaM2", element.Category == ElementCategory.WallFinish ? Q(element, "NetFinishAreaM2") : 0d), element.Id + "/SideAreaM2");
                row.BottomAreaM2 = QuantityReportMath.Add(row.BottomAreaM2, Q(element, "BottomAreaM2", element.Category == ElementCategory.FloorFinish || element.Category == ElementCategory.Waterproofing ? Q(element, "AreaM2") : 0d), element.Id + "/BottomAreaM2");
                row.TopAreaM2 = QuantityReportMath.Add(row.TopAreaM2, Q(element, "TopAreaM2", element.Category == ElementCategory.CeilingFinish ? Q(element, "AreaM2") : 0d), element.Id + "/TopAreaM2");
                row.OtherAreaM2 = QuantityReportMath.Add(row.OtherAreaM2, Q(element, "OtherAreaM2"), element.Id + "/OtherAreaM2");
            }

            return order.Select(x => rows[x]).ToList();
        }

        private static void AddHandles(IList<string> destination, IEnumerable<string> source)
        {
            foreach (var handle in source.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                if (!destination.Contains(handle, StringComparer.OrdinalIgnoreCase)) destination.Add(handle);
        }

        private static double Q(ProjectElement element, string name, double fallback = 0d)
        {
            var value = element.Quantities.TryGetValue(name, out var stored) ? stored : fallback;
            return QuantityReportMath.Finite(value, element.Id + "/" + name);
        }
    }
}
