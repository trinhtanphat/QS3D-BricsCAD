#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "QuantitySettingsStore.cs"


def main():
    store = STORE.read_text(encoding="utf-8")

    required = [
        "var backupPath = GetBackupPath(_settingsPath);",
        "? ReadAndValidate(backupPath)",
        "catch (InvalidDataException ex) when (!IsUnsupportedSchema(ex) && File.Exists(backupPath))",
        "catch (FileNotFoundException) when (File.Exists(backupPath))",
        "var backup = GetBackupPath(path);",
        "File.Replace(temp, path, backup, true);",
        "if (value.SchemaVersion > QuantityCalculationSettings.CurrentSchemaVersion)",
        "throw CreateUnsupportedSchemaException(value.SchemaVersion);",
        "private static InvalidDataException CreateUnsupportedSchemaException(int schemaVersion)",
        "exception.Data[UnsupportedSchemaMarker] = true;",
        "private static bool IsUnsupportedSchema(Exception exception)",
        "catch (Exception ex) when (!(ex is FileNotFoundException) && !IsUnsupportedSchema(ex))",
    ]
    missing = [token for token in required if token not in store]
    if missing:
        print("ERROR: quantity settings backup-recovery contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    load_pos = store.find("public QuantityCalculationSettings Load()")
    primary_pos = store.find("return ReadAndValidate(_settingsPath);", load_pos)
    corrupt_fallback_pos = store.find("return ReadAndValidate(backupPath);", primary_pos)
    read_pos = store.find("private static QuantityCalculationSettings ReadAndValidate")
    future_check_pos = store.find("if (value.SchemaVersion > QuantityCalculationSettings.CurrentSchemaVersion)", read_pos)
    normalize_pos = store.find("value.NormalizeAndValidate();", future_check_pos)
    write_pos = store.find("private static void WriteAtomic")
    backup_write_pos = store.find("var backup = GetBackupPath(path);", write_pos)
    replace_pos = store.find("File.Replace(temp, path, backup, true);", backup_write_pos)

    positions = [
        load_pos,
        primary_pos,
        corrupt_fallback_pos,
        read_pos,
        future_check_pos,
        normalize_pos,
        write_pos,
        backup_write_pos,
        replace_pos,
    ]
    if min(positions) < 0:
        print("ERROR: quantity settings recovery ordering markers are missing.")
        return 1

    if not (load_pos < primary_pos < corrupt_fallback_pos < read_pos):
        print("ERROR: Load must prefer the primary settings file and only then fall back to the validated backup.")
        return 1
    if not (read_pos < future_check_pos < normalize_pos):
        print("ERROR: unsupported future settings schemas must fail closed before normal validation/fallback can hide incompatibility.")
        return 1
    if "UnsupportedSchemaException : InvalidDataException" in store:
        print("ERROR: InvalidDataException is sealed on the V25 target; unsupported schemas must use the marked InvalidDataException factory.")
        return 1
    if not (write_pos < backup_write_pos < replace_pos):
        print("ERROR: atomic settings replacement must keep producing the same recovery backup path consumed by Load.")
        return 1

    print("PASS: quantity settings prefer primary state, recover missing/corrupt state from the validated atomic backup, and keep future schemas fail closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
