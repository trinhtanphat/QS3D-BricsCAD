#!/usr/bin/env python3
"""Source guard for unified MCP diagnostics and host-wide QS3D/BricsCAD theme control."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
DIAGNOSTICS = V25 / "McpDiagnosticHub.cs"
THEME = V25 / "Qs3dThemeCoordinator.cs"
ENTRY = V25 / "PluginEntry.cs"
CAD = V25 / "McpCadAgentRuntime.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"


def fail(message: str) -> None:
    print("ERROR: MCP diagnostics/theme preflight failed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(label + " is missing: " + needle)


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        fail(label + " must not contain: " + needle)


def read(path: Path) -> str:
    if not path.is_file():
        fail("required source is missing: " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def main() -> int:
    diagnostics = read(DIAGNOSTICS)
    theme = read(THEME)
    entry = read(ENTRY)
    cad = read(CAD)
    domain = read(DOMAIN)

    for needle in (
        "McpCadAgentRuntime.AuditFilePath",
        'Record("mcp"',
        'Record("qs3d"',
        'Record("bricscad"',
        '"qs3d-audit"',
        "McpEmbeddedServer.LastError",
        "McpEmbeddedServer.LastOAuthMcpActivityUtc",
        "CommandWillStart",
        "CommandEnded",
        "CommandCancelled",
        "CommandFailed",
        "AppDomain.CurrentDomain.UnhandledException",
        "TaskScheduler.UnobservedTaskException",
        "AuditTrail.ForProject(project).Events",
        "AuthorizationRegex",
        "SecretRegex",
        "MaxMessageCharacters",
        'CommandMethod("QS3DDIAGNOSTICSSNAPSHOT"',
        "McpEmbeddedServer.Describe()",
        'GetSystemVariable("CMDACTIVE")',
        'GetSystemVariable("COLORTHEME")',
    ):
        require(diagnostics, needle, "bounded unified diagnostics")

    # Diagnostics are intentionally routed through the existing bounded MCP audit
    # rather than exposing arbitrary files, shell/process launch or eval surfaces.
    for forbidden in (
        "Process.Start(",
        "cmd.exe",
        "powershell.exe",
        "CSharpCodeProvider",
        "Assembly.Load(",
    ):
        reject(diagnostics, forbidden, "diagnostics security boundary")

    for needle in (
        "Qs3dThemeMode.System",
        "Qs3dThemeMode.Dark",
        "Qs3dThemeMode.Light",
        'GetSystemVariable("COLORTHEME")',
        'SetSystemVariable("COLORTHEME"',
        'OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")',
        'GetValue("AppsUseLightTheme")',
        "SystemEvents.UserPreferenceChanged",
        "FrameworkElement.LoadedEvent",
        "Button.ClickEvent",
        "McpAgentControlCenterWindow",
        "ApplyDictionary",
        "ApplyVisualTree",
        'ApplyBrushKey(dictionary, "Bg0Brush"',
        'ApplyBrushKey(dictionary, "TextBrush"',
        "SystemColors.HighlightBrushKey",
        'CommandMethod("QS3DTHEMESYSTEM"',
        'CommandMethod("QS3DTHEMEDARK"',
        'CommandMethod("QS3DTHEMELIGHT"',
        'CommandMethod("QS3DTHEMESTATUS"',
        "ThemeFilePath",
        "PersistMode",
    ):
        require(theme, needle, "host-wide theme propagation")

    for needle in (
        "McpDiagnosticHub.Start();",
        "Qs3dThemeCoordinator.Start();",
        "TryCleanup(Qs3dThemeCoordinator.Stop);",
        "TryCleanup(McpDiagnosticHub.Stop);",
        "McpDiagnosticHub.Record(",
    ):
        require(entry, needle, "plugin lifecycle wiring")

    # Preserve the same confirmation/retrieval boundary after qs3d_run_command moved
    # into the isolated QS3D-domain runtime. Confirmation remains owned by Mutation;
    # the domain runtime keeps the bounded command allowlist and native dispatch.
    for needle in (
        'case "qs3d_run_command": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));',
        'case "cad_audit_tail": return ReadAuditTail(',
        'if (!McpTopLevelJson.ExtractBoolean(body, "confirmMutation"))',
        'internal const string Qs3dCommandPattern = "^QS3D[A-Za-z0-9_]*$";',
    ):
        require(cad, needle, "confirmed MCP bridge")

    for needle in (
        'Regex.IsMatch(command, McpCadAgentRuntime.Qs3dCommandPattern',
        'document.SendStringToExecute(command + "\\n", true, false, true);',
        'McpCadAgentRuntime.EnsureCurrentMutationRunning();',
        'McpCadAgentRuntime.AuditDomainMutation("qs3d_run_command"',
    ):
        require(domain, needle, "QS3D domain command bridge")

    print("PASS unified MCP/QS3D/BricsCAD diagnostics + host-wide System/Dark/Light theme contract")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
