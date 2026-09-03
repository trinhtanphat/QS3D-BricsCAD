#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
COORD = SRC / "McpCadMutationCoordinator.cs"
CONSENT = SRC / "McpOAuthConsent.cs"

errors: list[str] = []
coord = COORD.read_text(encoding="utf-8") if COORD.is_file() else ""
consent = CONSENT.read_text(encoding="utf-8") if CONSENT.is_file() else ""


def method_slice(text: str, start_token: str, next_token: str) -> str:
    start = text.find(start_token)
    end = text.find(next_token, start + 1) if start >= 0 else -1
    return text[start:end] if start >= 0 and end > start else ""


if not coord:
    errors.append("missing McpCadMutationCoordinator.cs")
else:
    modal = method_slice(
        coord,
        "internal static IDisposable EnterInteractiveModal(",
        "internal static NativeCommandReservation? PrepareNativeCommand(",
    )
    if not modal:
        errors.append("missing semantic EnterInteractiveModal admission")
    else:
        if "McpCadMutationCoordinator.EnterMutation(" in modal or "EnterMutation(" in modal:
            errors.append("interactive modal admission must not pretend to be a mutation")

        gate = modal.find("MutationGate.Wait(")
        if gate < 0:
            errors.append("interactive modal admission must use the shared MutationGate")
        else:
            for token, label in (
                ("CurrentOperationId.Value.HasValue", "active mutation nested-admission rejection"),
                ("PreparedNativeCommand.Value != null", "prepared native-command nested-admission rejection"),
                ("RequireNoWriterOwnershipForInteractiveModal", "writer/native ownership preflight"),
                ("RequireNoModalCommandBeforeMutationGate", "CAD modal preflight"),
            ):
                at = modal.find(token)
                if at < 0 or at > gate:
                    errors.append(f"{label} must occur before MutationGate acquisition")

        if modal.count("RequireNoWriterOwnershipForInteractiveModal") < 2:
            errors.append("interactive modal admission must check writer/native ownership before and after gate acquisition")
        if "RequireNoModalCommandInCadContext" not in modal:
            errors.append("interactive modal admission must re-check BricsCAD modal state after gate acquisition")
        if "new InteractiveModalScope(" not in modal:
            errors.append("interactive modal admission must return a scope that retains shared serialization ownership")

    if "private sealed class InteractiveModalScope : IDisposable" not in coord:
        errors.append("missing interactive modal lifetime scope")
    else:
        scope = coord[coord.find("private sealed class InteractiveModalScope : IDisposable"):]
        if "MutationGate.Release();" not in scope:
            errors.append("interactive modal scope must release MutationGate exactly on disposal path")

if not consent:
    errors.append("missing McpOAuthConsent.cs")
else:
    request_at = consent.find("internal static McpOAuthConsentResult RequestApproval")
    callback_at = consent.find("private static void ShowConsentInCadContext")
    request = consent[request_at:callback_at] if request_at >= 0 and callback_at > request_at else consent
    if "McpCadMutationCoordinator.EnterInteractiveModal(" not in request:
        errors.append("OAuth consent must use semantic EnterInteractiveModal admission")
    if '"oauth_interactive_consent"' not in request:
        errors.append("OAuth consent must name its interactive admission")
    if "McpCadMutationCoordinator.EnterMutation(" in request:
        errors.append("OAuth consent must not acquire modal ownership by entering a fake mutation")

if errors:
    print("ERROR: MCP shared interactive-modal admission preflight failed")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: plugin-owned interactive UI has semantic shared admission, rejects nested mutation/native ownership, checks writer and CAD modal state before/after acquisition, holds MutationGate for modal lifetime, and OAuth no longer masquerades as a mutation.")
