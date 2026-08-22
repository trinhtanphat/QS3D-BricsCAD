using System;
using System.Reflection;
using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25.Updates
{
    public sealed class UpdateCommands
    {
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

        [CommandMethod("QS3DVERSION", CommandFlags.Modal)]
        public void ShowVersion()
        {
            WriteVersionCore("QS3DVERSION");
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
            catch (Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\n" + commandName + " error: " + ex.Message);
            }
        }

        private static void WriteVersionCore(string commandName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var result = UpdateCoordinator.Instance.LastResult;
                var originalVersion = result.CurrentVersion?.Original ?? "unknown";
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
                    "\nUpdate status: " + result.Message +
                    "\nVersion command: QS3DVERSION (aliases: QS3DVER, QSVER)." +
                    "\nUpdate command: QS3DUPDATE (alias: QSUPDATE)." +
                    "\nRun QS3DUPDATE to check GitHub Releases.");
            }
            catch (Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\n" + commandName + " error: " + ex.Message);
            }
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
