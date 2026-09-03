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

# Tunnel autostart must not share one try block with optional Agent Center UI startup.
resume_at = plugin.find("McpDesktopControlSession.ResumeFromLocalUser();")
tunnel_at = plugin.find("McpTransportCoordinator.TryAutoStartPreferred();")
agent_at = plugin.find("McpTransportAgentCenterAugmenter.Start();")
if min(resume_at, tunnel_at, agent_at) >= 0:
    between_agent_and_tunnel = plugin[agent_at:tunnel_at]
    if "catch (Exception ex)" not in between_agent_and_tunnel:
        errors.append("tunnel autostart is still coupled to Agent Center startup failure")

# cad_command_state is read-only: it must be published but excluded from MutationTools.
if '"cad_command_state"' not in view:
    errors.append("cad_command_state is not published")
mutation_start = view.find("private static readonly HashSet<string> MutationTools")
mutation_end = view.find("};", mutation_start)
if mutation_start >= 0 and mutation_end > mutation_start:
    if '"cad_command_state"' in view[mutation_start:mutation_end]:
        errors.append("cad_command_state must not require confirmMutation")
else:
    errors.append("MutationTools block not found")

# Preserve the merged #5330 display-race contract.
for token in ("RequireViewMutationIdle", '"CMDACTIVE"', "Editor.SetCurrentView"):
    if token not in view:
        errors.append("view idle/screen-update safety missing: " + token)
for forbidden in ("Editor.Regen(", ".UpdateScreen("):
    if forbidden in view:
        errors.append("view runtime must not force screen refresh: " + forbidden)

# RED contract for the eCantOpenFile fix: current-document saves must be host-owned native QSAVE,
# not Database.Save/SaveAs against the already-open active DWG path. The helper queues QSAVE in
# CAD context, waits for a terminal event outside that callback, leaves the coordinator barrier
# armed on uncertain timeout, and only reports success after persistent DBMOD is clean.
if not native_save:
    errors.append("missing McpNativeCurrentDocumentSave.cs")
else:
    for token in (
        "SaveCurrentDocument",
        "McpCadMutationCoordinator.QueueNativeCommand",
        'document.SendStringToExecute("_.QSAVE\\n", true, false, true)',
        "ManualResetEventSlim",
        "CommandEnded",
        "CommandCancelled",
        "CommandFailed",
        "document.IsReadOnly",
        'Application.GetSystemVariable("CMDACTIVE")',
        "DbmodPersistentContentMask",
    ):
        require(native_save, token, "McpNativeCurrentDocumentSave")
    for forbidden in ("Database.Save();", "Database.SaveAs("):
        if forbidden in native_save:
            errors.append("native current-document save helper must not call " + forbidden)

# cad_save must leave the generic single CAD-context callback before invoking the two-phase helper;
# otherwise waiting for QSAVE would block the application context that must execute QSAVE.
require(direct, 'if (string.Equals(tool, "cad_save", StringComparison.Ordinal)) return Save();', "McpCadDirectModelRuntime.Call")
require(direct, "McpNativeCurrentDocumentSave.SaveCurrentDocument", "McpCadDirectModelRuntime.Save")
save_start = direct.find("private static string Save()")
save_end = direct.find("private static string SaveAs", save_start)
save_body = direct[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
if not save_body:
    errors.append("unable to isolate McpCadDirectModelRuntime.Save")
elif "document.Database.Save();" in save_body or "document.Database.SaveAs(" in save_body:
    errors.append("cad_save still writes the active DWG through Database.Save/SaveAs")

# Bounded cad_command_sequence QSAVE must use the same helper before entering InvokeCadMutation;
# both public save routes therefore share one host-owned lifecycle and one completion contract.
require(agent, 'if (command == "QSAVE") return SaveActiveDocument();', "McpCadAgentRuntime.RunCadCommandSequence")
require(agent, "McpNativeCurrentDocumentSave.SaveCurrentDocument", "McpCadAgentRuntime.SaveActiveDocument")
agent_save_start = agent.find("private static string SaveActiveDocument(")
agent_save_end = agent.find("private static string CommandCatalogJson", agent_save_start)
agent_save_body = agent[agent_save_start:agent_save_end] if agent_save_start >= 0 and agent_save_end > agent_save_start else ""
if not agent_save_body:
    errors.append("unable to isolate McpCadAgentRuntime.SaveActiveDocument")
elif "document.Database.Save();" in agent_save_body or "document.Database.SaveAs(" in agent_save_body:
    errors.append("bounded QSAVE still writes the active DWG through Database.Save/SaveAs")

# True SaveAs remains path-changing and separate; never emulate current-document save by SaveAs
# over the currently open filename.
if "document.Database.SaveAs(filename" in direct:
    errors.append("cad_save must not SaveAs over the active drawing path")

print("QS3D MCP startup/save stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: startup/tunnel/view contracts remain intact and cad_save/QSAVE share a host-owned native QSAVE lifecycle with terminal-event and DBMOD completion verification.")
