#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Coordination/DuplicateDetection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DuplicateDetectionOptionsSnapshotSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/duplicate-detection-options-snapshot.md"

for path, label in ((SOURCE, "source"), (SMOKE, "smoke"), (RUNBOOK, "runbook")):
    if not path.is_file():
        raise SystemExit("Duplicate options snapshot guard missing " + label + ": " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for token in (
    "private sealed class DuplicateDetectionPolicySnapshot",
    "private static DuplicateDetectionPolicySnapshot CapturePolicy(DuplicateDetectionOptions? options)",
    "var policy = new DuplicateDetectionPolicySnapshot(",
    "ValidateCoordinateTolerance(policy.CoordinateToleranceM);",
    "return DetectSnapshot(MaterializeElements(elements), policy);",
    "return DetectSnapshot(MaterializeCandidates(candidates), policy);",
    "DuplicateDetectionPolicySnapshot policy",
    "policy.EnableSemanticIdentity",
    "policy.RequireSameDisciplineForGeometry",
    "policy.RequireSameCategoryForGeometry",
    "policy.CoordinateToleranceM",
):
    if token not in source:
        raise SystemExit("Duplicate options snapshot source contract missing token: " + token)

for overload_marker, materialize_marker in (
    ("IEnumerable<CoordinationElement> elements", "MaterializeElements(elements)"),
    ("IEnumerable<DuplicateCandidate> candidates", "MaterializeCandidates(candidates)"),
):
    start = source.index(overload_marker)
    end = source.index("}", start)
    body = source[start:end]
    capture = body.index("var policy = CapturePolicy(options);")
    materialize = body.index(materialize_marker)
    if capture >= materialize:
        raise SystemExit("Duplicate options must be snapshotted before caller-controlled materialization.")

if "DetectSnapshot(MaterializeElements(elements), effective)" in source or "DetectSnapshot(MaterializeCandidates(candidates), effective)" in source:
    raise SystemExit("Duplicate detection must not retain the mutable options alias across enumeration.")

for token in (
    "[ModuleInitializer]",
    "ElementTraversalCannotWidenAdmittedTolerance",
    "CandidateTraversalCannotEnableSemanticIdentity",
    "CandidateTraversalCannotRelaxCategoryPolicy",
    "InvalidInitialToleranceFailsBeforeEnumeration",
    "StableOptionsRemainAccepted",
    "options.CoordinateToleranceM = 1d",
    "options.EnableSemanticIdentity = true",
    "options.RequireSameCategoryForGeometry = false",
    "Equal(0, source.GetEnumeratorCalls",
):
    if token not in smoke:
        raise SystemExit("Duplicate options snapshot smoke contract missing token: " + token)

print("PASS duplicate detection immutable options snapshot guard")
