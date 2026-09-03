#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
LEDGER = V25 / "McpMutationAckLedger.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"
COORD = V25 / "McpCadMutationCoordinator.cs"
SERVER = V25 / "McpEmbeddedServerV2.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8") if path.exists() else ""


def main() -> int:
    ledger = read(LEDGER)
    agent = read(AGENT)
    coord = read(COORD)
    server = read(SERVER)
    errors = []

    if not LEDGER.exists():
        errors.append("missing McpMutationAckLedger.cs")
    else:
        for token in (
            "MaxActionIdLength = 128",
            "MaxDurableRecords = 1024",
            "MaxLedgerBytes = 1024 * 1024",
            "MaxStoredResultBytes = 16 * 1024",
            "ReserveOrReplay",
            "MarkApplied",
            "PromoteDurableForDocument",
            "StatusJson",
            "ResetForServerStart",
            "SHA256.Create()",
            '"accepted"',
            '"applied"',
            '"durable"',
        ):
            if token not in ledger:
                errors.append(f"ledger contract missing: {token}")

        for excluded in (
            '"actionId"',
            '"writerToken"',
            '"confirmMutation"',
            '"executionMode"',
            '"execution_mode"',
        ):
            if excluded not in ledger:
                errors.append(f"semantic fingerprint exclusion missing: {excluded}")

        if "if (fingerprint.Length == 0) return string.Empty;" not in ledger:
            errors.append("durable promotion must require a stable database fingerprint, not path-only identity")
        if "if (fingerprint.Length == 0 && path.Length == 0) return string.Empty;" in ledger:
            errors.append("path-only durable identity is forbidden")

    if 'case "cad_mutation_status"' not in agent:
        errors.append("cad_mutation_status must route read-only in McpCadAgentRuntime")
    if "McpMutationAckLedger.ReserveOrReplay" not in agent:
        errors.append("Mutation wrapper must reserve/replay action identity")
    if "McpMutationAckLedger.MarkApplied" not in agent:
        errors.append("synchronous mutation success must mark applied")
    if "McpMutationAckLedger.PromoteDurableForDocument" not in agent:
        errors.append("save mutation wrapper must promote only after the verified save action returns")
    if "IsDurabilitySaveTool" not in agent:
        errors.append("save-backed promotion must be restricted to verified save semantics")

    reserve = agent.find("McpMutationAckLedger.ReserveOrReplay")
    writer = agent.find("McpCadMutationCoordinator.EnterMutation")
    if reserve >= 0 and writer >= 0 and reserve > writer:
        errors.append("reserve/replay must occur before writer-gate entry")

    applied = agent.find("McpMutationAckLedger.MarkApplied")
    promote = agent.find("McpMutationAckLedger.PromoteDurableForDocument")
    if applied >= 0 and promote >= 0 and promote < applied:
        errors.append("the save action must be marked applied before save-backed durable promotion")

    if "McpMutationAckLedger.MarkNativeCommandTerminal" not in coord:
        errors.append("native command terminal events must report ACK outcome")
    if "MarkNativeCommandTerminal(completed" not in coord:
        errors.append("matching native terminal path must carry the completed pending command")

    if not SERVER.exists():
        errors.append("missing McpEmbeddedServerV2.cs public tool surface")
    else:
        if 'Tool("cad_mutation_status"' not in server:
            errors.append("tools/list must publish read-only cad_mutation_status")
        if "ActionIdProperty()" not in server:
            errors.append("public tool schemas must define bounded actionId")
        if 'raw.IndexOf("\\\"confirmMutation\\\"", StringComparison.Ordinal) >= 0' not in server:
            errors.append("descriptor augmentation must detect mutation schemas through confirmMutation")
        if "additions.Add(ActionIdProperty());" not in server:
            errors.append("mutation descriptors must receive optional actionId without manual per-tool duplication")

    if errors:
        print("FAIL: MCP durable mutation acknowledgement guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: durable mutation ACK identity, public retry schema, stable fingerprint proof, replay, native terminal success and save-backed durability contracts are present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
