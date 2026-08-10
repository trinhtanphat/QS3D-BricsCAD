using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridAutoNumberCommands
    {
        private const int MaxGridBatch = 2000;
        private const double PlanElevationTolerance = 1e-6d;

        [CommandMethod("QS3DGRIDNUMBERAUTO", CommandFlags.UsePickSet)]
        public void AutoNumberGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(document);

            try
            {
                var selected = EntitySnapshotReader.ReadCurrentSelection(document);
                if (selected.Count == 0) return;
                if (selected.Count > MaxGridBatch)
                    throw new InvalidOperationException("Grid auto-number selection vượt giới hạn " + MaxGridBatch + ".");

                var extraction = ExtractParallelLineCandidates(document, project, selected);
                var orderingAxis = AcquireOrderingAxis(document.Editor);
                if (!orderingAxis.HasValue) return;

                var ordered = GridSpatialOrderingPlanner.OrderParallelLines(extraction.Curves, orderingAxis.Value);
                var orderedIds = ordered.Select(x => x.ElementId).ToList();
                var namingOptions = AcquireNamingOptions(document.Editor);
                if (namingOptions == null) return;

                if (!ConfirmPlan(document.Editor, ordered, namingOptions)) return;

                var rollback = ProjectStateSnapshot.Capture(project);
                IReadOnlyList<GridLabelAssignment> assignments;
                try
                {
                    assignments = GridNamingService.Renumber(project, orderedIds, namingOptions);
                    AuditTrail.ForProject(project).Record(
                        "grid.autonumber",
                        project.ProjectId,
                        assignments.Count.ToString(CultureInfo.InvariantCulture) + " Grid • " +
                        assignments[0].Label + " → " + assignments[assignments.Count - 1].Label +
                        " • explicit WCS ordering axis");
                }
                catch (Exception operationError)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Grid auto-number thất bại và semantic rollback cũng thất bại.",
                            new AggregateException(operationError, restoreError));
                    }
                    throw;
                }

                FinalizeUi(document, assignments, namingOptions, orderedIds, extraction.ObjectIdsByElementId);
            }
            catch (Exception ex)
            {
                ReportFailure(document, "QS3DGRIDNUMBERAUTO lỗi: " + ex.Message);
            }
        }

        private static CandidateExtraction ExtractParallelLineCandidates(
            Document document,
            ProjectState project,
            IReadOnlyList<QS3D.Core.Model.EntitySnapshot> selected)
        {
            var curves = new List<GridReferenceCurve>(selected.Count);
            var objectIds = new Dictionary<string, ObjectId>(StringComparer.OrdinalIgnoreCase);
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
                        throw new InvalidOperationException("Grid " + element.Id + " phải có đúng một authoritative source Handle để auto-number.");

                    var objectId = ResolveHandle(document.Database, snapshot.Handle);
                    var line = transaction.GetObject(objectId, OpenMode.ForRead, false) as Line;
                    if (line == null || line.IsErased)
                        throw new InvalidOperationException("QS3DGRIDNUMBERAUTO hiện chỉ hỗ trợ semantic Grid source kiểu LINE; nhận " + snapshot.EntityType + " tại " + snapshot.Handle + ".");

                    ValidatePoint(line.StartPoint, element.Id + "/start");
                    ValidatePoint(line.EndPoint, element.Id + "/end");
                    if (Math.Abs(line.StartPoint.Z - line.EndPoint.Z) > PlanElevationTolerance)
                        throw new InvalidOperationException("Grid LINE 3D nghiêng không được auto-order: " + element.Id + ".");

                    var elevation = 0.5d * line.StartPoint.Z + 0.5d * line.EndPoint.Z;
                    if (!Finite(elevation)) throw new InvalidOperationException("Grid LINE elevation không hữu hạn: " + element.Id + ".");
                    if (!planElevation.HasValue) planElevation = elevation;
                    else if (Math.Abs(elevation - planElevation.Value) > PlanElevationTolerance)
                        throw new InvalidOperationException("Các Grid LINE auto-number phải nằm trên cùng một plan elevation. Grid " + element.Id + " lệch cao độ.");

                    curves.Add(GridReferenceCurve.Line(
                        element.Id,
                        new Point2(line.StartPoint.X, line.StartPoint.Y),
                        new Point2(line.EndPoint.X, line.EndPoint.Y)));
                    objectIds[element.Id] = objectId;
                }
                transaction.Commit();
            }

            return new CandidateExtraction(curves.AsReadOnly(), objectIds);
        }

        private static Point2? AcquireOrderingAxis(Editor editor)
        {
            var first = editor.GetPoint("\nChọn điểm đầu trục sắp thứ tự Grid: ");
            if (first.Status != PromptStatus.OK) return null;

            var secondOptions = new PromptPointOptions("\nChọn điểm cuối trục sắp thứ tự Grid: ")
            {
                UseBasePoint = true,
                BasePoint = first.Value
            };
            var second = editor.GetPoint(secondOptions);
            if (second.Status != PromptStatus.OK) return null;

            var ucs = editor.CurrentUserCoordinateSystem;
            var firstWcs = first.Value.TransformBy(ucs);
            var secondWcs = second.Value.TransformBy(ucs);
            ValidatePoint(firstWcs, "ordering axis start WCS");
            ValidatePoint(secondWcs, "ordering axis end WCS");

            var dx = secondWcs.X - firstWcs.X;
            var dy = secondWcs.Y - firstWcs.Y;
            if (!Finite(dx) || !Finite(dy) || Math.Abs(dx) + Math.Abs(dy) <= 1e-12d)
                throw new InvalidOperationException("Ordering axis phải có hướng XY hữu hạn và khác 0.");
            return new Point2(dx, dy);
        }

        private static GridNamingOptions? AcquireNamingOptions(Editor editor)
        {
            var modeResult = editor.GetKeywords("\nKiểu nhãn Grid [Numeric/Alphabetic] <Numeric>: ", "Numeric Alphabetic");
            GridLabelSequence sequence;
            if (modeResult.Status == PromptStatus.None) sequence = GridLabelSequence.Numeric;
            else if (modeResult.Status != PromptStatus.OK) return null;
            else sequence = string.Equals(modeResult.StringResult, "Alphabetic", StringComparison.OrdinalIgnoreCase)
                ? GridLabelSequence.Alphabetic
                : GridLabelSequence.Numeric;

            var startOptions = new PromptIntegerOptions("\nChỉ số bắt đầu <1>: ")
            {
                AllowNone = true,
                AllowNegative = false,
                AllowZero = false,
                UseDefaultValue = true,
                DefaultValue = 1,
                LowerLimit = 1,
                UpperLimit = 999999
            };
            var startResult = editor.GetInteger(startOptions);
            if (startResult.Status != PromptStatus.OK && startResult.Status != PromptStatus.None) return null;
            var startIndex = startResult.Status == PromptStatus.OK ? startResult.Value : 1;

            var padding = 0;
            if (sequence == GridLabelSequence.Numeric)
            {
                var paddingOptions = new PromptIntegerOptions("\nSố chữ số zero-padding 0..6 <0>: ")
                {
                    AllowNone = true,
                    AllowNegative = false,
                    AllowZero = true,
                    UseDefaultValue = true,
                    DefaultValue = 0,
                    LowerLimit = 0,
                    UpperLimit = 6
                };
                var paddingResult = editor.GetInteger(paddingOptions);
                if (paddingResult.Status != PromptStatus.OK && paddingResult.Status != PromptStatus.None) return null;
                padding = paddingResult.Status == PromptStatus.OK ? paddingResult.Value : 0;
            }

            var prefix = PromptOptionalText(editor, "\nPrefix nhãn Grid <không>: ");
            if (prefix == null) return null;
            var suffix = PromptOptionalText(editor, "\nSuffix nhãn Grid <không>: ");
            if (suffix == null) return null;

            return new GridNamingOptions
            {
                Sequence = sequence,
                StartIndex = startIndex,
                NumericPadding = padding,
                Prefix = prefix,
                Suffix = suffix
            };
        }

        private static bool ConfirmPlan(Editor editor, IReadOnlyList<GridSpatialOrderingEntry> ordered, GridNamingOptions options)
        {
            var firstId = ordered[0].ElementId;
            var lastId = ordered[ordered.Count - 1].ElementId;
            editor.WriteMessage(
                "\nQS3D Grid Auto: planner đã sắp " + ordered.Count.ToString(CultureInfo.InvariantCulture) +
                " Grid từ " + firstId + " đến " + lastId + ". Axis start→end quyết định chiều tăng nhãn.");

            var confirm = editor.GetKeywords("\nÁp dụng auto-number theo thứ tự này? [Yes/No] <No>: ", "Yes No");
            if (confirm.Status == PromptStatus.None) return false;
            return confirm.Status == PromptStatus.OK && string.Equals(confirm.StringResult, "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string? PromptOptionalText(Editor editor, string prompt)
        {
            var result = editor.GetString(prompt);
            if (result.Status == PromptStatus.None) return string.Empty;
            if (result.Status != PromptStatus.OK) return null;
            return (result.StringResult ?? string.Empty).Trim();
        }

        private static void FinalizeUi(
            Document document,
            IReadOnlyList<GridLabelAssignment> assignments,
            GridNamingOptions options,
            IReadOnlyList<string> orderedIds,
            IReadOnlyDictionary<string, ObjectId> objectIdsByElementId)
        {
            var orderedObjectIds = orderedIds
                .Where(objectIdsByElementId.ContainsKey)
                .Select(x => objectIdsByElementId[x])
                .ToArray();
            try { document.Editor.SetImpliedSelection(orderedObjectIds); } catch { }

            var mode = options.Sequence == GridLabelSequence.Alphabetic ? "Alphabetic" : "Numeric";
            var status = "Grid Auto: đã đánh nhãn " + assignments.Count.ToString(CultureInfo.InvariantCulture) +
                         " trục • " + assignments[0].Label + " → " + assignments[assignments.Count - 1].Label + " • " + mode + ".";
            try { SelectionSyncCoordinator.Refresh(document); } catch { }
            try { PaletteCoordinator.RefreshProject(); } catch { }
            try { PaletteCoordinator.SetStatus(status); } catch { }
            TryWriteMessage(document, "\nQS3D " + status);
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

        private sealed class CandidateExtraction
        {
            public CandidateExtraction(IReadOnlyList<GridReferenceCurve> curves, IReadOnlyDictionary<string, ObjectId> objectIdsByElementId)
            {
                Curves = curves;
                ObjectIdsByElementId = objectIdsByElementId;
            }

            public IReadOnlyList<GridReferenceCurve> Curves { get; }
            public IReadOnlyDictionary<string, ObjectId> ObjectIdsByElementId { get; }
        }
    }
}
