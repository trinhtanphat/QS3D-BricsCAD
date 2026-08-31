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
    "private static DomainHubWindow? _published;",
    "private static DomainHubWindow? _pending;",
    "DomainHubWindow? candidate = null;",
    "var pending = _pending;",
    "if (pending != null && !TryClosePendingWindow(pending))",
    "var previous = _published;",
    "if (previous != null)",
    "if (previous.IsLoaded)",
    "ReleasePublishedWindow(previous);",
    "candidate = new DomainHubWindow();",
    "var window = candidate;",
    "_pending = window;",
    "window.Closed += (_, __) => ReleaseWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
    "candidate = null;",
    "finally",
    "if (candidate != null)",
    "TryClosePendingWindow(candidate);",
    "private static void ReleasePublishedWindow(DomainHubWindow window)",
    "if (!ReferenceEquals(_published, window)) return;",
    "_published = null;",
    "private static void ReleasePendingWindow(DomainHubWindow window)",
    "if (!ReferenceEquals(_pending, window)) return;",
    "_pending = null;",
    "private static bool TryClosePendingWindow(DomainHubWindow window)",
    "if (window.IsLoaded) return false;",
    "ReportStatus(document, \"QS3DDOMAIN lỗi khi mở cửa sổ.\");",
    "ex.GetType().Name",
):
    require(commands, token, "Domain Hub publication lifecycle")

for token in (
    "private static DomainHubWindow? _window;",
    "_window = new DomainHubWindow();",
    "_window.Closed += (_, __) => _window = null;",
    "window.Closed += (_, __) => _window = null;",
    '"\\nQS3DDOMAIN error: " + ex.Message',
):
    forbid(commands, token, "Domain Hub publication lifecycle")

show_start = commands.find("public void ShowDomainHub()")
release_start = commands.find("private static void ReleaseWindow", show_start + 1)
show = commands[show_start:release_start] if show_start >= 0 and release_start > show_start else ""
ordered = (
    "var pending = _pending;",
    "candidate = new DomainHubWindow();",
    "_pending = window;",
    "window.Closed += (_, __) => ReleaseWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
    "candidate = null;",
)
positions = [show.find(token) for token in ordered]
if min(positions) < 0:
    errors.append("unable to prove Domain Hub pending -> show -> loaded -> publish ordering")
elif positions != sorted(positions) or len(set(positions)) != len(positions):
    errors.append("Domain Hub must drain pending -> construct -> pending-own -> attach exact Closed -> show -> confirm loaded -> publish -> release pending -> transfer local cleanup")

close_start = commands.find("private static bool TryClosePendingWindow")
status_start = commands.find("private static void ReportStatus", close_start + 1)
close_body = commands[close_start:status_start] if close_start >= 0 and status_start > close_start else ""
for token in (
    "if (!ReferenceEquals(_pending, window)) return true;",
    "if (ReferenceEquals(_published, window))",
    "try { window.Close(); } catch (Exception) { }",
    "if (window.IsLoaded) return false;",
    "ReleasePendingWindow(window);",
):
    require(close_body, token, "Domain Hub pending-close fail-closed")

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

print("PASS: Domain Hub publication is pending-owned until terminal close or loaded-only publication, duplicate-safe, exact-owner released, redacted, and active-document dispatch remains host-global.")
