#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TOLERANT_FILES = [
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
]
REBAR = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs"
FILES = TOLERANT_FILES + [REBAR]
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

for path in TOLERANT_FILES:
    if path.is_file() and "if (element == null) continue;" not in path.read_text(encoding="utf-8"):
        errors.append(path.name + " diagnostic provider no longer tolerates isolated null entries.")

if REBAR.is_file():
    text = REBAR.read_text(encoding="utf-8")
    null_guard = 'throw new InvalidOperationException("Generated rebar health cannot inspect a null project element.")'
    if text.count(null_guard) != 4:
        errors.append("GeneratedRebarHealthService must fail closed in exactly four semantic traversals.")
    if "if (element == null) continue;" in text:
        errors.append("GeneratedRebarHealthService must not silently skip null semantic entries.")

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
    if "if (element == null) continue;" not in text:
        errors.append("GeneratedHandleOwnershipIndex diagnostic cache no longer tolerates isolated null entries.")
    if "if (entry.Ambiguity != null) throw new InvalidOperationException" not in text:
        errors.append("GeneratedHandleOwnershipIndex must remain fail-closed on ambiguous generated owners.")

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

print("PASS: generated wall/tie/stirrup diagnostics remain null-tolerant and order-independent; GeneratedRebar and canonical ownership policy fail closed on corrupt semantic element sets and ambiguity.")
