#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeKeepTargetImporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeKeepTargetImporterSmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing KeepTarget resolution-index file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)

index_start = source.find("private sealed class ResolutionActionIndex")
index_end = source.find("public const string ImportMode", index_start)
index = source[index_start:index_end] if index_start >= 0 and index_end > index_start else ""

import_start = source.find("public static ProjectInterchangeKeepTargetImportResult Import(")
prepare_start = source.find("private static PreparedImport Prepare(", import_start)
import_method = source[import_start:prepare_start] if import_start >= 0 and prepare_start > import_start else ""

for token in (
    "Dictionary<InterchangeIdentityKind, Dictionary<string, InterchangeImportResolutionAction>>",
    "foreach (var item in plan.Items)",
    "new Dictionary<string, InterchangeImportResolutionAction>(StringComparer.OrdinalIgnoreCase)",
    "if (actionsById.ContainsKey(item.Id))",
    'throw new InvalidOperationException("Sequence contains more than one matching element")',
    "!actionsById.TryGetValue(id ?? string.Empty, out var action)",
    'throw new InvalidOperationException("Sequence contains no matching element")',
    "InterchangeImportResolutionAction.AddSourceSemanticData",
    "InterchangeImportResolutionAction.KeepTarget",
    "KeepTarget interchange mutation reached a non-executable resolution for",
):
    if token not in index:
        errors.append("KeepTarget resolution index missing contract token: " + token)

for token in (
    "var resolutionActions = new ResolutionActionIndex(prepared.Resolution);",
    "resolutionActions.ShouldAdd(InterchangeIdentityKind.Zone, zone.Id)",
    "resolutionActions.ShouldAdd(InterchangeIdentityKind.Floor, floor.Id)",
    "resolutionActions.ShouldAdd(InterchangeIdentityKind.Family, familySnapshot.Id)",
    "resolutionActions.ShouldAdd(InterchangeIdentityKind.Element, elementSnapshot.Id)",
):
    if token not in import_method:
        errors.append("KeepTarget import missing indexed mutation-selection token: " + token)

if import_method.count("resolutionActions.ShouldAdd(") != 4:
    errors.append("KeepTarget import must use exactly four indexed Add/Keep lookups")

if ".Items.Single(" in source or "plan.Items.Single(" in source:
    errors.append("KeepTarget importer restored a per-identity full-plan Single scan")

if import_method.find("new ResolutionActionIndex(prepared.Resolution)") > import_method.find("ProjectStateSnapshot.Capture(target)"):
    errors.append("KeepTarget resolution index must be complete before the mutation rollback snapshot")

for token in (
    "CaseInsensitiveCollisionsKeepTargetAndAddDistinctItems",
    "LowercaseIdentityTargetProject",
    'Equal(4, result.TargetIdentitiesKept);',
    'Equal("z1", target.ActiveZoneId);',
    'Equal("f1", target.ActiveFloorId);',
    'Equal("fam1", target.Metadata["ActiveFamilyId"]);',
    'True(target.FindElement("E2") != null);',
):
    if token not in smoke:
        errors.append("KeepTarget smoke missing mixed-case Add/Keep regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: KeepTarget import indexes validated resolution actions once, preserves case-insensitive Add/Keep behavior, and performs exactly four indexed mutation-loop lookup families without per-item plan scans.")
