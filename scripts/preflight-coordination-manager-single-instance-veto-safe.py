#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"
source = COMMAND.read_text(encoding="utf-8") if COMMAND.exists() else ""
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


require("private static PublishedManager? _published;" in source,
        "manager ownership must retain one published owner")
require("private static PublishedManager? _publicationInFlight;" in source,
        "manager must reserve one exact unpublished owner across native publication")
require("private static PublishedManager? _cleanupInFlight;" in source,
        "manager must reserve one exact cleanup owner across synchronous Close unwind")
require("private static bool _nativePublicationCallActive;" in source,
        "manager must fence synchronous native publication reentrancy")
for token in (
    "public CoordinationManagerWindow Window { get; }",
    "public IntPtr NativeDatabaseIdentity { get; }",
    "public string ProjectId { get; }",
    "public string DrawingFingerprint { get; }",
    "database.UnmanagedObject == NativeDatabaseIdentity",
    "string.Equals(ProjectId, projectId, StringComparison.Ordinal)",
    "string.Equals(DrawingFingerprint, drawingFingerprint, StringComparison.Ordinal)",
    "public bool Matches(Document document, string projectId, string drawingFingerprint)",
):
    require(token in source, "published owner missing wrapper/project-affinity token: " + token)
require("public Document Document { get; }" not in source,
        "published modeless ownership must not retain a managed Document wrapper across lifetime")

show_start = source.find("public void ShowCoordinationManager()")
show_end = source.find("private static bool PrepareUnpublishedCandidate()", show_start)
require(show_start >= 0 and show_end > show_start, "ShowCoordinationManager method was not found/bounded")
if show_start >= 0 and show_end > show_start:
    body = source[show_start:show_end]

    blocked = body.find("if (_nativePublicationCallActive || _cleanupInFlight != null)")
    prepare = body.find("if (!PrepareUnpublishedCandidate())")
    capture = body.find("var previous = _published;")
    live = body.find("if (previous.Window.IsLoaded)")
    same_context = body.find("if (previous.Matches(document, project.ProjectId, project.DrawingFingerprint))")
    activate = body.find("previous.Window.Activate();")
    same_return = body.find("return;", activate if activate >= 0 else 0)
    close = body.find("if (!TryCloseManager(previous))")
    active_fence = body.find("RequireActiveDocument(document);", close if close >= 0 else 0)
    construct = body.find("candidate = new CoordinationManagerWindow", active_fence if active_fence >= 0 else 0)

    require(min(blocked, prepare, capture, live, same_context, activate, same_return, close, active_fence, construct) >= 0,
            "single-instance reuse/cross-context arbitration structure is incomplete")
    require(
        blocked < prepare < capture < live < same_context < activate < same_return < close < active_fence < construct,
        "reentrant block, stale unpublished cleanup, same-context activation, terminal close proof, and active-document fence must all precede candidate construction",
    )

    require("ReferenceEquals(previous.Document, document)" not in body,
            "same-context reuse must not depend on managed Document wrapper identity")
    require("_published = null;\n" not in body[:construct],
            "published ownership must never be pre-cleared before terminal close proof")
    require("try { previous.Window.Close(); } catch { }" not in body,
            "cross-context close failures must not be swallowed")
    require("Previous Coordination Manager did not reach terminal Closed." in body,
            "vetoed/non-terminal cross-context close must fail closed")
    require("_publicationInFlight = exactPublished;" in body,
            "new exact candidate must reserve singleton ownership before native show")
    require("_nativePublicationCallActive = true;" in body,
            "native show stack must be reentrancy-fenced")
    require("_published = exactPublished;" in body,
            "successful exact candidate must transition to published ownership")
    require("_publicationInFlight = null;" in body,
            "publication reservation must release only after published ownership commits")

    reserve = body.find("_publicationInFlight = exactPublished;")
    native_fence = body.find("_nativePublicationCallActive = true;", reserve)
    show = body.find("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);", native_fence)
    semantic = body.find("ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)", show)
    publish = body.find("_published = exactPublished;", semantic)
    release = body.find("_publicationInFlight = null;", publish)
    require(min(reserve, native_fence, show, semantic, publish, release) >= 0 and
            reserve < native_fence < show < semantic < publish < release,
            "exact unpublished ownership must span native show and semantic revalidation until published ownership commits")

close_start = source.find("private static bool TryCloseManager(PublishedManager manager)")
close_end = source.find("private static void ReleaseClosedManager", close_start)
require(close_start >= 0 and close_end > close_start, "exact manager close helper was not found/bounded")
if close_start >= 0 and close_end > close_start:
    cleanup = source[close_start:close_end]
    reserve = cleanup.find("_cleanupInFlight = manager;")
    close = cleanup.find("manager.Window.Close();")
    loaded = cleanup.find("if (manager.Window.IsLoaded)")
    release_terminal = cleanup.find("ReleaseTerminalManager(manager);")
    exact_release = cleanup.find("if (ReferenceEquals(_cleanupInFlight, manager))")
    require(min(reserve, close, loaded, release_terminal, exact_release) >= 0,
            "cleanup helper is missing exact reservation/close/terminal-release structure")
    require(reserve < close < loaded < release_terminal < exact_release,
            "cleanup reservation must survive Close and terminal evidence before exact reservation release")
    require("return false;" in cleanup,
            "vetoed or still-loaded close must remain fail closed")

closed_start = source.find("private static void ReleaseClosedManager(PublishedManager manager)")
closed_end = source.find("private static void ReleaseTerminalManager", closed_start)
require(closed_start >= 0 and closed_end > closed_start, "instance-safe Closed ownership release helper was not found")
if closed_start >= 0 and closed_end > closed_start:
    closed = source[closed_start:closed_end]
    require("if (ReferenceEquals(_published, manager))" in closed and "_published = null;" in closed,
            "terminal Closed may release only the same published owner")
    require("_publicationInFlight = null;" not in closed and "_cleanupInFlight = null;" not in closed,
            "synchronous Closed must not steal outer-stack publication/cleanup reservations")

if errors:
    print("Coordination Manager single-instance veto-safe preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print(
    "PASS Coordination Manager keeps exact published/in-flight/cleanup ownership, reuses only the same native+project context, and fails closed on vetoed or reentrant replacement."
)
