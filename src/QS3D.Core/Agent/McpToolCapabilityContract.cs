using System;

namespace QS3D.Core.Agent
{
    public enum McpExecutionMode
    {
        Auto,
        CadDirect,
        Qs3dDomain
    }

    public enum McpToolLane
    {
        Mcp,
        BricsCadHost,
        CadDirect,
        DesktopAutomation,
        Qs3dDomain,
        Unknown
    }

    public sealed class McpToolFailure
    {
        public McpToolFailure(string code, McpToolLane lane, string message)
        {
            Code = string.IsNullOrWhiteSpace(code) ? McpToolCapabilityContract.ToolFailedCode : code;
            Lane = lane;
            Message = string.IsNullOrWhiteSpace(message) ? "MCP tool failed." : message;
        }

        public string Code { get; }
        public McpToolLane Lane { get; }
        public string Message { get; }
    }

    public sealed class McpToolContractException : InvalidOperationException
    {
        public McpToolContractException(string code, McpToolLane lane, string message)
            : base(message)
        {
            Code = code;
            Lane = lane;
        }

        public McpToolContractException(string code, McpToolLane lane, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
            Lane = lane;
        }

        public string Code { get; }
        public McpToolLane Lane { get; }
    }

    public static class McpToolCapabilityContract
    {
        public const string CadHostUnavailableCode = "CAD_HOST_UNAVAILABLE";
        public const string CadCommandFailedCode = "CAD_COMMAND_FAILED";
        public const string DesktopConsentRequiredCode = "DESKTOP_CONSENT_REQUIRED";
        public const string DesktopAutomationFailedCode = "DESKTOP_AUTOMATION_FAILED";
        public const string Qs3dDomainUnavailableCode = "QS3D_DOMAIN_UNAVAILABLE";
        public const string Qs3dContextRequiredCode = "QS3D_CONTEXT_REQUIRED";
        public const string Qs3dSourceBugCode = "QS3D_SOURCE_BUG";
        public const string ExecutionModeViolationCode = "EXECUTION_MODE_VIOLATION";
        public const string InvalidArgumentCode = "MCP_INVALID_ARGUMENT";
        public const string ToolFailedCode = "MCP_TOOL_FAILED";

        public static McpToolLane Classify(string toolName)
        {
            var tool = (toolName ?? string.Empty).Trim();
            if (tool.Length == 0) return McpToolLane.Unknown;
            if (string.Equals(tool, "connector_info", StringComparison.Ordinal) || tool.StartsWith("mcp_", StringComparison.Ordinal))
                return McpToolLane.Mcp;
            if (tool.StartsWith("bricscad_", StringComparison.Ordinal)) return McpToolLane.BricsCadHost;
            if (tool.StartsWith("cad_", StringComparison.Ordinal)) return McpToolLane.CadDirect;
            if (tool.StartsWith("desktop_", StringComparison.Ordinal)) return McpToolLane.DesktopAutomation;
            if (tool.StartsWith("qs3d_", StringComparison.Ordinal)) return McpToolLane.Qs3dDomain;
            return McpToolLane.Unknown;
        }

        public static McpExecutionMode ResolveExecutionMode(string executionMode, string executionModeAlias)
        {
            var primary = NormalizeModeText(executionMode);
            var alias = NormalizeModeText(executionModeAlias);
            if (primary.Length > 0 && alias.Length > 0 && !string.Equals(primary, alias, StringComparison.Ordinal))
                throw new McpToolContractException(InvalidArgumentCode, McpToolLane.Mcp,
                    "executionMode and execution_mode must resolve to the same value when both are supplied.");
            var value = primary.Length > 0 ? primary : alias;
            if (value.Length == 0 || value == "AUTO") return McpExecutionMode.Auto;
            if (value == "CAD_DIRECT") return McpExecutionMode.CadDirect;
            if (value == "QS3D_DOMAIN") return McpExecutionMode.Qs3dDomain;
            throw new McpToolContractException(InvalidArgumentCode, McpToolLane.Mcp,
                "executionMode must be AUTO, CAD_DIRECT, or QS3D_DOMAIN.");
        }

        public static void EnsureAllowed(string toolName, McpExecutionMode mode, bool mutation)
        {
            if (!mutation || mode == McpExecutionMode.Auto) return;
            var lane = Classify(toolName);
            if (mode == McpExecutionMode.CadDirect && lane == McpToolLane.Qs3dDomain)
                throw ModeViolation(toolName, mode, lane,
                    "CAD_DIRECT forbids QS3D business mutations; use a cad_* tool or switch execution mode.");
            if (mode == McpExecutionMode.Qs3dDomain && lane == McpToolLane.CadDirect && !IsEmergencyCadControl(toolName))
                throw ModeViolation(toolName, mode, lane,
                    "QS3D_DOMAIN forbids native CAD mutations so business failures cannot silently degrade to approximate geometry.");
        }

