#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantitySettingsDiagnosticCommands.cs"


def main():
    text = CODE.read_text(encoding="utf-8")
    required = [
        '[CommandMethod("QS3DQSETTINGSHEALTH", CommandFlags.Modal)]',
        "new QuantitySettingsStore().Load();",
        "QuantityCalculationMatrixDiagnostics.Analyze(settings);",
        "diagnostics.ExistingDirectedRuleCount",
        "diagnostics.ExpectedDirectedRuleCount",
        "diagnostics.MissingDirectedPairs.Count",
        "diagnostics.IntersectionOnlyCategoryCodes",
        "diagnostics.UnreferencedCategoryRuleCodes",
        "diagnostics.MissingDirectedPairs.Take(DetailLimit)",
        "private const int DetailLimit = 20;",
        'WriteLine(document, "QS3DQSETTINGSHEALTH lỗi: " + ex.Message);',
    ]
    missing = [token for token in required if token not in text]
    if missing:
        print("ERROR: Quantity Settings health command contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    load_at = text.find("new QuantitySettingsStore().Load();")
    analyze_at = text.find("QuantityCalculationMatrixDiagnostics.Analyze(settings);")
    if load_at < 0 or analyze_at <= load_at:
        print("ERROR: settings must be loaded through QuantitySettingsStore before diagnostics analysis.")
        return 1

    forbidden = [
        ".Save(",
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
        "File.Write",
        "File.Delete",
        "File.Move",
        "File.Replace",
        "SettingsPath",
        "Process.Start",
        "BooleanOperation",
        "Solid3d",
        "Brep",
    ]
    present = [token for token in forbidden if token in text]
    if present:
        print("ERROR: settings health command crossed a read-only/privacy boundary:")
        for item in present:
            print(" -", item)
        return 1

    if "Take(DetailLimit)" not in text:
        print("ERROR: diagnostic detail output must remain bounded.")
        return 1

    print("PASS: QS3DQSETTINGSHEALTH is a bounded read-only Load -> Analyze diagnostic command with no project/settings/drawing writes or path disclosure.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
