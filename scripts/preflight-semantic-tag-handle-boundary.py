#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RENDERER = ROOT / "src/QS3D.Core/Documentation/SemanticTagRenderer.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs"

errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing semantic tag handle-boundary file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


renderer = read(RENDERER)
smoke = read(SMOKE)

for token in (
    'GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)',
    'key.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)',
    'key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
    'key.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0',
):
    if token not in renderer:
        errors.append("SemanticTagRenderer missing native-handle boundary token: " + token)

for token in (
    "OptionalPropertyAndQuantityRender",
    'fixture.Element.SetProperty("Mark", "B-12")',
    "NativeHandleMetadataCannotLeakIntoTag",
    'fixture.Element.Properties["CadHandle"] = "ABCD"',
    'fixture.Element.Properties["SourceHandleRef"] = "EF12"',
    '"{P:cAdHaNdLe}"',
    '"{P:SOURCEHANDLEREF}"',
):
    if token not in smoke:
        errors.append("SemanticTagRenderer smoke missing native-handle regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic P: tags preserve ordinary properties while rejecting generated/native and arbitrary handle-bearing ProjectElement metadata case-insensitively.")
