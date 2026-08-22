#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "QuantitySettingsStore.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml.cs"


def main():
    store = STORE.read_text(encoding="utf-8")
    window = WINDOW.read_text(encoding="utf-8")

    required_store = [
        "var copy = settings.Clone();",
        "copy.NormalizeAndValidate();",
        "copy.SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion;",
        "WriteAtomic(_settingsPath, Prepare(settings));",
        "WriteAtomic(Path.GetFullPath(path), Prepare(settings));",
    ]
    required_window = [
        "SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion,",
        "CategoryRules = CategoryRows.Select(x => x.ToSetting()).ToList(),",
        "IntersectionRules = IntersectionRows.Select(x => x.ToSetting()).ToList()",
        "result.NormalizeAndValidate();",
    ]

    missing = ["store: " + token for token in required_store if token not in store]
    missing += ["window: " + token for token in required_window if token not in window]
    if missing:
        print("ERROR: quantity settings schema hardening contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    validate_pos = store.find("copy.NormalizeAndValidate();")
    stamp_pos = store.find("copy.SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion;", validate_pos)
    return_pos = store.find("return copy;", stamp_pos)
    if min(validate_pos, stamp_pos, return_pos) < 0 or not (validate_pos < stamp_pos < return_pos):
        print("ERROR: future schemas must be validated/rejected before stamping current schema on a write copy.")
        return 1

    stale_ui = "SchemaVersion = _loadedSettings.SchemaVersion"
    stale_prepare = "if (copy.SchemaVersion <= 0) copy.SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion;"
    if stale_ui in window or stale_prepare in store:
        print("ERROR: stale imported schema markers must not survive user save/export writes.")
        return 1

    print("PASS: quantity settings validate supported input first, preserve rule payloads, and stamp current schema at UI/write boundaries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
