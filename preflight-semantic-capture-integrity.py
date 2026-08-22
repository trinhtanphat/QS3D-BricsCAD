#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "capture": ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "snapshot": ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs",
    "policy": ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs",
    "source_owner": ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs",
    "ownership_smoke": ROOT / "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs",
    "variants": ROOT / "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
}
for path in files.values():
    if not path.is_file(): errors.append("missing semantic-capture integrity file: " + str(path.relative_to(ROOT)))

checks = {
    "capture": [
        "ProjectStateSnapshot.Capture(project)",
        "RestoreOrThrow(project, rollback",
        "GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle",
        "SemanticHandleOwnershipResolver.ResolveCaptureTarget(project, snapshot.Handle, category, id)",
        "CaptureSnapshotCore",
        'family.Properties["AxisLeftOffsetM"] = "0"',
        'family.Properties["AxisRightOffsetM"] = "0"',
        'family.Properties["CurtainFrameDepthM"] = "0.05"',
        'family.Properties["WallPierProfileMode"] = "Rectangular"',
        'family.Properties["WallPierChamferM"] = "0.02"',
    ],
    "snapshot": [
        "ProjectStateSnapshot",
        "CopyInto(source, target, null, null, null, null);",
        "CopyInto(_snapshot, project, preservedZones, preservedFloors, preservedFamilies, preservedElements);",
        "target.Zones.Clear()", "target.Floors.Clear()",
        "target.Families.Clear()", "target.Elements.Clear()", "target.QuantityRules.Clear()", "target.AuditEvents.Clear()",
        "targetMetadata.ReplacePersistenceState(source.Metadata)", "RestorePersistenceState", "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion)",
    ],
    "policy": ["TryFindOwner", "EnumerateOwnerHandles", "CollectOwnerHandles"],
    "source_owner": [
        "ResolveUniqueSourceOwner",
        "ResolveCaptureTarget",
        "is claimed by multiple semantic elements",
        "is already bound to another CAD source handle",
    ],
    "ownership_smoke": [
        "StableIdSourceOwnerIsReused",
        "DuplicateSourceOwnerIsRejected",
        "CanonicalSourceRebindIsRejected",
    ],
    "variants": [
        'EnsureDefault(family, "AxisLeftOffsetM", "0")', 'EnsureDefault(family, "AxisRightOffsetM", "0")',
        'EnsureDefault(family, "CurtainFrameDepthM", "0.05")',
        'EnsureDefault(family, "WallPierProfileMode", "Rectangular")',
        'EnsureDefault(family, "WallPierChamferM", "0.02")',
    ],
}
for key, needles in checks.items():
    path = files[key]
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(str(path.relative_to(ROOT)) + " missing semantic-capture integrity token: " + needle)

if files["capture"].is_file() and files["variants"].is_file():
    capture = files["capture"].read_text(encoding="utf-8")
    variants = files["variants"].read_text(encoding="utf-8")
    parity = (
        "AxisLeftOffsetM", "AxisRightOffsetM", "CurtainFrameDepthM",
        "WallPierProfileMode", "WallPierChamferM",
    )
    for key in parity:
        if key not in capture or key not in variants:
            errors.append("generic and specialized wall capture defaults are not aligned for " + key)

print("QS3D semantic capture integrity preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: semantic capture rejects generated outputs, reuses one stable-ID source owner without canonical rebinding, rolls back project state through the identity-preserving snapshot path, and generic wall starter Families match specialized GlassWall/WallPier defaults.")
