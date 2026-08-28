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

            try
            {
                McpEmbeddedServer.Start();
            }
            catch (Exception ex)
            {
                ReportOptionalStartupFailure("MCP server", ex);
            }

            try
            {
                McpCloudflareTunnelManager.TryAutoStart();
            }
            catch (Exception ex)
            {
                ReportOptionalStartupFailure("MCP Cloudflare tunnel", ex);
            }

            try
            {
                QuantityContextMenuCoordinator.Start();
            }
            catch (Exception ex)
            {
                ReportOptionalStartupFailure("Quantity context menu", ex);
            }

            try
            {
                UpdateBootstrapper.Start();
            }
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
            TryCleanup(McpCloudflareTunnelManager.StopForHostShutdown);
            TryCleanup(McpEmbeddedServer.Stop);
            TryCleanup(UpdateBootstrapper.Stop);
            TryCleanup(QuantityContextMenuCoordinator.Stop);
            TryCleanup(RibbonInitializationCoordinator.Stop);
            TryCleanup(DocumentLifecycleCoordinator.Stop);
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
        }

        private static void TryCleanup(Action cleanup)
        {
            try { cleanup(); }
            catch
            {
                // BricsCAD may already be tearing native UI/document services down.
                // One cleanup failure must never strand the remaining host services.
            }
        }

        private static void ReportOptionalStartupFailure(string component, Exception error)
        {
            try
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D " + component + " startup warning: " + error.Message +
                    " Core CAD commands remain available; restart BricsCAD before release qualification.");
            }
            catch
            {
                // Startup diagnostics must never turn an optional service failure into a load failure.
            }
        }
    }
}
