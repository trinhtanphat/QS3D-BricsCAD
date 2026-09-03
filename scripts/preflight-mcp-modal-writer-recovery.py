#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORD = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"
VIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs"

errors = []
coord = COORD.read_text(encoding="utf-8") if COORD.is_file() else ""
view = VIEW.read_text(encoding="utf-8") if VIEW.is_file() else ""


def method_slice(text: str, start_token: str, next_token: str) -> str:
    start = text.find(start_token)
    end = text.find(next_token, start + 1) if start >= 0 else -1
    return text[start:end] if start >= 0 and end > start else ""


if not coord:
    errors.append("missing McpCadMutationCoordinator.cs")
else:
    if "RequireNoModalCommandBeforeMutationGate" not in coord:
        errors.append("missing pre-gate CAD modal preflight helper")
    for start_token, next_token, label in (
        ("internal static string Prepare(", "internal static Lease EnterMutation(", "Prepare"),
        ("internal static Lease EnterMutation(", "internal static void RequireLeaseCurrent(", "EnterMutation"),
    ):
        body = method_slice(coord, start_token, next_token)
        if not body:
            errors.append(f"unable to isolate {label}")
            continue
        pre = body.find("RequireNoModalCommandBeforeMutationGate")
        gate = body.find("Monitor.TryEnter(MutationGate")
        if pre < 0 or gate < 0 or pre > gate:
            errors.append(f"{label} must perform modal preflight before acquiring MutationGate")
        if "RequireNoModalCommandInCadContext();" not in body:
            errors.append(f"{label} must re-check modal state after gate acquisition")
    if "interaction_required:" not in coord:
        errors.append("coordinator modal failure must expose bounded interaction_required marker")
    for forbidden in ("SendKeys", "SendStringToExecute(\"^C", "PostMessage(", "WM_CLOSE", "ESCAPE"):
        if forbidden in coord:
            errors.append("coordinator must not force-dismiss arbitrary UI: " + forbidden)

if not view:
    errors.append("missing McpCadViewStatusRuntime.cs")
else:
    for token in ('\\"modal\\"', '\\"busyKind\\"', '\\"interactionRequired\\"'):
        if token not in view:
            errors.append("cad_command_state missing structured interaction field: " + token)
    if "interaction_required:" not in view:
        errors.append("view modal failure must expose bounded interaction_required marker")

if errors:
    print("ERROR: MCP modal/writer recovery preflight failed")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: modal state is rejected before writer acquisition, revalidated after acquisition, command state exposes bounded interaction metadata, and arbitrary dialogs are never force-dismissed.")
