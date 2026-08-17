using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Host-safe command adapters for the BLT3D-familiar NHẬN DẠNG ribbon.
    /// Only actions backed by a matching production recognition workflow may dispatch;
    /// unsupported recognition labels fail closed instead of routing to unrelated commands.
    /// </summary>
    public sealed class RecognitionTopbarCommands
    {
        [CommandMethod("QS3DRECOGNITIONRESTORE", CommandFlags.UsePickSet)]
        public void RestoreSelected() => WriteUnavailable("Khôi phục đã chọn");

        [CommandMethod("QS3DRECOGNITIONOPTIONS")]
        public void RecognitionOptions() => WriteUnavailable("Tùy chọn nhận dạng");

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
        public void SelectLabel() => WriteUnavailable("Chọn nhãn");

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

        private static void WriteUnavailable(string action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            document.Editor.WriteMessage(
                "\nQS3D Nhận dạng: " + action +
                " chưa có workflow nhận dạng tương ứng; hành động bị vô hiệu hóa để tránh chạy sai chức năng.");
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
