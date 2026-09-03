#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
VIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs"
DIRECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"

errors = []
plugin = PLUGIN.read_text(encoding="utf-8") if PLUGIN.is_file() else ""
view = VIEW.read_text(encoding="utf-8") if VIEW.is_file() else ""
direct = DIRECT.read_text(encoding="utf-8") if DIRECT.is_file() else ""

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
if min(resume_at, tunnel_at, agent_at) < 0:
    pass
else:
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

# Current-document save must remain distinct from SaveAs-over-current-path. This static gate does
# not claim licensed-host runtime PASS; live eCantOpenFile qualification remains LOCAL_ONLY.
if 'case "cad_save": result = Save();' not in direct:
    errors.append("cad_save is no longer routed through the bounded current-document save wrapper")
if "document.Database.SaveAs(filename" in direct:
    errors.append("cad_save must not SaveAs over the active drawing path")

print("QS3D MCP startup/save stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: startup auto-resume/tunnel isolation, read-only command-state contract, and screen-update guards are present; live current-document save remains runtime-qualified separately.")
