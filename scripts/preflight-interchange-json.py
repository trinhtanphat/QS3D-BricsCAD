#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


core = read("src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs")
adapter = read("src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs")
smoke = read("tests/QS3D.Core.SmokeTests/ProjectInterchangeJsonSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")

for token in [
    'FormatName = "QS3D.SemanticSnapshot"',
    "FormatVersion = 1",
    '\"length\":\"m\"',
    '\"area\":\"m2\"',
    '\"volume\":\"m3\"',
    '\"mass\":\"kg\"',
    'Property(json, 3, "id", element.Id',
    'Property(json, 3, "category", element.Category.ToString()',
    'Property(json, 3, "familyId", element.FamilyId',
    'Property(json, 3, "floorId", element.FloorId',
    'Property(json, 3, "zoneId", element.ZoneId',
    '"sourceRefScope", "drawing-local"',
    'AppendStringArray(json, element.DependsOn)',
    'AppendNumberMap(json, element.Quantities)',
    "GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)",
    'normalized.StartsWith("Generated"',
    'normalized.StartsWith("QS3D.Generated"',
    'normalized.StartsWith("PhysicalOpeningCut"',
    "OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)",
    "File.Replace(tempPath, fullPath, backupPath, true)",
]:
    require(core, token, "interchange core contract")

for token in [
    '[CommandMethod("QS3DINTERCHANGEJSON"',
    "RegenerateDirty(project)",
    "ProjectInterchangeJsonExporter.Export(dialog.FileName, project)",
    "read-only semantic interchange",
    "không chứa generated CAD ownership handles",
]:
    require(adapter, token, "BricsCAD interchange command")

for token in [
    "SnapshotIsDeterministicAndUsesStableIds",
    "GeneratedOwnershipIsExcluded",
    "NumericContractFailsClosed",
]:
    require(smoke, token, "interchange smoke")
require(registration, "ProjectInterchangeJsonSmoke.Run();", "smoke registration")

for forbidden in [
    "QS3DINTERCHANGEIMPORT",
    "IFC",
    "Revit",
    "GeneratedSolidHandle\"",
    "GeneratedRebarHandles\"",
]:
    if forbidden in adapter:
        print(f"[FAIL] adapter overclaims or exposes unsafe round-trip contract: {forbidden}")
        sys.exit(1)

print("[PASS] semantic JSON interchange is stable-ID/SI/read-only, smoke-covered and excludes generated CAD ownership handles")
