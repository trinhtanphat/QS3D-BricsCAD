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
    reserve_at = ledger.find("internal static Reservation ReserveOrReplay(")
    enter_at = ledger.find("internal static IDisposable EnterActionContext(")
    reserve = ledger[reserve_at:enter_at] if reserve_at >= 0 and enter_at > reserve_at else ""
    if not reserve:
        errors.append("missing ReserveOrReplay implementation")
    else:
        for token, label in (
            ("RequiresDurableReplayDocumentAffinity", "durable-state discrimination"),
            ("BuildStableDocumentIdentityForReplay", "active-drawing identity capture"),
            ("RequireMatchingReplayDocument", "fail-closed replay affinity check"),
        ):
            if token not in reserve:
                errors.append(f"ReserveOrReplay missing {label}")
        first_lock = reserve.find("lock (Sync)")
        capture = reserve.find("BuildStableDocumentIdentityForReplay")
        second_lock = reserve.find("lock (Sync)", first_lock + 1) if first_lock >= 0 else -1
        if first_lock < 0 or capture < 0 or second_lock < 0 or not (first_lock < capture < second_lock):
            errors.append("native active-drawing identity capture must occur outside ledger Sync and be revalidated under a second lock")
        if "ReferenceEquals(existing, durableCandidate)" not in reserve or "existing.State != AckState.Durable" not in reserve:
            errors.append("durable candidate must be revalidated after application-context identity capture")

    capture_at = ledger.find("private static string BuildStableDocumentIdentityForReplay(")
    execute_at = ledger.find("private static void ExecuteReplayIdentityWork(")
    capture_body = ledger[capture_at:execute_at] if capture_at >= 0 and execute_at > capture_at else ""
    if not capture_body:
        errors.append("missing bounded durable-replay application-context dispatcher")
    else:
        if "Application.DocumentManager.ExecuteInApplicationContext" not in capture_body:
            errors.append("durable replay must capture active drawing identity in BricsCAD application context")
        if "CancelBeforeStart()" not in capture_body:
            errors.append("queued identity work must be cancellable before start on dispatch timeout")
        if "work.Done.Wait(ReplayIdentityDispatchTimeoutMilliseconds)" not in capture_body:
            errors.append("durable replay application-context dispatch must have a bounded initial wait")

    execute_body = ledger[execute_at:ledger.find("internal static IDisposable EnterActionContext(", execute_at)] if execute_at >= 0 else ""
    if "Application.DocumentManager.MdiActiveDocument" not in execute_body:
        errors.append("active document must be resolved inside the application-context callback")
    if "BuildStableDocumentIdentity(document)" not in execute_body:
        errors.append("application-context callback must derive the existing stable persisted drawing identity")

    if "ExtractStableFingerprint" not in ledger:
        errors.append("durable replay must compare stable fingerprint identity instead of pathname-only identity")
    if "string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)" not in ledger:
        errors.append("stable drawing fingerprint comparison must be deterministic and case-insensitive")
    if "cross-drawing replay is rejected" not in ledger:
        errors.append("cross-drawing durable replay must fail closed with an explicit bounded error")

if not agent:
    errors.append("missing McpCadAgentRuntime.cs")
else:
    mutation_at = agent.find("private static string Mutation(")
    save_at = agent.find("private static bool IsDurabilitySaveTool(")
    mutation = agent[mutation_at:save_at] if mutation_at >= 0 and save_at > mutation_at else ""
    reserve = mutation.find("McpMutationAckLedger.ReserveOrReplay")
    writer = mutation.find("McpCadMutationCoordinator.EnterMutation")
    if reserve < 0:
        errors.append("mutation wrapper must still reserve/replay action identity")
    if writer >= 0 and reserve >= 0 and reserve > writer:
        errors.append("replay decision must remain before process-global writer-gate entry")

if errors:
    print("FAIL: MCP durable mutation document-affinity guard")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: durable mutation replay captures stable active-drawing identity in bounded BricsCAD application context outside ledger Sync, revalidates the durable record, rejects cross-drawing replay, and preserves pre-writer replay admission.")
