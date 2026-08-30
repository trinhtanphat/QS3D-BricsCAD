#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Rebar3DHubCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(token: str) -> None:
    if token not in text:
        errors.append("missing Rebar 3D Hub single-instance token: " + token)


def forbid(token: str) -> None:
    if token in text:
        errors.append("forbidden Rebar 3D Hub lifecycle token remains: " + token)


for token in (
    "private static Rebar3DHubWindow? _window;",
    "var published = _window;",
    "if (published.IsLoaded)",
    "published.Activate();",
    "ReleasePublishedWindow(published);",
    "var window = new Rebar3DHubWindow();",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "private static void ReleasePublishedWindow(Rebar3DHubWindow window)",
    "if (!ReferenceEquals(_window, window)) return;",
    "_window = null;",
):
    require(token)

for token in (
    "var window = new Rebar3DHubWindow();\n                Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "_window = new Rebar3DHubWindow();",
):
    forbid(token)

show_start = text.find("public void ShowRebarHub()")
release_start = text.find("private static void ReleasePublishedWindow", show_start + 1)
show = text[show_start:release_start] if show_start >= 0 and release_start > show_start else ""

published_pos = show.find("var published = _window;")
loaded_owner_pos = show.find("if (published.IsLoaded)", published_pos + 1)
activate_pos = show.find("published.Activate();", loaded_owner_pos + 1)
return_pos = show.find("return;", activate_pos + 1)
stale_release_pos = show.find("ReleasePublishedWindow(published);", return_pos + 1)
construct_pos = show.find("var window = new Rebar3DHubWindow();", stale_release_pos + 1)
closed_pos = show.find("window.Closed += (_, __) => ReleasePublishedWindow(window);", construct_pos + 1)
show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);", closed_pos + 1)
loaded_candidate_pos = show.find("if (!window.IsLoaded) return;", show_pos + 1)
publish_pos = show.find("_window = window;", loaded_candidate_pos + 1)

positions = (
    published_pos,
    loaded_owner_pos,
    activate_pos,
    return_pos,
    stale_release_pos,
    construct_pos,
    closed_pos,
    show_pos,
    loaded_candidate_pos,
    publish_pos,
)
if min(positions) < 0:
    errors.append("unable to prove Rebar 3D Hub reuse/stale-release/publication ordering")
elif list(positions) != sorted(positions):
    errors.append(
        "Rebar 3D Hub must reuse a live owner, clear only stale ownership, then construct -> attach Closed -> show -> confirm loaded -> publish"
    )

release = text[release_start:] if release_start >= 0 else ""
match_pos = release.find("if (!ReferenceEquals(_window, window)) return;")
clear_pos = release.find("_window = null;", match_pos + 1)
if match_pos < 0 or clear_pos < 0 or match_pos >= clear_pos:
    errors.append("Rebar 3D Hub terminal release must clear only the matching published owner")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: Rebar 3D Hub is application-wide single-instance, reuses the live owner, clears stale ownership, and publishes only a loaded candidate with exact terminal Closed release."
)
