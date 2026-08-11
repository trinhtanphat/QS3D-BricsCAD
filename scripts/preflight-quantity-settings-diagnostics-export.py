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
        "var temp = Path.Combine(",
        "Guid.NewGuid().ToString(\"N\") + \".tmp\"",
        "File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)",
        "Write(stream, snapshot);",
        "stream.Flush(true);",
        "File.Replace(temp, fullPath, null, true);",
        "File.Move(temp, fullPath);",
        "if (File.Exists(temp)) File.Delete(temp);",
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
        "SavePublishesAndReplacesPortableJson();",
        "SnapshotCreationDoesNotMutateCaller();",
        "Sequence(snapshot.ObservedCategoryCodes, 1301, 1302);",
        "Pair(snapshot.MissingDirectedPairs[1], 1301, 1302);",
        'Contains(firstJson, "\\\"observedCategoryCodes\\\":[10]");',
        'Contains(secondJson, "\\\"observedCategoryCodes\\\":[20]");',
        "Equal(1, Directory.GetFiles(directory).Length);",
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

    save_start = core.find("public static void Save(string path, QuantityCalculationMatrixDiagnosticSnapshot snapshot)")
    write_start = core.find("public static void Write(Stream stream, QuantityCalculationMatrixDiagnosticSnapshot snapshot)", save_start)
    if save_start < 0 or write_start <= save_start:
        print("ERROR: cannot isolate diagnostic snapshot Save/Write methods.")
        return 1
    save_method = core[save_start:write_start]

    temp_at = save_method.find("var temp = Path.Combine(")
    create_at = save_method.find("File.Open(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)", temp_at)
    write_at = save_method.find("Write(stream, snapshot);", create_at)
    flush_at = save_method.find("stream.Flush(true);", write_at)
    exists_at = save_method.find("if (File.Exists(fullPath))", flush_at)
    replace_at = save_method.find("File.Replace(temp, fullPath, null, true);", exists_at)
    move_at = save_method.find("File.Move(temp, fullPath);", replace_at)
    finally_at = save_method.find("finally", move_at)
    cleanup_at = save_method.find("if (File.Exists(temp)) File.Delete(temp);", finally_at)
    if not (0 <= temp_at < create_at < write_at < flush_at < exists_at < replace_at < move_at < finally_at < cleanup_at):
        print("ERROR: diagnostic Save must serialize/flush a same-directory temp before replace/move and always clean temp in finally.")
        return 1
    if "File.Open(fullPath, FileMode.Create" in save_method:
        print("ERROR: diagnostic Save must not truncate/open the destination before successful temp serialization.")
        return 1

    load_at = command.find("new QuantitySettingsStore().Load();")
    snapshot_at = command.find("QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);")
    dialog_at = command.find("new SaveFileDialog")
    command_save_at = command.find("QuantityCalculationMatrixDiagnosticSnapshotExporter.Save(dialog.FileName, snapshot);")
    if not (0 <= load_at < snapshot_at < dialog_at < command_save_at):
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

    print("PASS: QS3DQSETTINGSHEALTHEXPORT writes only a sanitized portable matrix snapshot, publishes it atomically through a same-directory temp, and preserves settings/project/drawing read-only boundaries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())