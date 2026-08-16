using QS3D.BricsCAD.V25.Ribbon;
using QS3D.BricsCAD.V25.UI;
using QS3D.BricsCAD.V25.Updates;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity();
            ProductionUiPolish.EnsureRegistered();
            PaletteCoordinator.EnsureCreated();
            DocumentLifecycleCoordinator.Start();
            RibbonBootstrapper.TryInitialize();
            ReferenceWallRibbonAugmenter.TryInitialize();
            ProjectRibbonAugmenter.TryInitialize();
            QuickWorkflowRibbonAugmenter.TryInitialize();
            QuantityReferenceRibbonAugmenter.TryInitialize();
            RibbonBootstrapIconAugmenter.TryInitialize();

            // V26 historically performed one synchronous Ribbon pass only. Keep that fast pass for
            // compatibility, then use the shared retry coordinator so the BLT3D shell presentation
            // is applied after BricsCAD has finished constructing its native Ribbon visual tree.
            RibbonInitializationCoordinator.Start();
            UpdateBootstrapper.Start();
        }

        public void Terminate()
        {
            UpdateBootstrapper.Stop();
            RibbonInitializationCoordinator.Stop();
            DocumentLifecycleCoordinator.Stop();
            ProjectSetupPaletteCoordinator.Dispose();
            PaletteCoordinator.Dispose();
            RibbonBootstrapIconAugmenter.Reset();
            QuantityReferenceRibbonAugmenter.Reset();
            QuickWorkflowRibbonAugmenter.Reset();
            ReferenceWallRibbonAugmenter.Reset();
            ProjectRibbonAugmenter.Reset();
            RibbonBootstrapper.Reset();
        }
    }
}
