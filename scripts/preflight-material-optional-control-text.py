#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMaterialOptionalTextSmoke.cs"
registration_path = ROOT / "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogRegistration.cs"
errors = []

for path in (source_path, smoke_path, registration_path):
    if not path.is_file():
        errors.append("missing required material optional-text integrity file: " + str(path.relative_to(ROOT)))

if source_path.is_file():
    source = source_path.read_text(encoding="utf-8")
    optional_start = source.find("private static string Optional")
    unicode_start = source.find("private static void RequireWellFormedUnicode", optional_start)
    optional = source[optional_start:unicode_start] if optional_start >= 0 and unicode_start > optional_start else ""
    required = [
        "var raw = value ?? string.Empty;",
        "raw.Any(char.IsControl)",
        "var text = raw.Trim();",
        "RequireWellFormedUnicode(text, name);",
        "RequireXmlText(text, name);",
    ]
    for token in required:
        if token not in optional:
            errors.append("ProjectMaterial.Optional missing fail-closed token: " + token)
    if optional:
        control = optional.find("raw.Any(char.IsControl)")
        trim = optional.find("raw.Trim()")
        if control < 0 or trim < 0 or control > trim:
            errors.append("ProjectMaterial.Optional must reject controls before Trim normalization")

if smoke_path.is_file():
    smoke = smoke_path.read_text(encoding="utf-8")
    for token in [
        "RejectsDirectConstructorControls",
        "RejectsUpsertControlsBeforeMutation",
        "RejectsControlBearingPersistedRecords",
        "PreservesCanonicalOptionalTextRoundTrip",
        '"kg\\tbar"',
        '"line1\\nline2"',
        '"line1\\rline2"',
        "Finish 😀 supplementary",
        "ProjectMaterialCatalog.MetadataKey",
    ]:
        if token not in smoke:
            errors.append("material optional-text smoke missing regression token: " + token)

if registration_path.is_file():
    registration = registration_path.read_text(encoding="utf-8")
    if "ProjectMaterialOptionalTextSmoke.Run();" not in registration:
        errors.append("material optional-text smoke is not registered in deterministic Core suite")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: optional persisted material Unit/Description reject controls before trim while deterministic constructor/upsert/read/round-trip coverage remains registered.")
