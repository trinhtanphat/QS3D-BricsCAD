#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticHandleBoundaryOwnershipBoundSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/semantic-handle-boundary-ownership-bound.md"

for path, label in ((SOURCE, "source"), (SMOKE, "smoke"), (RUNBOOK, "runbook")):
    if not path.is_file():
        raise SystemExit("Semantic boundary ownership guard missing " + label + ": " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for token in (
    "private const int MaxBoundarySourceHandleCount = 5000;",
    "GetCanonicalBoundarySourceHandles(element, boundaryHandles)",
    "boundaryHandles.Split(",
    "MaxBoundarySourceHandleCount + 1",
    "StringSplitOptions.None",
    "tokens.Length > MaxBoundarySourceHandleCount",
    "AutoRoomLifecycle.NormalizeSourceHandles(tokens)",
    "!string.Equals(boundaryHandles, canonical, StringComparison.Ordinal)",
    "element.SourceHandles.Count == 0",
):
    if token not in source:
        raise SystemExit("Semantic boundary ownership source contract missing token: " + token)

if "boundaryHandles.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)" in source:
    raise SystemExit("Semantic boundary ownership must not use unbounded RemoveEmptyEntries tokenization.")

for token in (
    "ExactBoundaryCountRemainsSelectable",
    "OverLimitBoundaryMetadataFailsClosed",
    "NonCanonicalBoundaryMetadataFailsClosed",
    "ExplicitSourceHandlesStillSuppressBoundaryAliases",
    "MaxBoundarySourceHandleCount = 5000",
    "BoundaryHandles(MaxBoundarySourceHandleCount + 1)",
    '"A;;B"',
    'room.SourceHandles.Add("E1")',
):
    if token not in smoke:
        raise SystemExit("Semantic boundary ownership smoke contract missing token: " + token)

print("PASS semantic handle Auto Room boundary ownership bound guard")
