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
        "private static bool CanRotatePrimaryIntoBackup(string path)",
        "ReadAndValidate(path);",
        "catch (InvalidDataException ex) when (!IsUnsupportedSchema(ex))",
        "if (CanRotatePrimaryIntoBackup(path))",
        "var backup = GetBackupPath(path);",
        "File.Replace(temp, path, backup, true);",
        "File.Replace(temp, path, null, true);",
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
    rotate_check_pos = store.find("private static bool CanRotatePrimaryIntoBackup(string path)")
    rotate_validate_pos = store.find("ReadAndValidate(path);", rotate_check_pos)
    rotate_corrupt_pos = store.find("catch (InvalidDataException ex) when (!IsUnsupportedSchema(ex))", rotate_validate_pos)
    write_pos = store.find("private static void WriteAtomic")
    backup_write_pos = store.find("var backup = GetBackupPath(path);", write_pos)
    can_rotate_pos = store.find("if (CanRotatePrimaryIntoBackup(path))", backup_write_pos)
    valid_replace_pos = store.find("File.Replace(temp, path, backup, true);", can_rotate_pos)
    preserve_replace_pos = store.find("File.Replace(temp, path, null, true);", valid_replace_pos)

    positions = [
        load_pos,
        primary_pos,
        corrupt_fallback_pos,
        read_pos,
        future_check_pos,
        normalize_pos,
        rotate_check_pos,
        rotate_validate_pos,
        rotate_corrupt_pos,
        write_pos,
        backup_write_pos,
        can_rotate_pos,
        valid_replace_pos,
        preserve_replace_pos,
    ]
    if min(positions) < 0:
        print("ERROR: quantity settings recovery/rotation ordering markers are missing.")
        return 1

    if not (load_pos < primary_pos < corrupt_fallback_pos < read_pos):
        print("ERROR: Load must prefer the primary settings file and only then fall back to the validated backup.")
        return 1
    if not (read_pos < future_check_pos < normalize_pos < rotate_check_pos):
        print("ERROR: unsupported future settings schemas must fail closed before normal validation/fallback or backup rotation can hide incompatibility.")
        return 1
    if not (rotate_check_pos < rotate_validate_pos < rotate_corrupt_pos < write_pos):
        print("ERROR: backup rotation must validate the current primary and classify only ordinary corruption as non-rotatable.")
        return 1
    if not (write_pos < backup_write_pos < can_rotate_pos < valid_replace_pos < preserve_replace_pos):
        print("ERROR: valid primaries must rotate into backup, while corrupt primaries must be replaced atomically without overwriting the existing backup.")
        return 1
    if "UnsupportedSchemaException : InvalidDataException" in store:
        print("ERROR: InvalidDataException is sealed on the V25 target; unsupported schemas must use the marked InvalidDataException factory.")
        return 1
    if "if (File.Exists(backup)) File.Delete(backup);" in store:
        print("ERROR: do not pre-delete the last-known-good backup before atomic replacement; File.Replace owns normal backup rotation.")
        return 1

    print("PASS: quantity settings prefer primary state, recover from validated backup, preserve last-known-good backup after corrupt-primary recovery, rotate valid primaries atomically, and keep future schemas fail closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
