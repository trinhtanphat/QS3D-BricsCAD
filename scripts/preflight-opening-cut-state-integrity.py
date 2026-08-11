#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad"
FILES = (
    CAD / "OpeningBooleanService.cs",
    CAD / "CurvedOpeningBooleanService.cs",
)
errors = []

for path in FILES:
    if not path.is_file():
        errors.append("missing opening boolean service: " + path.name)
        continue
    text = path.read_text(encoding="utf-8")
    for token in (
        'TryGetValue("PhysicalOpeningCutSolidHandle"',
        'TryGetValue("PhysicalOpeningCutFingerprint"',
        "hasCutSolid",
        "hasCutFingerprint",
        "if (hasCutSolid != hasCutFingerprint)",
        "GeneratedGeometryService.RequireMatchingOwnership(",
        "ProjectStateSnapshot.Capture(project)",
        "rollback.Restore(project)",
        "BooleanOperation(BooleanOperationType.BoolSubtract",
    ):
        if token not in text:
            errors.append(path.name + " missing cut-state integrity token: " + token)

    mismatch = text.find("if (hasCutSolid != hasCutFingerprint)")
    subtract = text.find("BooleanOperation(BooleanOperationType.BoolSubtract")
    if mismatch < 0 or subtract < 0 or mismatch > subtract:
        errors.append(path.name + " must reject incomplete cut-state metadata before boolean subtraction")

    ownership = text.find("GeneratedGeometryService.RequireMatchingOwnership(")
    if ownership < 0 or subtract < 0 or ownership > subtract:
        errors.append(path.name + " must verify generated-host native ownership before boolean subtraction")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: straight and curved physical-opening cuts reject incomplete handle/fingerprint state, verify native host ownership and retain rollback before boolean subtraction.")
