using System;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Updates
{
    internal static class UpdateBootstrapper
    {
        private static bool _started;

        internal static void Start()
        {
            if (_started) return;
            _started = true;
            UpdateCoordinator.Instance.AutomaticUpdateFound += OnAutomaticUpdateFound;
            UpdateCoordinator.Instance.Start();
        }

        internal static void Stop()
        {
            if (!_started) return;
            _started = false;
            UpdateCoordinator.Instance.AutomaticUpdateFound -= OnAutomaticUpdateFound;
            UpdateCoordinator.Instance.Stop();
            UpdateCenterWindowHost.Close();
        }

        private static void OnAutomaticUpdateFound(object sender, UpdateCheckResult result)
        {
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3D: " + result.Message + " Run QS3DUPDATE to review/update.");
                UpdateCenterWindowHost.Show(result, false);
            }
            catch
            {
                // Update notification must never break plugin initialization or drawing work.
            }
        }
    }
}