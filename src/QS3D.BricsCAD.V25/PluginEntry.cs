using System;
using Bricscad.ApplicationServices;
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
            try
            {
                ModelessHostQuiescenceCoordinator.EnsureInitialized();
                DocumentLifecycleCoordinator.Start();
                RibbonInitializationCoordinator.Start();
            }
            catch
            {
                TeardownHostServices();
                throw;
            }

            try { McpDiagnosticHub.Start(); }
            catch (Exception ex) { ReportOptionalStartupFailure("diagnostics bridge", ex); }

            try { McpPopupObserver.Start(); }
            catch (Exception ex) { ReportOptionalStartupFailure("popup notification observer", ex); }

            try { Qs3dThemeCoordinator.Start(); }
            catch (Exception ex) { ReportOptionalStartupFailure("host-wide theme coordinator", ex); }

            try { McpPersistentUserSettings.ApplyStartupSecretsToProcessEnvironment(); }
            catch (Exception ex) { ReportOptionalStartupFailure("MCP secure settings", ex); }

            try
            {
                McpEmbeddedServer.Start();
                McpEmbeddedServerWatchdog.Start();
            }
            catch (Exception ex) { ReportOptionalStartupFailure("MCP server", ex); }

            try
            {
                McpTransportAgentCenterAugmenter.Start();
                McpPersistentAgentCenterAugmenter.Start();
                McpTransportCoordinator.TryAutoStartPreferred();
                McpPublicEndpointResolver.Resolve();
            }
            catch (Exception ex) { ReportOptionalStartupFailure("MCP transport", ex); }

            try { McpProjectRecoveryService.Start(); }
            catch (Exception ex) { ReportOptionalStartupFailure("MCP recovery service", ex); }

            try { McpFirstRunExperience.Start(); }
            catch (Exception ex) { ReportOptionalStartupFailure("MCP onboarding experience", ex); }

            try { QuantityContextMenuCoordinator.Start(); }
            catch (Exception ex) { ReportOptionalStartupFailure("Quantity context menu", ex); }

            try { UpdateBootstrapper.Start(); }
            catch (Exception ex)
            {
                ReportOptionalStartupFailure("Update service", ex);
            }
        }

        public void Terminate()
        {
            TeardownHostServices();
        }

        private static void TeardownHostServices()
        {
            TryCleanup(McpDesktopControlSession.Shutdown);
            TryCleanup(McpPopupObserver.Stop);
            TryCleanup(McpFirstRunExperience.Stop);
            TryCleanup(McpProjectRecoveryService.Stop);
            TryCleanup(McpPersistentAgentCenterAugmenter.Stop);
            TryCleanup(McpTransportAgentCenterAugmenter.Stop);
            TryCleanup(McpTransportCoordinator.StopAllForHostShutdown);
            TryCleanup(McpEmbeddedServerWatchdog.Stop);
            TryCleanup(McpEmbeddedServer.Stop);
            TryCleanup(UpdateBootstrapper.Stop);
            TryCleanup(QuantityContextMenuCoordinator.Stop);
            TryCleanup(RibbonInitializationCoordinator.Stop);
            TryCleanup(DocumentLifecycleCoordinator.Stop);
            TryCleanup(Qs3dThemeCoordinator.Stop);
            TryCleanup(StartCenterPaletteCoordinator.Dispose);
            TryCleanup(PaletteCoordinator.Dispose);
            TryCleanup(UpdateRibbonAugmenter.Reset);
            TryCleanup(QuantityReferenceRibbonAugmenter.Reset);
            TryCleanup(RaftFoundationRibbonAugmenter.Reset);
            TryCleanup(QuickWorkflowRibbonAugmenter.Reset);
            TryCleanup(ReferenceWallRibbonAugmenter.Reset);
            TryCleanup(ProjectRibbonAugmenter.Reset);
            TryCleanup(RibbonBootstrapper.Reset);
            TryCleanup(ModelessHostQuiescenceCoordinator.Stop);
            TryCleanup(McpDiagnosticHub.Stop);
        }

        private static void TryCleanup(Action cleanup)
        {
            try { cleanup(); }
            catch { }
        }

        private static void ReportOptionalStartupFailure(string component, Exception error)
        {
            try
            {
                McpDiagnosticHub.Record("qs3d", "warning", "startup-warning", component + ": " + error.Message,
                    Application.DocumentManager.MdiActiveDocument);
            }
            catch { }

            try
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D " + component + " startup warning: " + error.Message +
                    " Core CAD commands remain available; restart BricsCAD before release qualification.");
            }
            catch { }
        }
    }
}
