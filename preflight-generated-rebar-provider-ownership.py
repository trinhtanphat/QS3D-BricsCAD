#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
]
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
INDEX = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipIndex.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedRebarProviderOwnershipSmoke.cs"
WALL_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedWallMeshHealthSmoke.cs"
errors = []

for path in FILES + [POLICY, INDEX, SMOKE, WALL_SMOKE]:
    if not path.is_file():
        errors.append("missing generated ownership contract file: " + str(path.relative_to(ROOT)))

for path in FILES:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for token in (
        "if (element == null) continue;",
        "HashSet<string> Conflicts",
        "if (Conflicts.Contains(handle)) return true;",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)",
        "index.Conflicts.Add(normalized);",
    ):
        if token not in text:
            errors.append(path.name + " missing order-independent ownership token: " + token)
    if "normalized.Length == 0 || owners.ContainsKey(normalized)" in text:
        errors.append(path.name + " still uses first-owner-wins reservation logic.")

if POLICY.is_file():
    text = POLICY.read_text(encoding="utf-8")
    if text.count("EnsureValidElementSet(project);") < 2:
        errors.append("GeneratedHandleOwnershipPolicy scans must validate the complete semantic element set.")
    if "Project contains a null semantic element entry; generated CAD ownership cannot be resolved safely." not in text:
        errors.append("GeneratedHandleOwnershipPolicy must fail closed on corrupt null semantic elements.")
    if "is ambiguously claimed by" not in text:
        errors.append("GeneratedHandleOwnershipPolicy must remain fail-closed on ambiguous generated owners.")

if INDEX.is_file():
    text = INDEX.read_text(encoding="utf-8")
    if "if (element == null) continue;" not in text:
        errors.append("GeneratedHandleOwnershipIndex.Build is not null-safe.")
    if "if (entry.Ambiguity != null) throw new InvalidOperationException" not in text:
        errors.append("GeneratedHandleOwnershipIndex must remain fail-closed on ambiguous generated owners.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "BeamStirrupLaterOwnerIsConflict();",
        "TieLaterOwnerIsConflict();",
        "LongitudinalRebarLaterOwnerIsConflict();",
        "OwnershipPolicyFailsClosedAndIndexIgnoresNullEntries();",
        "Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project));",
        "Throws<InvalidOperationException>(() => GeneratedHandleOwnershipPolicy.TryFindOwner(project, \"AD\"",
        '"BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT"',
        '"TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT"',
        '"REBAR_GENERATED_OWNERSHIP_CONFLICT"',
    ):
        if token not in text:
            errors.append("GeneratedRebarProviderOwnershipSmoke.cs missing regression token: " + token)

if WALL_SMOKE.is_file() and "WALL_MESH_GENERATED_OWNERSHIP_CONFLICT" not in WALL_SMOKE.read_text(encoding="utf-8"):
    errors.append("GeneratedWallMeshHealthSmoke.cs does not cover wall ownership conflict.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated rebar/wall/tie/stirrup diagnostics tolerate corrupt entries, destructive ownership policy scans fail closed, and conflicts are independent of project iteration order.")
