using QS3D.BricsCAD.V25.Ribbon;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            PaletteCoordinator.EnsureCreated();
            RibbonBootstrapper.TryInitialize();
        }

        public void Terminate()
        {
            PaletteCoordinator.Dispose();
            RibbonBootstrapper.Reset();
        }
    }
}
