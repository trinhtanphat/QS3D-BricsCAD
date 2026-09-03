#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "AuditCommands.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")

    require("CloseUnpublishedCandidate" in source, "missing unpublished-candidate terminal cleanup helper")
    require("_unpublishedCandidate" in source, "failed unpublished cleanup must remain quarantined across invocations")
    require("PrepareUnpublishedCandidate" in source, "command must retry/quarantine unresolved unpublished candidates before creating another")
    require(
        "if (!PrepareUnpublishedCandidate())" in source,
        "Audit Log command must fail closed before a new candidate while prior unpublished cleanup is unresolved",
    )
    require(
        "if (!candidate.IsLoaded)" in source and "CloseUnpublishedCandidate(candidate)" in source,
        "non-loaded modeless candidate must be terminally cleaned before return",
    )
    require(
        "catch (System.Exception)" in source and "CloseUnpublishedCandidate(candidate)" in source,
        "ShowModelessWindow exception path must clean the unpublished candidate",
    )
    require(
        "if (candidate.IsLoaded)" in source and "_window = candidate;" in source,
        "Audit Log publication must remain conditional on a loaded candidate",
    )
    helper_start = source.find("private static bool CloseUnpublishedCandidate")
    require(helper_start >= 0, "missing cleanup helper body")
    helper_end = source.find("private static void ReleaseCandidate", helper_start)
    require(helper_end > helper_start, "cannot bound cleanup helper")
    helper = source[helper_start:helper_end]
    require("candidate.Close();" in helper, "cleanup helper must attempt terminal Close")
    require("_unpublishedCandidate = candidate;" in helper, "failed cleanup must quarantine the exact candidate")
    require("_window = candidate" not in helper, "cleanup helper must never publish an unproven candidate")
    require("ProjectContextCoordinator.GetOrCreate" not in source, "Audit Log command must keep project reads non-creating")

    print("PASS: Audit Log modeless publication is atomic and unresolved unpublished candidates remain quarantined")


if __name__ == "__main__":
    main()
