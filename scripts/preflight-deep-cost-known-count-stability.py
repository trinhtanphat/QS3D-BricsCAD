from pathlib import Path

SOURCE = Path("src/QS3D.Core/Cost/DeepCostWorkflows.cs")
SMOKE = Path("tests/QS3D.Core.SmokeTests/DeepCostTransientCountSmoke.cs")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source_tokens = [
    "RequireKnownCountStableDuringTraversal",
    "Rate reference edge collection",
    "Build-up analysis rate collection",
    "Trade analysis item collection",
    "BQ library entry collection",
    "BQ project import collection",
]
for token in required_source_tokens:
    if token not in source:
        raise SystemExit(f"missing DeepCost traversal Count stability token: {token}")

stale_loops = [
    "while (edgeEnumerator.MoveNext())",
    "while (rateEnumerator.MoveNext())",
    "while (itemEnumerator.MoveNext())",
    "while (entryEnumerator.MoveNext())",
    "while (projectEntryEnumerator.MoveNext())",
]
for token in stale_loops:
    if token in source:
        raise SystemExit(f"DeepCost traversal still crosses MoveNext without explicit Count rebound: {token}")

for enumerator in [
    "edgeEnumerator",
    "rateEnumerator",
    "itemEnumerator",
    "entryEnumerator",
    "projectEntryEnumerator",
]:
    move = f"if (!{enumerator}.MoveNext())"
    current = f"{enumerator}.Current"
    if move not in source or current not in source:
        raise SystemExit(f"missing explicit DeepCost enumerator traversal for {enumerator}")
    move_pos = source.index(move)
    current_pos = source.index(current, move_pos)
    rebound_pos = source.find("RequireKnownCountStableDuringTraversal", move_pos, current_pos)
    if rebound_pos < 0:
        raise SystemExit(f"DeepCost {enumerator} must rebind Count after successful MoveNext before Current")

required_smoke_tokens = [
    "RateReferenceGraphRejectsTransientCountBeforeCurrent",
    "BuildUpAnalysisRejectsTransientCountBeforeCurrent",
    "TradeAnalysisRejectsTransientCountBeforeCurrent",
    "BqLibraryRejectsTransientCountBeforeCurrent",
    "BqProjectImportRejectsTransientCountBeforeCurrent",
    "CurrentReads == 0",
    "StableCountedAndStreamingControlsSucceed",
]
for token in required_smoke_tokens:
    if token not in smoke:
        raise SystemExit(f"missing DeepCost transient Count smoke token: {token}")

print("PASS deep cost known-count traversal stability guard")
