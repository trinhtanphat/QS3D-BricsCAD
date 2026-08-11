#!/usr/bin/env python3
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml.cs"
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "QuantitySettingsStore.cs"


def method_body(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:] if end < 0 else text[start:end]


def main():
    try:
        ET.parse(str(XAML))
    except ET.ParseError as exc:
        print("ERROR: QuantitySettingsWindow.xaml is not well-formed XML:", exc)
        return 1

    xaml = XAML.read_text(encoding="utf-8")
    code = CODE.read_text(encoding="utf-8")
    store = STORE.read_text(encoding="utf-8")

    required_store = [
        'private const string UnsupportedSchemaMarker = "QS3D.QuantitySettings.UnsupportedSchema";',
        'exception.Data[UnsupportedSchemaMarker] = true;',
        'if (value.SchemaVersion > QuantityCalculationSettings.CurrentSchemaVersion)',
    ]
    required_code = [
        'private const string UnsupportedSchemaMarker = "QS3D.QuantitySettings.UnsupportedSchema";',
        'private bool _persistentSettingsWriteBlocked;',
        'var unsupportedSchema = IsUnsupportedSettingsSchema(ex);',
        '_persistentSettingsWriteBlocked = true;',
        'SaveSettingsButton.IsEnabled = false;',
        'SettingsPathText.Text = _store.SettingsPath + "  •  CHỈ ĐỌC: schema mới hơn";',
        'private static bool IsUnsupportedSettingsSchema(Exception exception)',
        'exception is System.IO.InvalidDataException',
        'Equals(exception.Data[UnsupportedSchemaMarker], true)',
        'if (_persistentSettingsWriteBlocked && SamePath(dialog.FileName, _store.SettingsPath))',
        'private static bool SamePath(string left, string right)',
        'System.IO.Path.GetFullPath(left)',
        'System.IO.Path.GetFullPath(right)',
        'IntersectionRules = IntersectionRows.Select(x => x.ToSetting()).ToList()',
    ]
    required_xaml = [
        'x:Name="SaveSettingsButton"',
        'Click="Save_Click"',
        'x:Name="PrimaryCategoryList"',
        'x:Name="ReferenceCategoryList"',
        'x:Name="SelectedRuleEditor"',
        'Thông số engine chung',
        'Ngưỡng lọc khối lượng',
        'Pick Room (pick biên phòng trong View 3D)',
        'Nhãn kích thước (Dim) khi Diễn giải khối lượng',
    ]

    missing = ["store: " + token for token in required_store if token not in store]
    missing += ["code: " + token for token in required_code if token not in code]
    missing += ["xaml: " + token for token in required_xaml if token not in xaml]
    if missing:
        print("ERROR: future-schema settings UI contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    if "_persistentSettingsWriteBlocked = false" in code:
        print("ERROR: future-schema write block must be monotonic for the lifetime of the window.")
        return 1

    constructor = method_body(
        code,
        "public QuantitySettingsWindow(QuantitySettingsStore store)",
        "public ObservableCollection<QuantityCategoryRuleRow> CategoryRows",
    )
    detect_pos = constructor.find("var unsupportedSchema = IsUnsupportedSettingsSchema(ex);")
    default_pos = constructor.find("LoadIntoView(QuantityCalculationSettings.CreateDefault());", detect_pos)
    block_pos = constructor.find("_persistentSettingsWriteBlocked = true;", default_pos)
    disable_pos = constructor.find("SaveSettingsButton.IsEnabled = false;", block_pos)
    if min(detect_pos, default_pos, block_pos, disable_pos) < 0 or not detect_pos < default_pos < block_pos < disable_pos:
        print("ERROR: startup must identify future schema, load a read-only fallback view, then lock and disable persistent Save.")
        return 1

    save = method_body(code, "private void Save_Click(object sender, RoutedEventArgs e)", "private static bool IsUnsupportedSettingsSchema")
    guard_pos = save.find("if (_persistentSettingsWriteBlocked)")
    return_pos = save.find("return;", guard_pos)
    persist_pos = save.find("_store.Save(current);")
    if min(guard_pos, return_pos, persist_pos) < 0 or not guard_pos < return_pos < persist_pos:
        print("ERROR: Save must fail closed before any persistent settings write when future-schema protection is active.")
        return 1

    export = method_body(code, "private void ExportTemplate_Click(object sender, RoutedEventArgs e)", "private void RestoreDefaults_Click")
    path_guard_pos = export.find("if (_persistentSettingsWriteBlocked && SamePath(dialog.FileName, _store.SettingsPath))")
    path_return_pos = export.find("return;", path_guard_pos)
    export_pos = export.find("_store.Export(dialog.FileName, current);")
    if min(path_guard_pos, path_return_pos, export_pos) < 0 or not path_guard_pos < path_return_pos < export_pos:
        print("ERROR: Export must refuse the protected per-user settings path before writing a supported template.")
        return 1

    import_method = method_body(code, "private void ImportTemplate_Click(object sender, RoutedEventArgs e)", "private void ExportTemplate_Click")
    restore_method = method_body(code, "private void RestoreDefaults_Click(object sender, RoutedEventArgs e)", "private void Save_Click")
    if "_persistentSettingsWriteBlocked =" in import_method or "_persistentSettingsWriteBlocked =" in restore_method:
        print("ERROR: Import/reset must not clear or rewrite the startup future-schema protection state.")
        return 1

    print("PASS: future-schema quantity settings remain protected from Save and same-path Export while supported fallback/import/export UI contracts stay intact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
