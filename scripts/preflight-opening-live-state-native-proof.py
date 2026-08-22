#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "PhysicalOpeningCutLiveStateService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing PhysicalOpeningCutLiveStateService.cs")
else:
    text = SERVICE.read_text(encoding="utf-8")
    required = (
        'HasValue(x, "PhysicalOpeningCutSolidHandle") || HasValue(x, "PhysicalOpeningCutFingerprint")',
        "if (hasCutSolid != hasCutFingerprint)",
        '"PHYSICAL_OPENING_CUT_STATE_INCOMPLETE"',
        "RequireOwnedGeneratedSolid(document, transaction, project, host, generated, \"inspect physical opening live state\")",
        "RequireOwnedGeneratedSolid(document, transaction, project, host, generated, \"stamp physical opening live state\")",
        "GeneratedGeometryService.RequireMatchingOwnership(solid, project, host",
        "transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d",
        "if (changed > 0) project.Touch();",
    )
    for token in required:
        if token not in text:
            errors.append("opening live-state missing guard: " + token)

    inspect_ownership = text.find('RequireOwnedGeneratedSolid(document, transaction, project, host, generated, "inspect physical opening live state")')
    inspect_fingerprint = text.find("PhysicalOpeningCutLiveFingerprint.Compute(")
    if inspect_ownership < 0 or inspect_fingerprint < 0 or inspect_ownership > inspect_fingerprint:
        errors.append("health inspection must prove native generated-solid ownership before computing live fingerprint")

    stamp_start = text.find("private static int Stamp(")
    stamp_ownership = text.find('RequireOwnedGeneratedSolid(document, transaction, project, host, generated, "stamp physical opening live state")', stamp_start)
    stamp_fingerprint = text.find("PhysicalOpeningCutLiveFingerprint.Compute(", stamp_start)
    if stamp_start < 0 or stamp_ownership < 0 or stamp_fingerprint < 0 or stamp_ownership > stamp_fingerprint:
        errors.append("live-state stamp must prove native generated-solid ownership before computing/storing fingerprint")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: physical-opening live state detects incomplete cut metadata and verifies native generated-solid ownership before health/stamp fingerprint work.")
