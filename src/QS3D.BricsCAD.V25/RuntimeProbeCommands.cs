using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Ribbon;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RuntimeProbeCommands
    {
        [CommandMethod("QS3DRUNTIMEPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var resultPath = Environment.GetEnvironmentVariable("QS3D_RUNTIME_RESULT");
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D runtime probe skipped: QS3D_RUNTIME_RESULT is not set.");
                return;
            }

            try
            {
                if (!Environment.Is64BitProcess) throw new InvalidOperationException("QS3D BricsCAD runtime must be 64-bit.");
                PaletteCoordinator.Show();
                var ribbonReady = RibbonBootstrapper.TryInitialize();
                if (!ribbonReady) throw new InvalidOperationException("QS3D ribbon initialization did not complete.");
                if (!PaletteCoordinator.IsWorkspaceVisible) throw new InvalidOperationException("QS3D workspace palette is not visible after initialization.");
                if (PaletteCoordinator.IsRightPanelVisible) throw new InvalidOperationException("QS3D drawing/layer palette should remain hidden after initialization.");
                if (PaletteCoordinator.IsQuantityInsightVisible) throw new InvalidOperationException("QS3D quantity insight palette should remain hidden after initialization.");

                var process = Process.GetCurrentProcess();
                var assembly = typeof(RuntimeProbeCommands).Assembly;
                var hostVersion = "unknown";
                try { hostVersion = process.MainModule?.FileVersionInfo?.FileVersion ?? "unknown"; } catch { }

                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DRUNTIMEPROBE",
                    "utc=" + DateTime.UtcNow.ToString("O"),
                    "process=" + OneLine(process.ProcessName),
                    "host_file_version=" + OneLine(hostVersion),
                    "clr=" + OneLine(Environment.Version.ToString()),
                    "is_64bit=true",
                    "assembly=" + OneLine(assembly.Location),
                    "assembly_version=" + OneLine(assembly.GetName().Version?.ToString() ?? "unknown"),
                    "ribbon_ready=true",
                    // Keep the legacy key for downstream artifact compatibility while also reporting
                    // the specific Workspace/Right/Quantity states that define the Ribbon-first contract.
                    "palette_visible=true",
                    "workspace_palette_visible=true",
                    "right_palette_visible=false",
                    "quantity_palette_visible=false"
                });

                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D runtime probe PASS. Marker: " + Path.GetFullPath(resultPath));
            }
            catch (System.Exception ex)
            {
                TryWriteFailure(resultPath, ex);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D runtime probe FAIL: " + ex.Message);
                throw;
            }
        }

        private static void TryWriteFailure(string resultPath, System.Exception error)
        {
            try
            {
                WriteMarkerAtomic(resultPath, new[]
                {
                    "status=FAIL",
                    "command=QS3DRUNTIMEPROBE",
                    "utc=" + DateTime.UtcNow.ToString("O"),
                    "error=" + OneLine(error.GetType().FullName + ": " + error.Message)
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, string[] lines)
        {
            if (string.IsNullOrWhiteSpace(resultPath)) throw new ArgumentException("Runtime result path is required.", nameof(resultPath));
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var fullPath = Path.GetFullPath(resultPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var backupPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".replace.bak";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }

                if (!File.Exists(fullPath)) File.Move(tempPath, fullPath);
                else
                {
                    File.Replace(tempPath, fullPath, backupPath, true);
                    TryDelete(backupPath);
                }
            }
            finally
            {
                TryDelete(tempPath);
                TryDelete(backupPath);
            }
        }

        private static string OneLine(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
}
