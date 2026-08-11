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
        "private const long MaxSettingsFileBytes = 32L * 1024L * 1024L;",
        "EnsureSupportedFileLength(stream.Length);",
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
    open_pos = store.find("File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)", read_pos)
    size_guard_pos = store.find("EnsureSupportedFileLength(stream.Length);", open_pos)
    serializer_pos = store.find("new DataContractJsonSerializer(typeof(QuantityCalculationSettings))", size_guard_pos)
    read_object_pos = store.find("serializer.ReadObject(stream)", serializer_pos)
    future_check_pos = store.find("if (value.SchemaVersion > QuantityCalculationSettings.CurrentSchemaVersion)", read_object_pos)
    normalize_pos = store.find("value.NormalizeAndValidate();", future_check_pos)
    rotate_check_pos = store.find("private static bool CanRotatePrimaryIntoBackup(string path)")
    rotate_validate_pos = store.find("ReadAndValidate(path);", rotate_check_pos)
    rotate_corrupt_pos = store.find("catch (InvalidDataException ex) when (!IsUnsupportedSchema(ex))", rotate_validate_pos)
    write_pos = store.find("private static void WriteAtomic")
    write_object_pos = store.find("serializer.WriteObject(stream, settings);", write_pos)
    flush_pos = store.find("stream.Flush(true);", write_object_pos)
    written_size_guard_pos = store.find("EnsureSupportedFileLength(stream.Length);", flush_pos)
    backup_write_pos = store.find("var backup = GetBackupPath(path);", write_pos)
    can_rotate_pos = store.find("if (CanRotatePrimaryIntoBackup(path))", backup_write_pos)
    valid_replace_pos = store.find("File.Replace(temp, path, backup, true);", can_rotate_pos)
    preserve_replace_pos = store.find("File.Replace(temp, path, null, true);", valid_replace_pos)

    positions = [
        load_pos,
        primary_pos,
        corrupt_fallback_pos,
        read_pos,
        open_pos,
        size_guard_pos,
        serializer_pos,
        read_object_pos,
        future_check_pos,
        normalize_pos,
        rotate_check_pos,
        rotate_validate_pos,
        rotate_corrupt_pos,
        write_pos,
        write_object_pos,
        flush_pos,
        written_size_guard_pos,
        backup_write_pos,
        can_rotate_pos,
        valid_replace_pos,
        preserve_replace_pos,
    ]
    if min(positions) < 0:
        print("ERROR: quantity settings recovery/size/rotation ordering markers are missing.")
        return 1

    if not (load_pos < primary_pos < corrupt_fallback_pos < read_pos):
        print("ERROR: Load must prefer the primary settings file and only then fall back to the validated backup.")
        return 1
    if not (read_pos < open_pos < size_guard_pos < serializer_pos < read_object_pos < future_check_pos < normalize_pos < rotate_check_pos):
        print("ERROR: settings size must be checked on the opened stream before JSON deserialization, then future schemas must fail closed before normal validation/backup rotation.")
        return 1
    if not (rotate_check_pos < rotate_validate_pos < rotate_corrupt_pos < write_pos):
        print("ERROR: backup rotation must validate the current primary and classify only ordinary corruption as non-rotatable.")
        return 1
    if not (write_pos < write_object_pos < flush_pos < written_size_guard_pos < backup_write_pos < can_rotate_pos < valid_replace_pos < preserve_replace_pos):
        print("ERROR: serialized settings must be size-checked before atomic replacement; valid primaries rotate to backup and invalid primaries preserve the last-known-good backup.")
        return 1
    if "UnsupportedSchemaException : InvalidDataException" in store:
        print("ERROR: InvalidDataException is sealed on the V25 target; unsupported schemas must use the marked InvalidDataException factory.")
        return 1
    if "if (File.Exists(backup)) File.Delete(backup);" in store:
        print("ERROR: do not pre-delete the last-known-good backup before atomic replacement; File.Replace owns normal backup rotation.")
        return 1

    print("PASS: quantity settings check file size before deserialization, prefer primary state, recover from validated backup, preserve last-known-good backup after invalid-primary recovery, size-check writes before replacement, rotate valid primaries atomically, and keep future schemas fail closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())