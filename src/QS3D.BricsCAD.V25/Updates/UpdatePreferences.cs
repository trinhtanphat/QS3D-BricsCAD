using System;
using Microsoft.Win32;

namespace QS3D.BricsCAD.V25.Updates
{
    internal static class UpdatePreferences
    {
        private const string RegistryPath = @"Software\QS3D\BricsCAD-V25\Updates";
        private const string InstallOnExitValue = "InstallOnExit";

        internal static bool InstallOnExit => ReadBoolean(InstallOnExitValue, false);

        internal static bool TrySetInstallOnExit(bool enabled, out string error)
        {
            error = string.Empty;
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath, true))
                {
                    if (key == null)
                    {
                        error = "Không mở được vùng cài đặt QS3D trong Windows Registry.";
                        return false;
                    }

                    key.SetValue(InstallOnExitValue, enabled ? 1 : 0, RegistryValueKind.DWord);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = "Không lưu được tùy chọn cập nhật: " + ex.Message;
                return false;
            }
        }

        private static bool ReadBoolean(string name, bool defaultValue)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
                {
                    var value = key?.GetValue(name);
                    if (value is int number) return number != 0;
                    if (value is long longNumber) return longNumber != 0;
                    if (value is string text && bool.TryParse(text, out var parsed)) return parsed;
                }
            }
            catch
            {
                // Preferences must never prevent QS3D from loading.
            }

            return defaultValue;
        }
    }
}
