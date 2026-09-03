#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LEDGER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpMutationAckLedger.cs"

errors: list[str] = []
source = LEDGER.read_text(encoding="utf-8") if LEDGER.is_file() else ""
if not source:
    errors.append("missing McpMutationAckLedger.cs")
else:
    load_at = source.find("private static void LoadDurableLocked()")
    trim_at = source.find("private static void TrimDurableToBoundsLocked()", load_at)
    load = source[load_at:trim_at] if load_at >= 0 and trim_at > load_at else ""
    if not load:
        errors.append("missing LoadDurableLocked implementation")
    else:
        action_at = load.find("var actionId = ValidateActionId(FromBase64(fields[0]));")
        assign_at = load.find("Records[actionId] = new AckRecord")
        duplicate_at = load.find("Records.ContainsKey(actionId)")
        if action_at < 0 or assign_at < 0:
            errors.append("durable ledger loader no longer exposes the expected actionId decode/store sequence")
        elif duplicate_at < 0 or not (action_at < duplicate_at < assign_at):
            errors.append("durable ledger loader must reject duplicate persisted actionId values before storing a record")
        if "throw new InvalidDataException(\"Mutation ACK ledger contains a duplicate actionId.\")" not in load:
            errors.append("duplicate persisted actionId must fail closed as ledger corruption")
        catch_at = load.find("catch (Exception ex)")
        clear_at = load.find("Records.Clear();", catch_at)
        if catch_at < 0 or clear_at < catch_at:
            errors.append("corrupt durable ledger must still clear all partially loaded records")
        limit_at = load.find("if (loaded >= MaxDurableRecords)")
        parse_at = load.find("var fields = lines[i].Split('|');")
        if "loaded++" not in load or limit_at < 0:
            errors.append("duplicate-action hardening must preserve the bounded durable-record admission")
        elif parse_at < 0 or limit_at > parse_at:
            errors.append("durable record limit must fail closed before parsing or storing record 1025+")

if errors:
    print("FAIL: MCP durable mutation duplicate-action ledger guard")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: durable mutation ledger loading rejects duplicate persisted actionId identities before store, clears partial state on corruption, and preserves bounded fail-closed admission.")
