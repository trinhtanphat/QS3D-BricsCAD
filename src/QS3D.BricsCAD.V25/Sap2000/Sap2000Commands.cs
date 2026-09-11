using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Interoperability.Sap2000;
using Teigha.Runtime;

namespace QS3D.BricsCAD.Sap2000
{
    public sealed class Sap2000Commands
    {
        [CommandMethod("QS3DSAPCONNECT", CommandFlags.Modal)]
        public void Connect()
        {
            Execute("SAP Connect", document =>
            {
                var bridge = Sap2000Session.GetOrConnect();
                Report(document, bridge.StartedNewInstance
                    ? "SAP2000: đã khởi động và kết nối OAPI."
                    : "SAP2000: đã kết nối OAPI tới phiên đang chạy.");
            });
        }

        [CommandMethod("QS3DSAPNEW", CommandFlags.Modal)]
        public void NewModel()
        {
            Execute("SAP New", document =>
            {
                var answer = System.Windows.MessageBox.Show(
                    "Tạo model SAP2000 trắng sẽ thay model hiện tại trong phiên SAP được kết nối. Tiếp tục?",
                    "QS3D • SAP2000",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);

                if (answer != System.Windows.MessageBoxResult.Yes)
                {
                    Report(document, "SAP2000: đã hủy tạo model trắng.");
                    return;
                }

                Sap2000Session.GetOrConnect().InitializeBlankModel();
                Report(document, "SAP2000: đã tạo model trắng, đơn vị kN-m-C.");
            });
        }

        [CommandMethod("QS3DSAPEXPORT", CommandFlags.Modal)]
        public void ExportSelection()
        {
            Execute("SAP Export", document =>
            {
                var plan = Sap2000GeometryExporter.BuildFromSelection(document);
                foreach (var warning in plan.Warnings)
                {
                    document.Editor.WriteMessage("\n[QS3D SAP] " + warning);
                }

                if (plan.ObjectCount == 0)
                {
                    Report(document, "SAP2000: không có frame/area hợp lệ để xuất.");
                    return;
                }

                var bridge = Sap2000Session.GetOrConnect();
                var frameCount = 0;
                var areaCount = 0;
                var failures = 0;

                foreach (var frame in plan.Frames)
                {
                    try
                    {
                        bridge.AddFrame(frame);
                        frameCount++;
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        document.Editor.WriteMessage("\n[QS3D SAP] Frame " + frame.UserName + " lỗi: " + SafeMessage(ex));
                    }
                }

                foreach (var area in plan.Areas)
                {
                    try
                    {
                        bridge.AddArea(area);
                        areaCount++;
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        document.Editor.WriteMessage("\n[QS3D SAP] Area " + area.UserName + " lỗi: " + SafeMessage(ex));
                    }
                }

                var summary = new Sap2000ExportSummary(frameCount, areaCount, failures);
                Report(
                    document,
                    "SAP2000 Export: " + summary.FramesAdded + " frame • " + summary.AreasAdded +
                    " area • " + summary.Failures + " lỗi. Model SAP không bị reset tự động.");
            });
        }

        [CommandMethod("QS3DSAPSAVE", CommandFlags.Modal)]
        public void SaveModel()
        {
            Execute("SAP Save", document =>
            {
                var drawingName = string.IsNullOrWhiteSpace(document.Name)
                    ? "QS3D-SAP2000"
                    : Path.GetFileNameWithoutExtension(document.Name) + "-SAP2000";

                var dialog = new SaveFileDialog
                {
                    Title = "Lưu model SAP2000",
                    Filter = "SAP2000 Database (*.sdb)|*.sdb",
                    DefaultExt = ".sdb",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + ".sdb"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                Sap2000Session.GetOrConnect().Save(dialog.FileName);
                Report(document, "SAP2000: đã lưu model • " + dialog.FileName);
            });
        }

        [CommandMethod("QS3DSAPRUN", CommandFlags.Modal)]
        public void RunAnalysis()
        {
            Execute("SAP Run", document =>
            {
                Sap2000Session.GetOrConnect().RunAnalysis();
                Report(document, "SAP2000: RunAnalysis hoàn tất theo OAPI.");
            });
        }

        [CommandMethod("QS3DSAPRESET", CommandFlags.Modal)]
        public void ResetConnection()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            Sap2000Session.Reset();
            if (document != null)
            {
                Report(document, "SAP2000: đã giải phóng kết nối OAPI của QS3D (không đóng SAP2000).");
            }
        }

        private static void Execute(string operation, Action<Document> action)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                return;
            }

            try
            {
                action(document);
            }
            catch (Exception ex)
            {
                Report(document, operation + " lỗi: " + SafeMessage(ex));
            }
        }

        private static string SafeMessage(Exception exception)
        {
            if (exception == null || string.IsNullOrWhiteSpace(exception.Message))
            {
                return "không có thông tin lỗi từ OAPI.";
            }

            var message = exception.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return message.Length <= 300 ? message : message.Substring(0, 300) + "…";
        }

        private static void Report(Document document, string status)
        {
            try
            {
                PaletteCoordinator.SetStatus(status);
            }
            catch
            {
                // Palette reporting is best effort; command-line reporting remains available.
            }

            try
            {
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch
            {
                // Do not convert successful SAP mutation into a UI-reporting failure.
            }
        }
    }
}
