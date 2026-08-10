using System;
using System.Diagnostics;
using System.IO;
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
                PaletteCoordinator.Show();
                var ribbonReady = RibbonBootstrapper.TryInitialize();
                var process = Process.GetCurrentProcess();
                var assembly = typeof(RuntimeProbeCommands).Assembly;
                var hostVersion = "unknown";
                try { hostVersion = process.MainModule?.FileVersionInfo?.FileVersion ?? "unknown"; } catch { }

                var directory = Path.GetDirectoryName(resultPath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

                File.WriteAllLines(resultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DRUNTIMEPROBE",
                    "utc=" + DateTime.UtcNow.ToString("O"),
                    "process=" + process.ProcessName,
                    "host_file_version=" + hostVersion,
                    "clr=" + Environment.Version,
                    "is_64bit=" + Environment.Is64BitProcess,
                    "assembly=" + assembly.Location,
                    "assembly_version=" + (assembly.GetName().Version?.ToString() ?? "unknown"),
                    "ribbon_ready=" + ribbonReady,
                    "palette_requested=true"
                });

                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D runtime probe PASS. Marker: " + resultPath);
            }
            catch (Exception ex)
            {
                TryWriteFailure(resultPath, ex);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nQS3D runtime probe FAIL: " + ex.Message);
                throw;
            }
        }

        private static void TryWriteFailure(string resultPath, Exception error)
        {
            try
            {
                var directory = Path.GetDirectoryName(resultPath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.WriteAllLines(resultPath, new[]
                {
                    "status=FAIL",
                    "command=QS3DRUNTIMEPROBE",
                    "utc=" + DateTime.UtcNow.ToString("O"),
                    "error=" + error.GetType().FullName + ": " + error.Message
                });
            }
            catch { }
        }
    }
}
