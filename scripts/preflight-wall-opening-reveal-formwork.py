#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "QuantityGeometryExplanationService.cs"


def fail(message: str) -> None:
    print("ERROR: wall opening reveal formwork preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")

required_tokens = {
    "face seed keeps outer-horizontal identity": "public bool IsOuterHorizontal;",
    "face result uses outer-horizontal identity": "IncludeFormworkFace(targetCategory, seed.Type, seed.IsOuterHorizontal)",
    "wall bounds are inspected": "if (category == ElementCategory.StructuralWall)",
    "wall min bound is captured": "wallMinZ = ext.MinPoint.Z;",
    "wall max bound is captured": "wallMaxZ = ext.MaxPoint.Z;",
    "opening-safe fail-open path exists": "outer horizontal faces remain included to preserve opening reveals",
    "outer horizontal classifier is called": "IsOuterHorizontal = IsOuterHorizontalFace(category, plane, wallBoundsAvailable, wallMinZ, wallMaxZ)",
}
for label, token in required_tokens.items():
    if token not in text:
        fail(label + " is missing")

include = re.search(
    r"private static bool IncludeFormworkFace\(ElementCategory category, string faceType, bool isOuterHorizontal\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static bool IsOuterHorizontalFace",
    text,
    re.DOTALL,
)
if include is None:
    fail("IncludeFormworkFace policy method was not found")
include_body = include.group("body")
if 'category == ElementCategory.Foundation' not in include_body or 'string.Equals(faceType, "Side", StringComparison.Ordinal)' not in include_body:
    fail("Foundation side-only policy must remain intact")
if 'category == ElementCategory.StructuralWall' not in include_body or 'return !isOuterHorizontal;' not in include_body:
    fail("StructuralWall must exclude only faces explicitly identified as outer-horizontal")
if 'string.Equals(faceType, "Top"' in include_body or 'string.Equals(faceType, "Bottom"' in include_body:
    fail("StructuralWall must not blanket-drop Top/Bottom face types because opening head/sill reveals are horizontal")

outer = re.search(
    r"private static bool IsOuterHorizontalFace\((?P<signature>.*?)\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static int DominantHorizontalAxis",
    text,
    re.DOTALL,
)
if outer is None:
    fail("IsOuterHorizontalFace classifier was not found")
outer_body = outer.group("body")
outer_required = {
    "classifier is StructuralWall-only": "category != ElementCategory.StructuralWall",
    "classifier fails open without trustworthy bounds": "!wallBoundsAvailable",
    "classifier rejects vertical faces": "Math.Abs(normal.Z) < 0.70710678118d",
    "classifier compares minimum elevation": "Math.Abs(z - wallMinZ) <= toleranceCad",
    "classifier compares maximum elevation": "Math.Abs(z - wallMaxZ) <= toleranceCad",
}
for label, token in outer_required.items():
    if token not in outer_body:
        fail(label + " is missing")

# FaceType intentionally remains orientation-only. Internal horizontal opening faces can
# therefore still display as Top/Bottom, but the independent outer-plane bit decides whether
# they contribute to formwork. This guards against the unsafe shortcut `Top/Bottom => skip`.
face_type = re.search(
    r"private static string FaceType\(PlanarEntity\? plane, int endAxis\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static Solid3d\? TryIntersection",
    text,
    re.DOTALL,
)
if face_type is None:
    fail("FaceType method was not found")
if 'return "Bottom";' not in face_type.group("body") or 'return "Top";' not in face_type.group("body"):
    fail("orientation labels must remain available independently of formwork eligibility")

print("PASS: StructuralWall exact formwork drops only exterior top/bottom planes and keeps horizontal opening reveals")
