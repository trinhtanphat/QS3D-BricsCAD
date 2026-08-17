#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadPostCommitUi.cs"
BUILDERS = [
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
        "public static int BuildSelected(Document document, ProjectState project, ElementCategory category)",
        "private static bool UsesLine",
        'CadPostCommitUi.TryRegen(document, "Structural native 3D");',
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs",
        "public static int BuildSelectedLineWalls(\n            Document document,\n            ProjectState project,\n            ElementCategory category,\n            bool allowPostCommitUi = true)",
        "private static SourceBatchKind ValidateSourceBatch",
        'CadPostCommitUi.TryRegen(document, "LINE wall native 3D");',
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs",
        "public static int BuildSelected(\n            Document document,\n            ProjectState project,\n            ElementCategory category,\n            bool allowPostCommitUi = true)",
        "private static void CommitWallPierPathSnapshot",
        'CadPostCommitUi.TryRegen(document, "Polyline wall native 3D");',
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs",
        "public static int BuildSelectedLinePiers(Document document, ProjectState project)",
        "private static void ClearPathProfileSnapshot",
        'CadPostCommitUi.TryRegen(document, "WallPier profile native 3D");',
    ),
]

errors = []

if not HELPER.is_file():
    errors.append("missing " + str(HELPER.relative_to(ROOT)))
else:
    helper = HELPER.read_text(encoding="utf-8")
    for token in (
        "internal static class CadPostCommitUi",
        "public static void TryRegen(Document document, string operation)",
        "document.Editor.Regen();",
        "catch (Exception ex)",
        "viewport regen warning",
        "document.Editor.WriteMessage(",
    ):
        if token not in helper:
            errors.append("post-commit UI helper missing token: " + token)
    if "throw;" in helper or "throw new" in helper:
        errors.append("post-commit UI helper must never rethrow a viewport/warning failure")
    if helper.count("catch") < 2:
        errors.append("post-commit UI helper must isolate both Regen and warning-message failures")

for path, start_token, end_token, regen_call in BUILDERS:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        errors.append(path.name + ": cannot isolate native build method")
        continue
    method = text[start:end]

    ownership = method.rfind("GeneratedGeometryService.CommitReplacement(")
    touch = method.find("project.Touch();", ownership + 1)
    commit = method.find("transaction.Commit();", touch + 1)
    committed = method.find("cadCommitted = true;", commit + 1)
    regen = method.find(regen_call, committed + 1)
    ret = method.rfind("return pending.Count;")

    if min(ownership, touch, commit, committed, regen, ret) < 0:
        errors.append(path.name + ": missing ownership/touch/commit/post-commit UI lifecycle token")
    elif not ownership < touch < commit < committed < regen < ret:
        errors.append(path.name + ": expected ownership -> Touch -> CAD commit -> cadCommitted -> best-effort Regen -> return")

    if "document.Editor.Regen();" in method:
        errors.append(path.name + ": direct viewport Regen must not remain in mutation builder")
    if method.count("project.Touch();") != 1:
        errors.append(path.name + ": native build method must Touch project exactly once inside rollback-capable scope")
    if "ProjectStateSnapshot.Capture(project)" not in method or "rollback.Restore(project)" not in method:
        errors.append(path.name + ": project snapshot rollback contract missing")

print("QS3D native builder post-commit UI preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: native body builders commit ownership and project timestamp before CAD commit; viewport Regen is best-effort post-commit UI and cannot turn a durable model commit into a rollback path.")
