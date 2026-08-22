using QS3D.BricsCAD.V25.Ribbon;
using QS3D.BricsCAD.V25.Updates;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
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
            UpdateBootstrapper.Start();
        }
        public void Terminate()
        {
            UpdateBootstrapper.Stop();
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
