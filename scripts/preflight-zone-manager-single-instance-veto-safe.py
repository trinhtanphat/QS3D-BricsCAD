#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs"
source = COMMAND.read_text(encoding="utf-8") if COMMAND.exists() else ""
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


require("private static PublishedManager? _pending;" in source,
        "Zone Manager must retain an unpublished pending owner across failed publication")
require("private static PublishedManager? _published;" in source,
        "Zone Manager must retain one published modeless owner")
for token in (
    "private readonly WeakReference<Document> _document;",
    "public ZoneManagerWindow Window { get; }",
    "public IntPtr NativeDatabaseIdentity { get; }",
    "database.UnmanagedObject == NativeDatabaseIdentity",
    "public bool Matches(Document document)",
    "public bool MatchesManagedWrapper(Document document)",
    "_document.TryGetTarget(out var ownedDocument)",
    "ReferenceEquals(ownedDocument, document)",
):
    require(token in source, "published owner missing native/wrapper provenance token: " + token)
require("public Document Document { get; }" not in source,
        "static publication metadata must not add a strong managed Document owner")
require("ex.Message" not in source,
        "Zone Manager user-visible failure paths must not expose raw host exception messages")

try:
    method_start = source.index("public void ShowZoneManager()")
    pending_capture = source.index("var pending = _pending;", method_start)
    pending_close = source.index('CloseOwnerBeforeReplacement(pending, "pending")', pending_capture)
    published_capture = source.index("var previous = _published;", pending_close)
    live = source.index("previous.Window.IsLoaded", published_capture)
    native_match = source.index("previous.Matches(document)", live)
    wrapper_match = source.index("previous.MatchesManagedWrapper(document)", native_match)
    activate = source.index("previous.Window.Activate();", wrapper_match)
    same_return = source.index("return;", activate)
    published_close = source.index('CloseOwnerBeforeReplacement(previous, "published")', same_return)
    construct = source.index("var window = new ZoneManagerWindow(document);", published_close)
    owner_construct = source.index("var owner = new PublishedManager(window, document);", construct)
    candidate_own = source.index("candidate = owner;", owner_construct)
    closed_handler = source.index("window.Closed +=", candidate_own)
    closed_pending_release = source.index("if (ReferenceEquals(_pending, owner)) _pending = null;", closed_handler)
    closed_published_release = source.index("if (ReferenceEquals(_published, owner)) _published = null;", closed_pending_release)
    pending_publish = source.index("_pending = owner;", closed_published_release)
    show = source.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending_publish)
    loaded_check = source.index("if (!window.IsLoaded)", show)
    exact_owner_check = source.index("if (!ReferenceEquals(_pending, owner))", loaded_check)
    pending_release = source.index("_pending = null;", exact_owner_check)
    publish = source.index("_published = owner;", pending_release)
    candidate_release = source.index("candidate = null;", publish)
    require(
        method_start < pending_capture < pending_close < published_capture < live < native_match < wrapper_match < activate < same_return < published_close < construct < owner_construct < candidate_own < closed_handler < closed_pending_release < closed_published_release < pending_publish < show < loaded_check < exact_owner_check < pending_release < publish < candidate_release,
        "pending-first native/wrapper arbitration and loaded-only publication ordering is unsafe",
    )
except ValueError as exc:
    errors.append("Zone Manager pending/publication structure is incomplete: " + str(exc))

require("if (previous.Matches(document))\n                        {" not in source,
        "native-database identity alone must not reuse a window bound to an older managed wrapper")
require("try { previous.Window.Close(); } catch { }" not in source,
        "replacement close failures must not be swallowed")

try:
    helper = source.index("private static void CloseOwnerBeforeReplacement")
    stale_published = source.index('string.Equals(state, "published", StringComparison.Ordinal)', helper)
    stale_release = source.index("if (ReferenceEquals(_published, owner)) _published = null;", stale_published)
    close = source.index("owner.Window.Close();", stale_release)
    terminal = source.index("owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)", close)
    refusal = source.index("owner did not reach terminal close; replacement was refused.", terminal)
    require(helper < stale_published < stale_release < close < terminal < refusal,
            "replacement cleanup must prove terminal close and fail closed while ownership remains")
except ValueError as exc:
    errors.append("Zone Manager replacement cleanup structure is incomplete: " + str(exc))

try:
    catch_start = source.index("catch (Exception ex)", method_start)
    exact_candidate = source.index("candidate != null && ReferenceEquals(_pending, candidate)", catch_start)
    cleanup_close = source.index("candidate.Window.Close();", exact_candidate)
    redacted = source.index("ex.GetType().Name", cleanup_close)
    require(catch_start < exact_candidate < cleanup_close < redacted,
            "failed publication must clean only the exact pending owner before type-only reporting")
except ValueError as exc:
    errors.append("Zone Manager failed-publication cleanup structure is incomplete: " + str(exc))

if errors:
    print("Zone Manager single-instance veto-safe preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Zone Manager retains exact pending/published ownership, reuses only the exact wrapper, and replaces only after terminal close")
