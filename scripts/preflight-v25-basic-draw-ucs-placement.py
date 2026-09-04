#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REL = "src/QS3D.BricsCAD.V25/BasicDrawingCommands.cs"


def fail(message):
    raise SystemExit("FAIL: " + message)


def require(text, needle):
    if needle not in text:
        fail(f"{REL} missing Basic Draw UCS placement contract: {needle}")


def forbid(text, needle):
    if needle in text:
        fail(f"{REL} retains double-transform Basic Draw UCS behavior: {needle}")


def main():
    path = ROOT / REL
    if not path.exists():
        fail(f"missing required source: {REL}")
    source = path.read_text(encoding="utf-8")

    # Editor prompt points are host/world coordinates. Each prompt point must be normalized
    # into the captured prompt UCS before the existing publication TransformBy(promptUcs).
    for needle in (
        "private static Point3d ToPromptUcsPoint(Point3d worldPoint, Matrix3d promptUcs)",
        "worldPoint.TransformBy(promptUcs.Inverse())",
        "var start = ToPromptUcsPoint(startResult.Value, promptUcs);",
        "var end = ToPromptUcsPoint(endResult.Value, promptUcs);",
        "var first = ToPromptUcsPoint(firstResult.Value, promptUcs);",
        "var opposite = ToPromptUcsPoint(oppositeResult.Value, promptUcs);",
        "var center = ToPromptUcsPoint(centerResult.Value, promptUcs);",
        "() => new Line(start, end)",
        "() => new Circle(center, Vector3d.ZAxis, radius)",
        "entity.TransformBy(promptUcs);",
        "RequireFreshContext(document, context, promptUcs",
    ):
        require(source, needle)

    for needle in (
        "() => new Line(startResult.Value, endResult.Value)",
        "var first = firstResult.Value;\n                var opposite = oppositeResult.Value;",
        "() => new Circle(centerResult.Value, Vector3d.ZAxis, radius)",
    ):
        forbid(source, needle)

    print("PASS: V25 Basic Draw normalizes prompt WCS points into the captured UCS exactly once before the existing UCS-to-WCS publication transform, preserving freshness and native ownership boundaries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
