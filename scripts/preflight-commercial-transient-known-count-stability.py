from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CommercialTransientKnownCountStabilitySmoke.cs").read_text(encoding="utf-8")
legacy_guard = (ROOT / "scripts/preflight-commercial-known-count-overrun.py").read_text(encoding="utf-8")

required_smoke = [
    "AuditBatchTransientGrowthRejectsBeforeCurrent",
    "SourceRevisionTransientNegativeRejectsBeforeCurrent",
    "SourceRevisionTransientConflictRejectsBeforeCurrent",
    "StableMultiSurfaceCountRemainsAccepted",
    "CurrentReads",
    "[ModuleInitializer]",
]

required_source = [
    "RequireStableKnownCountDuringTraversal(records, knownCount)",
    "RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum)",
    "while (enumerator.MoveNext())",
    "CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count, \"Commercial audit batch source\")",
    "RequireCanProcessNext(knownCount, result.Count, paramName)",
]

missing = [token for token in required_smoke if token not in smoke]
missing += [token for token in required_source if token not in source]
if "preflight-commercial-known-count-overrun.py" not in str(ROOT / "scripts/preflight-commercial-known-count-overrun.py"):
    missing.append("historical commercial known-count guard")
if missing:
    raise SystemExit("Commercial transient known-Count stability preflight failed; missing: " + ", ".join(missing))

# Strong ordering contract: traversal-time Count rebound must occur after a successful
# MoveNext and before both the existing cardinality guard and semantic Current read.
audit_loop = source.index("while (enumerator.MoveNext())", source.index("public void AppendBatch"))
audit_rebound = source.index("RequireStableKnownCountDuringTraversal(records, knownCount)", audit_loop)
audit_overrun = source.index("CommercialGuard.RequireCanProcessNext(knownCount, snapshot.Count", audit_loop)
audit_current = source.index("var record = enumerator.Current", audit_loop)
if not audit_loop < audit_rebound < audit_overrun < audit_current:
    raise SystemExit("Commercial audit traversal must rebind Count before overrun admission and semantic Current.")

snapshot_method = source.index("internal static IReadOnlyList<T> Snapshot<T>")
snapshot_loop = source.index("while (enumerator.MoveNext())", snapshot_method)
snapshot_rebound = source.index("RequireStableSnapshotKnownCountDuringTraversal(source, knownCount, paramName, maximum)", snapshot_loop)
snapshot_overrun = source.index("RequireCanProcessNext(knownCount, result.Count, paramName)", snapshot_loop)
snapshot_current = source.index("var item = enumerator.Current", snapshot_loop)
if not snapshot_loop < snapshot_rebound < snapshot_overrun < snapshot_current:
    raise SystemExit("Commercial shared snapshot traversal must rebind Count before overrun admission and semantic Current.")

# Historical N+1/null precedence must remain explicitly pinned.
for token in [
    "Commercial known-Count overrun guard must precede record semantic validation",
    "Commercial snapshot known-Count overrun guard must precede item semantic validation",
]:
    if token not in legacy_guard:
        raise SystemExit("Historical commercial known-count guard was weakened; missing: " + token)

print("PASS commercial transient known-Count stability source contract")
