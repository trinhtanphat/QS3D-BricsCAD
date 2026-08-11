using QS3D.BricsCAD.V25.Ribbon;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    // V26 keeps the established source namespace so shared XAML/classes do not fork,
    // while the project emits a distinct QS3D.BricsCAD.V26 assembly for the .NET 8 host.
    public sealed class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            PaletteCoordinator.EnsureCreated();
            DocumentLifecycleCoordinator.Start();
            RibbonBootstrapper.TryInitialize();
            ReferenceWallRibbonAugmenter.TryInitialize();
            ProjectRibbonAugmenter.TryInitialize();
            QuickWorkflowRibbonAugmenter.TryInitialize();
            QuantityReferenceRibbonAugmenter.TryInitialize();
        }

        public void Terminate()
        {
            DocumentLifecycleCoordinator.Stop();
            PaletteCoordinator.Dispose();
            QuantityReferenceRibbonAugmenter.Reset();
            QuickWorkflowRibbonAugmenter.Reset();
            ReferenceWallRibbonAugmenter.Reset();
            ProjectRibbonAugmenter.Reset();
            RibbonBootstrapper.Reset();
        }
    }
}