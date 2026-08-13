using QS3D.BricsCAD.V25.Ribbon;
using QS3D.BricsCAD.V25.Updates;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity();
            PaletteCoordinator.EnsureCreated();
            DocumentLifecycleCoordinator.Start();
            RibbonInitializationCoordinator.Start();
            UpdateBootstrapper.Start();
        }
        public void Terminate()
        {
            UpdateBootstrapper.Stop();
            RibbonInitializationCoordinator.Stop();
            DocumentLifecycleCoordinator.Stop();
            PaletteCoordinator.Dispose();
            UpdateRibbonAugmenter.Reset();
            QuantityReferenceRibbonAugmenter.Reset();
            QuickWorkflowRibbonAugmenter.Reset();
            ReferenceWallRibbonAugmenter.Reset();
            ProjectRibbonAugmenter.Reset();
            RibbonBootstrapper.Reset();
        }
    }
}
