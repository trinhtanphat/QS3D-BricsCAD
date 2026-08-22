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

            var subscribed = false;
            try
            {
                UpdateCoordinator.Instance.AutomaticUpdateFound += OnAutomaticUpdateFound;
                subscribed = true;
                UpdateCoordinator.Instance.Start();
                _started = true;
            }
            catch
            {
                if (subscribed)
                {
                    try { UpdateCoordinator.Instance.AutomaticUpdateFound -= OnAutomaticUpdateFound; }
                    catch { }
                }
                try { UpdateCoordinator.Instance.Stop(); }
                catch { }
                _started = false;
                throw;
            }
        }

        internal static void Stop()
        {
            if (!_started) return;

            _started = false;
            TryScheduleVerifiedUpdateOnExit();
            try { UpdateCoordinator.Instance.AutomaticUpdateFound -= OnAutomaticUpdateFound; }
            catch { }
            try { UpdateCoordinator.Instance.Stop(); }
            catch { }
            try { UpdateCenterWindowHost.Close(); }
            catch { }
        }

        private static void TryScheduleVerifiedUpdateOnExit()
        {
            try
            {
                if (!UpdatePreferences.InstallOnExit || SecureUpdateLauncher.IsScheduled) return;

                var result = UpdateCoordinator.Instance.LastResult;
                var release = result.Release;
                if (!result.CanAutoInstall || release == null) return;

                SecureUpdateLauncher.TrySchedule(release, out _);
            }
            catch
            {
                // Shutdown must never be blocked by update-on-close preparation.
            }
        }

        private static void OnAutomaticUpdateFound(object sender, UpdateCheckResult result)
        {
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                var suffix = UpdatePreferences.InstallOnExit
                    ? " Update khi đóng đang bật; QS3D sẽ lên lịch bản đã xác minh khi bạn đóng BricsCAD bình thường. Run QS3DUPDATE để xem hoặc cập nhật ngay."
                    : " Run QS3DUPDATE to review/update.";
                document?.Editor.WriteMessage("\nQS3D: " + result.Message + suffix);
                UpdateCenterWindowHost.Show(result, false);
            }
            catch
            {
                // Update notification must never break plugin initialization or drawing work.
            }
        }
    }
}
