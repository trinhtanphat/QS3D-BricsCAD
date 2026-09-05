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

            var snapshot = CaptureProjectRevision(project);
            EnsureProjectRevision(project, snapshot);

            var floors = snapshot.Floors.ToDictionary(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase);
            var families = snapshot.Families.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var rows = new Dictionary<string, CurtainWallScheduleRow>(StringComparer.OrdinalIgnoreCase);
            var accumulators = new Dictionary<string, CurtainWallAggregateState>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            foreach (var element in snapshot.Elements.Where(x => x.Category == ElementCategory.GlassWall).OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                EnsureProjectRevision(project, snapshot);
                var floorId = element.FloorId;
                var familyId = element.FamilyId;
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
                        ProjectId = snapshot.ProjectId,
                        DrawingFingerprint = snapshot.DrawingFingerprint,
                        Floor = floor,
                        FamilyName = family
                    };
                    rows[key] = row;
                    accumulators[key] = new CurtainWallAggregateState();
                    order.Add(key);
                }

                var aggregate = accumulators[key];
                row.WallCount = checked(row.WallCount + 1);
                row.PanelCount = AddInt(row.PanelCount, element.PanelCount, element.Id + "/CurtainPanelCount");
                row.VerticalFrameCount = AddInt(row.VerticalFrameCount, element.VerticalFrameCount, element.Id + "/CurtainVerticalFrameCount");
                row.HorizontalFrameCount = AddInt(row.HorizontalFrameCount, element.HorizontalFrameCount, element.Id + "/CurtainHorizontalFrameCount");
                aggregate.TotalWallLengthM.Add(element.LengthM, element.Id + "/LengthM");
                aggregate.GrossWallAreaM2.Add(element.GrossWallAreaM2, element.Id + "/GrossWallAreaM2");
                aggregate.OpeningAreaM2.Add(element.OpeningAreaM2, element.Id + "/OpeningAreaM2");
                aggregate.NetGlassAreaM2.Add(element.NetGlassAreaM2, element.Id + "/CurtainNetGlassAreaM2");
                aggregate.FrameFaceAreaM2.Add(element.FrameFaceAreaM2, element.Id + "/CurtainFrameFaceAreaM2");
                aggregate.FrameLengthM.Add(element.FrameLengthM, element.Id + "/CurtainFrameLengthM");
                row.MinimumClearPanelWidthM = Math.Min(row.MinimumClearPanelWidthM, element.MinimumClearPanelWidthM);
                row.MaximumClearPanelWidthM = Math.Max(row.MaximumClearPanelWidthM, element.MaximumClearPanelWidthM);
                row.MinimumClearPanelHeightM = Math.Min(row.MinimumClearPanelHeightM, element.MinimumClearPanelHeightM);
                row.MaximumClearPanelHeightM = Math.Max(row.MaximumClearPanelHeightM, element.MaximumClearPanelHeightM);
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                EnsureProjectRevision(project, snapshot);
            }

            EnsureProjectRevision(project, snapshot);
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
            EnsureProjectRevision(project, snapshot);
            return order.Select(x => rows[x]).ToList().AsReadOnly();
        }

        private static CurtainScheduleSnapshot CaptureProjectRevision(ProjectState project)
        {
            return new CurtainScheduleSnapshot(
                project.ChangeVersion,
                project.ProjectId,
                project.DrawingFingerprint,
                project.Elements.Select(CurtainElementSnapshot.Capture).ToList().AsReadOnly(),
                project.Floors.Select(x => new CurtainFloorSnapshot(x.Id, x.Name)).ToList().AsReadOnly(),
                project.Families.Select(x => new CurtainFamilySnapshot(x.Id, x.Name, x.Category)).ToList().AsReadOnly());
        }

        private static void EnsureProjectRevision(ProjectState project, CurtainScheduleSnapshot snapshot)
        {
            if (project.ChangeVersion != snapshot.Version ||
                !string.Equals(project.ProjectId, snapshot.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(project.DrawingFingerprint, snapshot.DrawingFingerprint, StringComparison.Ordinal) ||
                !SameElements(project.Elements, snapshot.Elements) ||
                !SameFloors(project.Floors, snapshot.Floors) ||
                !SameFamilies(project.Families, snapshot.Families))
                throw new InvalidOperationException(
                    "Project changed while the curtain wall schedule was being built; recompute the schedule against the current project state.");
        }

        private static bool SameElements(IList<ProjectElement> current, IReadOnlyList<CurtainElementSnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static bool SameFloors(IList<FloorDefinition> current, IReadOnlyList<CurtainFloorSnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static bool SameFamilies(IList<ProjectFamily> current, IReadOnlyList<CurtainFamilySnapshot> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!snapshot[index].Matches(current[index])) return false;
            return true;
        }

        private static string NormalizeReferenceId(string value)
        {
            return ReportingProjectIdentityGuard.NormalizeReferenceId(value);
        }

        private static bool SameSourceHandles(IList<string> current, IReadOnlyList<string> snapshot)
        {
            if (current.Count != snapshot.Count) return false;
            for (var index = 0; index < current.Count; index++)
                if (!string.Equals(current[index], snapshot[index], StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool SameQuantity(ProjectElement element, string key, double expected)
        {
            if (!element.Quantities.TryGetValue(key, out var value)) value = 0d;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) return false;
            return value.Equals(expected);
        }

        private static int QInt(ProjectElement element, string key)
        {
            var value = Q(element, key);
            var rounded = Math.Round(value);
            if (Math.Abs(value - rounded) > 1e-9d || rounded > int.MaxValue)
                throw new InvalidOperationException(element.Id + "/" + key + " must be an integer quantity within range.");
            return (int)rounded;
        }

        private static double Q(ProjectElement element, string key)
        {
            if (!element.Quantities.TryGetValue(key, out var value)) return 0d;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " must be finite and non-negative.");
            return value;
        }

        private static string GroupKey(string floorId, string familyId)
        {
            var floor = floorId ?? string.Empty;
            var family = familyId ?? string.Empty;
            return floor.Length.ToString(CultureInfo.InvariantCulture) + ":" + floor +
                   family.Length.ToString(CultureInfo.InvariantCulture) + ":" + family;
        }

        private static int AddInt(int left, int right, string label)
        {
            try { return checked(left + right); }
            catch (OverflowException ex) { throw new OverflowException(label + " overflowed.", ex); }
        }

        private sealed class CurtainScheduleSnapshot
        {
            internal CurtainScheduleSnapshot(long version, string projectId, string drawingFingerprint, IReadOnlyList<CurtainElementSnapshot> elements, IReadOnlyList<CurtainFloorSnapshot> floors, IReadOnlyList<CurtainFamilySnapshot> families)
            {
                Version = version;
                ProjectId = projectId;
                DrawingFingerprint = drawingFingerprint;
                Elements = elements;
                Floors = floors;
                Families = families;
            }

            internal long Version { get; }
            internal string ProjectId { get; }
            internal string DrawingFingerprint { get; }
            internal IReadOnlyList<CurtainElementSnapshot> Elements { get; }
            internal IReadOnlyList<CurtainFloorSnapshot> Floors { get; }
            internal IReadOnlyList<CurtainFamilySnapshot> Families { get; }
        }

        private sealed class CurtainFloorSnapshot
        {
            internal CurtainFloorSnapshot(string id, string name) { Id = id; Name = name; }
            internal string Id { get; }
            internal string Name { get; }
            internal bool Matches(FloorDefinition current) =>
                string.Equals(current.Id, Id, StringComparison.Ordinal) &&
                string.Equals(current.Name, Name, StringComparison.Ordinal);
        }

        private sealed class CurtainFamilySnapshot
        {
            internal CurtainFamilySnapshot(string id, string name, ElementCategory category) { Id = id; Name = name; Category = category; }
            internal string Id { get; }
            internal string Name { get; }
            internal ElementCategory Category { get; }
            internal bool Matches(ProjectFamily current) =>
                string.Equals(current.Id, Id, StringComparison.Ordinal) &&
                string.Equals(current.Name, Name, StringComparison.Ordinal) &&
                current.Category == Category;
        }

        private sealed class CurtainElementSnapshot
        {
            private CurtainElementSnapshot(ProjectElement element)
            {
                Id = element.Id;
                Category = element.Category;
                FloorId = NormalizeReferenceId(element.FloorId);
                FamilyId = NormalizeReferenceId(element.FamilyId);
                UpdatedUtc = element.UpdatedUtc;
                LengthM = Q(element, "LengthM");
                GrossWallAreaM2 = Q(element, "GrossWallAreaM2");
                OpeningAreaM2 = Q(element, "OpeningAreaM2");
                NetGlassAreaM2 = Q(element, "CurtainNetGlassAreaM2");
                FrameFaceAreaM2 = Q(element, "CurtainFrameFaceAreaM2");
                FrameLengthM = Q(element, "CurtainFrameLengthM");
                PanelCount = QInt(element, "CurtainPanelCount");
                VerticalFrameCount = QInt(element, "CurtainVerticalFrameCount");
                HorizontalFrameCount = QInt(element, "CurtainHorizontalFrameCount");
                MinimumClearPanelWidthM = Q(element, "CurtainMinClearPanelWidthM");
                MaximumClearPanelWidthM = Q(element, "CurtainMaxClearPanelWidthM");
                MinimumClearPanelHeightM = Q(element, "CurtainMinClearPanelHeightM");
                MaximumClearPanelHeightM = Q(element, "CurtainMaxClearPanelHeightM");
                if (MinimumClearPanelWidthM > MaximumClearPanelWidthM)
                    throw new InvalidOperationException(element.Id + "/CurtainClearPanelWidthM minimum cannot exceed maximum.");
                if (MinimumClearPanelHeightM > MaximumClearPanelHeightM)
                    throw new InvalidOperationException(element.Id + "/CurtainClearPanelHeightM minimum cannot exceed maximum.");
                SourceHandles = element.SourceHandles.ToList().AsReadOnly();
            }

            internal static CurtainElementSnapshot Capture(ProjectElement element) => new CurtainElementSnapshot(element);
            internal string Id { get; }
            internal ElementCategory Category { get; }
            internal string FloorId { get; }
            internal string FamilyId { get; }
            internal DateTime UpdatedUtc { get; }
            internal double LengthM { get; }
            internal double GrossWallAreaM2 { get; }
            internal double OpeningAreaM2 { get; }
            internal double NetGlassAreaM2 { get; }
            internal double FrameFaceAreaM2 { get; }
            internal double FrameLengthM { get; }
            internal int PanelCount { get; }
            internal int VerticalFrameCount { get; }
            internal int HorizontalFrameCount { get; }
            internal double MinimumClearPanelWidthM { get; }
            internal double MaximumClearPanelWidthM { get; }
            internal double MinimumClearPanelHeightM { get; }
            internal double MaximumClearPanelHeightM { get; }
            internal IReadOnlyList<string> SourceHandles { get; }

            internal bool Matches(ProjectElement current)
            {
                return string.Equals(current.Id, Id, StringComparison.Ordinal) &&
                       current.Category == Category &&
                       string.Equals(NormalizeReferenceId(current.FloorId), FloorId, StringComparison.Ordinal) &&
                       string.Equals(NormalizeReferenceId(current.FamilyId), FamilyId, StringComparison.Ordinal) &&
                       current.UpdatedUtc == UpdatedUtc &&
                       SameQuantity(current, "LengthM", LengthM) &&
                       SameQuantity(current, "GrossWallAreaM2", GrossWallAreaM2) &&
                       SameQuantity(current, "OpeningAreaM2", OpeningAreaM2) &&
                       SameQuantity(current, "CurtainNetGlassAreaM2", NetGlassAreaM2) &&
                       SameQuantity(current, "CurtainFrameFaceAreaM2", FrameFaceAreaM2) &&
                       SameQuantity(current, "CurtainFrameLengthM", FrameLengthM) &&
                       SameQuantity(current, "CurtainPanelCount", PanelCount) &&
                       SameQuantity(current, "CurtainVerticalFrameCount", VerticalFrameCount) &&
                       SameQuantity(current, "CurtainHorizontalFrameCount", HorizontalFrameCount) &&
                       SameQuantity(current, "CurtainMinClearPanelWidthM", MinimumClearPanelWidthM) &&
                       SameQuantity(current, "CurtainMaxClearPanelWidthM", MaximumClearPanelWidthM) &&
                       SameQuantity(current, "CurtainMinClearPanelHeightM", MinimumClearPanelHeightM) &&
                       SameQuantity(current, "CurtainMaxClearPanelHeightM", MaximumClearPanelHeightM) &&
                       SameSourceHandles(current.SourceHandles, SourceHandles);
            }
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
