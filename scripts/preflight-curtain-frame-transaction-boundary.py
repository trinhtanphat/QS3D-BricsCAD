#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDERS = [
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
        "public static CurtainFrameBuildResult BuildSelectedLineWalls(Document document, ProjectState project)",
        "private static void CommitSemanticUpdate",
        "Curtain LINE frame",
    ),
    (
        ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs",
        "public static CurtainFrameBuildResult BuildSelectedOpenPolylines(Document document, ProjectState project)",
        "private static void CommitSemanticUpdate",
        "Curtain path frame",
    ),
]

errors = []

for path, start_token, end_token, label in BUILDERS:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        errors.append(path.name + ": cannot isolate curtain frame build method")
        continue

    method = text[start:end]
    semantic = method.find("foreach (var update in pending) CommitSemanticUpdate(project, update);")
    touch = method.find("if (pending.Count > 0) project.Touch();", semantic + 1)
    commit = method.find("transaction.Commit();", touch + 1)
    committed = method.find("cadCommitted = true;", commit + 1)
    restore = method.find("rollback.Restore(project)", committed + 1)
    ret = method.rfind("return new CurtainFrameBuildResult")

    if min(semantic, touch, commit, committed, restore, ret) < 0:
        errors.append(path.name + ": missing semantic/touch/commit/rollback lifecycle token")
    elif not semantic < touch < commit < committed < restore < ret:
        errors.append(path.name + ": expected semantic update -> Touch -> CAD commit -> cadCommitted -> rollback catch -> return")

    if method.count("project.Touch();") != 1:
        errors.append(path.name + ": curtain build method must Touch project exactly once")
    if method.find("project.Touch();", commit + 1) >= 0:
        errors.append(path.name + ": project.Touch must not run after CAD commit")
    if "ProjectStateSnapshot.Capture(project)" not in method:
        errors.append(path.name + ": project snapshot capture missing")
    if "if (!cadCommitted)" not in method:
        errors.append(path.name + ": rollback must remain guarded by CAD commit state")

print("QS3D curtain frame transaction-boundary preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain LINE/path semantic metadata and project timestamp commit inside the rollback-capable phase before native CAD commit.")