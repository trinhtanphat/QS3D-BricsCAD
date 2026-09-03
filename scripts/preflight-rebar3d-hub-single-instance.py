#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Rebar3DHubCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str) -> None:
    if token not in text:
        errors.append("missing Rebar 3D Hub lifecycle token: " + token)


def forbid(token: str) -> None:
    if token in text:
        errors.append("forbidden Rebar 3D Hub lifecycle token remains: " + token)


for token in (
    "private static Rebar3DHubWindow? _pending;",
    "private static Rebar3DHubWindow? _published;",
    "Rebar3DHubWindow? candidate = null;",
    "var pending = _pending;",
    'CloseOwnerBeforeReplacement(pending, "pending");',
    "var published = _published;",
    "if (published.IsLoaded)",
    "published.Activate();",
    'CloseOwnerBeforeReplacement(published, "published");',
    "var window = new Rebar3DHubWindow();",
    "candidate = window;",
    "if (ReferenceEquals(_pending, window)) _pending = null;",
    "if (ReferenceEquals(_published, window)) _published = null;",
    "_pending = window;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, window))",
    "_pending = null;",
    "_published = window;",
    "candidate = null;",
    "private static void CloseOwnerBeforeReplacement(Rebar3DHubWindow window, string state)",
    "window.Close();",
    "if (window.IsLoaded || ReferenceEquals(_pending, window) || ReferenceEquals(_published, window))",
    "ex.GetType().Name",
):
    require(token)

for token in (
    "private static Rebar3DHubWindow? _window;",
    "var published = _window;",
    "ReleasePublishedWindow",
    "TryCloseUnpublishedWindow",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "+ ex.Message",
):
    forbid(token)

show_start = text.find("public void ShowRebarHub()")
cleanup_start = text.find("private static void CloseOwnerBeforeReplacement", show_start + 1)
show = text[show_start:cleanup_start] if show_start >= 0 and cleanup_start > show_start else ""

try:
    candidate = show.index("Rebar3DHubWindow? candidate = null;")
    pending_read = show.index("var pending = _pending;", candidate)
    pending_drain = show.index('CloseOwnerBeforeReplacement(pending, "pending");', pending_read)
    published_read = show.index("var published = _published;", pending_drain)
    construct = show.index("var window = new Rebar3DHubWindow();", published_read)
    candidate_assign = show.index("candidate = window;", construct)
    pending_assign = show.index("_pending = window;", candidate_assign)
    host_show = show.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending_assign)
    loaded = show.index("if (!window.IsLoaded)", host_show)
    exact = show.index("if (!ReferenceEquals(_pending, window))", loaded)
    clear = show.index("_pending = null;", exact)
    publish = show.index("_published = window;", clear)
    transfer = show.index("candidate = null;", publish)
    if not (candidate < pending_read < pending_drain < published_read < construct < candidate_assign < pending_assign < host_show < loaded < exact < clear < publish < transfer):
        errors.append("Rebar 3D Hub must drain pending before construction, own pending through show/load/exact proof, then transfer to published")
except ValueError as exc:
    errors.append("unable to prove Rebar 3D Hub pending/publication ordering: " + str(exc))

cleanup = text[cleanup_start:] if cleanup_start >= 0 else ""
close_pos = cleanup.find("window.Close();")
terminal_pos = cleanup.find("if (window.IsLoaded || ReferenceEquals(_pending, window) || ReferenceEquals(_published, window))", close_pos + 1)
refusal_pos = cleanup.find("replacement was refused", terminal_pos + 1)
if min(close_pos, terminal_pos, refusal_pos) < 0:
    errors.append("unable to prove fail-closed terminal cleanup contract")
elif not (close_pos < terminal_pos < refusal_pos):
    errors.append("Rebar 3D Hub replacement cleanup must prove terminal ownership release after Close before replacement may proceed")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Rebar 3D Hub reuses the loaded published owner, retains failed publication in exact pending ownership, and refuses replacement until cleanup is terminal.")
