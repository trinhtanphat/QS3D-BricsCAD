#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ENGINE = ROOT / "src" / "QS3D.Core" / "Geometry" / "RoomBoundaryEngine.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomBoundaryKeyCanonicalizationSmoke.cs"
DOC = ROOT / "docs" / "ROOM-BOUNDARY-KEY-CANONICALIZATION.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


engine = read(ENGINE)
smoke = read(SMOKE)
doc = read(DOC)

for token, label in [
    ("private static string CanonicalRotation", "canonical rotation helper"),
    ("while (first < tokens.Count && second < tokens.Count && offset < tokens.Count)", "linear minimal-rotation pass"),
    ("CompareRotationToken", "separator-aware comparator"),
    ("index < left.Length ? left[index] : '|'", "left separator comparison"),
    ("index < right.Length ? right[index] : '|'", "right separator comparison"),
    ("return string.Join(\"|\", ordered);", "single final serialization"),
]:
    require(engine, token, label)

if "for (var start = 0; start < tokens.Count; start++)" in engine:
    errors.append("CanonicalRotation regressed to enumerating every rotation")

rotation_start = engine.find("private static string CanonicalRotation")
quantized_start = engine.find("private static string QuantizedToken")
rotation = engine[rotation_start:quantized_start] if rotation_start >= 0 and quantized_start > rotation_start else ""
if rotation.count('string.Join("|", ordered)') != 1:
    errors.append("CanonicalRotation must serialize exactly one selected rotation")

for token, label in [
    ("LargePolygonBoundaryKeyIsStable", "large polygon key smoke"),
    ("const int count = 2048", "large vertex fixture"),
    ("BoundaryKeyIsOrientationAndStartInvariant", "orientation/start invariance smoke"),
    ("Equal(original[0].Key, reversed[0].Key)", "orientation key assertion"),
]:
    require(smoke, token, label)

for token, label in [
    ("quadratic", "old complexity documentation"),
    ("minimal-rotation", "new algorithm documentation"),
    ("separator", "lexical compatibility documentation"),
    ("LOCAL-010", "local performance boundary"),
]:
    require(doc, token, label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Room boundary key canonicalization selects one deterministic minimal rotation without enumerating every serialized rotation, preserves separator-aware lexical semantics, and retains start/orientation invariance coverage.")
