#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Rebar" / "ShapeRebarDistributionPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ShapeRebarDistributionCountBoundSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "private const int MaxBars = 10000;",
    "if (input.Count <= 0 || input.Count > MaxBars) throw new ArgumentOutOfRangeException(nameof(input.Count));",
    "var offsets = new double[input.Count];",
)
for marker in required_source:
    if marker not in source:
        raise SystemExit("missing shape-rebar count-bound contract: " + marker)

guard = source.index("if (input.Count <= 0 || input.Count > MaxBars)")
allocation = source.index("var offsets = new double[input.Count];")
if guard >= allocation:
    raise SystemExit("shape-rebar count guard must run before offsets allocation")

required_smoke = (
    "[ModuleInitializer]",
    "ExactLimitIsAccepted();",
    "FirstCountBeyondLimitIsRejected();",
    "PathologicalCountIsRejectedBeforeAllocation();",
    "Input(MaxBars)",
    "Input(MaxBars + 1)",
    "Input(int.MaxValue)",
)
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit("missing shape-rebar count-bound smoke contract: " + marker)

print("shape rebar distribution count bound preflight: PASS")
