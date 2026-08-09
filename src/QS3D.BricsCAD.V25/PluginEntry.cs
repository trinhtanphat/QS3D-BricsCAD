using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class PluginEntry : IExtensionApplication
    {
        public void Initialize() { PaletteCoordinator.EnsureCreated(); }
        public void Terminate() { PaletteCoordinator.Dispose(); }
    }
}
