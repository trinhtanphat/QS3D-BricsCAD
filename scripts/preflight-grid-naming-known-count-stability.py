#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/GridNamingKnownCountStabilitySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing Grid known-Count stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    renumber_start = text.find("public static IReadOnlyList<GridLabelAssignment> Renumber(")
    renumber_end = text.find("public static string FormatLabel", renumber_start)
    renumber = text[renumber_start:renumber_end] if renumber_start >= 0 and renumber_end > renumber_start else ""
    required = (
        "var knownCount = TryGetKnownCount(orderedGridElementIds",
        "foreach (var value in orderedGridElementIds)",
        "ids.Count != knownCount.Value",
        "RevalidateKnownCountAfterTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);",
        "var originalTargets = ResolveOriginalTargets",
        "if (changed)",
        "project.Touch();",
    )
    positions = [renumber.find(token) for token in required]
    if not renumber or any(pos < 0 for pos in positions) or positions != sorted(positions):
        errors.append("GridNamingService.Renumber must rebind Count after exact traversal and before target planning/mutation.")
    if "known Count was exceeded during traversal" not in renumber:
        errors.append("GridNamingService.Renumber must reject known-Count overrun before retaining the extra id.")

    stable_start = text.find("private static void RevalidateKnownCountAfterTraversal(")
    stable_end = text.find("private static int? TryGetKnownCount(", stable_start)
    stable = text[stable_start:stable_end] if stable_start >= 0 and stable_end > stable_start else ""
    stable_required = (
        "TryGetKnownCount(source",
        "project.ChangeVersion != targetEnumerationVersion",
        "invalidNegativeKnownCount",
        "conflictingKnownCounts",
        "!reboundCount.HasValue || reboundCount.Value != admittedCount.Value",
    )
    if not stable or any(token not in stable for token in stable_required):
        errors.append("Grid post-traversal Count validation must re-read all Count surfaces and preserve project-version anti-race checks.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "GenericCountDriftFailsAtomically",
        "ReadOnlyCountDriftFailsAtomically",
        "NonGenericCountDriftFailsAtomically",
        "NegativePostTraversalCountFailsAtomically",
        "ConflictingPostTraversalCountsFailAtomically",
        "PostTraversalCountProjectMutationFailsBeforeGridMutation",
        "StableCountedInputSucceeds",
        "PureStreamingInputSucceeds",
        "ProjectMutatingCountCollection",
    ):
        if token not in text:
            errors.append("Grid known-Count stability smoke missing regression token: " + token)

print("QS3D Grid renumber known-Count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Grid renumber rebinds deterministic Count evidence before target planning/mutation.")
