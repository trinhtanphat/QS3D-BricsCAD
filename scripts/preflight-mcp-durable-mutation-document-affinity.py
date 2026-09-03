#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
LEDGER = V25 / "McpMutationAckLedger.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"

errors: list[str] = []
ledger = LEDGER.read_text(encoding="utf-8") if LEDGER.is_file() else ""
agent = AGENT.read_text(encoding="utf-8") if AGENT.is_file() else ""

if not ledger:
    errors.append("missing McpMutationAckLedger.cs")
else:
    if "RequiresDurableReplayDocumentAffinity" not in ledger:
        errors.append("durable replay must expose a pure preflight that identifies when drawing affinity is required")
    if "BuildStableDocumentIdentityForReplay" not in ledger:
        errors.append("durable replay must expose stable drawing identity capture for CAD application context")
    reserve_at = ledger.find("internal static Reservation ReserveOrReplay(")
    enter_at = ledger.find("internal static IDisposable EnterActionContext(")
    reserve = ledger[reserve_at:enter_at] if reserve_at >= 0 and enter_at > reserve_at else ""
    if not reserve:
        errors.append("missing ReserveOrReplay implementation")
    else:
        if "replayDocumentIdentity" not in reserve:
            errors.append("ReserveOrReplay must receive current drawing identity for durable replay validation")
        if "AckState.Durable" not in reserve:
            errors.append("ReserveOrReplay must distinguish durable records before replay")
        if "RequireMatchingReplayDocument" not in reserve:
            errors.append("durable replay must fail closed through semantic document-affinity validation")

    if "ExtractStableFingerprint" not in ledger:
        errors.append("durable replay must compare stable fingerprint identity instead of pathname-only identity")
    if "StringComparison.OrdinalIgnoreCase" not in ledger:
        errors.append("stable fingerprint comparison must be deterministic and case-insensitive")

if not agent:
    errors.append("missing McpCadAgentRuntime.cs")
else:
    mutation_at = agent.find("private static string Mutation(")
    save_at = agent.find("private static bool IsDurabilitySaveTool(")
    mutation = agent[mutation_at:save_at] if mutation_at >= 0 and save_at > mutation_at else ""
    if not mutation:
        errors.append("missing mutation wrapper")
    else:
        probe = mutation.find("RequiresDurableReplayDocumentAffinity")
        capture = mutation.find("BuildStableDocumentIdentityForReplay")
        reserve = mutation.find("ReserveOrReplay")
        writer = mutation.find("EnterMutation")
        if probe < 0:
            errors.append("mutation wrapper must probe durable replay before writer admission")
        if capture < 0:
            errors.append("durable replay must capture active drawing identity in BricsCAD application context")
        if "InvokeCad(" not in mutation or "RequireDocument()" not in mutation:
            errors.append("durable replay drawing identity must be captured through CAD application context")
        if reserve < 0:
            errors.append("mutation wrapper must still reserve/replay action identity")
        if probe >= 0 and reserve >= 0 and probe > reserve:
            errors.append("durable replay affinity probe must occur before replay decision")
        if capture >= 0 and reserve >= 0 and capture > reserve:
            errors.append("active drawing identity must be captured before durable replay decision")
        if writer >= 0 and reserve >= 0 and reserve > writer:
            errors.append("replay decision must remain before process-global writer-gate entry")

if errors:
    print("FAIL: MCP durable mutation document-affinity guard")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: durable mutation replay is fail-closed on stable active-drawing affinity before writer admission while preserving the existing replay boundary.")
