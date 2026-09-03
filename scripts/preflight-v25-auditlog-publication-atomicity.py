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

    candidate_index = source.find("var candidate = new AuditLogWindow(document);")
    subscribe_index = source.find("candidate.Closed +=", candidate_index)
    show_index = source.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", candidate_index)
    reservation_index = source.find("_unpublishedCandidate = candidate;", candidate_index, show_index)
    require(candidate_index >= 0 and subscribe_index > candidate_index and show_index > subscribe_index,
            "cannot bound Audit Log candidate publication sequence")
    require(
        reservation_index > subscribe_index,
        "Audit Log must reserve the exact unpublished candidate before native ShowModelessWindow can reenter",
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

    publish_index = source.find("_window = candidate;", show_index)
    release_index = source.find("_unpublishedCandidate = null;", publish_index)
    require(
        publish_index > show_index and release_index > publish_index,
        "successful publication must transition the same candidate from unpublished reservation to published state",
    )
    transition = source[publish_index:release_index + len("_unpublishedCandidate = null;")]
    require(
        "ReferenceEquals(_unpublishedCandidate, candidate)" in transition,
        "successful publication may clear only the exact in-flight candidate reservation",
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

    print("PASS: Audit Log modeless publication is atomic, reentrancy-safe, and unresolved unpublished candidates remain quarantined")


if __name__ == "__main__":
    main()
