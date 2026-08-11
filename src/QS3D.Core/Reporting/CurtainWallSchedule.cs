using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Reporting
{
    public sealed class CurtainWallScheduleRow
    {
        public string Floor { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public int WallCount { get; set; }
        public double TotalWallLengthM { get; set; }
        public double GrossWallAreaM2 { get; set; }
        public double OpeningAreaM2 { get; set; }
        public double NetGlassAreaM2 { get; set; }
        public double FrameFaceAreaM2 { get; set; }
        public double FrameLengthM { get; set; }
        public int PanelCount { get; set; }
        public int VerticalFrameCount { get; set; }
        public int HorizontalFrameCount { get; set; }
        public double MinimumClearPanelWidthM { get; set; } = double.MaxValue;
        public double MaximumClearPanelWidthM { get; set; }
        public double MinimumClearPanelHeightM { get; set; } = double.MaxValue;
        public double MaximumClearPanelHeightM { get; set; }
        public IList<string> ElementIds { get; } = new List<string>();
    }

    public static class CurtainWallScheduleBuilder
    {
        public static IReadOnlyList<CurtainWallScheduleRow> Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ReportingProjectIdentityGuard.RequireUniqueElementIds(project, "Curtain wall schedule");
            var floors = project.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = project.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, CurtainWallScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements.Where(x => x.Category == ElementCategory.GlassWall).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var floor = floors.TryGetValue(element.FloorId, out var floorName) ? floorName : element.FloorId;
                var family = families.TryGetValue(element.FamilyId, out var familyDefinition) ? familyDefinition.Name : element.FamilyId;
                var key = element.FloorId + "\u001f" + element.FamilyId;
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new CurtainWallScheduleRow { Floor = floor, FamilyName = family };
                    rows[key] = row;
                    order.Add(key);
                }

                row.WallCount = checked(row.WallCount + 1);
                row.PanelCount = AddInt(row.PanelCount, QInt(element, "CurtainPanelCount"), element.Id + "/CurtainPanelCount");
                row.VerticalFrameCount = AddInt(row.VerticalFrameCount, QInt(element, "CurtainVerticalFrameCount"), element.Id + "/CurtainVerticalFrameCount");
                row.HorizontalFrameCount = AddInt(row.HorizontalFrameCount, QInt(element, "CurtainHorizontalFrameCount"), element.Id + "/CurtainHorizontalFrameCount");
                row.TotalWallLengthM = Add(row.TotalWallLengthM, Q(element, "LengthM"), element.Id + "/LengthM");
                row.GrossWallAreaM2 = Add(row.GrossWallAreaM2, Q(element, "GrossWallAreaM2"), element.Id + "/GrossWallAreaM2");
                row.OpeningAreaM2 = Add(row.OpeningAreaM2, Q(element, "OpeningAreaM2"), element.Id + "/OpeningAreaM2");
                row.NetGlassAreaM2 = Add(row.NetGlassAreaM2, Q(element, "CurtainNetGlassAreaM2"), element.Id + "/CurtainNetGlassAreaM2");
                row.FrameFaceAreaM2 = Add(row.FrameFaceAreaM2, Q(element, "CurtainFrameFaceAreaM2"), element.Id + "/CurtainFrameFaceAreaM2");
                row.FrameLengthM = Add(row.FrameLengthM, Q(element, "CurtainFrameLengthM"), element.Id + "/CurtainFrameLengthM");
                row.MinimumClearPanelWidthM = Math.Min(row.MinimumClearPanelWidthM, Q(element, "CurtainMinClearPanelWidthM"));
                row.MaximumClearPanelWidthM = Math.Max(row.MaximumClearPanelWidthM, Q(element, "CurtainMaxClearPanelWidthM"));
                row.MinimumClearPanelHeightM = Math.Min(row.MinimumClearPanelHeightM, Q(element, "CurtainMinClearPanelHeightM"));
                row.MaximumClearPanelHeightM = Math.Max(row.MaximumClearPanelHeightM, Q(element, "CurtainMaxClearPanelHeightM"));
                row.ElementIds.Add(element.Id);
            }

            foreach (var row in rows.Values)
            {
                if (row.MinimumClearPanelWidthM == double.MaxValue) row.MinimumClearPanelWidthM = 0d;
                if (row.MinimumClearPanelHeightM == double.MaxValue) row.MinimumClearPanelHeightM = 0d;
            }
            return order.Select(x => rows[x]).ToList();
        }

        private static double Q(ProjectElement element, string key)
        {
            if (!element.Quantities.TryGetValue(key, out var value)) return 0d;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " must be finite and non-negative.");
            return value;
        }

        private static int QInt(ProjectElement element, string key)
        {
            var value = Q(element, key);
            var rounded = Math.Round(value);
            if (Math.Abs(value - rounded) > 1e-9d || rounded > int.MaxValue)
                throw new InvalidOperationException(element.Id + "/" + key + " must be an integer quantity within range.");
            return (int)rounded;
        }

        private static int AddInt(int left, int right, string label)
        {
            try { return checked(left + right); }
            catch (OverflowException ex) { throw new OverflowException(label + " overflowed.", ex); }
        }

        private static double Add(double left, double right, string label)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || left < 0d || double.IsNaN(right) || double.IsInfinity(right) || right < 0d)
                throw new InvalidOperationException(label + " requires finite non-negative values.");
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            return result;
        }
    }
}
