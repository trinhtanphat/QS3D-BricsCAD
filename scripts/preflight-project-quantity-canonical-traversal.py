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

# Canonical ordering must be applied to the detached immutable generation snapshot.
# Do not regress to ordering/traversing mutable project.Elements references directly.
required_source = (
    "var snapshot = ProjectQuantityGenerationSnapshot.Capture(project);",
    "var elements = snapshot.Elements.OrderBy(x => x.Element.Id, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Element.Id, StringComparer.Ordinal).ToList();",
    "foreach (var elementSnapshot in elements)",
    "var element = elementSnapshot.Element;",
    "EnsureProjectRevision(project, snapshot);",
)
for token in required_source:
    if token not in source:
        fail("ProjectQuantityReportBuilder missing frozen canonical traversal token: " + token)

for forbidden in (
    "var elementInstances = project.Elements.ToList();",
    "var elements = project.Elements.ToList();",
    "var elements = project.Elements.OrderBy(",
):
    if forbidden in source:
        fail("ProjectQuantityReportBuilder canonical traversal must remain detached from mutable project element instances: " + forbidden)

if source.count("EnsureProjectRevision(project, snapshot);") < 4:
    fail("ProjectQuantityReportBuilder must revalidate the immutable generation during canonical traversal and before publication")

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
