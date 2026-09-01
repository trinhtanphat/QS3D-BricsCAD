#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipInputIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/generated-handle-ownership-input-integrity.md"


def fail(message: str) -> None:
    print("ERROR: generated handle ownership input integrity preflight failed: " + message)
    raise SystemExit(1)


for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        fail("missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

method_start = source.find("public static IReadOnlyList<string> ValidateAllBeforeErase(")
helper_start = source.find("private static int? ResolveKnownDestructiveHandleCount", method_start)
if method_start < 0 or helper_start < 0:
    fail("ValidateAllBeforeErase or known-Count helper missing")
region = source[method_start:helper_start]

required = [
    "private const int MaxDestructiveHandleCount = 10000;",
    "var knownCount = ResolveKnownDestructiveHandleCount(handles);",
    "using (var enumerator = handles.GetEnumerator())",
    "while (true)",
    "RequireStableKnownDestructiveHandleCount(handles, knownCount);",
    "var moved = enumerator.MoveNext();",
    "if (knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (observedCount >= MaxDestructiveHandleCount)",
    "var rawHandle = enumerator.Current;",
    "if (knownCount.HasValue && observedCount != knownCount.Value)",
    "normalized.Sort(StringComparer.OrdinalIgnoreCase);",
    "nativeOwnershipValidator(handle);",
    "handles is ICollection<string> genericCollection",
    "handles is IReadOnlyCollection<string> readOnlyCollection",
    "handles is ICollection nonGenericCollection",
    "candidate < 0",
    "candidate > MaxDestructiveHandleCount",
    "knownCount.HasValue && knownCount.Value != candidate",
]
missing = [token for token in required if token not in source]
if missing:
    fail("source contract token(s) missing: " + repr(missing))

loop = region.find("while (true)")
pre = region.find("RequireStableKnownDestructiveHandleCount(handles, knownCount);", loop)
move = region.find("var moved = enumerator.MoveNext();", pre)
post = region.find("RequireStableKnownDestructiveHandleCount(handles, knownCount);", pre + 1)
break_guard = region.find("if (!moved) break;", post)
known_overrun = region.find("if (knownCount.HasValue && observedCount >= knownCount.Value)", break_guard)
hard_cap = region.find("if (observedCount >= MaxDestructiveHandleCount)", known_overrun)
current = region.find("var rawHandle = enumerator.Current;", hard_cap)
post_current = region.find("RequireStableKnownDestructiveHandleCount(handles, knownCount);", post + 1)
normalize = region.find("var handle = NormalizeHandleIdentity(rawHandle);", current)
final_stability = region.rfind("RequireStableKnownDestructiveHandleCount(handles, knownCount);")
under_yield = region.find("if (knownCount.HasValue && observedCount != knownCount.Value)", final_stability)
sort = region.find("normalized.Sort(StringComparer.OrdinalIgnoreCase);", under_yield)
callback = region.find("nativeOwnershipValidator(handle);", sort)

if min(loop, pre, move, post, break_guard, known_overrun, hard_cap, current, post_current, normalize, final_stability, under_yield, sort, callback) < 0:
    fail("unable to locate destructive traversal ordering")
if not (loop < pre < move < post < break_guard < known_overrun < hard_cap < current < post_current < normalize < final_stability < under_yield < sort < callback):
    fail("destructive traversal must order Count -> MoveNext -> Count -> admission -> Current -> Count -> normalize -> final Count -> cardinality -> sort -> native callback")
if "foreach (var rawHandle in handles)" in region:
    fail("caller-controlled destructive input regressed to foreach")
if region.count("var rawHandle = enumerator.Current;") != 1:
    fail("destructive traversal must read Current exactly once")

smoke_cases = [
    "KnownCountOverrunRejectsBeforeUnexpectedCurrentOrCallback",
    "MoveNextCountGrowthRejectsBeforeCurrentOrCallback",
    "MoveNextCountShrinkRejectsBeforeCurrentOrCallback",
    "MoveNextNegativeCountRejectsBeforeCurrentOrCallback",
    "MoveNextCrossInterfaceConflictRejectsBeforeCurrentOrCallback",
    "KnownCountUnderYieldRejectsBeforeCallback",
    "StreamingHardCapRejectsBeforeExtraCurrentOrCallback",
    "StableCountedInputPreservesSortedValidation",
    "PureStreamingInputPreservesSortedValidation",
]
missing_smoke = [name for name in smoke_cases if name not in smoke]
if missing_smoke:
    fail("smoke case(s) missing: " + repr(missing_smoke))
if "Equal(MaxHandleCount, source.CurrentReads, \"streaming hard-cap Current reads\")" not in smoke:
    fail("streaming hard cap must prove no extra Current read")
if smoke.count("Equal(0, callbacks") < 3:
    fail("hostile admission smoke must pin zero native callbacks")

for token in ("10,000", "before `Current`", "zero native callback", "NOT_APPLICABLE"):
    if token not in runbook:
        fail("runbook token missing: " + token)

print("PASS generated handle ownership destructive input integrity")
