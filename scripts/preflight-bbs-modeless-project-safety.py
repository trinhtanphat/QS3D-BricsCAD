#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RebarScheduleWindow.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
<<<<<<< HEAD
    if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text:
        errors.append("BBS modeless window must remain bound to its source DWG")
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" not in text:
        errors.append("BBS export must re-resolve an existing project without creating one")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("BBS modeless callbacks must not create/cache a replacement project")
    if "ProjectStateSnapshot.CreateDetachedCopy(project)" not in text:
        errors.append("BBS export must regenerate a detached project snapshot")
    if "RegenerateDirty(snapshot)" not in text:
        errors.append("BBS export must regenerate only the detached project snapshot")
    if "ProjectRebarScheduleBuilder.Build(snapshot)" not in text:
        errors.append("BBS export must continue using the authoritative schedule builder")
    if "RegenerateDirty(project)" in text:
        errors.append("BBS modeless export must not regenerate the live project")
    if "EnsureActive(\"xuất BBS XLSX\")" not in text:
        errors.append("BBS export must verify the active source DWG before resolving project state")
=======
    for token in (
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "ProjectRebarScheduleBuilder.Build(snapshot)",
        "EnsureActive(\"xuất BBS XLSX\")",
    ):
        if token not in text:
            errors.append("BBS modeless export missing detached/read-only token: " + token)
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "ProjectRebarScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("BBS modeless export must not create, bind, or regenerate live project state: " + forbidden)
>>>>>>> origin/main

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] modeless BBS export is source-DWG bound, existing-project-only, and regenerates an authoritative detached snapshot")
