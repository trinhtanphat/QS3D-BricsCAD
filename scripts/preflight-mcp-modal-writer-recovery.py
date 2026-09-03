#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORD = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadMutationCoordinator.cs"

errors = []
coord = COORD.read_text(encoding="utf-8") if COORD.is_file() else ""


def method_slice(text: str, start_token: str, next_token: str) -> str:
    start = text.find(start_token)
    end = text.find(next_token, start + 1) if start >= 0 else -1
    return text[start:end] if start >= 0 and end > start else ""


if not coord:
    errors.append("missing McpCadMutationCoordinator.cs")
else:
    if "RequireNoModalCommandBeforeMutationGate" not in coord:
        errors.append("missing pre-gate CAD modal preflight helper")

    enter = method_slice(
        coord,
        "internal static IDisposable EnterMutation(",
        "internal static NativeCommandReservation? PrepareNativeCommand(",
    )
    if not enter:
        errors.append("unable to isolate EnterMutation")
    else:
        pre = enter.find("RequireNoModalCommandBeforeMutationGate")
        gate = enter.find("MutationGate.Wait(")
        if pre < 0 or gate < 0 or pre > gate:
            errors.append("EnterMutation must perform modal preflight before acquiring MutationGate")
        if "RequireNoModalCommandInCadContext" not in enter:
            errors.append("EnterMutation must re-check modal state after gate acquisition")

    prepare = method_slice(
        coord,
        "internal static NativeCommandReservation? PrepareNativeCommand(",
        "internal static void QueueNativeCommand(",
    )
    if not prepare:
        errors.append("unable to isolate PrepareNativeCommand")
    else:
        pre = prepare.find("RequireNoModalCommandBeforeMutationGate")
        gate = prepare.find("MutationGate.Wait(")
        if pre < 0 or gate < 0 or pre > gate:
            errors.append("PrepareNativeCommand must perform modal preflight before acquiring MutationGate")
        if "RequireNoModalCommandInCadContext" not in prepare:
            errors.append("PrepareNativeCommand must re-check modal state after gate acquisition")

    if "interaction_required:" not in coord:
        errors.append("coordinator modal failure must expose bounded interaction_required marker")

    for forbidden in ("SendKeys", "PostMessage(", "WM_CLOSE", "SendStringToExecute(\"^C"):
        if forbidden in coord:
            errors.append("coordinator must not force-dismiss arbitrary UI: " + forbidden)

if errors:
    print("ERROR: MCP modal/writer recovery preflight failed")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: modal state is rejected before writer acquisition, revalidated after acquisition, returns interaction_required, and arbitrary dialogs are never force-dismissed.")
