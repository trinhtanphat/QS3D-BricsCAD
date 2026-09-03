#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/FloorZoneNameInvariantSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing floor/zone name invariant contract file: " + str(path.relative_to(ROOT)))


def class_block(text, class_name, next_class_name):
    start = text.find("public sealed class " + class_name)
    if start < 0:
        return ""
    end = text.find("public sealed class " + next_class_name, start + 1)
    return text[start:] if end < 0 else text[start:end]


def has_validated_name_setter(block):
    if "set => _name = Require(value, nameof(value));" in block:
        return True

    mutation_aware_setter = re.compile(
        r"public\s+string\s+Name\s*\{\s*"
        r"get\s*=>\s*_name;\s*"
        r"set\s*\{\s*"
        r"var\s+next\s*=\s*Require\(value,\s*nameof\(value\)\);"
        r".*?"
        r"_name\s*=\s*next;\s*"
        r"\}\s*\}",
        re.DOTALL,
    )
    return mutation_aware_setter.search(block) is not None


if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    if text.count("private string _name;") < 3:
        errors.append("ZoneDefinition and FloorDefinition must keep validated name backing fields alongside ProjectFamily.")

    zone_block = class_block(text, "ZoneDefinition", "FloorDefinition")
    floor_block = class_block(text, "FloorDefinition", "ProjectFamily")
    if not zone_block or not floor_block or not has_validated_name_setter(zone_block) or not has_validated_name_setter(floor_block):
        errors.append("ZoneDefinition and FloorDefinition setters must validate and canonicalize names.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in ('floor.Name = " Floor renamed ";', 'zone.Name = " Zone renamed ";', 'floor.Name = "   "', 'zone.Name = "\\t"'):
        if token not in text:
            errors.append("FloorZoneNameInvariantSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: FloorDefinition and ZoneDefinition names remain trimmed and non-blank through public setters.")
