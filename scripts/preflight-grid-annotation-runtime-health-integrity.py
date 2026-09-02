#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGridAnnotationRuntimeHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing grid-annotation runtime health source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"GRID_ANNOTATION_CAD_HANDLE_INVALID"',
        '"GRID_ANNOTATION_CAD_HANDLE_NON_CANONICAL"',
        '"GRID_ANNOTATION_CAD_MISSING"',
        '"GRID_ANNOTATION_CAD_TYPE_MISMATCH"',
        '"GRID_ANNOTATION_CAD_OWNERSHIP_MISMATCH"',
        '"GRID_ANNOTATION_CAD_TEXT_STALE"',
        '"GRID_ANNOTATION_CAD_OWNER_SPACE_MISMATCH"',
        "StringSplitOptions.None",
        "CadHandleService.NormalizeHexHandle(handle)",
        "string.Equals(handle, canonicalHandle, StringComparison.Ordinal)",
        "long.TryParse(canonicalHandle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)",
        "value <= 0L",
        "OpenMode.ForRead",
    )
    for token in required:
        if token not in text:
            errors.append("missing grid-annotation fail-visible/read-only/canonicality token: " + token)

    inspect_at = text.find("private static void InspectHandle(")
    next_method_at = text.find("private static bool MatchesExpectedType", inspect_at)
    if inspect_at < 0 or next_method_at < 0 or inspect_at >= next_method_at:
        errors.append("grid-annotation runtime health must retain a bounded InspectHandle implementation")
        inspect_text = ""
    else:
        inspect_text = text[inspect_at:next_method_at]

    forbidden_parser_drift = (
        "StringSplitOptions.RemoveEmptyEntries",
        ".Select(x => x.Trim())",
        "if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return;",
    )
    for token in forbidden_parser_drift:
        if token in inspect_text:
            errors.append("grid-annotation generated-handle health must preserve persisted slots and canonicalize before native resolution; forbidden token: " + token)

    normalize_at = inspect_text.find("CadHandleService.NormalizeHexHandle(handle)")
    resolve_at = inspect_text.find("document.Database.GetObjectId")
    if normalize_at < 0 or resolve_at < 0 or normalize_at > resolve_at:
        errors.append("grid-annotation generated-handle canonicality must be checked before native ObjectId resolution")

    owner_check_at = inspect_text.find("entity.OwnerId != authoritativeOwnerId")
    ownership_check_at = inspect_text.find("GeneratedGeometryService.HasMatchingOwnership")
    if owner_check_at < 0 or ownership_check_at < 0:
        errors.append("grid-annotation generated-handle health must report both owner-space and QS3D ownership drift")

    forbidden_mutation = (
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "ProjectMutationContext",
        "project.Touch(",
        ".Save(",
        ".Erase(",
        "StampOwnership(",
        "SetXData(",
    )
    for token in forbidden_mutation:
        if token in text:
            errors.append("grid-annotation health must remain read-only; forbidden token: " + token)

print("QS3D grid-annotation runtime-health integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: grid-annotation runtime health preserves generated-handle persisted slots, rejects non-canonical generated handles before native resolution, reports owner-space drift, and remains read-only while authoritative source lookup is validated separately.")
