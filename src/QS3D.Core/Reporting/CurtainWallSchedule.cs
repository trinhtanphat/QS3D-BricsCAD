using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.Reporting
{
    public sealed class CurtainWallScheduleRow
    {
        public string ProjectId { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
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
        public IList<string> SourceHandles { get; } = new List<string>();
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
            var accumulators = new Dictionary<string, CurtainWallAggregateState>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in project.Elements.Where(x => x.Category == ElementCategory.GlassWall).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var floorId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FloorId);
                var familyId = ReportingProjectIdentityGuard.NormalizeReferenceId(element.FamilyId);
                var floor = floors.TryGetValue(floorId, out var floorName) ? floorName : floorId;
                families.TryGetValue(familyId, out var familyDefinition);
                if (familyDefinition != null && familyDefinition.Category != element.Category)
                    throw new InvalidOperationException("Curtain wall schedule element " + element.Id + " category " + element.Category + " does not match Family " + familyDefinition.Id + " category " + familyDefinition.Category + ". Repair the Family relation before reporting.");
                var family = familyDefinition?.Name ?? familyId;
                var key = GroupKey(floorId, familyId);
                if (!rows.TryGetValue(key, out var row))
                {
                    row = new CurtainWallScheduleRow
                    {
                        ProjectId = project.ProjectId,
                        DrawingFingerprint = project.DrawingFingerprint,
                        Floor = floor,
                        FamilyName = family
                    };
                    rows[key] = row;
                    accumulators[key] = new CurtainWallAggregateState();
                    order.Add(key);
                }

                var aggregate = accumulators[key];
                row.WallCount = checked(row.WallCount + 1);
                row.PanelCount = AddInt(row.PanelCount, QInt(element, "CurtainPanelCount"), element.Id + "/CurtainPanelCount");
                row.VerticalFrameCount = AddInt(row.VerticalFrameCount, QInt(element, "CurtainVerticalFrameCount"), element.Id + "/CurtainVerticalFrameCount");
                row.HorizontalFrameCount = AddInt(row.HorizontalFrameCount, QInt(element, "CurtainHorizontalFrameCount"), element.Id + "/CurtainHorizontalFrameCount");
                aggregate.TotalWallLengthM.Add(Q(element, "LengthM"), element.Id + "/LengthM");
                aggregate.GrossWallAreaM2.Add(Q(element, "GrossWallAreaM2"), element.Id + "/GrossWallAreaM2");
                aggregate.OpeningAreaM2.Add(Q(element, "OpeningAreaM2"), element.Id + "/OpeningAreaM2");
                aggregate.NetGlassAreaM2.Add(Q(element, "CurtainNetGlassAreaM2"), element.Id + "/CurtainNetGlassAreaM2");
                aggregate.FrameFaceAreaM2.Add(Q(element, "CurtainFrameFaceAreaM2"), element.Id + "/CurtainFrameFaceAreaM2");
                aggregate.FrameLengthM.Add(Q(element, "CurtainFrameLengthM"), element.Id + "/CurtainFrameLengthM");
                row.MinimumClearPanelWidthM = Math.Min(row.MinimumClearPanelWidthM, Q(element, "CurtainMinClearPanelWidthM"));
                row.MaximumClearPanelWidthM = Math.Max(row.MaximumClearPanelWidthM, Q(element, "CurtainMaxClearPanelWidthM"));
                row.MinimumClearPanelHeightM = Math.Min(row.MinimumClearPanelHeightM, Q(element, "CurtainMinClearPanelHeightM"));
                row.MaximumClearPanelHeightM = Math.Max(row.MaximumClearPanelHeightM, Q(element, "CurtainMaxClearPanelHeightM"));
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
            }

            foreach (var key in order)
            {
                var row = rows[key];
                var aggregate = accumulators[key];
                row.TotalWallLengthM = aggregate.TotalWallLengthM.Value("TotalWallLengthM");
                row.GrossWallAreaM2 = aggregate.GrossWallAreaM2.Value("GrossWallAreaM2");
                row.OpeningAreaM2 = aggregate.OpeningAreaM2.Value("OpeningAreaM2");
                row.NetGlassAreaM2 = aggregate.NetGlassAreaM2.Value("NetGlassAreaM2");
                row.FrameFaceAreaM2 = aggregate.FrameFaceAreaM2.Value("FrameFaceAreaM2");
                row.FrameLengthM = aggregate.FrameLengthM.Value("FrameLengthM");
                if (row.MinimumClearPanelWidthM == double.MaxValue) row.MinimumClearPanelWidthM = 0d;
                if (row.MinimumClearPanelHeightM == double.MaxValue) row.MinimumClearPanelHeightM = 0d;
            }
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private static string GroupKey(string floorId, string familyId)
        {
            var floor = floorId ?? string.Empty;
            var family = familyId ?? string.Empty;
            return floor.Length.ToString(CultureInfo.InvariantCulture) + ":" + floor +
                   family.Length.ToString(CultureInfo.InvariantCulture) + ":" + family;
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

        private sealed class CurtainWallAggregateState
        {
            internal CompensatedQuantity TotalWallLengthM { get; } = new CompensatedQuantity();
            internal CompensatedQuantity GrossWallAreaM2 { get; } = new CompensatedQuantity();
            internal CompensatedQuantity OpeningAreaM2 { get; } = new CompensatedQuantity();
            internal CompensatedQuantity NetGlassAreaM2 { get; } = new CompensatedQuantity();
            internal CompensatedQuantity FrameFaceAreaM2 { get; } = new CompensatedQuantity();
            internal CompensatedQuantity FrameLengthM { get; } = new CompensatedQuantity();
        }

        private sealed class CompensatedQuantity
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var incoming = QuantityReportMath.NonNegative(value, label);

                var result = _sum + incoming;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Curtain wall schedule aggregate overflowed: " + label + ".");

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - result) + incoming
                    : (incoming - result) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Curtain wall schedule aggregate compensation overflowed: " + label + ".");

                _sum = result == 0d ? 0d : result;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Curtain wall schedule aggregate overflowed: " + label + ".");
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Curtain wall schedule aggregate lost a non-zero compensation at floating-point precision: " + label + ".");
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Curtain wall schedule aggregate lost a non-zero accumulated value at floating-point precision: " + label + ".");
                return result == 0d ? 0d : result;
            }

            private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
            {
                if (current <= 0d || compensation == 0d) return false;
                var currentBits = BitConverter.DoubleToInt64Bits(current);
                var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
                var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
                var spacing = Math.Abs(adjacent - current);
                return Math.Abs(compensation) < spacing / 2d;
            }
        }
    }
}