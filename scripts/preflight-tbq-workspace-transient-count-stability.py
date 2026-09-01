#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/TbqProjectWorkspaceState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TbqWorkspaceTransientCountSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/tbq-workspace-transient-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("TBQ transient Count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

def method_block(signature: str, next_signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        raise SystemExit("TBQ transient Count preflight missing source method: " + signature)
    end = source.find(next_signature, start + len(signature))
    if end < 0:
        raise SystemExit("TBQ transient Count preflight missing source boundary: " + next_signature)
    return source[start:end]

def require_explicit_traversal(block: str, source_name: str, enumerator_name: str, label_token: str) -> None:
    if "foreach (" in block:
        raise SystemExit("TBQ " + source_name + " traversal must not use caller-controlled foreach before Count guards.")
    get_enum = "using (var " + enumerator_name + " = " + source_name + ".GetEnumerator())"
    move = "var moved = " + enumerator_name + ".MoveNext();"
    current = "var item = " + enumerator_name + ".Current;"
    if get_enum not in block or move not in block or current not in block:
        raise SystemExit("TBQ " + source_name + " traversal must use an explicit enumerator/MoveNext/Current contract.")
    if block.count(label_token) < 3:
        raise SystemExit("TBQ " + source_name + " traversal must rebind known Count before MoveNext, after successful MoveNext, and after traversal.")
    enum_pos = block.index(get_enum)
    pre = block.index(label_token, enum_pos)
    move_pos = block.index(move, pre)
    post = block.index(label_token, move_pos)
    current_pos = block.index(current, post)
    if not enum_pos < pre < move_pos < post < current_pos:
        raise SystemExit("TBQ " + source_name + " Count ordering must be GetEnumerator -> Count -> MoveNext -> Count -> Current.")

bill = method_block(
    "private static IReadOnlyList<TbqBillItem> SnapshotBillItems",
    "private static IReadOnlyList<BuildUpRateSnapshot> SnapshotBuildUpRates")
require_explicit_traversal(
    bill,
    "items",
    "enumerator",
    'RequireKnownCountStable(items, MaxBillItems, "bill items", knownCount);')

build = method_block(
    "private static IReadOnlyList<BuildUpRateSnapshot> SnapshotBuildUpRates",
    "private static int? ValidateKnownCount")
require_explicit_traversal(
    build,
    "rates",
    "enumerator",
    'RequireKnownCountStable(rates, MaxBuildUpRates, "build-up rates", knownCount);')

bounded = method_block(
    "private static IEnumerable<T> Bounded<T>",
    "private static void RequireKnownCountMatchesTraversal")
if "foreach (" in bounded:
    raise SystemExit("TBQ bounded wrapper must not use caller-controlled foreach before Count guards.")
for token in (
    "using (var enumerator = source.GetEnumerator())",
    "RequireKnownCountStable(source, maximum, label, knownCount);",
    "var moved = enumerator.MoveNext();",
    "var item = enumerator.Current;",
):
    if token not in bounded:
        raise SystemExit("TBQ bounded wrapper missing traversal contract: " + token)
if bounded.count("RequireKnownCountStable(source, maximum, label, knownCount);") < 3:
    raise SystemExit("TBQ bounded wrapper must rebind known Count before MoveNext, after successful MoveNext, and after traversal.")
b_enum = bounded.index("using (var enumerator = source.GetEnumerator())")
b_pre = bounded.index("RequireKnownCountStable(source, maximum, label, knownCount);", b_enum)
b_move = bounded.index("var moved = enumerator.MoveNext();", b_pre)
b_post = bounded.index("RequireKnownCountStable(source, maximum, label, knownCount);", b_move)
b_current = bounded.index("var item = enumerator.Current;", b_post)
if not b_enum < b_pre < b_move < b_post < b_current:
    raise SystemExit("TBQ bounded Count ordering must be GetEnumerator -> Count -> MoveNext -> Count -> Current.")

for token in (
    "BillItemsRejectTransientCountBeforeCurrent();",
    "BuildUpRatesRejectTransientCountBeforeCurrent();",
    "RateReferencesRejectTransientCountBeforeCurrent();",
    "LibraryEntriesRejectTransientCountBeforeCurrent();",
    "KnownCountOverYieldDoesNotReadExtraCurrent();",
    "StableCountedAndStreamingControlsSucceed();",
    "CurrentReads == 0",
    "CurrentReads == 1",
    "TransientCountCollection<T>",
    "OverYieldCountCollection<T>",
):
    if token not in smoke:
        raise SystemExit("TBQ transient Count smoke missing contract: " + token)

for phrase in (
    "before MoveNext",
    "after successful MoveNext",
    "before Current",
    "bill items",
    "build-up rates",
    "rate references",
    "BQ library entries",
    "pure streaming",
    "10,000",
    "50,000",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("TBQ transient Count runbook missing boundary: " + phrase)

print("PASS TBQ workspace transient Count stability")
