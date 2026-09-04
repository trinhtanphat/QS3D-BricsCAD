#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMaterialCatalog.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
upsert_start = text.index("public static ProjectMaterial UpsertCustom")
upsert_end = text.index("public static bool DeleteCustom", upsert_start)
upsert = text[upsert_start:upsert_end]

admit_call = "RequireRenameRevisionCapacity(project, referenceScope!, previousName!, material.Name);"
write_call = "WriteCustom(project, custom);"
rename_call = "RenameReferences(referenceScope!, previousName!, material.Name);"
helper = "private static void RequireRenameRevisionCapacity("
checked_budget = "checked(project.ChangeVersion + requiredProjectTouches)"
material_probe = 'properties.TryGetValue("Material", out var material)'
frame_probe = 'properties.TryGetValue("CurtainFrameMaterial", out var frame)'

if admit_call not in upsert:
    fail("material rename must pre-admit the complete project revision budget")
if write_call not in upsert or rename_call not in upsert:
    fail("material rename catalog-write/reference-propagation contract is missing")
if not (upsert.index(admit_call) < upsert.index(write_call) < upsert.index(rename_call)):
    fail("revision admission must occur before the first catalog mutation and before reference propagation")
if helper not in text:
    fail("material rename must expose a dedicated fail-closed revision-capacity helper")
if checked_budget not in text:
    fail("revision admission must use checked arithmetic against ProjectState.ChangeVersion")
if material_probe not in text or frame_probe not in text:
    fail("revision admission must account for both family material-reference properties")

print("PASS: material rename admits its complete project revision budget before catalog/reference mutation")
