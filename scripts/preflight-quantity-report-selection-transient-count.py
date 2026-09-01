from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/QuantityReportSelectionTransientCountSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs").read_text(encoding="utf-8")

helper_start = source.index("private static HashSet<string>? ResolveSelection")
helper_end = source.index("private static int? SnapshotKnownSelectionCount", helper_start)
helper = source[helper_start:helper_end]

required = [
    "using var enumerator = elementIds.GetEnumerator();",
    "RequireStableKnownSelectionCount(elementIds, knownCount.Value);",
    "var moved = enumerator.MoveNext();",
    "if (knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (observedCount >= MaxSelectionElementIds)",
    "var raw = enumerator.Current;",
    "observedCount++;",
]
for marker in required:
    if marker not in helper:
        raise SystemExit(f"quantity report transient selection Count guard missing marker: {marker}")

if "foreach (var raw in elementIds)" in helper:
    raise SystemExit("quantity report selection must not use implicit foreach across caller-controlled Count evidence")

move = helper.index("var moved = enumerator.MoveNext();")
pre = helper.rfind("RequireStableKnownSelectionCount(elementIds, knownCount.Value);", 0, move)
post_move = helper.index("RequireStableKnownSelectionCount(elementIds, knownCount.Value);", move)
overrun = helper.index("if (knownCount.HasValue && observedCount >= knownCount.Value)", post_move)
bound = helper.index("if (observedCount >= MaxSelectionElementIds)", overrun)
current = helper.index("var raw = enumerator.Current;", bound)
post_current = helper.index("RequireStableKnownSelectionCount(elementIds, knownCount.Value);", current)
retain = helper.index("observedCount++;", post_current)
semantic = helper.index("if (string.IsNullOrWhiteSpace(raw))", retain)
if not (pre < move < post_move < overrun < bound < current < post_current < retain < semantic):
    raise SystemExit("quantity report selection traversal ordering must be Count -> MoveNext -> Count -> overrun/bound -> Current -> Count -> retention")

for marker in [
    "MoveNextCountDriftFailsBeforeCurrent();",
    "CurrentCountDriftFailsBeforeSelectionRetention();",
    "StableCountedSelectionUsesTraversalWideRebounds();",
    "PureStreamingSelectionRemainsSinglePass();",
    "Expected 7, got ",
]:
    if marker not in smoke:
        raise SystemExit(f"quantity report transient selection Count smoke missing marker: {marker}")

if "QuantityReportSelectionTransientCountSmoke.Run();" not in registration:
    raise SystemExit("quantity report transient selection Count smoke is not registered")

print("quantity report selection transient Count preflight: PASS")
