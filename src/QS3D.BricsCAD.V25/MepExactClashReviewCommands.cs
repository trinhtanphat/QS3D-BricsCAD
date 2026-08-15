using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Mep;
using QS3D.Core.Model;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Transient review for an already-isolated exact MEP clash pair.
    /// Requires exactly two selected recognized Solid3d entities, rechecks native interference,
    /// highlights both for review, and best-effort removes highlight state after the review.
    /// Entity.Highlight/Unhighlight does not provide an ownership token in this implementation,
    /// so pre-existing external highlight preservation is not claimed.
    /// </summary>
    public sealed class MepExactClashReviewCommands
    {
        private static MepRecognitionProfile RecognitionProfile => MepRecognitionProfileProvider.Current;

        [CommandMethod("QS3DMEPEXACTCLASHHIGHLIGHT", CommandFlags.UsePickSet)]
        public void HighlightExactPair()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            string? leftHandle = null;
            string? rightHandle = null;
            var highlightApplied = false;
            try
            {
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count != 2)
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPEXACTCLASHHIGHLIGHT: cần đúng 2 entity selected; dùng clash Locate để cô lập pair trước.");
                    return;
                }

                if (!TryRecognize(snapshots[0], out var leftDiscipline) ||
                    !TryRecognize(snapshots[1], out var rightDiscipline) ||
                    (leftDiscipline != MepRecognitionDiscipline.Mep && rightDiscipline != MepRecognitionDiscipline.Mep))
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPEXACTCLASHHIGHLIGHT: pair không có classification hợp lệ với ít nhất một phía MEP.");
                    return;
                }

                leftHandle = snapshots[0].Handle;
                rightHandle = snapshots[1].Handle;
                var ids = CadHandleService.Resolve(document, new[] { leftHandle, rightHandle });
                if (ids.Count != 2)
                {
                    document.Editor.WriteMessage(
                        "\nQS3DMEPEXACTCLASHHIGHLIGHT: pair đã stale/không resolve đủ 2 live entity.");
                    return;
                }

                if (!TryVerifyAndHighlight(document, ids, out var error))
                {
                    document.Editor.WriteMessage("\nQS3DMEPEXACTCLASHHIGHLIGHT: " + error);
                    return;
                }
                highlightApplied = true;

                document.Editor.SetImpliedSelection(new List<ObjectId>(ids).ToArray());
                document.Editor.WriteMessage(
                    "\nQS3DMEPEXACTCLASHHIGHLIGHT: ExactHard • " + leftHandle + " ↔ " + rightHandle +
                    " • đang highlight transient.");
                document.Editor.GetString("\nNhấn Enter hoặc Esc để kết thúc highlight review: ");
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DMEPEXACTCLASHHIGHLIGHT lỗi: " + ex.Message);
            }
            finally
            {
                if (highlightApplied && !string.IsNullOrWhiteSpace(leftHandle) && !string.IsNullOrWhiteSpace(rightHandle))
                    UnhighlightBestEffort(document, leftHandle!, rightHandle!);
            }
        }

        private static bool TryVerifyAndHighlight(Document document, IReadOnlyList<ObjectId> ids, out string error)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                Solid3d? left = null;
                Solid3d? right = null;
                var leftHighlighted = false;
                var rightHighlighted = false;
                try
                {
                    left = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                    right = transaction.GetObject(ids[1], OpenMode.ForRead, false) as Solid3d;
                    if (left == null || right == null || left.IsErased || right.IsErased)
                    {
                        error = "pair không còn là 2 Solid3d live.";
                        return false;
                    }
                    if (!left.CheckInterference(right))
                    {
                        error = "pair hiện không còn exact Solid3d interference; không highlight.";
                        return false;
                    }

                    left.Highlight();
                    leftHighlighted = true;
                    right.Highlight();
                    rightHighlighted = true;
                    transaction.Commit();
                    error = string.Empty;
                    return true;
                }
                catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
                {
                    if (rightHighlighted && right != null) TryUnhighlight(right);
                    if (leftHighlighted && left != null) TryUnhighlight(left);
                    error = "native exact-review thất bại: " + ex.Message;
                    return false;
                }
            }
        }

        private static void UnhighlightBestEffort(Document document, string leftHandle, string rightHandle)
        {
            try
            {
                var ids = CadHandleService.Resolve(document, new[] { leftHandle, rightHandle });
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    for (var i = 0; i < ids.Count; i++)
                    {
                        try
                        {
                            var entity = transaction.GetObject(ids[i], OpenMode.ForRead, false) as Entity;
                            if (entity != null && !entity.IsErased) entity.Unhighlight();
                        }
                        catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
                        {
                        }
                    }
                    transaction.Commit();
                }
            }
            catch (System.Exception ex) when (IsRecoverableEntityFailure(ex))
            {
            }
        }

        private static void TryUnhighlight(Entity entity)
        {
            try { entity.Unhighlight(); }
            catch (System.Exception ex) when (IsRecoverableEntityFailure(ex)) { }
        }

        private static bool TryRecognize(EntitySnapshot snapshot, out MepRecognitionDiscipline discipline)
        {
            snapshot.Metadata.TryGetValue("BlockName", out var blockName);
            var recognition = RecognitionProfile.Recognize(snapshot.Layer, blockName);
            if (recognition.Status != MepRecognitionStatus.Matched || !recognition.Discipline.HasValue)
            {
                discipline = default(MepRecognitionDiscipline);
                return false;
            }
            discipline = recognition.Discipline.Value;
            return true;
        }

        private static bool IsRecoverableEntityFailure(System.Exception exception) =>
            !(exception is OutOfMemoryException) &&
            !(exception is StackOverflowException) &&
            !(exception is AccessViolationException);
    }
}
