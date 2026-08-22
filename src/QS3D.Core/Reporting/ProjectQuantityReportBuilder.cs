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
                if (AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) continue;
                var floor = floors.TryGetValue(element.FloorId, out var floorName) ? floorName : element.FloorId;
                var familyName = families.TryGetValue(element.FamilyId, out var family) ? family.Name : element.FamilyId;
                var category = element.Category.ToString();
                var key = element.FloorId + "\u001f" + category + "\u001f" + element.FamilyId;
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow { Floor = floor, Category = category, FamilyName = familyName, DrawingFingerprint = project.DrawingFingerprint };
                    rows[key] = row;
                    order.Add(key);
                }

                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(element.Id);
                AddHandles(row.SourceHandles, SourceHandleResolver.Resolve(project, new[] { element.Id }));
                var gross = QFirst(element, "GrossConcreteM3", "GrossVolumeM3");
                var net = QFirstOrFallback(element, gross, "NetConcreteM3", "NetVolumeM3");
                row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3, gross, element.Id + "/GrossConcreteM3");
                row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3, net, element.Id + "/NetConcreteM3");
                row.DeductionM3 = QuantityReportMath.Add(row.DeductionM3, Q(element, "DeductionM3", Math.Max(0d, gross - net)), element.Id + "/DeductionM3");
                row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2, Q(element, "FormworkM2"), element.Id + "/FormworkM2");
                row.LengthM = QuantityReportMath.Add(row.LengthM, Q(element, "LengthM"), element.Id + "/LengthM");
                row.OuterPerimeterM = QuantityReportMath.Add(row.OuterPerimeterM,
                    element.Category == ElementCategory.Room ? QFirst(element, "OuterPerimeterM", "PerimeterM") : Q(element, "OuterPerimeterM"),
                    element.Id + "/OuterPerimeterM");
                row.InnerPerimeterM = QuantityReportMath.Add(row.InnerPerimeterM,
                    element.Category == ElementCategory.Skirting ? QFirst(element, "InnerPerimeterM", "PerimeterM") : Q(element, "InnerPerimeterM"),
                    element.Id + "/InnerPerimeterM");
                row.DoorAreaM2 = QuantityReportMath.Add(row.DoorAreaM2,
                    element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening ? Q(element, "OpeningAreaM2") : Q(element, "DoorAreaM2"),
                    element.Id + "/DoorAreaM2");
                row.SideAreaM2 = QuantityReportMath.Add(row.SideAreaM2,
                    element.Category == ElementCategory.WallFinish ? QFirst(element, "NetFinishAreaM2", "SideAreaM2") : Q(element, "SideAreaM2"),
                    element.Id + "/SideAreaM2");
                row.BottomAreaM2 = QuantityReportMath.Add(row.BottomAreaM2,
                    element.Category == ElementCategory.FloorFinish || element.Category == ElementCategory.Waterproofing ? QFirst(element, "BottomAreaM2", "AreaM2") : Q(element, "BottomAreaM2"),
                    element.Id + "/BottomAreaM2");
                row.TopAreaM2 = QuantityReportMath.Add(row.TopAreaM2,
                    element.Category == ElementCategory.CeilingFinish ? QFirst(element, "TopAreaM2", "AreaM2") : Q(element, "TopAreaM2"),
                    element.Id + "/TopAreaM2");
                row.OtherAreaM2 = QuantityReportMath.Add(row.OtherAreaM2, Q(element, "OtherAreaM2"), element.Id + "/OtherAreaM2");
            }

            return order.Select(x => rows[x]).ToList();
        }

        private static void AddHandles(IList<string> destination, IEnumerable<string> source)
        {
            foreach (var handle in source.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                if (!destination.Contains(handle, StringComparer.OrdinalIgnoreCase)) destination.Add(handle);
        }

        private static double QFirst(ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return 0d;
        }

        private static double QFirstOrFallback(ProjectElement element, double fallback, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Quantities.ContainsKey(key)) return Q(element, key);
            return QuantityReportMath.Finite(fallback, element.Id + "/fallback");
        }

        private static double Q(ProjectElement element, string name, double fallback = 0d)
        {
            var value = element.Quantities.TryGetValue(name, out var stored) ? stored : fallback;
            return QuantityReportMath.Finite(value, element.Id + "/" + name);
        }
    }
}
