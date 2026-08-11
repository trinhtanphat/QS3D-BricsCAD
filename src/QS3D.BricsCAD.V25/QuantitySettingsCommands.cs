using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantitySettingsCommands
    {
        [CommandMethod("QS3DSETUP", CommandFlags.Modal)]
        public void ShowQuantitySettings()
        {
            var window = new QuantitySettingsWindow(new QuantitySettingsStore());
            window.ShowDialog();
        }

        [CommandMethod("QS3DQUANTITYSETTINGS", CommandFlags.Modal)]
        public void ShowQuantitySettingsLongName()
        {
            ShowQuantitySettings();
        }
    }
}
