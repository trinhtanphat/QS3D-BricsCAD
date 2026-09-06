#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
VIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs"
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
AGENT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
NATIVE_SAVE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs"

errors = []
plugin = PLUGIN.read_text(encoding="utf-8") if PLUGIN.is_file() else ""
view = VIEW.read_text(encoding="utf-8") if VIEW.is_file() else ""
direct = DIRECT.read_text(encoding="utf-8") if DIRECT.is_file() else ""
agent = AGENT.read_text(encoding="utf-8") if AGENT.is_file() else ""
native_save = NATIVE_SAVE.read_text(encoding="utf-8") if NATIVE_SAVE.is_file() else ""


def require(text, token, where):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")


for token in (
    "McpDesktopControlSession.ResumeFromLocalUser();",
    "McpTransportCoordinator.TryAutoStartPreferred();",
    'ReportOptionalStartupFailure("MCP tunnel autostart"',
):
    if token not in plugin:
        errors.append("startup contract missing token: " + token)

resume_at = plugin.find("McpDesktopControlSession.ResumeFromLocalUser();")
tunnel_at = plugin.find("McpTransportCoordinator.TryAutoStartPreferred();")
agent_at = plugin.find("McpTransportAgentCenterAugmenter.Start();")
if min(resume_at, tunnel_at, agent_at) >= 0:
    if "catch (Exception ex)" not in plugin[agent_at:tunnel_at]:
        errors.append("tunnel autostart is still coupled to Agent Center startup failure")

if '"cad_command_state"' not in view:
    errors.append("cad_command_state is not published")
mutation_start = view.find("private static readonly HashSet<string> MutationTools")
mutation_end = view.find("};", mutation_start)
if mutation_start >= 0 and mutation_end > mutation_start:
    if '"cad_command_state"' in view[mutation_start:mutation_end]:
        errors.append("cad_command_state must not require confirmMutation")
else:
    errors.append("MutationTools block not found")

direct_dispatch_start = agent.find("if (McpCadDirectModelRuntime.IsTool(tool))")
desktop_dispatch_start = agent.find("if (McpDesktopAutomationRuntime.IsTool(tool))", direct_dispatch_start)
direct_dispatch = agent[direct_dispatch_start:desktop_dispatch_start] if direct_dispatch_start >= 0 and desktop_dispatch_start > direct_dispatch_start else ""
if not direct_dispatch:
    errors.append("unable to isolate outer direct-tool dispatch")
else:
    for token in (
        "if (!McpCadDirectModelRuntime.RequiresMutation(tool))",
        "return McpCadDirectModelRuntime.Call(tool, args);",
        "return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));",
    ):
        require(direct_dispatch, token, "McpCadAgentRuntime direct dispatch")
    read_return = direct_dispatch.find("return McpCadDirectModelRuntime.Call(tool, args);")
    mutation_return = direct_dispatch.find("return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));")
    if read_return < 0 or mutation_return < 0 or read_return > mutation_return:
        errors.append("direct read-only tools must bypass Mutation before the mutating direct route")

for token in ("RequireViewMutationIdle", '"CMDACTIVE"', "Editor.SetCurrentView"):
    if token not in view:
        errors.append("view idle/screen-update safety missing: " + token)
for forbidden in ("Editor.Regen(", ".UpdateScreen("):
    if forbidden in view:
        errors.append("view runtime must not force screen refresh: " + forbidden)

if not native_save:
    errors.append("missing McpNativeCurrentDocumentSave.cs")
else:
    synchronous = (
        "Application.DocumentManager.ExecuteInCommandContextAsync(" in native_save
        and 'document.Editor.Command("_.QSAVE");' in native_save
    )
    common = (
        "SaveCurrentDocument", "document.IsReadOnly", 'Application.GetSystemVariable("CMDACTIVE")',
        "DbmodPersistentContentMask", "Do not retry automatically", "WaitForCleanDbmod",
    )
    for token in common:
        require(native_save, token, "McpNativeCurrentDocumentSave")
    if synchronous:
        for token in ("Task.WaitAny(", "completion.GetAwaiter().GetResult();"):
            require(native_save, token, "synchronous McpNativeCurrentDocumentSave")
        for forbidden in ("document.SendStringToExecute(", "ManualResetEventSlim", "CommandEnded +=", "CommandCancelled +=", "CommandFailed +="):
            if forbidden in native_save:
                errors.append("synchronous native current-document save retains obsolete event topology: " + forbidden)
        if native_save.count('document.Editor.Command("_.QSAVE");') != 1:
            errors.append("synchronous native current-document save helper must execute exactly one native command attempt")
    else:
        for token in (
            "McpCadMutationCoordinator.QueueNativeCommand", "document.SendStringToExecute(", "_.QSAVE", "true, false, true",
            "ManualResetEventSlim", "CommandEnded", "CommandCancelled", "CommandFailed",
        ):
            require(native_save, token, "legacy McpNativeCurrentDocumentSave")
        if native_save.count("document.SendStringToExecute(") != 1:
            errors.append("legacy native current-document save helper must queue exactly one native command attempt")
    for forbidden in ("Database.Save();", "Database.SaveAs("):
        if forbidden in native_save:
            errors.append("native current-document save helper must not call " + forbidden)

require(direct, 'if (string.Equals(tool, "cad_save", StringComparison.Ordinal)) return Save();', "McpCadDirectModelRuntime.Call")
require(direct, "McpNativeCurrentDocumentSave.SaveCurrentDocument", "McpCadDirectModelRuntime.Save")
save_start = direct.find("private static string Save()")
save_end = direct.find("private static string SaveAs", save_start)
save_body = direct[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
if not save_body:
    errors.append("unable to isolate McpCadDirectModelRuntime.Save")
elif "document.Database.Save();" in save_body or "document.Database.SaveAs(" in save_body:
    errors.append("cad_save still writes the active DWG through Database.Save/SaveAs")

require(agent, "McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)", "McpCadAgentRuntime")
require(direct, 'string.Equals(command, "QSAVE", StringComparison.Ordinal)', "McpCadDirectModelRuntime.CanHandleCadCommandSequence")
require(direct, 'if (string.Equals(command, "QSAVE", StringComparison.Ordinal)) return SaveCadCommandSequence();', "McpCadDirectModelRuntime.CallCadCommandSequence")
require(direct, "private static string SaveCadCommandSequence()", "McpCadDirectModelRuntime")
command_save_start = direct.find("private static string SaveCadCommandSequence()")
command_save_end = direct.find("private static string CreateBox", command_save_start)
command_save_body = direct[command_save_start:command_save_end] if command_save_start >= 0 and command_save_end > command_save_start else ""
if not command_save_body:
    errors.append("unable to isolate direct bounded QSAVE wrapper")
elif "Save();" not in command_save_body:
    errors.append("bounded QSAVE wrapper must share the cad_save native lifecycle")

if 'if (command == "QSAVE") return SaveActiveDocument(document);' in agent and "McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)" not in agent:
    errors.append("QSAVE can bypass the canonical direct native-save route")
if "document.Database.SaveAs(filename" in direct:
    errors.append("cad_save must not SaveAs over the active drawing path")

print("QS3D MCP startup/save stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: startup/tunnel/view contracts remain intact, direct read-only tools bypass mutation confirmation, and cad_save/QSAVE share the admitted host-owned native QSAVE lifecycle.")