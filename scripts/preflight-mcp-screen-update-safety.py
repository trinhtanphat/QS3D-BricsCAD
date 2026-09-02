#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
VIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs"
AGENT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
WRITER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"

errors = []
view = VIEW.read_text(encoding="utf-8") if VIEW.is_file() else ""
agent = AGENT.read_text(encoding="utf-8") if AGENT.is_file() else ""
writer = WRITER.read_text(encoding="utf-8") if WRITER.is_file() else ""

if not view:
    errors.append("missing McpCadViewStatusRuntime.cs")
else:
    required = (
        '"cad_view_zoom_extents"',
        '"cad_view_fit_entities"',
        '"cad_view_set"',
        '"CMDACTIVE"',
        "RequireViewMutationIdle",
        "BricsCAD view update is busy",
        "Editor.SetCurrentView",
    )
    for token in required:
        if token not in view:
            errors.append("view runtime missing screen-update safety token: " + token)

    # The idle gate must be applied at least once per SetCurrentView call site so a
    # future helper call at method entry cannot drift away from the actual graphics mutation.
    cursor = 0
    set_view_calls = 0
    while True:
        index = view.find("Editor.SetCurrentView", cursor)
        if index < 0:
            break
        set_view_calls += 1
        window = view[max(0, index - 900):index]
        if "RequireViewMutationIdle();" not in window:
            errors.append("Editor.SetCurrentView is not immediately protected by RequireViewMutationIdle")
        cursor = index + len("Editor.SetCurrentView")
    if set_view_calls < 2:
        errors.append("expected direct set-view and fit/extents SetCurrentView call sites")

    # Do not turn a graphics interruption into an aggressive redraw loop; historical
    # BricsCAD failures can themselves surface around regen/display operations.
    for forbidden in ("Editor.Regen(", ".UpdateScreen("):
        if forbidden in view:
            errors.append("screen-update recovery must not force graphics refresh: " + forbidden)

for token in ("return Mutation(args, tool", "McpCadDirectModelRuntime.IsTool"):
    if token not in agent:
        errors.append("direct view tools must remain mutation-routed: " + token)

for token in ("SemaphoreSlim MutationGate", "single-writer"):
    if token not in writer:
        errors.append("process-global writer invariant missing: " + token)

print("QS3D MCP screen-update safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: direct MCP view mutations fail closed while BricsCAD is command-busy, stay on the single-writer lane, and do not force redraw recovery.")