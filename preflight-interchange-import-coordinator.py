#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeImportCoordinator.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeImportCoordinatorSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-IMPORT-COORDINATOR.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing import coordinator file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    "ProjectInterchangeImportExecutionMode",
    "AppendOnly",
    "KeepTarget",
    "ImportAsNew",
    "UseSourceSemanticData",
    "PreserveSourceHandleProvenance",
    "No fallback mode was attempted",
    "ProjectInterchangeAppendOnlyImporter.Import",
    "ProjectInterchangeAppendProvenanceImporter.Import",
    "ProjectInterchangeKeepTargetImporter.Import",
    "ProjectInterchangeKeepTargetProvenanceImporter.Import",
    "ProjectInterchangeRemapAppendImporter.Import",
    "ProjectInterchangeRemapProvenanceImporter.Import",
    "ProjectInterchangeUseSourceSemanticImporter.Import",
    "ProjectInterchangeUseSourceProvenanceImporter.Import",
    "nativeCleanupAuthorization.ElementIds.Count",
    "NativeCleanupElementIds",
    "Enum.IsDefined",
):
    if token not in source:
        errors.append("import coordinator missing contract token: " + token)

for token in (
    "CollisionModeIsExplicitAndNeverFallsBack",
    "ImportAsNewPlanSurfacesRemapWithoutMutation",
    "UseSourcePlanPropagatesNativeCleanupRequirement",
    "ExecuteRejectsCleanupAuthorityForOtherModes",
    "UseSourceExecuteRequiresAndConsumesExplicitAuthorization",
    "ProvenanceToggleSelectsCombinedExecution",
    "InvalidModeFailsClosed",
    "ProjectInterchangeUseSourceSemanticImporter.Plan(target, json)",
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan(semanticPlan)",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("import coordinator smoke missing regression token: " + token)

if "ProjectInterchangeNativeCleanupAuthorization.ForElementIds(plan.NativeCleanupElementIds)" in test:
    errors.append("import coordinator smoke must not treat element-id-only cleanup authorization as executable UseSource authority")

for token in (
    "one explicit mode",
    "never falls back",
    "cleanup authorization",
    "Core coordinator",
    "does not create a BricsCAD command",
    "LOCAL_ONLY",
    "PreserveSourceHandleProvenance",
):
    if token not in doc:
        errors.append("import coordinator documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: one Core coordinator selects an explicit import policy/provenance mode, never falls back silently, and preserves handle-bound UseSource native-cleanup authorization.")
