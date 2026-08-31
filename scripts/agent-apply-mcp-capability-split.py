#!/usr/bin/env python3
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
CORE = ROOT / "src" / "QS3D.Core" / "Agent" / "McpToolCapabilityContract.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"
DIRECT = V25 / "McpCadDirectModelRuntime.cs"
SERVER = V25 / "McpEmbeddedServerV2.cs"
SINGLE_PREFLIGHT = ROOT / "scripts" / "preflight-mcp-single-footing-direct.py"
SMOKE_PROJECT = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QS3D.Core.SmokeTests.csproj"


def read(path):
    return path.read_text(encoding="utf-8")


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def replace_once(path, old, new):
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{path.relative_to(ROOT)} expected one replacement target, found {count}: {old[:120]!r}")
    write(path, text.replace(old, new, 1))


def remove_between(path, start, end):
    text = read(path)
    first = text.find(start)
    if first < 0:
        raise RuntimeError(f"{path.relative_to(ROOT)} missing start marker: {start!r}")
    last = text.find(end, first + len(start))
    if last < 0:
        raise RuntimeError(f"{path.relative_to(ROOT)} missing end marker: {end!r}")
    write(path, text[:first] + text[last:])


def run(command, expect_success=True):
    print("RUN:", " ".join(str(x) for x in command), flush=True)
    completed = subprocess.run(command, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    print(completed.stdout, flush=True)
    if expect_success and completed.returncode != 0:
        raise RuntimeError("command failed with exit code " + str(completed.returncode))
    return completed


def verify_red():
    completed = run(["dotnet", "run", "--project", str(SMOKE_PROJECT), "-c", "Release"], expect_success=False)
    if completed.returncode == 0:
        raise RuntimeError("RED verification unexpectedly passed before the capability contract existed")
    output = completed.stdout or ""
    if "QS3D.Core.Agent" not in output and "McpToolCapabilityContract" not in output:
        raise RuntimeError("RED verification failed for an unexpected reason; capability-contract symbol was not reported")
    print("RED verified: smoke build fails because the capability contract is missing.", flush=True)


CORE_SOURCE = r'''using System;

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
            if (exception is McpToolContractException contract)
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
'''


DOMAIN_SOURCE = r'''using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Bricscad.ApplicationServices;
using QS3D.Core.Agent;
using QS3D.Core.Domain;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// QS3D business-domain MCP runtime. This boundary owns project/family/floor semantics
    /// and deliberately does not participate in native cad_* capability availability.
    /// </summary>
    internal static class McpQs3dDomainRuntime
    {
        private static readonly object Sync = new object();
        private static bool _available = true;
        private static string _lastErrorCode = string.Empty;
        private static string _lastErrorMessage = string.Empty;

        internal static bool IsTool(string tool)
        {
            return string.Equals(tool, "qs3d_status", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_domain_status", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal);
        }

        internal static bool RequiresMutation(string tool)
        {
            return string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal)
                || string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal);
        }

        internal static void ResetForServerStart()
        {
            lock (Sync)
            {
                _available = true;
                _lastErrorCode = string.Empty;
                _lastErrorMessage = string.Empty;
            }
        }

        internal static string BuildStatusJson(bool deprecatedAlias)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var contextAvailable = false;
            var contextReason = string.Empty;
            if (document == null)
            {
                contextReason = "No active BricsCAD document; QS3D business context cannot be resolved.";
            }
            else
            {
                try
                {
                    ProjectState project;
                    contextAvailable = ProjectContextCoordinator.TryGetCached(document, out project);
                    if (!contextAvailable)
                        contextReason = "No cached QS3D project context. Open or bind QS3D business context before using qs3d_* mutations.";
                }
                catch (Exception ex)
                {
                    var failure = McpToolCapabilityContract.ClassifyFailure("qs3d_domain_status", ex);
                    RecordFailure(failure);
                    contextReason = failure.Message;
                }
            }

            bool available;
            string errorCode;
            string errorMessage;
            lock (Sync)
            {
                available = _available;
                errorCode = _lastErrorCode;
                errorMessage = _lastErrorMessage;
            }

            var errorJson = string.IsNullOrWhiteSpace(errorCode)
                ? "null"
                : "{\"code\":\"" + Escape(errorCode) + "\",\"message\":\"" + Escape(errorMessage) + "\"}";
            return "{\"lane\":\"qs3d_domain\",\"available\":" + JsonBool(available)
                + ",\"context\":{\"available\":" + JsonBool(contextAvailable) + ",\"reason\":\"" + Escape(contextReason) + "\"}"
                + ",\"lastError\":" + errorJson + ",\"deprecatedAlias\":" + JsonBool(deprecatedAlias) + "}";
        }

        internal static string Call(string tool, string arguments)
        {
            if (!RequiresMutation(tool))
                throw new InvalidOperationException("Unknown QS3D domain mutation tool: " + tool);
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            try
            {
                var result = McpDiagnosticHub.InvokeInCadContext(() =>
                {
                    McpCadAgentRuntime.EnsureCurrentMutationRunning();
                    if (string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal)) return RunQs3dCommand(body);
                    if (string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal)) return PlaceSingleFooting(body);
                    throw new InvalidOperationException("Unknown QS3D domain mutation tool: " + tool);
                });
                RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                RecordFailure(McpToolCapabilityContract.ClassifyFailure(tool, ex));
                throw;
            }
        }

        private static string RunQs3dCommand(string body)
        {
            var command = McpTopLevelJson.ExtractString(body, "command").Trim();
            if (command.Length == 0 || command.Length > 80 || !Regex.IsMatch(command, McpCadAgentRuntime.Qs3dCommandPattern, RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Only one QS3D command name matching " + McpCadAgentRuntime.Qs3dCommandPattern + " is allowed.");
            McpCadAgentRuntime.EnsureCurrentMutationRunning();
            var document = RequireDocument();
            document.SendStringToExecute(command + "\n", true, false, true);
            McpCadAgentRuntime.AuditDomainMutation("qs3d_run_command", "command=" + command.ToUpperInvariant());
            return "{\"accepted\":true,\"command\":\"" + Escape(command.ToUpperInvariant()) + "\"}";
        }

        private static string PlaceSingleFooting(string body)
        {
            var x = NumberRequired(body, "x");
            var y = NumberRequired(body, "y");
            McpCadAgentRuntime.EnsureCurrentMutationRunning();
            var document = RequireDocument();
            var handle = SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d));
            McpCadAgentRuntime.AuditDomainMutation("qs3d_place_single_footing", "handle=" + handle);
            return "{\"created\":true,\"handle\":\"" + Escape(handle) + "\",\"type\":\"SingleFooting\",\"elevationPolicy\":\"active-floor\"}";
        }

        private static Document RequireDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document is available.");
            return document;
        }

        private static double NumberRequired(string body, string property)
        {
            double value;
            if (!McpTopLevelJson.TryExtractDouble(body, property, out value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(property + " must be a finite number.");
            return value;
        }

        private static void RecordSuccess()
        {
            lock (Sync)
            {
                _available = true;
                _lastErrorCode = string.Empty;
                _lastErrorMessage = string.Empty;
            }
        }

        private static void RecordFailure(McpToolFailure failure)
        {
            if (failure == null) return;
            lock (Sync)
            {
                _lastErrorCode = failure.Code;
                _lastErrorMessage = failure.Message;
                if (failure.Code == McpToolCapabilityContract.Qs3dSourceBugCode
                    || failure.Code == McpToolCapabilityContract.Qs3dDomainUnavailableCode)
                    _available = false;
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
'''


SINGLE_PREFLIGHT_SOURCE = r'''#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
DIRECT = V25 / "McpCadDirectModelRuntime.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"
SINGLE = V25 / "SingleFootingCommands.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"
SERVER = V25 / "McpEmbeddedServerV2.cs"


def require(errors, condition, message):
    if not condition:
        errors.append(message)


def between(text, start, end):
    first = text.find(start)
    if first < 0:
        return ""
    last = text.find(end, first + len(start))
    return text[first:] if last < 0 else text[first:last]


def main():
    missing = [path for path in (DIRECT, DOMAIN, SINGLE, AGENT, SERVER) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    direct = DIRECT.read_text(encoding="utf-8")
    domain = DOMAIN.read_text(encoding="utf-8")
    single = SINGLE.read_text(encoding="utf-8")
    agent = AGENT.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    errors = []

    direct_registry = between(direct, "private static readonly HashSet<string> Tools", "private static readonly HashSet<string> KnownCommandTokens")
    require(errors, '"qs3d_place_single_footing"' not in direct_registry,
            "Móng đơn business tool must not be registered in direct CAD runtime")
    require(errors, 'case "qs3d_place_single_footing"' not in direct,
            "Móng đơn business tool must not dispatch in direct CAD runtime")
    require(errors, "private static string PlaceSingleFooting" not in direct,
            "Móng đơn business implementation must not live in direct CAD runtime")

    for token in (
        'string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal)',
        'SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d))',
        'McpCadAgentRuntime.EnsureCurrentMutationRunning();',
        'McpCadAgentRuntime.AuditDomainMutation("qs3d_place_single_footing", "handle=" + handle);',
        '\\"elevationPolicy\\":\\"active-floor\\"',
    ):
        require(errors, token in domain, "QS3D Móng đơn domain runtime lost token: " + token)
    for forbidden in ('NumberRequired(body, "z")', "SendStringToExecute", "Editor.GetPoint"):
        placement = between(domain, "private static string PlaceSingleFooting", "private static Document RequireDocument")
        require(errors, forbidden not in placement, "QS3D Móng đơn placement must remain prompt-free XY authoring: " + forbidden)

    bridge = between(single, "internal static string PlaceActiveSingleFootingAt(Document document, Point3d center)", "private static string PlaceOne(")
    place_one = between(single, "private static string PlaceOne(", "private static Solid3d BuildSolid(")
    require(errors, "return PlaceOne(document, project, family, dimensions, center);" in bridge,
            "Móng đơn MCP bridge no longer reuses shared PlaceOne authoring")
    require(errors, "var baseElevationM = ResolveActiveFloorElevation(project);" in place_one,
            "shared Móng đơn authoring no longer resolves Active Floor elevation")
    require(errors, "SingleFootingBaseElevationM" in place_one,
            "shared Móng đơn authoring no longer records base elevation provenance")

    for token in (
        'case "qs3d_place_single_footing": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));',
        'Tool("qs3d_place_single_footing"',
    ):
        require(errors, token in agent + server, "canonical MCP/domain routing lost token: " + token)

    if errors:
        print("ERROR: MCP QS3D-domain Móng đơn preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: qs3d_place_single_footing is QS3D-domain owned, prompt-free, confirmation/epoch gated, and reuses shared Active Floor authoring while CAD-direct remains independent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
'''


def apply_source():
    write(CORE, CORE_SOURCE)
    write(DOMAIN, DOMAIN_SOURCE)

    replace_once(DIRECT,
        '            "cad_save_as",\n            "qs3d_place_single_footing"\n',
        '            "cad_save_as"\n')
    remove_between(DIRECT,
        '            yield return Descriptor(\n                "qs3d_place_single_footing",',
        '            foreach (var descriptor in McpCadViewStatusRuntime.ToolDescriptors())')
    replace_once(DIRECT,
        '                    case "qs3d_place_single_footing": result = PlaceSingleFooting(body); break;\n',
        '')
    remove_between(DIRECT,
        '        private static string PlaceSingleFooting(string body)\n',
        '        private static string Save()\n')

    replace_once(AGENT, 'using System.Threading;\n', 'using System.Threading;\nusing QS3D.Core.Agent;\n')
    replace_once(AGENT,
        '        private const int CadWorkCancelledBeforeStart = 2;\n',
        '        private const int CadWorkCancelledBeforeStart = 2;\n        internal const string Qs3dCommandPattern = "^QS3D[A-Za-z0-9_]*$";\n')
    replace_once(AGENT,
        '            _automationStopped = false;\n        }\n\n        public static void StopAutomation()',
        '            _automationStopped = false;\n            McpQs3dDomainRuntime.ResetForServerStart();\n        }\n\n        public static void StopAutomation()')
    replace_once(AGENT,
        '            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;\n            switch (tool)\n',
        '            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;\n            var executionMode = McpToolCapabilityContract.ResolveExecutionMode(\n                McpTopLevelJson.ExtractString(args, "executionMode"),\n                McpTopLevelJson.ExtractString(args, "execution_mode"));\n            McpToolCapabilityContract.EnsureAllowed(tool, executionMode, ToolRequiresMutation(tool));\n            switch (tool)\n')
    replace_once(AGENT,
        '                case "qs3d_status": return InvokeCad(BuildStatusJson);\n',
        '                case "mcp_status": return InvokeCad(() => BuildMcpStatusJson(executionMode));\n                case "bricscad_status": return InvokeCad(BuildBricscadStatusJson);\n                case "qs3d_status": return InvokeCad(() => McpQs3dDomainRuntime.BuildStatusJson(true));\n                case "qs3d_domain_status": return InvokeCad(() => McpQs3dDomainRuntime.BuildStatusJson(false));\n')
    replace_once(AGENT,
        '                case "qs3d_run_command": return Mutation(args, tool, () => RunQs3dCommand(args));\n',
        '                case "qs3d_run_command": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));\n                case "qs3d_place_single_footing": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));\n')

    old_status = '''        private static string BuildStatusJson()\n        {\n            var document = Application.DocumentManager.MdiActiveDocument;\n            var layer = SafeSystemVariable("CLAYER");\n            return "{\\\"product\\\":\\\"QS3D-BricsCAD\\\",\\\"bricscadVersion\\\":\\\"" + Escape(Convert.ToString(Application.Version) ?? string.Empty)\n                   + "\\\",\\\"activeDocument\\\":\\\"" + Escape(document == null ? string.Empty : SafeDocumentName(document))\n                   + "\\\",\\\"currentLayer\\\":\\\"" + Escape(layer)\n                   + "\\\",\\\"fullCadAgent\\\":true,\\\"automationStopped\\\":" + (_automationStopped ? "true" : "false") + "}";\n        }\n'''
    new_status = '''        private static string BuildBricscadStatusJson()\n        {\n            var document = Application.DocumentManager.MdiActiveDocument;\n            var layer = SafeSystemVariable("CLAYER");\n            return "{\\\"product\\\":\\\"BricsCAD\\\",\\\"connected\\\":true,\\\"bricscadVersion\\\":\\\"" + Escape(Convert.ToString(Application.Version) ?? string.Empty)\n                   + "\\\",\\\"activeDocument\\\":\\\"" + Escape(document == null ? string.Empty : SafeDocumentName(document))\n                   + "\\\",\\\"currentLayer\\\":\\\"" + Escape(layer)\n                   + "\\\",\\\"automationStopped\\\":" + (_automationStopped ? "true" : "false") + "}";\n        }\n\n        private static string BuildMcpStatusJson(McpExecutionMode executionMode)\n        {\n            var document = Application.DocumentManager.MdiActiveDocument;\n            var documentName = document == null ? string.Empty : SafeDocumentName(document);\n            return "{\\\"executionMode\\\":\\\"" + McpToolCapabilityContract.ModeName(executionMode)\n                   + "\\\",\\\"bricscad\\\":{\\\"connected\\\":true,\\\"activeDocument\\\":\\\"" + Escape(documentName) + "\\\"}"\n                   + ",\\\"cadDirect\\\":{\\\"available\\\":" + (document == null ? "false" : "true") + "}"\n                   + ",\\\"desktopAutomation\\\":{\\\"available\\\":true,\\\"consent\\\":\\\"runtime-gated\\\"}"\n                   + ",\\\"qs3dDomain\\\":" + McpQs3dDomainRuntime.BuildStatusJson(false) + "}";\n        }\n'''
    replace_once(AGENT, old_status, new_status)

    mutation_helper = '''        private static bool ToolRequiresMutation(string tool)\n        {\n            switch (tool ?? string.Empty)\n            {\n                case "cad_create_line":\n                case "cad_create_circle":\n                case "cad_create_arc":\n                case "cad_create_polyline":\n                case "cad_create_text":\n                case "cad_create_mtext":\n                case "cad_entity_transform":\n                case "cad_entity_delete":\n                case "cad_entity_set_layer":\n                case "cad_layer":\n                case "cad_command_sequence":\n                case "cad_ui_click":\n                case "cad_ui_type":\n                case "cad_ui_key":\n                case "cad_agent_stop":\n                case "cad_agent_resume":\n                case "cad_cancel_command":\n                    return true;\n            }\n            if (McpQs3dDomainRuntime.IsTool(tool)) return McpQs3dDomainRuntime.RequiresMutation(tool);\n            if (McpCadDirectModelRuntime.IsTool(tool)) return McpCadDirectModelRuntime.RequiresMutation(tool);\n            if (McpDesktopAutomationRuntime.IsTool(tool)) return McpDesktopAutomationRuntime.RequiresMutation(tool);\n            return false;\n        }\n\n'''
    replace_once(AGENT,
        '        private static string Mutation(string body, string tool, Func<string> action)\n',
        mutation_helper + '        private static string Mutation(string body, string tool, Func<string> action)\n')

    audit_helper = '''        internal static void AuditDomainMutation(string tool, string detail)\n        {\n            Audit(tool, detail);\n        }\n\n'''
    replace_once(AGENT,
        '        private static string CreateLine(string body)\n',
        audit_helper + '        private static string CreateLine(string body)\n')
    remove_between(AGENT,
        '        private static string RunQs3dCommand(string body)\n',
        '        private static string UiClick(string body)\n')

    replace_once(SERVER, 'using System.Threading;\n', 'using System.Threading;\nusing QS3D.Core.Agent;\n')
    replace_once(SERVER,
        '                Tool("connector_info", "Return embedded MCP endpoint, protocol, public endpoint and automation state.", ""),\n                Tool("qs3d_status", "Read privacy-safe BricsCAD/QS3D host status.", ""),\n',
        '                Tool("connector_info", "Return embedded MCP endpoint, protocol, public endpoint and automation state.", ""),\n                Tool("mcp_status", "Return separated MCP, BricsCAD, CAD-direct, desktop and QS3D-domain capability state.", ""),\n                Tool("bricscad_status", "Read privacy-safe BricsCAD host/document status without QS3D business state.", ""),\n                Tool("qs3d_status", "Deprecated compatibility alias for QS3D domain-only status.", ""),\n                Tool("qs3d_domain_status", "Read QS3D business-domain health and context without CAD host fields.", ""),\n')
    replace_once(SERVER,
        '                Tool("qs3d_run_command", "Run one QS3D* command name.", "\\\"command\\\":{\\\"type\\\":\\\"string\\\",\\\"pattern\\\":\\\"^QS3D[A-Za-z0-9_]*$\\\",\\\"maxLength\\\":80}," + ConfirmProperty(), "command","confirmMutation"),\n',
        '                Tool("qs3d_run_command", "Run one QS3D* business command name.", "\\\"command\\\":{\\\"type\\\":\\\"string\\\",\\\"pattern\\\":\\\"^QS3D[A-Za-z0-9_]*$\\\",\\\"maxLength\\\":80}," + ConfirmProperty(), "command","confirmMutation"),\n                Tool("qs3d_place_single_footing", "Place the active QS3D Móng đơn Family at drawing x,y using active Floor semantics.", "\\\"x\\\":{\\\"type\\\":\\\"number\\\"},\\\"y\\\":{\\\"type\\\":\\\"number\\\"}," + ConfirmProperty(), "x","y","confirmMutation"),\n')
    replace_once(SERVER,
        '            catch (Exception ex) { return ToolError(ex.Message); }\n',
        '            catch (McpToolContractException ex) { return ToolError(ex.Code, McpToolCapabilityContract.LaneName(ex.Lane), ex.Message); }\n            catch (Exception ex)\n            {\n                var failure = McpToolCapabilityContract.ClassifyFailure(tool, ex);\n                return ToolError(failure.Code, McpToolCapabilityContract.LaneName(failure.Lane), failure.Message);\n            }\n')

    old_tool_error = '''        private static string ToolError(string message)\n        {\n            return "{\\\"content\\\":[{\\\"type\\\":\\\"text\\\",\\\"text\\\":\\\"" + JsonEscape(message ?? "MCP tool failed.") + "\\\"}],\\\"isError\\\":true}";\n        }\n'''
    new_tool_error = '''        private static string ToolError(string code, string lane, string message)\n        {\n            var safeCode = string.IsNullOrWhiteSpace(code) ? McpToolCapabilityContract.ToolFailedCode : code;\n            var safeLane = string.IsNullOrWhiteSpace(lane) ? "unknown" : lane;\n            var safeMessage = string.IsNullOrWhiteSpace(message) ? "MCP tool failed." : message;\n            return "{\\\"content\\\":[{\\\"type\\\":\\\"text\\\",\\\"text\\\":\\\"" + JsonEscape(safeCode + ": " + safeMessage)\n                   + "\\\"}],\\\"structuredContent\\\":{\\\"error\\\":{\\\"code\\\":\\\"" + JsonEscape(safeCode)\n                   + "\\\",\\\"lane\\\":\\\"" + JsonEscape(safeLane) + "\\\",\\\"message\\\":\\\"" + JsonEscape(safeMessage)\n                   + "\\\"}},\\\"isError\\\":true}";\n        }\n'''
    replace_once(SERVER, old_tool_error, new_tool_error)

    mode_helpers = '''        private static string ExecutionModeProperties()\n        {\n            return "\\\"executionMode\\\":{\\\"type\\\":\\\"string\\\",\\\"enum\\\":[\\\"AUTO\\\",\\\"CAD_DIRECT\\\",\\\"QS3D_DOMAIN\\\"]}"\n                   + ",\\\"execution_mode\\\":{\\\"type\\\":\\\"string\\\",\\\"enum\\\":[\\\"AUTO\\\",\\\"CAD_DIRECT\\\",\\\"QS3D_DOMAIN\\\"]}";\n        }\n\n        private static string MergeToolProperties(string properties)\n        {\n            var modes = ExecutionModeProperties();\n            return string.IsNullOrWhiteSpace(properties) ? modes : modes + "," + properties;\n        }\n\n'''
    replace_once(SERVER,
        '        private static string Tool(string name, string description, string properties, params string[] required)\n',
        mode_helpers + '        private static string Tool(string name, string description, string properties, params string[] required)\n')
    replace_once(SERVER,
        '+ "\\\",\\\"inputSchema\\\":{\\\"type\\\":\\\"object\\\",\\\"properties\\\":{" + (properties ?? string.Empty)\n',
        '+ "\\\",\\\"inputSchema\\\":{\\\"type\\\":\\\"object\\\",\\\"properties\\\":{" + MergeToolProperties(properties)\n')

    replace_once(SERVER,
        '            var raw = (descriptor ?? string.Empty).Trim();\n            if (!LooksLikeJsonObject(raw) || raw.IndexOf("\\\"annotations\\\"", StringComparison.Ordinal) >= 0) return raw;\n',
        '            var raw = (descriptor ?? string.Empty).Trim();\n            if (!LooksLikeJsonObject(raw)) return raw;\n            raw = WithExecutionModeProperties(raw);\n            if (raw.IndexOf("\\\"annotations\\\"", StringComparison.Ordinal) >= 0) return raw;\n')
    dynamic_mode_helper = '''        private static string WithExecutionModeProperties(string descriptor)\n        {\n            var raw = (descriptor ?? string.Empty).Trim();\n            if (!LooksLikeJsonObject(raw) || raw.IndexOf("\\\"executionMode\\\"", StringComparison.Ordinal) >= 0) return raw;\n            const string marker = "\\\"properties\\\":{";\n            var index = raw.IndexOf(marker, StringComparison.Ordinal);\n            if (index < 0) return raw;\n            var insertion = index + marker.Length;\n            var modes = ExecutionModeProperties();\n            var suffix = insertion < raw.Length && raw[insertion] == '}' ? modes : modes + ",";\n            return raw.Insert(insertion, suffix);\n        }\n\n'''
    replace_once(SERVER,
        '        private static string WithToolAnnotations(string descriptor)\n',
        dynamic_mode_helper + '        private static string WithToolAnnotations(string descriptor)\n')
    replace_once(SERVER,
        '                case "connector_info":\n                case "qs3d_status":\n',
        '                case "connector_info":\n                case "mcp_status":\n                case "bricscad_status":\n                case "qs3d_status":\n                case "qs3d_domain_status":\n')

    write(SINGLE_PREFLIGHT, SINGLE_PREFLIGHT_SOURCE)


def verify_green():
    run(["dotnet", "run", "--project", str(SMOKE_PROJECT), "-c", "Release"])
    for script in (
        "scripts/preflight-mcp-capability-lanes.py",
        "scripts/preflight-mcp-single-footing-direct.py",
        "scripts/preflight-mcp-full-agent.py",
        "scripts/preflight-embedded-mcp.py",
    ):
        run([sys.executable, script])


def main():
    verify_red()
    apply_source()
    verify_green()
    print("PASS: one-shot MCP capability lane implementation applied and verified.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
