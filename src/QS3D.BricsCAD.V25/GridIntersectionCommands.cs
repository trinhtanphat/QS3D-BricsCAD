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
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridIntersectionCommands
    {
        private const int MaxGridBatch = 2000;
        private const int MaxPrintedIntersections = 100;
        private const double PlanElevationTolerance = 1e-6d;
        private const double NormalTolerance = 1e-8d;
        private const double TwoPi = Math.PI * 2d;

        [CommandMethod("QS3DGRIDINTERSECTIONS", CommandFlags.UsePickSet)]
        public void InspectIntersections()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var selected = EntitySnapshotReader.ReadCurrentSelection(document);
                if (selected.Count == 0) return;
                if (selected.Count < 2)
                    throw new InvalidOperationException("Chọn ít nhất 2 Grid source để tính giao điểm.");
                if (selected.Count > MaxGridBatch)
                    throw new InvalidOperationException("Grid intersection selection vượt giới hạn " + MaxGridBatch + ".");

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    var blocked = "Grid Intersections: BLOCKED • chưa có QS3D project state/sidecar; lệnh inspect không tạo project mới.";
                    try { PaletteCoordinator.SetStatus(blocked); } catch { }
                    TryWriteMessage(document, "\nQS3D " + blocked);
                    return;
                }

                var extraction = ExtractCurves(document, project, selected);
                var intersections = GridIntersectionPlanner.FindIntersections(extraction.Curves);
                Report(document, project, intersections, extraction.PlanElevation);
            }
            catch (Exception ex)
            {
                ReportFailure(document, "QS3DGRIDINTERSECTIONS lỗi: " + ex.Message);
            }
        }

        private static ExtractionResult ExtractCurves(
            Document document,
            ProjectState project,
            IReadOnlyList<QS3D.Core.Model.EntitySnapshot> selected)
        {
            var curves = new List<GridReferenceCurve>(selected.Count);
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
                        throw new InvalidOperationException("Grid " + element.Id + " phải có đúng một authoritative source Handle để inspect intersection.");

                    var objectId = ResolveHandle(document.Database, snapshot.Handle);
                    var entity = transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Grid source không còn live: " + snapshot.Handle + ".");

                    if (entity is Line line)
                    {
                        AddLine(curves, element, line, ref planElevation);
                    }
                    else if (entity is Arc arc)
                    {
                        AddArc(curves, element, arc, ref planElevation);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "QS3DGRIDINTERSECTIONS chỉ hỗ trợ semantic Grid source kiểu LINE/ARC; nhận " + entity.GetType().Name + " tại " + snapshot.Handle + ".");
                    }
                }
                transaction.Commit();
            }

            return new ExtractionResult(curves.AsReadOnly(), planElevation ?? 0d);
        }

        private static void AddLine(
            ICollection<GridReferenceCurve> curves,
            ProjectElement element,
            Line line,
            ref double? planElevation)
        {
            ValidatePoint(line.StartPoint, element.Id + "/LINE start");
            ValidatePoint(line.EndPoint, element.Id + "/LINE end");
            if (Math.Abs(line.StartPoint.Z - line.EndPoint.Z) > PlanElevationTolerance)
                throw new InvalidOperationException("Grid LINE 3D nghiêng chưa được V25 intersection adapter hỗ trợ: " + element.Id + ".");

            var elevation = 0.5d * line.StartPoint.Z + 0.5d * line.EndPoint.Z;
            RequireCommonElevation(ref planElevation, elevation, element.Id);
            curves.Add(GridReferenceCurve.Line(
                element.Id,
                new Point2(line.StartPoint.X, line.StartPoint.Y),
                new Point2(line.EndPoint.X, line.EndPoint.Y)));
        }

        private static void AddArc(
            ICollection<GridReferenceCurve> curves,
            ProjectElement element,
            Arc arc,
            ref double? planElevation)
        {
            ValidatePoint(arc.Center, element.Id + "/ARC center");
            ValidatePoint(arc.StartPoint, element.Id + "/ARC start");
            ValidatePoint(arc.EndPoint, element.Id + "/ARC end");
            ValidatePositive(arc.Radius, element.Id + "/ARC radius");
            ValidatePositive(arc.TotalAngle, element.Id + "/ARC total angle");
            if (!Finite(arc.StartAngle))
                throw new InvalidOperationException("Grid ARC StartAngle không hữu hạn: " + element.Id + ".");
            if (arc.TotalAngle > TwoPi + 1e-10d)
                throw new InvalidOperationException("Grid ARC TotalAngle vượt 2π: " + element.Id + ".");

            var normal = arc.Normal;
            if (!Finite(normal.X) || !Finite(normal.Y) || !Finite(normal.Z))
                throw new InvalidOperationException("Grid ARC normal không hữu hạn: " + element.Id + ".");
            var normalLength = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            if (!Finite(normalLength) || normalLength <= NormalTolerance)
                throw new InvalidOperationException("Grid ARC normal suy biến: " + element.Id + ".");
            var nx = normal.X / normalLength;
            var ny = normal.Y / normalLength;
            var nz = normal.Z / normalLength;
            if (Math.Abs(nx) > NormalTolerance || Math.Abs(ny) > NormalTolerance || nz < 1d - NormalTolerance)
                throw new InvalidOperationException(
                    "Grid ARC tilted/negative-normal chưa được remote V25 intersection adapter hỗ trợ: " + element.Id + ". Yêu cầu normal +Z/WCS-XY.");

            if (Math.Abs(arc.StartPoint.Z - arc.Center.Z) > PlanElevationTolerance ||
                Math.Abs(arc.EndPoint.Z - arc.Center.Z) > PlanElevationTolerance)
                throw new InvalidOperationException("Grid ARC không nằm trên một WCS-XY plan elevation ổn định: " + element.Id + ".");

            RequireCommonElevation(ref planElevation, arc.Center.Z, element.Id);
            curves.Add(GridReferenceCurve.Arc(
                element.Id,
                new Point2(arc.Center.X, arc.Center.Y),
                arc.Radius,
                arc.StartAngle,
                Math.Min(arc.TotalAngle, TwoPi)));
        }

        private static void RequireCommonElevation(ref double? planElevation, double elevation, string elementId)
        {
            if (!Finite(elevation)) throw new InvalidOperationException("Grid elevation không hữu hạn: " + elementId + ".");
            if (!planElevation.HasValue)
            {
                planElevation = elevation;
                return;
            }
            if (Math.Abs(elevation - planElevation.Value) > PlanElevationTolerance)
                throw new InvalidOperationException("Các Grid source intersection phải nằm trên cùng một plan elevation. Grid " + elementId + " lệch cao độ.");
        }

        private static void Report(
            Document document,
            ProjectState project,
            IReadOnlyList<GridIntersection> intersections,
            double elevation)
        {
            var status = "Grid Intersections: " + intersections.Count.ToString(CultureInfo.InvariantCulture) +
                         " finite intersection(s) tại WCS Z=" + elevation.ToString("G17", CultureInfo.InvariantCulture) + ".";
            try { PaletteCoordinator.SetStatus(status); } catch { }
            TryWriteMessage(document, "\nQS3D " + status);

            var printed = Math.Min(intersections.Count, MaxPrintedIntersections);
            for (var index = 0; index < printed; index++)
            {
                var item = intersections[index];
                var first = GridName(project, item.FirstElementId);
                var second = GridName(project, item.SecondElementId);
                TryWriteMessage(
                    document,
                    "\n  [" + (index + 1).ToString(CultureInfo.InvariantCulture) + "] " + first + " × " + second +
                    " = (" + item.Point.X.ToString("G17", CultureInfo.InvariantCulture) + ", " +
                    item.Point.Y.ToString("G17", CultureInfo.InvariantCulture) + ", " +
                    elevation.ToString("G17", CultureInfo.InvariantCulture) + ") WCS");
            }

            if (intersections.Count > printed)
                TryWriteMessage(document, "\n  ... " + (intersections.Count - printed).ToString(CultureInfo.InvariantCulture) + " intersection(s) nữa không in để giới hạn command output.");
        }

        private static string GridName(ProjectState project, string elementId)
        {
            var element = project.FindElement(elementId);
            if (element != null && element.Properties.TryGetValue(GridNamingService.GridLabelKey, out var label) && !string.IsNullOrWhiteSpace(label))
                return label.Trim() + "[" + elementId + "]";
            return elementId;
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

        private static void ValidatePoint(Point3d point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y) || !Finite(point.Z))
                throw new InvalidOperationException(label + " chứa tọa độ không hữu hạn.");
        }

        private static void ValidatePositive(double value, string label)
        {
            if (!Finite(value) || value <= 0d)
                throw new InvalidOperationException(label + " phải hữu hạn và > 0.");
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

        private sealed class ExtractionResult
        {
            public ExtractionResult(IReadOnlyList<GridReferenceCurve> curves, double planElevation)
            {
                Curves = curves;
                PlanElevation = planElevation;
            }

            public IReadOnlyList<GridReferenceCurve> Curves { get; }
            public double PlanElevation { get; }
        }
    }
}
