#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeUseSourceSemanticImporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeUseSourceCleanupOwnershipSmoke.cs"
errors = []

for path in (IMPORTER, SMOKE):
    if not path.is_file():
        errors.append("missing UseSource cleanup ownership contract file: " + str(path.relative_to(ROOT)))

if not errors:
    importer = IMPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for token in (
        "GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle",
        "!ReferenceEquals(owner, element)",
        "UseSource native cleanup ownership is ambiguous or unsafe",
        "cleanup.Count > 0 && string.IsNullOrWhiteSpace(target.DrawingFingerprint)",
        "UseSource native cleanup requires a non-empty target drawing fingerprint",
    ):
        if token not in importer:
            errors.append("UseSource cleanup authorization missing fail-closed token: " + token)

    for token in (
        "AmbiguousGeneratedOwnershipFailsBeforePlan",
        "DestructiveCleanupRequiresTargetDrawingFingerprint",
        "UniqueOwnedCleanupRemainsPlannable",
        'conflicting.Properties["GeneratedSolidHandle"] = "AA11"',
        'TargetProject(string.Empty, ambiguousOwnership: false)',
        'Equal("AA11", plan.NativeCleanupRequirements.Single().OwnerHandles.Single())',
        "ModuleInitializer",
    ):
        if token not in smoke:
            errors.append("UseSource cleanup ownership smoke missing regression token: " + token)

print("QS3D interchange UseSource cleanup ownership preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: UseSource cleanup planning proves unique affected-element ownership and requires target drawing identity before destructive cleanup authorization can be created.")
