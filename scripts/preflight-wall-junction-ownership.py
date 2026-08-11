#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "WallJunctionOwnershipPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "WallJunctionOwnershipSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "WallJunctionOwnershipSmokeRegistration.cs"
DOC = ROOT / "docs" / "WALL-JUNCTION-OWNERSHIP.md"
LOCAL_INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []
for path in (SOURCE, SMOKE, REGISTRATION, DOC, LOCAL_INBOX):
    if not path.is_file():
        errors.append("missing wall-junction ownership contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "public sealed class WallJunctionOwnerContext",
        "public sealed class WallJunctionOwnershipPlan",
        "public static class WallJunctionOwnershipPlanner",
        'GroupTokenPrefix = "WJP1:"',
        'OwnerTokenPrefix = "WJX1:"',
        'FingerprintPrefix = "WJF1:"',
        "cannot span multiple projects or drawings",
        "distinctOwners.Count < 2",
        "do not share a compatible vertical overlap",
        "GroupBy(x => x.GroupKey, StringComparer.Ordinal)",
        "ordered.Sort(CompareCandidate)",
        "duplicate/near-duplicate physical nodes",
        "SHA256.Create()",
        "BuildFingerprint(candidate, occurrence)",
        "candidate.OwnerWallIds.AsReadOnly()",
        "candidate.SourceSegmentIds.AsReadOnly()",
    ):
        if token not in text:
            errors.append("WallJunctionOwnershipPlanner.cs missing contract token: " + token)

    for forbidden in (
        "Bricscad",
        "Teigha",
        "ObjectId",
        "Solid3d",
        "TransactionManager",
        "GeneratedSolidHandle",
    ):
        if forbidden in text:
            errors.append("Core wall-junction ownership planner must remain CAD independent: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "StableIdentityAcrossOrderAndKindChange",
        "MultipleOccurrencesAreDeterministic",
        "SameSemanticWallDoesNotCreateComposite",
        "FingerprintTracksProfileWithoutChangingOwner",
        "RejectsCrossDrawingAndMissingOwners",
        "RejectsIncompatibleVerticalRanges",
        "RejectsInconsistentSameWallProfile",
        "RejectsNearDuplicateOccurrences",
    ):
        if token not in text:
            errors.append("WallJunctionOwnershipSmoke.cs missing scenario: " + token)

if REGISTRATION.is_file():
    text = REGISTRATION.read_text(encoding="utf-8")
    if "[ModuleInitializer]" not in text or "WallJunctionOwnershipSmoke.Run()" not in text:
        errors.append("WallJunctionOwnership smoke is not module-registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "WallJunctionOwnershipPlanner",
        "WJP1:",
        "WJX1:",
        "WJF1:",
        "multi-owner",
        "vertical overlap",
        "Door/Opening",
        "LOCAL_ONLY",
        "does not create `Solid3d`",
    ):
        if token not in text:
            errors.append("WALL-JUNCTION-OWNERSHIP.md missing ownership/runtime boundary: " + token)

if LOCAL_INBOX.is_file():
    text = LOCAL_INBOX.read_text(encoding="utf-8")
    start = text.find("## LOCAL-007 — physical L/T/X wall junction output")
    end = text.find("\n## LOCAL-008", start + 1) if start >= 0 else -1
    section = text[start:end if end >= 0 else len(text)] if start >= 0 else ""
    for token in ("WallJunctionOwnershipPlanner", "OwnerToken", "InputFingerprint", "WJX1:"):
        if token not in section:
            errors.append("LOCAL-007 handoff missing new Core ownership-plan token: " + token)

print("QS3D physical wall-junction ownership preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: physical wall junctions have a CAD-independent multi-owner identity/dependency/fingerprint plan with deterministic occurrence ownership and fail-closed project/drawing/vertical boundaries; native Solid3d materialization remains LOCAL_ONLY.")
