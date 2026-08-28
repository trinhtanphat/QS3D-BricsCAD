#!/usr/bin/env python3
# Lane-Key: issue-3273
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "RoomFinishXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomFinishXlsxCountIntegritySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    row_count = require(
        exporter,
        'var rowCount = RequireConsistentKnownCount(rows, MaxDataRows, "export rows");',
        "outer deterministic known-count binding",
    )
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)", "indexed outer traversal")
    row_bind = require(exporter, "if (rows.Count != rowCount)", "post-traversal outer Count binding")
    row_failure = require(
        exporter,
        'throw new InvalidOperationException("Room-finish XLSX export row count changed during snapshot.");',
        "outer count-drift failure",
    )
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem boundary")
    if not (row_count < row_loop < row_bind < row_failure < path_resolution):
        raise SystemExit("FAIL: Room-finish outer known-count/drift validation must fail before filesystem output")

    for token, label in (
        ("private static int RequireConsistentKnownCount<T>(IEnumerable<T> source, int maximum, string label)", "known-count helper"),
        ("var readOnly = source as IReadOnlyCollection<T>;", "IReadOnlyCollection<T> count binding"),
        ("var generic = source as ICollection<T>;", "ICollection<T> count binding"),
        ("var nonGeneric = source as ICollection;", "non-generic ICollection count binding"),
        ('throw new InvalidOperationException("Room-finish XLSX " + label + " exposes conflicting known collection counts.");', "conflicting known-count rejection"),
        ('throw new ArgumentException("Room-finish XLSX " + label + " must expose a deterministic collection count.", "rows");', "missing deterministic-count rejection"),
    ):
        require(exporter, token, label)

    joined_count = require(exporter, "var count = source.Count;", "joined-cell Count snapshot")
    joined_loop = require(exporter, "for (var index = 0; index < count; index++)", "joined-cell indexed traversal")
    joined_bind = require(exporter, "if (source.Count != count)", "joined-cell Count binding")
    joined_failure = require(
        exporter,
        'throw new InvalidOperationException("Room-finish XLSX row " + rowIndex + " field " + fieldName + " count changed during snapshot.");',
        "joined-cell count-drift failure",
    )
    if not (joined_count < joined_loop < joined_bind < joined_failure):
        raise SystemExit("FAIL: Room-finish joined-cell Count drift must fail after indexed snapshot traversal")

    for token in (
        "AssertRowCountDriftFailsBeforeExistingDestinationReplacement();",
        "AssertRowCountDriftFailsBeforeFilesystemCreation();",
        "RoomFinishXlsxExporter.Export(destination, new CountDriftingRows(ValidRow()))",
        "preserve-existing-room-finish-destination",
        "must-not-be-created",
        "row count changed during snapshot",
    ):
        require(smoke, token, "deterministic Room-finish XLSX count-drift smoke contract")

    require(registration, "RoomFinishXlsxCountIntegritySmoke.Run();", "smoke registration")

    print(
        "PASS: Room-finish XLSX binds all deterministic outer collection Counts before indexed traversal, "
        "preserves post-snapshot drift detection and nested concrete-list drift semantics, fails before filesystem output, "
        "and registers deterministic regression coverage."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
