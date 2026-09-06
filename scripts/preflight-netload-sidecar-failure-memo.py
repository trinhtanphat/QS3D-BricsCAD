#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DocumentLifecycleCoordinator.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def forbid(text: str, needle: str, message: str) -> None:
    if needle in text:
        raise SystemExit("ERROR: " + message + " (forbidden: " + needle + ")")


def slice_method(source: str, start_token: str, end_token: str) -> str:
    start = source.find(start_token)
    end = source.find(end_token, start + len(start_token))
    if start < 0 or end < 0:
        raise SystemExit("ERROR: could not locate expected lifecycle method slice")
    return source[start:end]


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")

    required = (
        "Dictionary<Document, FailedProjectReconcile> FailedProjectReconciliations",
        "if (TryUseStableFailedProjectReconcile(document, refreshUi)) return;",
        "ProjectSidecarRevisionStamp? attemptedRevision = null;",
        "TryCaptureProjectRevision(document, out attemptedRevision);",
        "catch (InvalidDataException)",
        "RememberStableProjectLoadFailure(document, attemptedRevision, message);",
        "if (ProjectContextCoordinator.TryGetCached(document, out _))",
        "!failed.Revision.Equals(current)",
        "attemptedRevision == null || !attemptedRevision.HasAnyFile",
        "!attemptedRevision.Equals(current)",
        "ProjectSidecarRevisionStamp.Capture(ProjectContextCoordinator.GetProjectPath(document))",
        "FailedProjectReconciliations.Remove(document);",
        "FailedProjectReconciliations.Clear();",
        "DispatcherOperation? _lifecycleIdleOperation",
        "Dispatcher.CurrentDispatcher.BeginInvoke",
        "DispatcherPriority.ApplicationIdle",
        "new Action(OnLifecycleIdle)",
    )
    for token in required:
        require(source, token, "NETLOAD invalid-sidecar reconcile contract regressed")

    forbid(
        source,
        "DispatcherTimer",
        "lifecycle reconciliation must use a one-shot ApplicationIdle dispatcher operation rather than a timer",
    )
    forbid(
        source,
        "TimeSpan.FromMilliseconds(1d)",
        "NETLOAD lifecycle reconciliation must not retain the 1 ms timer cadence",
    )

    ensure = slice_method(
        source,
        "private static void EnsureProject(Document? document, bool refreshUi)",
        "private static bool TryUseStableFailedProjectReconcile",
    )
    skip = ensure.find("TryUseStableFailedProjectReconcile(document, refreshUi)")
    capture = ensure.find("TryCaptureProjectRevision(document, out attemptedRevision)")
    readonly = ensure.find("ProjectContextCoordinator.TryGetReadOnly(document, out _)")
    remember = ensure.find("RememberStableProjectLoadFailure(document, attemptedRevision, message)")
    if min(skip, capture, readonly, remember) < 0 or not (skip < capture < readonly < remember):
        raise SystemExit(
            "ERROR: lifecycle must check the stable failure memo before capturing/reading the sidecar, "
            "then memoize only after the failed read attempt"
        )
    forbid(
        ensure,
        "Editor.WriteMessage",
        "automatic startup/activation project reconciliation must not print sidecar load failures to the command line",
    )
    require(
        ensure,
        "PaletteCoordinator.ResetForUnavailableProject(message)",
        "automatic sidecar load failures must remain visible through unavailable-project Palette state",
    )

    stable = slice_method(
        source,
        "private static bool TryUseStableFailedProjectReconcile",
        "private static void RememberStableProjectLoadFailure",
    )
    require(
        stable,
        "ProjectContextCoordinator.TryGetCached(document, out _)",
        "successful explicit reload/cache must invalidate a previous lifecycle failure memo",
    )
    require(
        stable,
        "!failed.Revision.Equals(current)",
        "a changed .qsdb/.bak generation must force a fresh lifecycle read attempt",
    )
    require(
        stable,
        "if (refreshUi)",
        "cached lifecycle failures must still be able to refresh the unavailable-project UI",
    )
    forbid(
        stable,
        "Editor.WriteMessage",
        "cached lifecycle failures must not repeat the command-line project-load diagnostic",
    )

    remember_method = slice_method(
        source,
        "private static void RememberStableProjectLoadFailure",
        "private static bool TryCaptureProjectRevision",
    )
    require(
        remember_method,
        "attemptedRevision == null || !attemptedRevision.HasAnyFile",
        "only a real existing sidecar generation may be memoized as a failed read",
    )
    require(
        remember_method,
        "!attemptedRevision.Equals(current)",
        "a sidecar that changes during the failed read must not be memoized",
    )

    forbid(
        ensure,
        "ProjectContextCoordinator.GetOrCreate",
        "lifecycle recovery from an unreadable sidecar must never create a replacement project",
    )
    forbid(
        ensure,
        "ProjectContextCoordinator.Save(",
        "read-only lifecycle reconciliation must never overwrite an unreadable sidecar",
    )

    print(
        "PASS: lifecycle uses one-shot ApplicationIdle reconciliation, keeps automatic sidecar failures out of the command line, "
        "memoizes only stable unreadable generations, retries changed/explicitly reloaded projects, and remains fail-closed/read-only."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
