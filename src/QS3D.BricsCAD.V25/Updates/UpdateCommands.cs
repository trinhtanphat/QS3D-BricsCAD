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
                var path = string.IsNullOrWhiteSpace(assembly.Location) ? "<unknown>" : assembly.Location;
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage(
                    "\nQS3D V25 product version: " + result.CurrentVersion +
                    "\nAssembly version: " + assembly.GetName().Version +
                    "\nLoaded DLL: " + path +
                    "\nUpdate status: " + result.Message +
                    "\nRun QS3DUPDATE to check GitHub Releases." +
                    "\nVersion command: QS3DVERSION (aliases: QS3DVER, QSVER)." +
                    "\nUpdate command: QS3DUPDATE (alias: QSUPDATE). ");
            }
            catch (Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\n" + commandName + " error: " + ex.Message);
            }
        }
    }
}
