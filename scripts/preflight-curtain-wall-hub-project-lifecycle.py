#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CurtainWallWindow.xaml.cs"

errors = []
if not SOURCE.is_file():
    errors.append("missing CurtainWallWindow.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Curtain Wall Hub must not create/cache replacement project state from modeless UI.")

    if text.count("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)") < 2:
        errors.append("Curtain Wall Hub read-only Refresh/Summary paths must resolve existing project state with TryGetReadOnly.")

    if text.count("ExistingProjectMutationContext.TryGet(_document, out var project)") < 2:
        errors.append("Curtain Wall Hub Save/Recalculate paths must bind canonical existing project state before mutation.")

    for token in (
        "ClearProjectView();",
        "Vách Kính Hub không tạo project mới",
        "RefreshSummary(project);",
        "ProjectStateSnapshot.Capture(project)",
        "RestoreOrThrow(project, rollback, operationError",
        "TrySyncCommittedUi",
    ):
        if token not in text:
            errors.append("Curtain Wall Hub lifecycle missing token: " + token)

    refresh = text.find("private void RefreshAll()")
    read_only = text.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", refresh)
    clear = text.find("ClearProjectView();", read_only)
    if min(refresh, read_only, clear) < 0 or not refresh < read_only < clear:
        errors.append("Curtain Wall Hub RefreshAll must fail closed through read-only lookup before clearing unavailable state.")

    save = text.find("private void OnSaveClick")
    save_bind = text.find("ExistingProjectMutationContext.TryGet(_document, out var project)", save)
    save_snapshot = text.find("ProjectStateSnapshot.Capture(project)", save_bind)
    if min(save, save_bind, save_snapshot) < 0 or not save < save_bind < save_snapshot:
        errors.append("Curtain Wall Hub Save must bind existing project before rollback snapshot/mutation.")

    recalc = text.find("private void OnRecalculateClick")
    recalc_bind = text.find("ExistingProjectMutationContext.TryGet(_document, out var project)", recalc)
    recalc_snapshot = text.find("ProjectStateSnapshot.Capture(project)", recalc_bind)
    if min(recalc, recalc_bind, recalc_snapshot) < 0 or not recalc < recalc_bind < recalc_snapshot:
        errors.append("Curtain Wall Hub Recalculate must bind existing project before rollback snapshot/mutation.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Curtain Wall Hub reads without creating project state, mutates only canonical existing state, and preserves rollback/UI isolation.")
