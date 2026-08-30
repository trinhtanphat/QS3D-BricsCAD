#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "DomainHubCommands.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DomainHubWindow.xaml.cs"
commands = COMMANDS.read_text(encoding="utf-8")
window = WINDOW.read_text(encoding="utf-8")
errors = []


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"missing {label} token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"forbidden {label} token remains: {token}")


for token in (
    "private static DomainHubWindow? _window;",
    "var published = _window;",
    "if (published != null)",
    "if (published.IsLoaded)",
    "ReleasePublishedWindow(published);",
    "var window = new DomainHubWindow();",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "private static void ReleasePublishedWindow(DomainHubWindow window)",
    "if (!ReferenceEquals(_window, window)) return;",
    "_window = null;",
):
    require(commands, token, "Domain Hub publication lifecycle")

for token in (
    "_window = new DomainHubWindow();",
    "_window.Closed += (_, __) => _window = null;",
    "window.Closed += (_, __) => _window = null;",
):
    forbid(commands, token, "Domain Hub publication lifecycle")

show_start = commands.find("public void ShowDomainHub()")
release_start = commands.find("private static void ReleasePublishedWindow", show_start + 1)
show = commands[show_start:release_start] if show_start >= 0 and release_start > show_start else ""
positions = [
    show.find("var published = _window;"),
    show.find("var window = new DomainHubWindow();"),
    show.find("window.Closed += (_, __) => ReleasePublishedWindow(window);"),
    show.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);"),
    show.find("if (!window.IsLoaded) return;"),
    show.find("_window = window;"),
]
if min(positions) < 0:
    errors.append("unable to prove Domain Hub transactional publication ordering")
elif positions != sorted(positions):
    errors.append("Domain Hub must inspect prior owner -> construct candidate -> attach exact Closed callback -> show -> confirm loaded -> publish")

release = commands[release_start:] if release_start >= 0 else ""
owner_check = release.find("if (!ReferenceEquals(_window, window)) return;")
clear = release.find("_window = null;", owner_check + 1)
if owner_check < 0 or clear < 0 or owner_check >= clear:
    errors.append("Domain Hub terminal release must verify exact published owner before clearing publication")

# The Domain Hub is intentionally host-global: it must not retain a managed Document.
for token in (
    "public DomainHubWindow()",
    "var document = Application.DocumentManager.MdiActiveDocument;",
    "document.SendStringToExecute(command + \" \", true, false, false);",
):
    require(window, token, "Domain Hub active-document dispatch")

for token in (
    "DomainHubWindow(Document",
    "private readonly Document",
    "private Document _document",
):
    forbid(window, token, "Domain Hub retained-document")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Domain Hub publication is post-show, loaded-only, exact-owner released, and remains host-global with active-document dispatch.")
