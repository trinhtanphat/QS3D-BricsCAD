#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STATUS = ROOT / "docs" / "research" / "BLT3D-QS3D-IMPLEMENTATION-STATUS-2026-08-14.md"
INDEX = ROOT / "docs" / "research" / "BLT3D-GEMINI-RESEARCH-INDEX.md"
RESEARCH = ROOT / "docs" / "BLT3D-RESEARCH.md"
WORKSTREAM = ROOT / "docs" / "BLT3D-RESEARCH-TO-QS3D-AGENT-WORKSTREAMS-2026-08-12.md"

EVIDENCE = [
    "src/QS3D.Core/Measurement/MeasurementTrace.cs",
    "src/QS3D.Core/Measurement/MeasurementTraceInspector.cs",
    "src/QS3D.Core/Measurement/MeasurementSnapshot.cs",
    "src/QS3D.Core/Measurement/MeasurementSnapshotDelta.cs",
    "src/QS3D.Core/Measurement/MeasurementSnapshotDeltaReason.cs",
    "src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs",
    "src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs",
    "src/QS3D.Core/Mapping/MeasurementWorkItemCoverageMatrix.cs",
    "src/QS3D.Core/Mapping/MeasurementWorkItemCoverageReport.cs",
    "src/QS3D.Core/Cost/RateBook.cs",
    "src/QS3D.Core/Cost/EstimateLine.cs",
    "src/QS3D.Core/Cost/EstimateLineFreshness.cs",
    "src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs",
    "src/QS3D.Core/Cost/FrozenEstimateProjection.cs",
]


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"required research/status file is missing: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def main() -> int:
    status = read(STATUS)
    index = read(INDEX)
    research = read(RESEARCH)
    workstream = read(WORKSTREAM)

    for marker in (
        "SOURCE_IMPLEMENTED",
        "FOUNDATION_PRESENT",
        "PARTIAL_OR_OPEN",
        "LOCAL_ONLY",
        "ENGINEERING_REQUIRED",
        "OUT_OF_SCOPE_OR_SEPARATE_PRODUCT",
        "ARCHIVE_ONLY",
    ):
        require(status, marker, "implementation status vocabulary")

    for lane in ("### MTR", "### REV", "### MAP", "### CST", "### NAT", "### PERF", "### QSC", "### TKO", "### IFC / BCF", "### REB", "### MEP", "### CIV", "### EXT"):
        require(status, lane, "research workstream classification")

    for relative in EVIDENCE:
        evidence_path = ROOT / relative
        if not evidence_path.is_file():
            raise AssertionError(f"documented research implementation evidence is missing: {relative}")
        require(status, f"`{relative}`", "documented source evidence")

    overlay_name = "BLT3D-QS3D-IMPLEMENTATION-STATUS-2026-08-14.md"
    require(index, overlay_name, "research archive implementation overlay link")
    require(research, overlay_name, "public research implementation overlay link")

    # Guard the semantic boundary without freezing one editorial sentence. The index
    # currently expresses this as preventing the dated advisory queue from being
    # mistaken for a live list; equivalent wording should not break a source-shape gate.
    require(index, "Advisory research/archive index", "archive advisory classification")
    require(index, "not canonical QS3D product truth", "archive non-canonical boundary")
    require(index, "dated advisory queue", "archive/advisory queue distinction")
    require(index, "live list of missing code", "archive/live-backlog concept")
    require(research, "retained as provenance and idea-generation material", "research provenance boundary")
    research_without_emphasis = research.replace("**", "").replace("__", "")
    require(research_without_emphasis, "not a live list of missing QS3D code", "research/live-backlog distinction")
    require(workstream, "Advisory implementation queue", "dated workstream advisory status")
    require(workstream, "This file does not reserve any implementation scope.", "claim ownership boundary")

    print("PASS: research archive, implementation truth overlay, and source evidence boundary are consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
