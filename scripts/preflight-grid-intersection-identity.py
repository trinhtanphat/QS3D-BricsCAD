#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IDENTITY = ROOT / "src/QS3D.Core/Geometry/GridIntersectionIdentityPlanner.cs"
PLANNER = ROOT / "src/QS3D.Core/Geometry/GridIntersectionPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridIntersectionIdentitySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/GridIntersectionIdentitySmokeRegistration.cs"
DOC = ROOT / "docs/GRID-INTERSECTION-OWNERSHIP.md"
errors = []

for path in (IDENTITY, PLANNER, SMOKE, REGISTRATION, DOC):
    if not path.is_file():
        errors.append("missing Grid intersection identity contract file: " + str(path.relative_to(ROOT)))

if IDENTITY.is_file():
    text = IDENTITY.read_text(encoding="utf-8")
    for token in (
        'public sealed class GridIntersectionIdentity',
        'public static class GridIntersectionIdentityPlanner',
        'MaxIntersections = 100000',
        'MaxElementIdLength = 128',
        'MaxIntersectionsPerPair = 2',
        'PairTokenPrefix = "GIP1:"',
        'OwnerTokenPrefix = "GIX1:"',
        'normalized.ToUpperInvariant()',
        'BuildPairKey(first, second)',
        'SHA256.Create()',
        'groups.Values.OrderBy(x => x.PairKey, StringComparer.Ordinal)',
        'group.Points.Sort(ComparePoint)',
        'Near(group.Points[index - 1], group.Points[index], pointTolerance)',
        'pair-token hash collision detected',
        'BuildIntersectionOwner(group.FirstElementId, group.SecondElementId, index)',
        'var pairToken = BuildPairToken(firstElementId, secondElementId);',
        'OwnerTokenPrefix + pairToken.Substring(PairTokenPrefix.Length) + ":" + occurrenceIndex',
    ):
        if token not in text:
            errors.append("GridIntersectionIdentityPlanner.cs missing deterministic ownership token: " + token)

    for forbidden in (
        'ProjectState',
        'ProjectElement',
        'ElementCategory',
        'GeneratedGeometryService',
        'ObjectId',
        'Handle',
    ):
        if forbidden in text:
            errors.append("Core Grid intersection identity must remain CAD/schema independent: " + forbidden)

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        'roots.Sort()',
        'points.Sort((left, right)',
        'var x = left.X.CompareTo(right.X)',
        'return x != 0 ? x : left.Y.CompareTo(right.Y)',
    ):
        if token not in text:
            errors.append("GridIntersectionPlanner.cs must retain deterministic multi-point ordering evidence: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        'PairOrderAndInputOrderAreStable',
        'CaseAndWhitespaceCanonicalize',
        'NearDuplicatePointFailsClosed',
        'MoreThanTwoPointsPerPairFailsClosed',
        'SameGridPairFailsClosed',
        'OwnerTokenIsCompact',
    ):
        if token not in text:
            errors.append("GridIntersectionIdentitySmoke.cs missing scenario: " + token)

if REGISTRATION.is_file():
    text = REGISTRATION.read_text(encoding="utf-8")
    if '[ModuleInitializer]' not in text or 'GridIntersectionIdentitySmoke.Run()' not in text:
        errors.append("GridIntersectionIdentity smoke is not module-registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'GridIntersectionIdentityPlanner',
        'GIP1:',
        'GIX1:',
        'canonical pair',
        'occurrence 0/1',
        'SHA-256',
        'does not add `ElementCategory.GridIntersection`',
        'pair-owned',
        'LOCAL_ONLY',
    ):
        if token not in text:
            errors.append("GRID-INTERSECTION-OWNERSHIP.md missing pair-owner boundary: " + token)

print("QS3D Grid intersection identity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Grid intersection marker ownership has a CAD-independent canonical pair identity, compact SHA-256 tokens, deterministic occurrence assignment and fail-closed ambiguity guards; native marker materialization remains LOCAL_ONLY.")
