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
        "_publicationInFlightCandidate" in source,
        "Audit Log must distinguish an in-flight native publication from a failed unpublished cleanup quarantine",
    )
    inflight_set_index = source.find("_publicationInFlightCandidate = candidate;", reservation_index, show_index)
    require(
        inflight_set_index > reservation_index,
        "Audit Log must mark the exact candidate in-flight before entering native ShowModelessWindow",
    )
    show_finally_index = source.find("finally", show_index)
    show_inflight_clear_index = source.find("ReferenceEquals(_publicationInFlightCandidate, candidate)", show_finally_index)
    show_post_index = source.find("if (!candidate.IsLoaded)", show_finally_index)
    require(
        show_finally_index > show_index and show_inflight_clear_index > show_finally_index and show_post_index > show_inflight_clear_index,
        "publication-in-flight reservation must remain held until native ShowModelessWindow unwinds",
    )

    prepare_start = source.find("private static bool PrepareUnpublishedCandidate")
    prepare_end = source.find("private static bool PreparePublishedWindow", prepare_start)
    require(prepare_start >= 0 and prepare_end > prepare_start, "cannot bound unpublished-candidate preparation helper")
    prepare = source[prepare_start:prepare_end]
    require(
        "ReferenceEquals(_publicationInFlightCandidate, candidate)" in prepare and "return false;" in prepare,
        "reentrant invocation must fail closed without closing a candidate while native publication is in flight",
    )
    require(
        prepare.find("ReferenceEquals(_publicationInFlightCandidate, candidate)") < prepare.find("CloseUnpublishedCandidate(candidate)"),
        "in-flight publication guard must run before any Close attempt",
    )

    require(
        "_cleanupInFlightCandidate" in source,
        "Audit Log must reserve the exact candidate while terminal Close is executing",
    )
    require(
        "if (_cleanupInFlightCandidate != null)" in prepare and prepare.find("if (_cleanupInFlightCandidate != null)") < prepare.find("var candidate = _unpublishedCandidate;"),
        "reentrant invocation must fail closed before reading singleton state while terminal Close is in flight",
    )

    published_start = source.find("private static bool PreparePublishedWindow")
    published_end = source.find("private static bool CloseUnpublishedCandidate", published_start)
    require(published_start >= 0 and published_end > published_start, "cannot bound published-window preparation helper")
    published = source[published_start:published_end]
    published_close_index = published.find("published.Close();")
    published_set_index = published.find("_cleanupInFlightCandidate = published;")
    published_finally_index = published.find("finally", published_close_index)
    published_clear_index = published.find("ReferenceEquals(_cleanupInFlightCandidate, published)", published_finally_index)
    require(
        published_set_index >= 0 and published_set_index < published_close_index,
        "cross-document published-window cleanup must reserve the exact window before Close can synchronously reenter",
    )
    require(
        published_finally_index > published_close_index and published_clear_index > published_finally_index,
        "cross-document published-window cleanup must retain its reservation until Close unwinds",
    )
    published_catch_index = published.find("catch", published_close_index)
    published_terminal_index = published.find("if (!published.IsLoaded)", published_catch_index, published_finally_index)
    published_terminal_release_index = published.find("ReleaseCandidate(published);", published_terminal_index, published_finally_index)
    published_terminal_success_index = published.find("return true;", published_terminal_release_index, published_finally_index)
    require(
        published_catch_index >= 0 and published_terminal_index > published_catch_index and published_terminal_release_index > published_terminal_index and published_terminal_success_index > published_terminal_release_index,
        "a Close exception after terminal Published-window closure must reconcile exact state and succeed instead of reporting a false UI failure",
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
    cleanup_set_index = helper.find("_cleanupInFlightCandidate = candidate;")
    cleanup_close_index = helper.find("candidate.Close();")
    cleanup_finally_index = helper.find("finally", cleanup_close_index)
    cleanup_clear_index = helper.find("ReferenceEquals(_cleanupInFlightCandidate, candidate)", cleanup_finally_index)
    require(
        cleanup_set_index >= 0 and cleanup_set_index < cleanup_close_index,
        "terminal cleanup must reserve the exact candidate before Close can synchronously reenter",
    )
    require(
        cleanup_finally_index > cleanup_close_index and cleanup_clear_index > cleanup_finally_index,
        "terminal cleanup must identity-release its reentrancy reservation only after Close unwinds",
    )
    require("_unpublishedCandidate = candidate;" in helper, "failed cleanup must quarantine the exact candidate")
    require("_window = candidate" not in helper, "cleanup helper must never publish an unproven candidate")

    release_start = source.find("private static void ReleaseCandidate")
    release_end = source.find("private static IntPtr GetNativeDatabaseIdentity", release_start)
    release = source[release_start:release_end]
    require(
        "_publicationInFlightCandidate" not in release,
        "synchronous Closed must not release publication-in-flight ownership before ShowModelessWindow returns",
    )
    require(
        "_cleanupInFlightCandidate" not in release,
        "Closed handler must not clear cleanup reentrancy reservation before Close returns",
    )
    require("ProjectContextCoordinator.GetOrCreate" not in source, "Audit Log command must keep project reads non-creating")

    print("PASS: Audit Log publication and terminal cleanup are atomic, reentrancy-safe, and terminal Close state is reconciled")


if __name__ == "__main__":
    main()
