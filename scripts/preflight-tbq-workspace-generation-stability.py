from pathlib import Path

SOURCE = Path("src/QS3D.Core/Cost/TbqProjectWorkspaceState.cs")
text = SOURCE.read_text(encoding="utf-8")

required = [
    "RequireStableBillItemGeneration",
    "SameBillItemState",
    "RequireStableBuildUpRateGeneration",
    "SameBuildUpRateState",
    "TBQ workspace bill item source content changed during traversal.",
    "TBQ workspace build-up rate source content changed during traversal.",
    "string.Equals(left.ItemCode, right.ItemCode, StringComparison.Ordinal)",
    "string.Equals(left.Description, right.Description, StringComparison.Ordinal)",
    "string.Equals(left.Unit, right.Unit, StringComparison.Ordinal)",
    "string.Equals(left.TradeCode, right.TradeCode, StringComparison.Ordinal)",
    "left.Quantity == right.Quantity",
    "left.UnitRate == right.UnitRate",
    "string.Equals(left.RateCode, right.RateCode, StringComparison.Ordinal)",
]
for token in required:
    if token not in text:
        raise SystemExit(f"TBQ workspace generation guard missing required contract: {token}")

bill_method = text.index("private static IReadOnlyList<TbqBillItem> SnapshotBillItems")
build_method = text.index("private static IReadOnlyList<BuildUpRateSnapshot> SnapshotBuildUpRates")
bill_replay = text.index("RequireStableBillItemGeneration(items, knownCount.Value, snapshot);", bill_method)
bill_sort = text.index("snapshot.Sort(CompareBillItems);", bill_method)
if not bill_method < bill_replay < bill_sort:
    raise SystemExit("TBQ bill-item semantic replay must occur before publication sort.")

build_replay = text.index("RequireStableBuildUpRateGeneration(rates, knownCount.Value, snapshot);", build_method)
build_sort = text.index("snapshot.Sort(CompareBuildUps);", build_method)
if not build_method < build_replay < build_sort:
    raise SystemExit("TBQ build-up semantic replay must occur before publication sort.")

bill_window = text[bill_method:build_method]
if "if (knownCount.HasValue)\n                RequireStableBillItemGeneration" not in bill_window:
    raise SystemExit("TBQ bill-item replay must remain conditional on authoritative Count.")

build_helper = text.index("private static void RequireStableBillItemGeneration")
build_window = text[build_method:build_helper]
if "if (knownCount.HasValue)\n                RequireStableBuildUpRateGeneration" not in build_window:
    raise SystemExit("TBQ build-up replay must remain conditional on authoritative Count.")

bill_helper = text.index("private static void RequireStableBillItemGeneration")
build_helper = text.index("private static void RequireStableBuildUpRateGeneration")
bill_helper_text = text[bill_helper:build_helper]
for token in [
    'RequireKnownCountStable(items, MaxBillItems, "bill items", knownCount)',
    "enumerator.MoveNext()",
    "enumerator.Current",
    "SameBillItemState(admittedItems[index], item)",
]:
    if token not in bill_helper_text:
        raise SystemExit(f"TBQ bill-item replay lost fail-closed traversal contract: {token}")

build_end = text.index("private static bool SameBuildUpRateState", build_helper)
build_helper_text = text[build_helper:build_end]
for token in [
    'RequireKnownCountStable(rates, MaxBuildUpRates, "build-up rates", knownCount)',
    "enumerator.MoveNext()",
    "enumerator.Current",
    "SameBuildUpRateState(admittedRates[index], rate)",
]:
    if token not in build_helper_text:
        raise SystemExit(f"TBQ build-up replay lost fail-closed traversal contract: {token}")

print("PASS TBQ workspace generation stability source guard")
