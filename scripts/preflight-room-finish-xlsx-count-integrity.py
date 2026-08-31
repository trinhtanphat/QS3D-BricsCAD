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
        'var rowCount = BindKnownCount(rows, MaxDataRows, "export rows");',
        "outer deterministic known-count contract binding",
    )
    pre_indexer = require(exporter, 'rowCount.Revalidate(rows, "before row indexer");', "pre-indexer Count revalidation")
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)", "indexed outer traversal")
    post_indexer = require(exporter, 'rowCount.Revalidate(rows, "after row indexer");', "post-indexer Count revalidation")
    post_traversal = require(exporter, 'rowCount.Revalidate(rows, "after snapshot traversal");', "post-traversal Count revalidation")
    post_stability = require(exporter, 'rowCount.Revalidate(rows, "after row stability validation");', "pre-filesystem Count revalidation")
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem boundary")
    if not (row_count < row_loop and pre_indexer < post_indexer < post_traversal < post_stability < path_resolution):
        raise SystemExit("FAIL: Room-finish outer known-count contract must revalidate caller Count channels through traversal before filesystem output")

    for token, label in (
        ("private static KnownCountContract<T> BindKnownCount<T>(IEnumerable<T> source, int maximum, string label)", "known-count contract binder"),
        ("private sealed class KnownCountContract<T>", "known-count contract type"),
        ("source is IReadOnlyCollection<T>", "IReadOnlyCollection<T> channel admission"),
        ("source is ICollection<T>", "ICollection<T> channel admission"),
        ("source is ICollection", "non-generic ICollection channel admission"),
        ("if (_readOnlyCount) observe(((IReadOnlyCollection<T>)source).Count);", "IReadOnlyCollection<T> count revalidation"),
        ("if (_genericCount) observe(((ICollection<T>)source).Count);", "ICollection<T> count revalidation"),
        ("if (_nonGenericCount) observe(((ICollection)source).Count);", "non-generic ICollection count revalidation"),
        ('throw new InvalidOperationException("Room-finish XLSX " + _label + " exposes conflicting known collection counts " + phase + ".");', "conflicting known-count rejection"),
        ('throw new ArgumentException("Room-finish XLSX " + _label + " must expose a deterministic collection count.", "rows");', "missing deterministic-count rejection"),
        ('throw new InvalidOperationException("Room-finish XLSX " + _label + " count changed " + phase + ". Expected " + Value + " but observed " + observed + ".");', "admitted count drift rejection"),
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
        "var rows = new CountDriftingRows(ValidRow());",
        "RoomFinishXlsxExporter.Export(destination, rows)",
        "export rows count changed before row indexer",
        "rows.IndexerReads != 0",
        "internal int IndexerReads { get; private set; }",
        "IndexerReads++;",
        "preserve-existing-room-finish-destination",
        "must-not-be-created",
    ):
        require(smoke, token, "deterministic Room-finish XLSX count-drift smoke contract")

    require(registration, "RoomFinishXlsxCountIntegritySmoke.Run();", "smoke registration")

    print(
        "PASS: Room-finish XLSX binds and revalidates all admitted deterministic outer collection Counts before caller indexing, "
        "preserves nested concrete-list drift semantics, fails before filesystem output, "
        "and registers deterministic regression coverage."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
