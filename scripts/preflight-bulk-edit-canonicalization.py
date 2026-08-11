#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkEditCanonicalizationSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SERVICE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing bulk-edit canonicalization file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    if text.count("var key = SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName);") < 2:
        errors.append("BulkEditService must canonicalize and validate propertyName through the shared edit policy in both set and multiply paths")
    for token in (
        "element.Properties.TryGetValue(key, out var before)",
        "element.Properties.TryGetValue(key, out var text)",
        "update.Element.Properties[key] = update.Value;",
        "update.Element.MarkDirty(DirtyFlags(update.Element, key));",
        'ProjectSemanticMutationExecutor.Execute(project, "bulk.set-property"',
        'ProjectSemanticMutationExecutor.Execute(project, "bulk.multiply-numeric-property"',
    ):
        if token not in text:
            errors.append("BulkEditService.cs missing canonical key token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SetPropertyUsesCanonicalKeyAndGeometryDirtyPolicy();",
        "MultiplyNumericPropertyUsesCanonicalKey();",
        '" WidthM "',
        "wall.Properties.Keys.Any(key => key != key.Trim())",
        "ElementDirtyFlags.Geometry",
    ):
        if token not in text:
            errors.append("BulkEditCanonicalizationSmoke.cs missing regression token: " + token)

if REG.is_file() and "BulkEditCanonicalizationSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("bulk-edit canonicalization smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] bulk property edit/multiply paths are statically guarded to use the shared canonical property policy and preserve geometry-dirty policy")
