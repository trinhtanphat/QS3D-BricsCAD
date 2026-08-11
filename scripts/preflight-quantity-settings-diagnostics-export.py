#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityCalculationMatrixDiagnosticSnapshot.cs"
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantitySettingsDiagnosticExportCommands.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationMatrixDiagnosticSnapshotSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityCalculationMatrixDiagnosticSnapshotSmokeRegistration.cs"


def require(text, tokens, label):
    return [label + ": " + token for token in tokens if token not in text]


def main():
    core = CORE.read_text(encoding="utf-8")
    command = COMMAND.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    missing = []
    missing += require(core, [
        "public sealed class QuantityCalculationMatrixDiagnosticSnapshot",
        "QuantityCalculationMatrixDiagnostics.Analyze(snapshot)",
        'DataMember(Name = "schemaVersion"',
        'DataMember(Name = "observedCategoryCodes"',
        'DataMember(Name = "intersectionOnlyCategoryCodes"',
        'DataMember(Name = "unreferencedCategoryRuleCodes"',
        'DataMember(Name = "existingDirectedRuleCount"',
        'DataMember(Name = "expectedDirectedRuleCount"',
        'DataMember(Name = "isCompleteDirectedMatrix"',
        'DataMember(Name = "missingDirectedPairs"',
        'DataMember(Name = "sourceCode"',
        'DataMember(Name = "targetCode"',
        "new ReadOnlyCollection<int>",
        "new ReadOnlyCollection<QuantityCalculationMatrixDiagnosticPairSnapshot>",
        "public static class QuantityCalculationMatrixDiagnosticSnapshotExporter",
        "new DataContractJsonSerializer(typeof(QuantityCalculationMatrixDiagnosticSnapshot))",
    ], "core snapshot")
    missing += require(command, [
        '[CommandMethod("QS3DQSETTINGSHEALTHEXPORT", CommandFlags.Modal)]',
        "new QuantitySettingsStore().Load();",
        "QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);",
        "new SaveFileDialog",
        "if (dialog.ShowDialog() != true) return;",
        "QuantityCalculationMatrixDiagnosticSnapshotExporter.Save(dialog.FileName, snapshot);",
        "Path.GetFileName(dialog.FileName)",
    ], "command")
    missing += require(smoke, [
        "SnapshotPreservesExactDirectedDiagnostics();",
        "JsonExportIsPortableAndSanitized();",
        "SnapshotCreationDoesNotMutateCaller();",
        "Sequence(snapshot.ObservedCategoryCodes, 1301, 1302);",
        "Pair(snapshot.MissingDirectedPairs[1], 1301, 1302);",
        'NotContains(json, "SettingsPath");',
        'NotContains(json, "ProjectId");',
        'NotContains(json, "Handle");',
    ], "smoke")
    missing += require(registration, [
        "[ModuleInitializer]",
        "QuantityCalculationMatrixDiagnosticSnapshotSmoke.Run();",
    ], "registration")

    if missing:
        print("ERROR: quantity settings health export contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    load_at = command.find("new QuantitySettingsStore().Load();")
    snapshot_at = command.find("QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);")
    dialog_at = command.find("new SaveFileDialog")
    save_at = command.find("QuantityCalculationMatrixDiagnosticSnapshotExporter.Save(dialog.FileName, snapshot);")
    if not (0 <= load_at < snapshot_at < dialog_at < save_at):
        print("ERROR: export command must preserve Load -> snapshot -> dialog -> selected-file write ordering.")
        return 1

    forbidden_core = [
        "SettingsPath",
        "ProjectId",
        "DrawingFingerprint",
        "DrawingPath",
        "GeneratedUtc",
        "DateTime",
        "Environment.UserName",
        "Handle",
        "ElementCategory",
        "ProjectState",
        "AuditTrail",
        "Solid3d",
        "Brep",
    ]
    present_core = [token for token in forbidden_core if token in core]
    if present_core:
        print("ERROR: portable snapshot contains sensitive identity, inferred mapping, project or CAD surfaces:")
        for item in present_core:
            print(" -", item)
        return 1

    forbidden_command = [
        "SettingsPath",
        ".Export(",
        ".Import(",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "GetOrCreate",
        "ProjectState",
        "AuditTrail",
        "LockDocument",
        "StartTransaction",
        "StartOpenCloseTransaction",
        "BooleanOperation",
        "Solid3d",
        "Brep",
        "Process.Start",
    ]
    present_command = [token for token in forbidden_command if token in command]
    if present_command:
        print("ERROR: health export crossed a settings/project/drawing mutation or privacy boundary:")
        for item in present_command:
            print(" -", item)
        return 1

    if "QuantitySettingsStore().Save" in command or "QuantitySettingsStore().Export" in command:
        print("ERROR: health export must never write through the machine settings store contract.")
        return 1

    print("PASS: QS3DQSETTINGSHEALTHEXPORT writes only a sanitized portable matrix snapshot to the user-selected JSON path and preserves settings/project/drawing read-only boundaries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
