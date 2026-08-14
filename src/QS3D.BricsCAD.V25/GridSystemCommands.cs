using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridSystemCommands
    {
        private const int MaxGridBatch = 2000;
        private const double CoordinateTolerance = 1e-8d;
        private const double DirectionTolerance = 1e-6d;
        private const double PlanElevationTolerance = 1e-6d;

        [CommandMethod("QS3DGRIDSYSTEMPREVIEW", CommandFlags.UsePickSet)]
        public void PreviewRectangularSystem()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var selected = EntitySnapshotReader.ReadCurrentSelection(document);
                if (selected.Count == 0) return;
                if (selected.Count < 2)
                    throw new InvalidOperationException("Chọn ít nhất 2 Grid LINE thuộc hai phương vuông góc để review Grid system.");
                if (selected.Count > MaxGridBatch)
                    throw new InvalidOperationException("Grid system selection vượt giới hạn " + MaxGridBatch + ".");

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    const string blocked = "Grid System Preview: BLOCKED • chưa có QS3D project state/sidecar; preview không tạo project mới.";
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    TryWriteMessage(document, "\nQS3D " + blocked);
                    return;
                }

                var extraction = ExtractLines(document, project, selected);
                var preview = BuildPreview(extraction.Lines);
                var planned = GridSystemPlanner.PlanRectangular(
                    preview.Input,
                    CoordinateTolerance,
                    DirectionTolerance);
                var intersections = GridIntersectionPlanner.FindIntersections(planned);

                var status = "Grid System Preview: U=" + preview.UCount.ToString(CultureInfo.InvariantCulture) +
                             ", V=" + preview.VCount.ToString(CultureInfo.InvariantCulture) +
                             ", planned=" + planned.Count.ToString(CultureInfo.InvariantCulture) +
                             ", intersections=" + intersections.Count.ToString(CultureInfo.InvariantCulture) +
                             ", WCS Z=" + extraction.PlanElevation.ToString("G17", CultureInfo.InvariantCulture) +
                             " • READ-ONLY; no CAD entities created.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                TryWriteMessage(document, "\nQS3D " + status);
            }
            catch (Exception ex)
            {
                ReportFailure(document, "QS3DGRIDSYSTEMPREVIEW lỗi: " + ex.Message);
            }
        }

        private static ExtractionResult ExtractLines(
            Document document,
            ProjectState project,
            IReadOnlyList<QS3D.Core.Model.EntitySnapshot> selected)
        {
            var lines = new List<LineSample>(selected.Count);
            var seenElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            double? planElevation = null;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var snapshot in selected)
                {
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.Grid &&
                                    x.SourceHandles.Any(h => string.Equals((h ?? string.Empty).Trim(), snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0)
                        throw new InvalidOperationException("Entity " + snapshot.Handle + " chưa phải Grid semantic source. Chạy QS3DGRID trước.");
                    if (matches.Count > 1)
                        throw new InvalidOperationException("Grid source Handle " + snapshot.Handle + " thuộc nhiều semantic Grid; sửa ownership trước.");

                    var element = matches[0];
                    if (!seenElements.Add(element.Id))
                        throw new InvalidOperationException("Selection chứa cùng semantic Grid nhiều lần: " + element.Id + ".");

                    var authoritative = element.SourceHandles
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (authoritative.Count != 1 || !string.Equals(authoritative[0], snapshot.Handle, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Grid " + element.Id + " phải có đúng một authoritative source Handle để preview Grid system.");

                    var objectId = ResolveHandle(document.Database, snapshot.Handle);
                    var line = transaction.GetObject(objectId, OpenMode.ForRead, false) as Line;
                    if (line == null || line.IsErased)
                        throw new InvalidOperationException("QS3DGRIDSYSTEMPREVIEW chỉ hỗ trợ semantic Grid source kiểu LINE; Handle " + snapshot.Handle + " không còn là LINE live.");

                    ValidateFinite(line.StartPoint.X, element.Id + "/LINE start X");
                    ValidateFinite(line.StartPoint.Y, element.Id + "/LINE start Y");
                    ValidateFinite(line.StartPoint.Z, element.Id + "/LINE start Z");
                    ValidateFinite(line.EndPoint.X, element.Id + "/LINE end X");
                    ValidateFinite(line.EndPoint.Y, element.Id + "/LINE end Y");
                    ValidateFinite(line.EndPoint.Z, element.Id + "/LINE end Z");
                    if (Math.Abs(line.StartPoint.Z - line.EndPoint.Z) > PlanElevationTolerance)
                        throw new InvalidOperationException("Grid LINE 3D nghiêng chưa được rectangular system preview hỗ trợ: " + element.Id + ".");

                    var elevation = 0.5d * line.StartPoint.Z + 0.5d * line.EndPoint.Z;
                    RequireCommonElevation(ref planElevation, elevation, element.Id);

                    var dx = line.EndPoint.X - line.StartPoint.X;
                    var dy = line.EndPoint.Y - line.StartPoint.Y;
                    var length = Math.Sqrt(dx * dx + dy * dy);
                    if (!Finite(length) || length <= CoordinateTolerance)
                        throw new InvalidOperationException("Grid LINE suy biến trong WCS-XY: " + element.Id + ".");

                    lines.Add(new LineSample(
                        element.Id,
                        new Point2(line.StartPoint.X, line.StartPoint.Y),
                        new Point2(line.EndPoint.X, line.EndPoint.Y),
                        new Point2(dx / length, dy / length)));
                }
                transaction.Commit();
            }

            return new ExtractionResult(lines.AsReadOnly(), planElevation ?? 0d);
        }

        private static PreviewResult BuildPreview(IReadOnlyList<LineSample> lines)
        {
            if (lines == null || lines.Count < 2)
                throw new InvalidOperationException("Rectangular Grid system preview requires at least two LINE sources.");

            var vAxis = lines[0].Direction;
            var uAxis = new Point2(-vAxis.Y, vAxis.X);
            var uLines = new List<LineSample>();
            var vLines = new List<LineSample>();

            var uMin = double.PositiveInfinity;
            var uMax = double.NegativeInfinity;
            var vMin = double.PositiveInfinity;
            var vMax = double.NegativeInfinity;

            foreach (var line in lines)
            {
                AccumulateExtent(line.Start, uAxis, vAxis, ref uMin, ref uMax, ref vMin, ref vMax);
                AccumulateExtent(line.End, uAxis, vAxis, ref uMin, ref uMax, ref vMin, ref vMax);

                var dot = line.Direction.X * vAxis.X + line.Direction.Y * vAxis.Y;
                if (!Finite(dot))
                    throw new InvalidOperationException("Grid direction dot product không hữu hạn: " + line.ElementId + ".");
                var absoluteDot = Math.Abs(dot);

                if (1d - absoluteDot <= DirectionTolerance)
                {
                    uLines.Add(line);
                    continue;
                }
                if (absoluteDot <= DirectionTolerance)
                {
                    vLines.Add(line);
                    continue;
                }

                throw new InvalidOperationException(
                    "Grid LINE " + line.ElementId + " không thuộc một trong hai phương rectangular vuông góc trong tolerance " +
                    DirectionTolerance.ToString("G17", CultureInfo.InvariantCulture) + ".");
            }

            if (uLines.Count == 0 || vLines.Count == 0)
                throw new InvalidOperationException("Selection phải chứa Grid LINE ở cả hai phương rectangular vuông góc.");
            if (!Finite(uMin) || !Finite(uMax) || !Finite(vMin) || !Finite(vMax))
                throw new InvalidOperationException("Grid system extent không hữu hạn.");

            var orderedU = GridSpatialOrderingPlanner.OrderParallelLines(
                uLines.Select(ToReferenceCurve),
                uAxis,
                false,
                DirectionTolerance,
                CoordinateTolerance);
            var orderedV = GridSpatialOrderingPlanner.OrderParallelLines(
                vLines.Select(ToReferenceCurve),
                vAxis,
                false,
                DirectionTolerance,
                CoordinateTolerance);
            var uStations = orderedU
                .Select(x => new GridLinearStation(x.ElementId, x.Coordinate))
                .ToList();
            var vStations = orderedV
                .Select(x => new GridLinearStation(x.ElementId, x.Coordinate))
                .ToList();

            var input = new RectangularGridSystemInput
            {
                OriginM = new Point2(0d, 0d),
                UAxis = uAxis,
                VAxis = vAxis,
                UStations = uStations.AsReadOnly(),
                VStations = vStations.AsReadOnly(),
                UMinM = uMin,
                UMaxM = uMax,
                VMinM = vMin,
                VMaxM = vMax
            };
            return new PreviewResult(input, uStations.Count, vStations.Count);
        }

        private static GridReferenceCurve ToReferenceCurve(LineSample line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            return GridReferenceCurve.Line(line.ElementId, line.Start, line.End);
        }

        private static void AccumulateExtent(
            Point2 point,
            Point2 uAxis,
            Point2 vAxis,
            ref double uMin,
            ref double uMax,
            ref double vMin,
            ref double vMax)
        {
            var u = Dot(point, uAxis);
            var v = Dot(point, vAxis);
            uMin = Math.Min(uMin, u);
            uMax = Math.Max(uMax, u);
            vMin = Math.Min(vMin, v);
            vMax = Math.Max(vMax, v);
        }

        private static double Dot(Point2 point, Point2 axis)
        {
            var value = point.X * axis.X + point.Y * axis.Y;
            if (!Finite(value)) throw new InvalidOperationException("Grid projection vượt numeric range.");
            return value;
        }

        private static ObjectId ResolveHandle(Database database, string text)
        {
            if (!long.TryParse((text ?? string.Empty).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException("Grid source Handle không hợp lệ: " + text + ".");
            try
            {
                var id = database.GetObjectId(false, new Handle(value), 0);
                if (!id.IsNull && id.IsValid) return id;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Không resolve được Grid source Handle " + text + ".", ex);
            }
            throw new InvalidOperationException("Không resolve được Grid source Handle " + text + ".");
        }

        private static void RequireCommonElevation(ref double? planElevation, double elevation, string elementId)
        {
            ValidateFinite(elevation, elementId + "/elevation");
            if (!planElevation.HasValue)
            {
                planElevation = elevation;
                return;
            }
            if (Math.Abs(elevation - planElevation.Value) > PlanElevationTolerance)
                throw new InvalidOperationException("Các Grid source phải nằm trên cùng một plan elevation. Grid " + elementId + " lệch cao độ.");
        }

        private static void ValidateFinite(double value, string label)
        {
            if (!Finite(value)) throw new InvalidOperationException(label + " không hữu hạn.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void ReportFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }

        private sealed class LineSample
        {
            public LineSample(string elementId, Point2 start, Point2 end, Point2 direction)
            {
                ElementId = elementId;
                Start = start;
                End = end;
                Direction = direction;
            }

            public string ElementId { get; }
            public Point2 Start { get; }
            public Point2 End { get; }
            public Point2 Direction { get; }
        }

        private sealed class ExtractionResult
        {
            public ExtractionResult(IReadOnlyList<LineSample> lines, double planElevation)
            {
                Lines = lines;
                PlanElevation = planElevation;
            }

            public IReadOnlyList<LineSample> Lines { get; }
            public double PlanElevation { get; }
        }

        private sealed class PreviewResult
        {
            public PreviewResult(RectangularGridSystemInput input, int uCount, int vCount)
            {
                Input = input;
                UCount = uCount;
                VCount = vCount;
            }

            public RectangularGridSystemInput Input { get; }
            public int UCount { get; }
            public int VCount { get; }
        }
    }
}
