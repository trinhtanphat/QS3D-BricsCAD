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
    "private static Rebar3DHubWindow? _window;",
    "Rebar3DHubWindow? candidate = null;",
    "var published = _window;",
    "if (published.IsLoaded)",
    "published.Activate();",
    "ReleasePublishedWindow(published);",
    "candidate = new Rebar3DHubWindow();",
    "var window = candidate;",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "candidate = null;",
    "finally",
    "if (candidate != null) TryCloseUnpublishedWindow(candidate);",
    "private static void ReleasePublishedWindow(Rebar3DHubWindow window)",
    "if (!ReferenceEquals(_window, window)) return;",
    "_window = null;",
    "private static void TryCloseUnpublishedWindow(Rebar3DHubWindow window)",
    "if (ReferenceEquals(_window, window)) return;",
    "try { window.Close(); } catch (System.Exception) { }",
):
    require(token)

for token in (
    "var window = new Rebar3DHubWindow();",
    "_window = new Rebar3DHubWindow();",
):
    forbid(token)

show_start = text.find("public void ShowRebarHub()")
release_start = text.find("private static void ReleasePublishedWindow", show_start + 1)
show = text[show_start:release_start] if show_start >= 0 and release_start > show_start else ""

publish_pos = show.find("_window = window;")
transfer_pos = show.find("candidate = null;", publish_pos + 1) if publish_pos >= 0 else -1
positions = [
    show.find("Rebar3DHubWindow? candidate = null;"),
    show.find("var published = _window;"),
    show.find("ReleasePublishedWindow(published);"),
    show.find("candidate = new Rebar3DHubWindow();"),
    show.find("var window = candidate;"),
    show.find("window.Closed += (_, __) => ReleasePublishedWindow(window);"),
    show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);"),
    show.find("if (!window.IsLoaded) return;"),
    publish_pos,
    transfer_pos,
    show.find("finally"),
    show.find("if (candidate != null) TryCloseUnpublishedWindow(candidate);"),
]
if min(positions) < 0:
    errors.append("unable to prove Rebar 3D Hub cleanup/publication ordering")
elif positions != sorted(positions):
    errors.append("Rebar 3D Hub candidate must remain cleanup-owned through show/load, transfer only after publication, then finally cleanup only when still unpublished")

release = text[release_start:] if release_start >= 0 else ""
match_pos = release.find("if (!ReferenceEquals(_window, window)) return;")
clear_pos = release.find("_window = null;", match_pos + 1)
cleanup_start = release.find("private static void TryCloseUnpublishedWindow", clear_pos + 1)
refuse_pos = release.find("if (ReferenceEquals(_window, window)) return;", cleanup_start + 1)
close_pos = release.find("window.Close();", refuse_pos + 1)
if min(match_pos, clear_pos, cleanup_start, refuse_pos, close_pos) < 0:
    errors.append("unable to prove exact published release and unpublished cleanup refusal")
elif not (match_pos < clear_pos < cleanup_start < refuse_pos < close_pos):
    errors.append("cleanup helper must refuse the authoritative published owner before best-effort close")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Rebar 3D Hub reuses the loaded owner, publishes only after host load, and terminally closes only still-unpublished failed candidates.")
