#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HEALTH = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantitySettingsDiagnosticCommands.cs"
EXPORT = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantitySettingsDiagnosticExportCommands.cs"
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "QuantitySettingsStore.cs"


def main():
    health = HEALTH.read_text(encoding="utf-8")
    export = EXPORT.read_text(encoding="utf-8")
    store = STORE.read_text(encoding="utf-8")

    if "Cannot read quantity settings template '" not in store or '" + path + "' not in store:
        print("ERROR: threat-source assertion changed; review QuantitySettingsStore error semantics before weakening this guard.")
        return 1

    required = {
        "health": [
            '[CommandMethod("QS3DQSETTINGSHEALTH", CommandFlags.Modal)]',
            "new QuantitySettingsStore().Load();",
            "QuantityCalculationMatrixDiagnostics.Analyze(settings);",
            "catch (System.Exception)",
            "QS3DQSETTINGSHEALTH lỗi: không thể đọc hoặc phân tích Quantity Settings.",
        ],
        "export": [
            '[CommandMethod("QS3DQSETTINGSHEALTHEXPORT", CommandFlags.Modal)]',
            "new QuantitySettingsStore().Load();",
            "QuantityCalculationMatrixDiagnosticSnapshot.Create(settings);",
            "QuantityCalculationMatrixDiagnosticSnapshotExporter.Save(dialog.FileName, snapshot);",
            "Path.GetFileName(dialog.FileName)",
            "catch (System.Exception)",
            "QS3DQSETTINGSHEALTHEXPORT lỗi: không thể tạo hoặc ghi báo cáo Quantity Settings Health.",
        ],
    }

    for label, tokens in required.items():
        text = health if label == "health" else export
        missing = [token for token in tokens if token not in text]
        if missing:
            print("ERROR: " + label + " diagnostics redaction contract is incomplete:")
            for token in missing:
                print(" -", token)
            return 1

    forbidden = [
        "ex.Message",
        "exception.Message",
        ".StackTrace",
        ".ToString()",
        "SettingsPath",
        "Path.GetFullPath",
        "Environment.GetFolderPath",
    ]
    for label, text in (("health", health), ("export", export)):
        present = [token for token in forbidden if token in text]
        if present:
            print("ERROR: " + label + " diagnostics can expose path-bearing exception or machine identity data:")
            for token in present:
                print(" -", token)
            return 1

    if "Path.GetFileName(dialog.FileName)" not in export:
        print("ERROR: successful export output must remain basename-only.")
        return 1
    if '" + dialog.FileName' in export:
        print("ERROR: successful export output must not echo the selected full path.")
        return 1

    print("PASS: Quantity Settings health commands redact path-bearing failures and expose only bounded diagnostics plus basename-only export success output.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
