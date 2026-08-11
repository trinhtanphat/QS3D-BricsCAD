#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "QuantitySettingsStore.cs"


def main():
    text = STORE.read_text(encoding="utf-8")

    required = [
        "private const long MaxSettingsFileBytes = 32L * 1024L * 1024L;",
        "private static void EnsureSupportedFileLength(long length)",
        "if (length > MaxSettingsFileBytes)",
        "EnsureSupportedFileLength(stream.Length);",
        "File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)",
        "serializer.ReadObject(stream)",
        "serializer.WriteObject(stream, settings);",
        "stream.Flush(true);",
        "catch (InvalidDataException ex) when (!IsUnsupportedSchema(ex) && File.Exists(backupPath))",
        "catch (InvalidDataException ex) when (!IsUnsupportedSchema(ex))",
        "File.Replace(temp, path, backup, true);",
        "File.Replace(temp, path, null, true);",
    ]
    missing = [token for token in required if token not in text]
    if missing:
        print("ERROR: Quantity Settings file-size contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    read_start = text.find("private static QuantityCalculationSettings ReadAndValidate")
    read_end = text.find("private static string GetBackupPath", read_start)
    write_start = text.find("private static void WriteAtomic")
    write_end = text.find("private static void EnsureSupportedFileLength", write_start)
    if min(read_start, read_end, write_start, write_end) < 0:
        print("ERROR: cannot isolate QuantitySettingsStore read/write boundaries.")
        return 1

    read = text[read_start:read_end]
    write = text[write_start:write_end]

    open_pos = read.find("File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)")
    guard_pos = read.find("EnsureSupportedFileLength(stream.Length);")
    serializer_pos = read.find("new DataContractJsonSerializer(typeof(QuantityCalculationSettings))")
    read_object_pos = read.find("serializer.ReadObject(stream)")
    future_schema_pos = read.find("if (value.SchemaVersion > QuantityCalculationSettings.CurrentSchemaVersion)")
    normalize_pos = read.find("value.NormalizeAndValidate();")
    if not (0 <= open_pos < guard_pos < serializer_pos < read_object_pos < future_schema_pos < normalize_pos):
        print("ERROR: read path must open -> length-check -> construct serializer -> deserialize -> future-schema check -> normalize.")
        return 1

    write_object_pos = write.find("serializer.WriteObject(stream, settings);")
    flush_pos = write.find("stream.Flush(true);")
    write_guard_pos = write.find("EnsureSupportedFileLength(stream.Length);")
    replace_pos = write.find("if (File.Exists(path))")
    if not (0 <= write_object_pos < flush_pos < write_guard_pos < replace_pos):
        print("ERROR: serialized output must be flushed and size-checked before replacing/moving the destination.")
        return 1

    forbidden = [
        "ReadAllBytes",
        "ReadAllText",
        "ReadToEnd",
        "MemoryStream",
        "ToArray()",
        "CopyTo(",
    ]
    present = [token for token in forbidden if token in read]
    if present:
        print("ERROR: file-size validation must not pre-read/materialize the settings file:")
        for token in present:
            print(" -", token)
        return 1

    if "MaxSettingsFileBytes = 8" in text:
        print("ERROR: do not use the earlier 8 MiB estimate; it can undercut the valid maximum directed-rule serialization envelope.")
        return 1

    print("PASS: Quantity Settings JSON is capped at 32 MiB on the exact opened stream before deserialization, writes are capped before replacement, and existing backup/future-schema recovery semantics remain intact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())