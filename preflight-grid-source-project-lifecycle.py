#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"

errors = []


def read(relative):
    path = ADAPTER / relative
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


def forbid(text, token, label):
    if token in text:
        errors.append(label + " forbidden token: " + token)


def require_order(text, first, second, label):
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        errors.append(label + " must preserve order: " + first + " -> " + second)


auto = read("GridAutoNumberCommands.cs")
require(auto, 'ExistingProjectMutationContext.Require(document, "Grid Auto Number")', "QS3DGRIDNUMBERAUTO")
forbid(auto, "ProjectContextCoordinator.GetOrCreate(document)", "QS3DGRIDNUMBERAUTO")
require_order(auto, "EntitySnapshotReader.ReadCurrentSelection(document)", 'ExistingProjectMutationContext.Require(document, "Grid Auto Number")', "QS3DGRIDNUMBERAUTO")
require_order(auto, "if (selected.Count == 0) return;", 'ExistingProjectMutationContext.Require(document, "Grid Auto Number")', "QS3DGRIDNUMBERAUTO empty selection")

intersections = read("GridIntersectionCommands.cs")
require(intersections, "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", "QS3DGRIDINTERSECTIONS")
forbid(intersections, "ProjectContextCoordinator.GetOrCreate(document)", "QS3DGRIDINTERSECTIONS")
forbid(intersections, "ExistingProjectMutationContext", "QS3DGRIDINTERSECTIONS")
require_order(intersections, "EntitySnapshotReader.ReadCurrentSelection(document)", "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", "QS3DGRIDINTERSECTIONS")
require_order(intersections, "if (selected.Count == 0) return;", "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", "QS3DGRIDINTERSECTIONS empty selection")

reconcile = read("Services/SourceReconcileService.cs")
require(reconcile, 'ExistingProjectMutationContext.Require(document, "Source Reconcile")', "QS3DSYNCSOURCE")
forbid(reconcile, "ProjectContextCoordinator.GetOrCreate(document)", "QS3DSYNCSOURCE")
require_order(reconcile, "if (snapshots.Count == 0) return new SourceReconcileResult();", 'ExistingProjectMutationContext.Require(document, "Source Reconcile")', "QS3DSYNCSOURCE empty selection")
require_order(reconcile, 'ExistingProjectMutationContext.Require(document, "Source Reconcile")', "ProjectStateSnapshot.Capture(project)", "QS3DSYNCSOURCE canonical mutation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Grid auto-number and Source Reconcile require canonical existing project state; Grid intersections stay read-only; empty selection remains side-effect free.")
