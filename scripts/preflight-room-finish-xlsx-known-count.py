#!/usr/bin/env python3
# Lane-Key: issue-4215
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "RoomFinishXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomFinishXlsxKnownCountContractSmoke.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    row_count = require(
        exporter,
        'var rowCount = BindKnownCount(rows, MaxDataRows, "export rows");',
        "top-level consistent known-count contract binding",
    )
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)", "indexed row traversal")
    pre_indexer = require(exporter, 'rowCount.Revalidate(rows, "before row indexer");', "pre-indexer known-count revalidation")
    post_indexer = require(exporter, 'rowCount.Revalidate(rows, "after row indexer");', "post-indexer known-count revalidation")
    post_snapshot = require(exporter, 'rowCount.Revalidate(rows, "after row snapshot");', "post-row-snapshot known-count revalidation")
    post_traversal = require(exporter, 'rowCount.Revalidate(rows, "after snapshot traversal");', "post-traversal known-count revalidation")
    post_stability = require(exporter, 'rowCount.Revalidate(rows, "after row stability validation");', "pre-filesystem known-count revalidation")
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem boundary")

    helper = require(
        exporter,
        "private static KnownCountContract<T> BindKnownCount<T>(IEnumerable<T> source, int maximum, string label)",
        "known-count contract binder",
    )
    contract = require(exporter, "private sealed class KnownCountContract<T>", "known-count contract type")
    read_only = require(exporter, "source is IReadOnlyCollection<T>", "IReadOnlyCollection channel admission")
    generic = require(exporter, "source is ICollection<T>", "generic ICollection channel admission")
    non_generic = require(exporter, "source is ICollection", "non-generic ICollection channel admission")
    conflict = require(exporter, "exposes conflicting known collection counts", "conflicting-count rejection")
    negative = require(exporter, "count must be non-negative", "negative-count rejection")
    maximum = require(exporter, "count exceeds the supported maximum", "maximum-count rejection")
    deterministic_required = require(exporter, "must expose a deterministic collection count", "deterministic-count requirement")
    drift = require(exporter, 'count changed " + phase + ". Expected " + Value + " but observed " + observed', "admitted count drift rejection")

    if not (row_count < row_loop and pre_indexer < post_indexer < post_snapshot < post_traversal < post_stability < path_resolution):
        raise SystemExit("FAIL: Room-finish top-level count contract must bind before traversal and revalidate admitted channels before filesystem mutation")
    if not (helper < read_only < generic < non_generic < contract):
        raise SystemExit("FAIL: Room-finish known-count binder must admit every deterministic collection interface before constructing the contract")
    if min(conflict, negative, maximum, deterministic_required, drift) < contract:
        raise SystemExit("FAIL: Room-finish known-count contract must preserve range/conflict/deterministic/drift rejection semantics")
    if "var count = BindKnownCount(source" in exporter or "var count = RequireConsistentKnownCount(source" in exporter:
        raise SystemExit("FAIL: issue-4215 must not broaden into nested concrete-list count semantics")
    require(exporter, "var count = source.Count;", "existing nested concrete-list snapshot behavior")
    require(exporter, "if (source.Count != count)", "existing nested post-snapshot drift check")

    for token in (
        "KnownCountRows",
        "IReadOnlyList<RoomFinishScheduleRow>",
        "ICollection<RoomFinishScheduleRow>",
        "ICollection",
        "RejectsConflictingKnownCountsBeforeTraversalOrFilesystem",
        "RejectsNegativeKnownCountBeforeTraversalOrFilesystem",
        "RejectsOversizedKnownCountBeforeTraversalOrFilesystem",
        "AcceptsHonestMultiInterfaceKnownCounts",
        "rows.IndexerReads != 0",
        "Directory.Exists(root)",
    ):
        require(smoke, token, "Room-finish deterministic known-count regression")

    print(
        "PASS: Room-finish XLSX binds top-level deterministic Count interfaces before traversal, "
        "rejects invalid/conflicting counts before filesystem mutation, revalidates admitted channels through caller traversal, "
        "and leaves nested concrete-list semantics unchanged."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
