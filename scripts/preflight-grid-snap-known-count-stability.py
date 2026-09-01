#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LINE = ROOT / "src/QS3D.Core/Geometry/GridLineSnapPlanner.cs"
ARC = ROOT / "src/QS3D.Core/Geometry/GridArcSnapPlanner.cs"
HELPER = ROOT / "src/QS3D.Core/Geometry/GridSnapInputMaterializer.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridSnapKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print(f"ERROR: Grid snap known-Count stability preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (LINE, ARC, SMOKE):
    if not path.is_file():
        fail(f"missing required file {path.relative_to(ROOT)}")

line = LINE.read_text(encoding="utf-8")
arc = ARC.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for label, source in (("LINE", line), ("ARC", arc)):
    if "curves.Take(MaxCurves + 1).ToList()" in source:
        fail(f"{label} still materializes caller Current before Count/cap admission")
    if "GridSnapInputMaterializer.Materialize(curves, MaxCurves" not in source:
        fail(f"{label} must use the shared bounded Grid snap materializer")

if not HELPER.is_file():
    fail("shared GridSnapInputMaterializer.cs is required")
helper = HELPER.read_text(encoding="utf-8")

required_helper_tokens = (
    "ICollection<GridReferenceCurve>",
    "IReadOnlyCollection<GridReferenceCurve>",
    "System.Collections.ICollection",
    "ReadKnownCount",
    "ValidateKnownCount",
    "enumerator.MoveNext()",
    "enumerator.Current",
    "known Count changed during traversal",
    "more curves than its known Count",
    "reported {0} curves but traversal produced {1}",
)
for token in required_helper_tokens:
    if token not in helper:
        fail(f"shared materializer is missing contract token: {token}")

validate_start = helper.find("private static void ValidateKnownCount(")
if validate_start < 0:
    fail("shared materializer must expose a dedicated Count-revalidation helper")
validate_body = helper[validate_start:]
if "ReadKnownCount(curves, label)" not in validate_body:
    fail("Count-revalidation helper must re-read all supported known-Count interfaces")

move_index = helper.find("enumerator.MoveNext()")
current_index = helper.find("enumerator.Current")
if move_index < 0 or current_index < 0 or current_index <= move_index:
    fail("shared materializer must call MoveNext before Current")

between = helper[move_index:current_index]
if "ValidateKnownCount(curves, admittedCount, label)" not in between:
    fail("shared materializer must revalidate Count after successful MoveNext before Current")

required_smoke_tokens = (
    "LineRejectsOverCapBeforeCurrent",
    "ArcRejectsOverCapBeforeCurrent",
    "LineRejectsTransientGrowthBeforeCurrent",
    "ArcRejectsTransientGrowthBeforeCurrent",
    "LineRejectsNegativeCountBeforeTraversal",
    "ArcRejectsConflictingCountBeforeTraversal",
    "StableCountedAndStreamingInputsRemainSupported",
    "Equal(0, source.CurrentReads)",
    "[ModuleInitializer]",
)
for token in required_smoke_tokens:
    if token not in smoke:
        fail(f"deterministic smoke is missing contract token: {token}")

print("PASS Grid LINE/ARC snap known-Count stability contract")