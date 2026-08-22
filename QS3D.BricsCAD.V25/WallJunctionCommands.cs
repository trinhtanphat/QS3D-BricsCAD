using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class WallJunctionCommands
    {
        [CommandMethod("QS3DWALLJUNCTIONS", CommandFlags.UsePickSet)]
        public void AnalyzeWallJunctions()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var tolerance = MetadataNumber(project, "WallJunctionToleranceM", 0.005d, 0d);
                var sagitta = MetadataNumber(project, "WallArcSagittaM", 0.002d, 0d);
                var segments = ReadSelection(document, sagitta);
                if (segments.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DWALLJUNCTIONS: chọn LINE/open POLYLINE tim tường.");
                    return;
                }

                var nodes = new WallJunctionPlanner().Plan(segments, tolerance);
                var grouped = nodes.GroupBy(x => x.Kind).ToDictionary(x => x.Key, x => x.Count());
                string Count(WallJunctionKind kind) => grouped.TryGetValue(kind, out var count) ? count.ToString(CultureInfo.InvariantCulture) : "0";
                var summary = "Wall Junctions: L=" + Count(WallJunctionKind.L) +
                              " • T=" + Count(WallJunctionKind.T) +
                              " • X=" + Count(WallJunctionKind.X) +
                              " • Straight=" + Count(WallJunctionKind.Straight) +
                              " • End=" + Count(WallJunctionKind.End) +
                              " • Multi=" + Count(WallJunctionKind.Multi);
                PaletteCoordinator.SetStatus(summary);
                document.Editor.WriteMessage("\nQS3D " + summary);
                foreach (var node in nodes.Where(x => x.Kind == WallJunctionKind.L || x.Kind == WallJunctionKind.T || x.Kind == WallJunctionKind.X || x.Kind == WallJunctionKind.Multi).Take(100))
                {
                    document.Editor.WriteMessage("\n  " + node.Kind + " @ (" + node.Point.X.ToString("0.###", CultureInfo.InvariantCulture) + ", " + node.Point.Y.ToString("0.###", CultureInfo.InvariantCulture) + ") • " + string.Join(",", node.SegmentIds));
                }
                if (nodes.Count > 100) document.Editor.WriteMessage("\n  … output truncated; total nodes=" + nodes.Count.ToString(CultureInfo.InvariantCulture));
                AuditTrail.ForProject(project).Record("wall.junction.analyze", string.Empty, summary + " • sourceSegments=" + segments.Count.ToString(CultureInfo.InvariantCulture));
            }
            catch (System.Exception ex)
            {
                var message = "QS3DWALLJUNCTIONS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static IReadOnlyList<WallAxisSegment> ReadSelection(Document document, double sagittaM)
        {
            var editor = document.Editor;
            var selection = editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                var prompted = editor.GetSelection();
                if (prompted.Status != PromptStatus.OK || prompted.Value == null) return Array.Empty<WallAxisSegment>();
                selection = prompted;
            }

            var units = CadUnitService.GetPolicy(document);
            var result = new List<WallAxisSegment>();
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var handle = entity.Handle.ToString();
                    if (entity is Line line)
                    {
                        result.Add(new WallAxisSegment(handle,
                            new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                            new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y))));
                        continue;
                    }

                    if (!(entity is Polyline polyline)) continue;
                    if (polyline.Closed) throw new InvalidOperationException("Wall junction analysis dùng open POLYLINE centerline; closed polyline cần tách trước: " + handle);
                    var normal = polyline.Normal;
                    if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                        throw new InvalidOperationException("Wall centerline POLYLINE phải plan-view +Z: " + handle);
                    for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
                    {
                        var a = polyline.GetPoint2dAt(index);
                        var b = polyline.GetPoint2dAt(index + 1);
                        var start = new Point2(units.ToMeters(a.X), units.ToMeters(a.Y));
                        var end = new Point2(units.ToMeters(b.X), units.ToMeters(b.Y));
                        var bulge = polyline.GetBulgeAt(index);
                        var points = Math.Abs(bulge) <= 1e-12d
                            ? (IReadOnlyList<Point2>)new[] { start, end }
                            : BulgeArcTessellator.Tessellate(start, end, bulge, sagittaM);
                        for (var part = 1; part < points.Count; part++)
                            result.Add(new WallAxisSegment(handle + "/" + index.ToString(CultureInfo.InvariantCulture) + "/" + part.ToString(CultureInfo.InvariantCulture), points[part - 1], points[part]));
                    }
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private static double MetadataNumber(QS3D.Core.Domain.ProjectState project, string key, double fallback, double minimumExclusive)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= minimumExclusive)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }
    }
}
