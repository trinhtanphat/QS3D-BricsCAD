using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Agent;

namespace QS3D.Core.SmokeTests
{
    internal static class McpToolCapabilityContractSmoke
    {
        internal static void Run()
        {
            ClassifiesLanes();
            ResolvesExecutionModeAliases();
            EnforcesCadDirectMode();
            EnforcesQs3dDomainMode();
            MapsStableFailures();
        }

        private static void ClassifiesLanes()
        {
            Equal(McpToolLane.Mcp, McpToolCapabilityContract.Classify("mcp_status"), "mcp lane");
            Equal(McpToolLane.BricsCadHost, McpToolCapabilityContract.Classify("bricscad_status"), "BricsCAD lane");
            Equal(McpToolLane.CadDirect, McpToolCapabilityContract.Classify("cad_create_line"), "CAD lane");
            Equal(McpToolLane.DesktopAutomation, McpToolCapabilityContract.Classify("desktop_mouse_click"), "desktop lane");
            Equal(McpToolLane.Qs3dDomain, McpToolCapabilityContract.Classify("qs3d_place_single_footing"), "QS3D lane");
        }

        private static void ResolvesExecutionModeAliases()
        {
            Equal(McpExecutionMode.Auto, McpToolCapabilityContract.ResolveExecutionMode("", ""), "default AUTO mode");
            Equal(McpExecutionMode.CadDirect, McpToolCapabilityContract.ResolveExecutionMode("CAD_DIRECT", ""), "camel mode");
            Equal(McpExecutionMode.Qs3dDomain, McpToolCapabilityContract.ResolveExecutionMode("", "QS3D_DOMAIN"), "snake alias mode");
            Equal(McpExecutionMode.CadDirect, McpToolCapabilityContract.ResolveExecutionMode("cad_direct", "CAD_DIRECT"), "matching aliases");

            var conflict = Capture<McpToolContractException>(() =>
                McpToolCapabilityContract.ResolveExecutionMode("CAD_DIRECT", "QS3D_DOMAIN"));
            Equal(McpToolCapabilityContract.InvalidArgumentCode, conflict.Code, "conflicting alias error code");
        }

        private static void EnforcesCadDirectMode()
        {
            McpToolCapabilityContract.EnsureAllowed("cad_create_line", McpExecutionMode.CadDirect, true);
            McpToolCapabilityContract.EnsureAllowed("desktop_mouse_click", McpExecutionMode.CadDirect, true);
            McpToolCapabilityContract.EnsureAllowed("qs3d_domain_status", McpExecutionMode.CadDirect, false);

            var blocked = Capture<McpToolContractException>(() =>
                McpToolCapabilityContract.EnsureAllowed("qs3d_place_single_footing", McpExecutionMode.CadDirect, true));
            Equal(McpToolCapabilityContract.ExecutionModeViolationCode, blocked.Code, "CAD_DIRECT must block QS3D mutations");
        }

        private static void EnforcesQs3dDomainMode()
        {
            McpToolCapabilityContract.EnsureAllowed("qs3d_place_single_footing", McpExecutionMode.Qs3dDomain, true);
            McpToolCapabilityContract.EnsureAllowed("cad_active_document", McpExecutionMode.Qs3dDomain, false);
            McpToolCapabilityContract.EnsureAllowed("desktop_cursor_position", McpExecutionMode.Qs3dDomain, false);
            McpToolCapabilityContract.EnsureAllowed("cad_agent_stop", McpExecutionMode.Qs3dDomain, true);

            var blockedCad = Capture<McpToolContractException>(() =>
                McpToolCapabilityContract.EnsureAllowed("cad_create_line", McpExecutionMode.Qs3dDomain, true));
            Equal(McpToolCapabilityContract.ExecutionModeViolationCode, blockedCad.Code, "QS3D_DOMAIN must block native CAD mutations");

            var blockedDesktop = Capture<McpToolContractException>(() =>
                McpToolCapabilityContract.EnsureAllowed("desktop_mouse_click", McpExecutionMode.Qs3dDomain, true));
            Equal(McpToolCapabilityContract.ExecutionModeViolationCode, blockedDesktop.Code, "QS3D_DOMAIN must block desktop automation mutations");
        }

        private static void MapsStableFailures()
        {
            Equal(McpToolCapabilityContract.CadHostUnavailableCode,
                McpToolCapabilityContract.ClassifyFailure("cad_active_document", new InvalidOperationException("No active BricsCAD document is available.")).Code,
                "CAD host error");
            Equal(McpToolCapabilityContract.CadCommandFailedCode,
                McpToolCapabilityContract.ClassifyFailure("cad_save", new InvalidOperationException("eCantOpenFile")).Code,
                "CAD command error");
            Equal(McpToolCapabilityContract.DesktopConsentRequiredCode,
                McpToolCapabilityContract.ClassifyFailure("desktop_mouse_click", new InvalidOperationException("Desktop-control consent is required.")).Code,
                "desktop consent error");
            Equal(McpToolCapabilityContract.Qs3dContextRequiredCode,
                McpToolCapabilityContract.ClassifyFailure("qs3d_place_single_footing", new InvalidOperationException("An active QS3D Family and active Floor are required.")).Code,
                "QS3D context error");
            Equal(McpToolCapabilityContract.Qs3dSourceBugCode,
                McpToolCapabilityContract.ClassifyFailure("qs3d_run_command", new NullReferenceException("VirtualizingStackPanel failed.")).Code,
                "QS3D source bug error");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class McpToolCapabilityContractSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            McpToolCapabilityContractSmoke.Run();
        }
    }
}
