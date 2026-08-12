using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantityBltPresetCommands
    {
        [CommandMethod("QS3DSETUPBLT", CommandFlags.Modal)]
        public void ShowBltQuantityPreset()
        {
            var window = new QuantitySettingsWindow(new QuantitySettingsStore());
            window.LoadBltPresetOnOpen();
            window.ShowDialog();
        }
    }
}
