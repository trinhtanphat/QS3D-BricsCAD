using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridNamingCommands
    {
        private const int MaxGridBatch = 2000;
        private const int MaxSequenceIndex = 999999;
        private const int MaxNumericPadding = 6;

        [CommandMethod("QS3DGRIDRENUMBER", CommandFlags.Modal)]
        public void RenumberGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var editor = document.Editor;
                var project = ProjectContextCoordinator.GetOrCreate(document);

                var count = PromptInteger(editor, "\nSố Grid cần đánh số: ", 1, MaxGridBatch, null);
                if (!count.HasValue) return;

                var orderedIds = new List<string>(count.Value);
                var pickedObjectIds = new List<ObjectId>(count.Value);
                var seenElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < count.Value; index++)
                {
                    var picked = editor.GetEntity("\nChọn Grid thứ " + (index + 1) + "/" + count.Value + ": ");
                    if (picked.Status != PromptStatus.OK) return;
                    if (picked.ObjectId.IsNull || !picked.ObjectId.IsValid || picked.ObjectId.IsErased)
                        throw new InvalidOperationException("CAD entity được chọn không còn hợp lệ.");

                    var handle = picked.ObjectId.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.Grid &&
                                    x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();

                    if (matches.Count == 0)
                        throw new InvalidOperationException("Entity " + handle + " chưa được capture bằng QS3DGRID.");
                    if (matches.Count > 1)
                        throw new InvalidOperationException("Grid source handle " + handle + " đang thuộc nhiều semantic Grid; chạy Health/repair trước khi renumber.");
                    if (!seenElementIds.Add(matches[0].Id))
                        throw new InvalidOperationException("Không được chọn cùng một Grid nhiều lần trong một batch renumber.");

                    orderedIds.Add(matches[0].Id);
                    pickedObjectIds.Add(picked.ObjectId);
                }

                var sequenceResult = editor.GetKeywords("\nKiểu nhãn Grid [Numeric/Alphabetic] <Numeric>: ", "Numeric Alphabetic");
                if (sequenceResult.Status != PromptStatus.OK && sequenceResult.Status != PromptStatus.None) return;
                var alphabetic = sequenceResult.Status == PromptStatus.OK &&
                                 string.Equals(sequenceResult.StringResult, "Alphabetic", StringComparison.OrdinalIgnoreCase);

                var start = PromptInteger(editor, "\nChỉ số bắt đầu <1>: ", 1, MaxSequenceIndex, 1);
                if (!start.HasValue) return;
                if (start.Value > MaxSequenceIndex - (count.Value - 1))
                    throw new InvalidOperationException("Dãy Grid vượt quá chỉ số tối đa " + MaxSequenceIndex + ".");

                var padding = 0;
                if (!alphabetic)
                {
                    var requestedPadding = PromptInteger(editor, "\nZero-padding số Grid 0..6 <0>: ", 0, MaxNumericPadding, 0);
                    if (!requestedPadding.HasValue) return;
                    padding = requestedPadding.Value;
                }

                var prefix = PromptOptionalText(editor, "\nPrefix Grid <trống>: ");
                if (prefix == null) return;
                var suffix = PromptOptionalText(editor, "\nSuffix Grid <trống>: ");
                if (suffix == null) return;

                var options = new GridNamingOptions
                {
                    Sequence = alphabetic ? GridLabelSequence.Alphabetic : GridLabelSequence.Numeric,
                    Prefix = prefix,
                    Suffix = suffix,
                    StartIndex = start.Value,
                    NumericPadding = padding
                };

                IReadOnlyList<GridLabelAssignment> assignments;
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    assignments = GridNamingService.Renumber(project, orderedIds, options);
                }
                catch (Exception operationError)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Grid renumber thất bại và semantic rollback cũng thất bại.",
                            new AggregateException(operationError, restoreError));
                    }
                    throw;
                }

                FinalizeUi(document, assignments, pickedObjectIds);
            }
            catch (Exception ex)
            {
                var message = "QS3DGRIDRENUMBER lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                TryWriteMessage(document, "\n" + message);
            }
        }

        private static int? PromptInteger(Editor editor, string message, int lower, int upper, int? defaultValue)
        {
            var options = new PromptIntegerOptions(message)
            {
                AllowNegative = lower < 0,
                AllowZero = lower <= 0,
                AllowNone = defaultValue.HasValue,
                LowerLimit = lower,
                UpperLimit = upper
            };
            if (defaultValue.HasValue)
            {
                options.UseDefaultValue = true;
                options.DefaultValue = defaultValue.Value;
            }

            var result = editor.GetInteger(options);
            if (result.Status == PromptStatus.None && defaultValue.HasValue) return defaultValue.Value;
            return result.Status == PromptStatus.OK ? result.Value : (int?)null;
        }

        private static string? PromptOptionalText(Editor editor, string message)
        {
            var result = editor.GetString(message);
            if (result.Status == PromptStatus.None) return string.Empty;
            if (result.Status != PromptStatus.OK) return null;
            return (result.StringResult ?? string.Empty).Trim();
        }

        private static void FinalizeUi(Document document, IReadOnlyList<GridLabelAssignment> assignments, IReadOnlyList<ObjectId> pickedObjectIds)
        {
            var first = assignments.Count > 0 ? assignments[0].Label : string.Empty;
            var last = assignments.Count > 0 ? assignments[assignments.Count - 1].Label : string.Empty;
            var status = "Grid/Trục: đã renumber " + assignments.Count + " Grid" +
                         (assignments.Count > 0 ? " (" + first + (assignments.Count > 1 ? " → " + last : string.Empty) + ")." : ".");

            try { document.Editor.SetImpliedSelection(pickedObjectIds.ToArray()); } catch { }
            try { SelectionSyncCoordinator.Refresh(document); } catch { }
            try { PaletteCoordinator.RefreshProject(); } catch { }
            try { PaletteCoordinator.SetStatus(status); } catch { }
            TryWriteMessage(document, "\nQS3D " + status);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
