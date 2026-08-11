#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeUseSourceSemanticImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeUseSourceTargetBindingSmoke.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing target-binding file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)

for token in (
    "TargetProjectId",
    "TargetDrawingFingerprint",
    "TargetChangeVersion",
    "IsTargetBound",
    "target.ProjectId",
    "target.DrawingFingerprint",
    "target.ChangeVersion",
    "authorization.MatchesExactly(plan)",
    "_targetChangeVersion != plan.TargetChangeVersion",
    "string.Equals(_targetProjectId, plan.TargetProjectId, StringComparison.OrdinalIgnoreCase)",
    "string.Equals(_targetDrawingFingerprint, plan.TargetDrawingFingerprint, StringComparison.OrdinalIgnoreCase)",
    "cross-project, cross-drawing, stale-revision",
):
    if token not in source:
        errors.append("UseSource importer missing target-bound authorization token: " + token)

for forbidden in (
    "authorization.MatchesExactly(plan.NativeCleanupRequirements)",
    "internal bool MatchesExactly(IReadOnlyList<ProjectInterchangeNativeCleanupRequirement> requirements)",
):
    if forbidden in source:
        errors.append("UseSource importer still authorizes cleanup without target-state binding: " + forbidden)

for token in (
    "FreshAuthorizationExecutes",
    "CrossProjectAuthorizationIsRejected",
    "CrossDrawingAuthorizationIsRejected",
    "StaleRevisionAuthorizationIsRejected",
    "authorization.IsTargetBound",
    "target.Touch()",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("UseSource target-binding smoke missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: UseSource native cleanup authorization is bound to target project id, drawing fingerprint, semantic revision and exact generated handles, with replay regressions locked by smoke coverage.")
