#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "ProjectQuantityReportBuilder.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectQuantityCanonicalTraversalSmoke.cs"
ENTRY = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestEntryPoint.cs"


def fail(message: str) -> None:
    print("FAIL preflight-project-quantity-canonical-traversal: " + message)
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
entry = ENTRY.read_text(encoding="utf-8")

required_source = (
    "var elementInstances = project.Elements.ToList();",
    "var elements = elementInstances",
    ".OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)",
    ".ThenBy(x => x.Id, StringComparer.Ordinal)",
    "EnsureProjectRevision(project, reportVersion, elementInstances, floorInstances, zoneInstances, familyInstances, drawingFingerprint);",
)
for token in required_source:
    if token not in source:
        fail("ProjectQuantityReportBuilder missing canonical traversal token: " + token)

if "var elements = project.Elements.ToList();" in source:
    fail("ProjectQuantityReportBuilder still binds report traversal directly to persisted element insertion order")

required_smoke = (
    "ProjectQuantityReportBuilder.Group(forward)",
    "ProjectQuantityReportBuilder.Group(reverse)",
    "ProjectQuantityReportBuilder.Detail(forward)",
    "ProjectQuantityReportBuilder.Detail(reverse)",
    "Grouped ElementIds must be insertion-order invariant.",
    "Grouped source handles must be insertion-order invariant.",
    "note concatenation changed with insertion order.",
)
for token in required_smoke:
    if token not in smoke:
        fail("canonical traversal smoke missing token: " + token)

if "ProjectQuantityCanonicalTraversalSmoke.Run();" not in entry:
    fail("canonical traversal smoke is not registered in SmokeTestEntryPoint")

print("PASS preflight-project-quantity-canonical-traversal")
