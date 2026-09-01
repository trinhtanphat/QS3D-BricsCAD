using System;
using System.Reflection;
using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25.Updates
{
    public sealed class UpdateCommands
    {
        private const string UpdateCenterFailure = "Không thể mở QS3D Update Center. QS3D vẫn tiếp tục hoạt động bình thường.";
        private const string VersionFailure = "Không thể đọc thông tin phiên bản QS3D đang chạy.";

        [CommandMethod("QS3DUPDATE", CommandFlags.Modal)]
        public void ShowUpdateCenter()
        {
            ShowUpdateCenterCore("QS3DUPDATE");
        }

        [CommandMethod("QSUPDATE", CommandFlags.Modal)]
        public void ShowUpdateCenterAlias()
        {
            ShowUpdateCenterCore("QSUPDATE");
        }

        [CommandMethod("QS3DVER", CommandFlags.Modal)]
        public void ShowVersionShortAlias()
        {
            WriteVersionCore("QS3DVER");
        }

        [CommandMethod("QSVER", CommandFlags.Modal)]
        public void ShowVersionLegacyAlias()
        {
            WriteVersionCore("QSVER");
        }

        private static void ShowUpdateCenterCore(string commandName)
        {
            try
            {
                UpdateCenterWindowHost.Show();
            }
            catch (Exception)
            {
                TryWriteFailure(commandName, UpdateCenterFailure);
            }
        }

        private static void WriteVersionCore(string commandName)
        {
            try
            {
                var assembly = typeof(global::QS3D.BricsCAD.V25.RuntimeDiagnosticsCommands).Assembly;
                var originalVersion = ProductVersionText(assembly);
                var displayVersion = ToDisplayVersion(originalVersion);
                var buildIdentity = GetBuildIdentity(originalVersion);
                var path = string.IsNullOrWhiteSpace(assembly.Location) ? "<unknown>" : assembly.Location;
                var document = Application.DocumentManager.MdiActiveDocument;
                var buildLine = string.IsNullOrWhiteSpace(buildIdentity)
                    ? string.Empty
                    : "\nBuild identity: " + buildIdentity;

                document?.Editor.WriteMessage(
                    "\nQS3D product version: " + displayVersion +
                    buildLine +
                    "\nAssembly ABI version: " + assembly.GetName().Version + " (internal compatibility version)" +
                    "\nLoaded DLL: " + path +
                    "\nVersion source: loaded QS3D assembly (not updater cache or GitHub metadata)." +
                    "\nVersion command: QS3DVERSION (aliases: QS3DVER, QSVER)." +
                    "\nUpdate command: QS3DUPDATE (alias: QSUPDATE)." +
                    "\nRun QS3DUPDATE to check GitHub Releases.");
            }
            catch (Exception)
            {
                TryWriteFailure(commandName, VersionFailure);
            }
        }

        private static void TryWriteFailure(string commandName, string message)
        {
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\n" + commandName + ": " + message);
            }
            catch (Exception)
            {
                // Failure reporting must not escape back into BricsCAD command processing.
            }
        }

        private static string ProductVersionText(Assembly assembly)
        {
            foreach (var attribute in assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))
            {
                var informational = attribute as AssemblyInformationalVersionAttribute;
                if (informational != null && !string.IsNullOrWhiteSpace(informational.InformationalVersion))
                    return informational.InformationalVersion.Trim();
            }

            return assembly.GetName().Version?.ToString() ?? "unknown";
        }

        private static string ToDisplayVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            var trimmed = value.Trim();
            var metadataIndex = trimmed.IndexOf('+');
            if (metadataIndex >= 0) trimmed = trimmed.Substring(0, metadataIndex);
            return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? trimmed : "v" + trimmed;
        }

        private static string GetBuildIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var metadataIndex = value.IndexOf('+');
            if (metadataIndex < 0 || metadataIndex + 1 >= value.Length) return string.Empty;
            return value.Substring(metadataIndex + 1).Trim();
        }
    }
}
