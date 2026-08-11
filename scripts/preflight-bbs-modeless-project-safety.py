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

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] modeless BBS export is source-DWG bound, existing-project-only, and regenerates an authoritative detached snapshot")
