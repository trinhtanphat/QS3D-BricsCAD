using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Host-safe command placeholders for the BLT3D-familiar NHẬN DẠNG ribbon.
    /// Recognition actions remain fail-closed until a workflow with matching semantics exists;
    /// these adapters must never redirect a recognition label to INSPECT, MEP preview, TAKEOFF,
    /// or another unrelated QS3D command merely to keep a button clickable.
    /// </summary>
    public sealed class RecognitionTopbarCommands
    {
        [CommandMethod("QS3DRECOGNITIONRESTORE", CommandFlags.UsePickSet)]
        public void RestoreSelected() => WriteUnavailable("Khôi phục đã chọn");

        [CommandMethod("QS3DRECOGNITIONOPTIONS")]
        public void RecognitionOptions() => WriteUnavailable("Tùy chọn nhận dạng");

        [CommandMethod("QS3DRECOGNITIONBOUNDARY", CommandFlags.UsePickSet)]
        public void SelectBoundary() => WriteUnavailable("Chọn đường biên");

        [CommandMethod("QS3DRECOGNITIONLABEL", CommandFlags.UsePickSet)]
        public void SelectLabel() => WriteUnavailable("Chọn nhãn");

        [CommandMethod("QS3DRECOGNITIONAUTO", CommandFlags.UsePickSet)]
        public void AutoRecognize() => WriteUnavailable("Tự động nhận dạng");

        private static void WriteUnavailable(string action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            document.Editor.WriteMessage(
                "\nQS3D Nhận dạng: " + action +
                " chưa có workflow nhận dạng tương ứng; hành động bị vô hiệu hóa để tránh chạy sai chức năng.");
        }
    }
}