        public static McpToolFailure ClassifyFailure(string toolName, Exception exception)
        {
            var contract = exception as McpToolContractException;
            if (contract != null)
                return new McpToolFailure(contract.Code, contract.Lane, contract.Message);

            var lane = Classify(toolName);
            var message = exception == null || string.IsNullOrWhiteSpace(exception.Message)
                ? "MCP tool failed."
                : exception.Message.Trim();

            if (ContainsAny(message, "No active BricsCAD document", "active BricsCAD document is unavailable", "BricsCAD host is unavailable"))
                return new McpToolFailure(CadHostUnavailableCode, lane, message);

            if (lane == McpToolLane.DesktopAutomation)
            {
                if (ContainsAny(message, "consent", "Resume desktop locally", "desktop control session"))
                    return new McpToolFailure(DesktopConsentRequiredCode, lane, message);
                return new McpToolFailure(DesktopAutomationFailedCode, lane, message);
            }

            if (lane == McpToolLane.Qs3dDomain)
            {
                if (IsQs3dSourceBug(exception, message))
                    return new McpToolFailure(Qs3dSourceBugCode, lane, message);
                if (ContainsAny(message, "active QS3D Family", "active Family", "active Floor", "QS3D context", "project context", "No cached QS3D project", "requires an existing QS3D project"))
                    return new McpToolFailure(Qs3dContextRequiredCode, lane, message);
                if (IsArgumentFailure(exception, message))
                    return new McpToolFailure(InvalidArgumentCode, lane, message);
                return new McpToolFailure(Qs3dDomainUnavailableCode, lane, message);
            }

            if (IsArgumentFailure(exception, message))
                return new McpToolFailure(InvalidArgumentCode, lane, message);
            if (lane == McpToolLane.CadDirect || lane == McpToolLane.BricsCadHost)
                return new McpToolFailure(CadCommandFailedCode, lane, message);
            return new McpToolFailure(ToolFailedCode, lane, message);
        }

        public static string LaneName(McpToolLane lane)
        {
            switch (lane)
            {
                case McpToolLane.Mcp: return "mcp";
                case McpToolLane.BricsCadHost: return "bricscad_host";
                case McpToolLane.CadDirect: return "cad_direct";
                case McpToolLane.DesktopAutomation: return "desktop_automation";
                case McpToolLane.Qs3dDomain: return "qs3d_domain";
                default: return "unknown";
            }
        }

        public static string ModeName(McpExecutionMode mode)
        {
            switch (mode)
            {
                case McpExecutionMode.CadDirect: return "CAD_DIRECT";
                case McpExecutionMode.Qs3dDomain: return "QS3D_DOMAIN";
                default: return "AUTO";
            }
        }

        private static McpToolContractException ModeViolation(string toolName, McpExecutionMode mode, McpToolLane lane, string reason)
        {
            return new McpToolContractException(ExecutionModeViolationCode, lane,
                "Tool " + (toolName ?? string.Empty) + " is not allowed in " + ModeName(mode) + ". " + reason);
        }

        private static bool IsEmergencyCadControl(string toolName)
        {
            return string.Equals(toolName, "cad_agent_stop", StringComparison.Ordinal)
                || string.Equals(toolName, "cad_agent_resume", StringComparison.Ordinal)
                || string.Equals(toolName, "cad_cancel_command", StringComparison.Ordinal);
        }

        private static string NormalizeModeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static bool IsArgumentFailure(Exception exception, string message)
        {
            if (exception is ArgumentException || exception is FormatException || exception is OverflowException)
                return true;
            return ContainsAny(message, "confirmMutation=true is required", "must be a finite number", "must be >", "must be >=", "is required and", "Only one QS3D command name", "invalid MCP");
        }

        private static bool IsQs3dSourceBug(Exception exception, string message)
        {
            if (exception is NullReferenceException || exception is IndexOutOfRangeException || exception is InvalidCastException)
                return true;
            return ContainsAny(message, "VirtualizingStackPanel", "Object reference not set", "NullReferenceException", "IndexOutOfRangeException");
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            if (string.IsNullOrEmpty(text) || values == null) return false;
            foreach (var value in values)
                if (!string.IsNullOrEmpty(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
