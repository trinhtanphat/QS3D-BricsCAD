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
using QS3D.Core.Persistence;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridNamingCommands
    {
        private const int MaxGridBatch = 2000;

        [CommandMethod("QS3DGRIDNUMBER", CommandFlags.Modal)]
        public void RenumberGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            var project = ProjectContextCoordinator.GetOrCreate(document);

            IReadOnlyList<string>? orderedIds;
            GridNamingOptions? options;
            try
            {
                orderedIds = AcquireOrderedGridIds(document, project);
                if (orderedIds == null || orderedIds.Count == 0) return;
                options = AcquireOptions(document.Editor);
                if (options == null) return;
            }
            catch (Exception ex)
            {
                ReportOperationFailure(document, "QS3DGRIDNUMBER lỗi nhập liệu: " + ex.Message);
                return;
            }

            IReadOnlyList<string> annotatedIds;
            try
            {
                annotatedIds = CaptureAnnotatedGridIds(project, orderedIds);
            }
            catch (Exception ex)
            {
                ReportOperationFailure(document, "QS3DGRIDNUMBER không thể xác định annotation hiện hữu: " + ex.Message);
                return;
            }

            var rollback = ProjectStateSnapshot.Capture(project);
            IReadOnlyList<GridLabelAssignment> assignments;
            try
            {
                assignments = GridNamingService.Renumber(project, orderedIds, options);
                var first = assignments[0].Label;
                var last = assignments[assignments.Count - 1].Label;
                AuditTrail.ForProject(project).Record(
                    "grid.renumber",
                    project.ProjectId,
                    assignments.Count.ToString(CultureInfo.InvariantCulture) + " Grid • " + first + " → " + last);

                // Preserve the user's annotation intent. Grids that already had bubble/text before
                // renumber are rebuilt with the new label; unannotated Grids remain unannotated.
                // Keep this as the final fallible operation in the semantic try block: Build owns a
                // CAD transaction, so on failure that transaction aborts and the outer snapshot can
                // safely restore the pre-renumber semantic state while the old annotation survives.
                if (annotatedIds.Count > 0)
                    GridAnnotationBuilder.Build(document, project, ResolveGridElements(project, annotatedIds));
            }
            catch (Exception operationError)
            {
                try { rollback.Restore(project); }
                catch (Exception restoreError)
                {
                    ReportOperationFailure(document, "QS3DGRIDNUMBER lỗi và rollback project cũng lỗi: " + restoreError.Message);
                    throw new InvalidOperationException(
                        "Grid renumber failed and project rollback also failed.",
                        new AggregateException(operationError, restoreError));
                }
                ReportOperationFailure(document, "QS3DGRIDNUMBER lỗi: " + operationError.Message);
                return;
            }

            FinalizeUi(document, assignments, options);
        }

        private static IReadOnlyList<string> CaptureAnnotatedGridIds(ProjectState project, IReadOnlyList<string> orderedIds)
        {
            var byId = BuildGridIndex(project);
            var result = new List<string>();
            foreach (var rawId in orderedIds)
            {
                var id = (rawId ?? string.Empty).Trim();
                if (id.Length == 0 || !byId.TryGetValue(id, out var element))
                    throw new InvalidOperationException("Không tìm thấy semantic Grid trong project: " + rawId + ".");
                if (!element.Properties.TryGetValue(GridAnnotationBuilder.HandlesKey, out var handles) || string.IsNullOrWhiteSpace(handles))
                    continue;
                result.Add(element.Id);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> ResolveGridElements(ProjectState project, IReadOnlyList<string> ids)
        {
            var byId = BuildGridIndex(project);
            var result = new List<ProjectElement>(ids.Count);
            foreach (var rawId in ids)
            {
                var id = (rawId ?? string.Empty).Trim();
                if (id.Length == 0 || !byId.TryGetValue(id, out var element))
                    throw new InvalidOperationException("Grid annotation target không còn tồn tại sau renumber: " + rawId + ".");
                result.Add(element);
            }
            return result.AsReadOnly();
        }

        private static Dictionary<string, ProjectElement> BuildGridIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || element.Category != ElementCategory.Grid) continue;
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project chứa semantic Grid trùng Id: " + element.Id + ".");
                result.Add(element.Id, element);
            }
            return result;
        }

        private static IReadOnlyList<string>? AcquireOrderedGridIds(Document document, ProjectState project)
        {
            var editor = document.Editor;
            var orderedIds = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (orderedIds.Count < MaxGridBatch)
            {
                var prompt = orderedIds.Count == 0
                    ? "\nChọn Grid source thứ 1 theo thứ tự đánh nhãn: "
                    : "\nChọn Grid source thứ " + (orderedIds.Count + 1).ToString(CultureInfo.InvariantCulture) + " hoặc Enter để kết thúc: ";
                var options = new PromptEntityOptions(prompt)
                {
                    AllowNone = orderedIds.Count > 0
                };
                var result = editor.GetEntity(options);
                if (result.Status == PromptStatus.None && orderedIds.Count > 0) break;
                if (result.Status != PromptStatus.OK) return null;

                var handle = result.ObjectId.Handle.ToString();
                var matches = project.Elements
                    .Where(x => x != null && x.Category == ElementCategory.Grid && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                    .Take(2)
                    .ToList();
                if (matches.Count == 0)
                {
                    TryWriteMessage(document, "\nĐối tượng " + handle + " chưa phải Grid semantic source. Dùng QS3DGRID để capture trước.");
                    continue;
                }
                if (matches.Count > 1)
                    throw new InvalidOperationException("Grid source handle " + handle + " thuộc nhiều semantic Grid; sửa ownership trước khi đánh nhãn.");

                var element = matches[0];
                if (!seen.Add(element.Id))
                {
                    TryWriteMessage(document, "\nGrid " + element.Id + " đã có trong thứ tự; chọn Grid khác hoặc Enter để kết thúc.");
                    continue;
                }
                orderedIds.Add(element.Id);
            }

            if (orderedIds.Count >= MaxGridBatch)
                TryWriteMessage(document, "\nĐã đạt giới hạn " + MaxGridBatch.ToString(CultureInfo.InvariantCulture) + " Grid trong một batch.");
            return orderedIds.AsReadOnly();
        }

        private static GridNamingOptions? AcquireOptions(Editor editor)
        {
            var modeResult = editor.GetKeywords("\nKiểu nhãn Grid [Numeric/Alphabetic] <Numeric>: ", "Numeric Alphabetic");
            GridLabelSequence sequence;
            if (modeResult.Status == PromptStatus.None)
                sequence = GridLabelSequence.Numeric;
            else if (modeResult.Status != PromptStatus.OK)
                return null;
            else
                sequence = string.Equals(modeResult.StringResult, "Alphabetic", StringComparison.OrdinalIgnoreCase)
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

        private static string? PromptOptionalText(Editor editor, string prompt)
        {
            var result = editor.GetString(prompt);
            if (result.Status == PromptStatus.None) return string.Empty;
            if (result.Status != PromptStatus.OK) return null;
            return (result.StringResult ?? string.Empty).Trim();
        }

        private static void FinalizeUi(Document document, IReadOnlyList<GridLabelAssignment> assignments, GridNamingOptions options)
        {
            var first = assignments[0].Label;
            var last = assignments[assignments.Count - 1].Label;
            var mode = options.Sequence == GridLabelSequence.Alphabetic ? "Alphabetic" : "Numeric";
            var status = "Grid: đã đánh nhãn " + assignments.Count.ToString(CultureInfo.InvariantCulture) + " trục theo thứ tự chọn • " + first + " → " + last + " • " + mode + ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + status + " UI sync warning: " + ex.Message);
            }
        }

        private static void ReportOperationFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
