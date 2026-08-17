using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Small command helpers required by the BLT3D-familiar MODELING ribbon where a visible action
    /// has stricter semantics than the raw native command name alone can express.
    /// </summary>
    public sealed class ModelingRibbonCommands
    {
        [CommandMethod("QS3DMOVEZ", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MoveAlongCurrentUcsZ()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var editor = document.Editor;
            try
            {
                var selection = editor.SelectImplied();
                if (selection.Status != PromptStatus.OK || selection.Value == null)
                {
                    var options = new PromptSelectionOptions
                    {
                        MessageForAdding = "\nChọn đối tượng cần di chuyển theo phương Z: "
                    };
                    selection = editor.GetSelection(options);
                }

                if (selection.Status != PromptStatus.OK || selection.Value == null)
                    return;

                var distanceOptions = new PromptDoubleOptions(
                    "\nNhập độ dịch chuyển theo trục Z của UCS hiện tại: ")
                {
                    AllowNegative = true,
                    AllowZero = false,
                    AllowNone = false
                };
                var distance = editor.GetDouble(distanceOptions);
                if (distance.Status != PromptStatus.OK)
                    return;
                if (double.IsNaN(distance.Value) || double.IsInfinity(distance.Value) || distance.Value == 0d)
                    throw new InvalidOperationException("Độ dịch chuyển Z phải là số hữu hạn khác 0.");

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("Bản vẽ active đã thay đổi trong lúc nhập độ dịch chuyển. Hãy chạy lại lệnh.");

                // Delegate the actual entity mutation to BricsCAD's native MOVE command so native
                // selection rules, locked-layer handling and Undo remain authoritative. MOVE's
                // Displacement option consumes the vector in the current UCS; X/Y are fixed at 0,
                // therefore the visible 'Theo phương Z' action cannot drift sideways.
                editor.Command(
                    "_.MOVE",
                    selection.Value,
                    string.Empty,
                    "_Displacement",
                    new Point3d(0d, 0d, distance.Value));

                PaletteCoordinator.SetStatus(
                    "MODELING • Theo phương Z: đã chuyển lựa chọn theo trục Z của UCS hiện tại.");
            }
            catch (Exception ex)
            {
                var message = "QS3DMOVEZ lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                try { editor.WriteMessage("\n" + message); } catch { }
            }
        }
    }
}
