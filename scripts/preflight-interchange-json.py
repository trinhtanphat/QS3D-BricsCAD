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


def require_before(text: str, first: str, second: str, label: str) -> None:
    require(text, first, label)
    require(text, second, label)
    if text.index(first) >= text.index(second):
        print(f"[FAIL] {label}: expected {first} before {second}")
        sys.exit(1)


core = read("src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs")
adapter = read("src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs")
snapshot = read("src/QS3D.Core/Persistence/ProjectStateSnapshot.cs")
smoke = read("tests/QS3D.Core.SmokeTests/ProjectInterchangeJsonSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")

for token in [
    'FormatName = "QS3D.SemanticSnapshot"',
    "FormatVersion = 1",
    '\\"length\\":\\"m\\"',
    '\\"area\\":\\"m2\\"',
    '\\"volume\\":\\"m3\\"',
    '\\"mass\\":\\"kg\\"',
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
    "public static ProjectState CreateDetachedCopy(ProjectState project)",
    "return Clone(project);",
]:
    require(snapshot, token, "detached project snapshot contract")

for token in [
    '[CommandMethod("QS3DINTERCHANGEJSON"',
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "RegenerateDirty(snapshot)",
    "ProjectInterchangeJsonExporter.Export(dialog.FileName, snapshot)",
    "read-only semantic interchange",
    "không mutate project live",
    "không chứa generated CAD ownership handles",
]:
    require(adapter, token, "BricsCAD interchange command")

require_before(
    adapter,
    "if (dialog.ShowDialog() != true) return;",
    "ProjectContextCoordinator.GetOrCreate(document)",
    "cancel must be zero-mutation",
)

for forbidden in [
    "RegenerateDirty(project)",
    "ProjectInterchangeJsonExporter.Export(dialog.FileName, project)",
    "QS3DINTERCHANGEIMPORT",
    "IFC",
    "Revit",
    "GeneratedSolidHandle\"",
    "GeneratedRebarHandles\"",
]:
    if forbidden in adapter:
        print(f"[FAIL] adapter mutates live state, overclaims, or exposes unsafe round-trip contract: {forbidden}")
        sys.exit(1)

for token in [
    "SnapshotIsDeterministicAndUsesStableIds",
    "GeneratedOwnershipIsExcluded",
    "NumericContractFailsClosed",
    "DetachedCopyDoesNotMutateLiveProject",
    "ProjectStateSnapshot.CreateDetachedCopy(live)",
]:
    require(smoke, token, "interchange smoke")
require(registration, "ProjectInterchangeJsonSmoke.Run();", "smoke registration")

print("[PASS] semantic JSON interchange is stable-ID/SI/read-only, detached from live state, smoke-covered and excludes generated CAD ownership handles")
