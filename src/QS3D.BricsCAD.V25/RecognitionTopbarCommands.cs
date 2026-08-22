using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Host-safe command adapters for the BLT3D-familiar NHẬN DẠNG ribbon.
    /// These commands reuse only recognition-compatible QS3D workflows; an action with no
    /// matching generic workflow fails closed instead of dispatching an unrelated command.
    /// </summary>
    public sealed class RecognitionTopbarCommands
    {
        [CommandMethod("QS3DRECOGNITIONRESTORE", CommandFlags.UsePickSet)]
        public void RestoreSelected()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var ids = ResolveSelection(document, "Chọn đối tượng cần khôi phục vào ngữ cảnh nhận dạng");
            if (ids == null) return;

            document.Editor.SetImpliedSelection(ids);
            document.Editor.WriteMessage("\nQS3D Nhận dạng: đã khôi phục " + ids.Length + " đối tượng vào selection nhận dạng hiện hành.");
            Queue(document, "QS3DINSPECT");
        }

        [CommandMethod("QS3DRECOGNITIONOPTIONS")]
        public void RecognitionOptions()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            document.Editor.WriteMessage(
                "\nQS3D Nhận dạng: Tùy chọn nhận dạng chưa có workflow generic tương ứng; " +
                "không mở MEP Review/Takeoff thay thế.");
        }

        [CommandMethod("QS3DRECOGNITIONBOUNDARY", CommandFlags.UsePickSet)]
        public void SelectBoundary()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var ids = ResolveSelection(document, "Chọn đường biên cần nhận dạng");
            if (ids == null) return;

            document.Editor.SetImpliedSelection(ids);
            document.Editor.WriteMessage("\nQS3D Nhận dạng: boundary selection=" + ids.Length + ".");
            Queue(document, "QS3DRECOGNIZE");
        }

        [CommandMethod("QS3DRECOGNITIONLABEL", CommandFlags.UsePickSet)]
        public void SelectLabel()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var ids = ResolveSelection(document, "Chọn nhãn/chữ cần kiểm tra nhận dạng");
            if (ids == null) return;

            document.Editor.SetImpliedSelection(ids);
            document.Editor.WriteMessage("\nQS3D Nhận dạng: label selection=" + ids.Length + ".");
            Queue(document, "QS3DINSPECT");
        }

        [CommandMethod("QS3DRECOGNITIONAUTO", CommandFlags.UsePickSet)]
        public void AutoRecognize()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var ids = ResolveSelection(document, "Chọn đối tượng cần tự động nhận dạng");
            if (ids == null) return;

            document.Editor.SetImpliedSelection(ids);
            document.Editor.WriteMessage("\nQS3D Nhận dạng: tự động xử lý selection=" + ids.Length + ".");
            Queue(document, "QS3DRECOGNIZEAUTO");
        }

        private static Teigha.DatabaseServices.ObjectId[]? ResolveSelection(Document document, string prompt)
        {
            var implied = document.Editor.SelectImplied();
            if (implied.Status == PromptStatus.OK && implied.Value != null && implied.Value.Count > 0)
                return implied.Value.GetObjectIds();

            var options = new PromptSelectionOptions
            {
                MessageForAdding = "\n" + prompt + ": "
            };
            var result = document.Editor.GetSelection(options);
            if (result.Status != PromptStatus.OK || result.Value == null || result.Value.Count == 0)
            {
                document.Editor.WriteMessage("\nQS3D Nhận dạng: không có đối tượng nào được chọn.");
                return null;
            }
            return result.Value.GetObjectIds();
        }

        private static void Queue(Document document, string command)
        {
            try
            {
                document.SendStringToExecute(command + " ", true, false, false);
            }
            catch (System.Exception ex)
            {
                document.Editor.WriteMessage("\nQS3D Nhận dạng: không queue được " + command + ": " + ex.Message);
            }
        }
    }
}
