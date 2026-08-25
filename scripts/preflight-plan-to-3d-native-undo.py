#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")

    method_marker = "private static void ConvertPlanWalls(string operation, bool promptStyle)"
    next_marker = "private static IReadOnlyList<ObjectId>? AcquireSelection"
    start = text.find(method_marker)
    if start < 0:
        fail("ConvertPlanWalls implementation not found")
    end = text.find(next_marker, start)
    if end < 0:
        fail("ConvertPlanWalls boundary not found")
    body = text[start:end]

    undo = "document.Database.StartUndoRecord();"
    if body.count(undo) != 1:
        fail("ConvertPlanWalls must establish exactly one native Database.StartUndoRecord() boundary")

    undo_pos = body.index(undo)
    mutation_tokens = (
        "projectPreview.ResolveForMutation(document, operation)",
        "SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall)",
        "element.Properties[\"ThicknessM\"] =",
        "regenerator.RegenerateDirtySubset",
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "project.Touch();",
    )
    for token in mutation_tokens:
        pos = body.find(token)
        if pos < 0:
            fail(f"expected Plan-to-3D mutation token missing: {token}")
        if undo_pos >= pos:
            fail(f"native undo record starts too late; mutation can precede it: {token}")

    # Prompting and final read-only source revalidation intentionally stay outside the
    # native mutation unit. If this ordering drifts, a cancelled prompt could create
    # an empty undo step or source mutation could escape the command-level boundary.
    final_preflight = "RequireSameSources(sources, refreshedSources);"
    preflight_pos = body.find(final_preflight)
    if preflight_pos < 0 or preflight_pos >= undo_pos:
        fail("native undo boundary must start after final prompt/source revalidation")

    print("PASS: Plan-to-3D native undo boundary encloses semantic and CAD mutations")


if __name__ == "__main__":
    main()
