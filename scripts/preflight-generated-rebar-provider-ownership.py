#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FAIL_CLOSED_FILES = {
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs": "Wall mesh health cannot inspect a null project element.",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs": "Beam stirrup health cannot inspect a null project element.",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs": "Tie rebar health cannot inspect a null project element.",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs": "Generated rebar health cannot inspect a null project element.",
}
FILES = list(FAIL_CLOSED_FILES)
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
INDEX = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipIndex.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedRebarProviderOwnershipSmoke.cs"
SAFETY_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipSafetySmoke.cs"
WALL_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedWallMeshHealthSmoke.cs"
errors = []

for path in FILES + [POLICY, INDEX, SMOKE, SAFETY_SMOKE, WALL_SMOKE]:
    if not path.is_file():
        errors.append("missing generated ownership contract file: " + str(path.relative_to(ROOT)))

for path in FILES:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for token in (
        "HashSet<string> Conflicts",
        "if (Conflicts.Contains(handle)) return true;",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)",
        "index.Conflicts.Add(normalized);",
    ):
        if token not in text:
            errors.append(path.name + " missing order-independent diagnostic ownership token: " + token)
    if "normalized.Length == 0 || owners.ContainsKey(normalized)" in text:
        errors.append(path.name + " still uses first-owner-wins reservation logic.")
    null_message = FAIL_CLOSED_FILES[path]
    null_guard = 'throw new InvalidOperationException("' + null_message + '")'
    if null_guard not in text:
        errors.append(path.name + " must reject null semantic entries fail closed: " + null_message)
    if "if (element == null) continue;" in text:
        errors.append(path.name + " must not silently skip null semantic entries.")

if POLICY.is_file():
    text = POLICY.read_text(encoding="utf-8")
    if text.count("EnsureValidElementSet(project);") < 2:
        errors.append("GeneratedHandleOwnershipPolicy scans must validate the complete semantic element set.")
    for token in (
        "Project contains a null semantic element entry; generated CAD ownership cannot be resolved safely.",
        "Project contains a blank semantic element id; generated CAD ownership cannot be resolved safely.",
        "Project contains duplicate element id:",
        "is ambiguously claimed by",
    ):
        if token not in text:
            errors.append("GeneratedHandleOwnershipPolicy missing fail-closed ownership token: " + token)
    for forbidden in (".Where(x => x != null)", "if (element == null) continue;"):
        if forbidden in text:
            errors.append("GeneratedHandleOwnershipPolicy must not silently skip corrupt semantic entries: " + forbidden)

if INDEX.is_file():
    text = INDEX.read_text(encoding="utf-8")
    for token in (
        "EnsureValidUniqueElementIds(project);",
        'throw new InvalidOperationException("Generated handle ownership index cannot inspect a null project element.")',
        "if (entry.Ambiguity != null) throw new InvalidOperationException",
    ):
        if token not in text:
            errors.append("GeneratedHandleOwnershipIndex missing fail-closed ownership token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("GeneratedHandleOwnershipIndex must not silently skip null semantic entries.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "BeamStirrupLaterOwnerIsConflict();",
        "TieLaterOwnerIsConflict();",
        "LongitudinalRebarLaterOwnerIsConflict();",
        "OwnershipPoliciesFailClosedOnNullEntries();",
        "RequireThrows<InvalidOperationException>",
        '"BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT"',
        '"TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT"',
        '"REBAR_GENERATED_OWNERSHIP_CONFLICT"',
    ):
        if token not in text:
            errors.append("GeneratedRebarProviderOwnershipSmoke.cs missing regression token: " + token)

if SAFETY_SMOKE.is_file():
    text = SAFETY_SMOKE.read_text(encoding="utf-8")
    for token in ("NullElementFailsClosed();", "DuplicateElementIdsFailClosed();", "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)"):
        if token not in text:
            errors.append("GeneratedHandleOwnershipSafetySmoke.cs missing canonical fail-closed regression: " + token)

if WALL_SMOKE.is_file() and "WALL_MESH_GENERATED_OWNERSHIP_CONFLICT" not in WALL_SMOKE.read_text(encoding="utf-8"):
    errors.append("GeneratedWallMeshHealthSmoke.cs does not cover wall ownership conflict.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated wall/tie/stirrup/rebar diagnostics and the canonical ownership index reject corrupt null semantic entries fail closed while preserving order-independent ownership ambiguity detection.")
