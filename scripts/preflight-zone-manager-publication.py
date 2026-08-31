#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs"

if not SOURCE.is_file():
    print("ERROR: missing Zone Manager command source")
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static PublishedManager? _pending;",
    "private static PublishedManager? _published;",
    'CloseOwnerBeforeReplacement(pending, "pending")',
    "previous.Matches(document)",
    "previous.MatchesManagedWrapper(document)",
    'CloseOwnerBeforeReplacement(previous, "published")',
    "var window = new ZoneManagerWindow(document);",
    "var owner = new PublishedManager(window, document);",
    "if (ReferenceEquals(_pending, owner)) _pending = null;",
    "if (ReferenceEquals(_published, owner)) _published = null;",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_published = owner;",
    "candidate != null && ReferenceEquals(_pending, candidate)",
    "candidate.Window.Close();",
    'string.Equals(state, "published", StringComparison.Ordinal)',
    "owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)",
    "ex.GetType().Name",
]
for marker in required:
    if marker not in source:
        errors.append("Zone Manager publication contract missing: " + marker)

if "ex.Message" in source:
    errors.append("Zone Manager user-visible failure paths must not expose raw exception messages")

try:
    pending_drain = source.index('CloseOwnerBeforeReplacement(pending, "pending")')
    published_read = source.index("var previous = _published;")
    construct = source.index("var window = new ZoneManagerWindow(document);")
    owner_construct = source.index("var owner = new PublishedManager(window, document);", construct)
    closed_handler = source.index("window.Closed +=", owner_construct)
    pending_own = source.index("_pending = owner;", closed_handler)
    host_show = source.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending_own)
    loaded_check = source.index("if (!window.IsLoaded)", host_show)
    owner_check = source.index("if (!ReferenceEquals(_pending, owner))", loaded_check)
    transfer_pending = source.index("_pending = null;", owner_check)
    transfer_published = source.index("_published = owner;", transfer_pending)
    candidate_release = source.index("candidate = null;", transfer_published)
    if not (pending_drain < published_read < construct < owner_construct < closed_handler < pending_own < host_show < loaded_check < owner_check < transfer_pending < transfer_published < candidate_release):
        errors.append("Zone Manager pending/publication transfer ordering is unsafe")
except ValueError as exc:
    errors.append("Zone Manager publication ordering marker missing: " + str(exc))

try:
    helper = source.index("private static void CloseOwnerBeforeReplacement")
    stale_repair = source.index('string.Equals(state, "published", StringComparison.Ordinal)', helper)
    close = source.index("owner.Window.Close();", helper)
    terminal = source.index("owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)", close)
    if not (helper < stale_repair < close < terminal):
        errors.append("Zone Manager replacement cleanup must reserve stale repair for published owners and prove terminal close")
except ValueError as exc:
    errors.append("Zone Manager cleanup ordering marker missing: " + str(exc))

try:
    catch_start = source.index("catch (Exception ex)", source.index("public void ShowZoneManager"))
    guarded_cleanup = source.index("candidate != null && ReferenceEquals(_pending, candidate)", catch_start)
    cleanup_close = source.index("candidate.Window.Close();", guarded_cleanup)
    redacted_message = source.index("ex.GetType().Name", cleanup_close)
    if not (catch_start < guarded_cleanup < cleanup_close < redacted_message):
        errors.append("Zone Manager failed-publication cleanup/redaction ordering is unsafe")
except ValueError as exc:
    errors.append("Zone Manager catch ordering marker missing: " + str(exc))

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Zone Manager failed-publication ownership is pending-first, loaded-only, exact-release and host-error-redacted.")
