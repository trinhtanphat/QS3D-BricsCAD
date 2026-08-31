#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DomainHubCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private static DomainHubWindow? _published;",
    "private static DomainHubWindow? _pending;",
    "var pending = _pending;",
    "pending != null && !TryClosePendingWindow(pending)",
    "candidate = new DomainHubWindow();",
    "_pending = window;",
    "window.Closed += (_, __) => ReleaseWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
    "candidate = null;",
    "if (candidate != null)",
    "TryClosePendingWindow(candidate);",
    "if (!ReferenceEquals(_pending, window)) return true;",
    "if (ReferenceEquals(_published, window))",
    "if (window.IsLoaded) return false;",
    "ReportStatus(document, \"QS3DDOMAIN lỗi khi mở cửa sổ.\");",
    "ex.GetType().Name",
]

missing = [token for token in required if token not in text]
if missing:
    raise SystemExit("Domain Hub publication guard missing tokens: " + ", ".join(missing))

for forbidden in [
    '"\\nQS3DDOMAIN error: " + ex.Message',
    "_window",
]:
    if forbidden in text:
        raise SystemExit(f"Domain Hub publication guard found forbidden legacy token: {forbidden}")

ordered = [
    "var pending = _pending;",
    "candidate = new DomainHubWindow();",
    "_pending = window;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
]
positions = [text.find(token) for token in ordered]
if positions != sorted(positions) or any(position < 0 for position in positions):
    raise SystemExit("Domain Hub publication ownership ordering changed")

release_pending_position = positions[-1]
cleanup_transfer_position = text.find(
    "candidate = null;",
    release_pending_position + len("ReleasePendingWindow(window);"),
)
if cleanup_transfer_position < 0 or cleanup_transfer_position <= release_pending_position:
    raise SystemExit("Domain Hub local cleanup ownership must transfer only after pending ownership is released")

close_body_start = text.find("private static bool TryClosePendingWindow")
close_body_end = text.find("private static void ReportStatus", close_body_start)
close_body = text[close_body_start:close_body_end]
for token in [
    "if (!ReferenceEquals(_pending, window)) return true;",
    "if (ReferenceEquals(_published, window))",
    "try { window.Close(); } catch (Exception) { }",
    "if (window.IsLoaded) return false;",
    "ReleasePendingWindow(window);",
]:
    if token not in close_body:
        raise SystemExit(f"Domain Hub pending-close guard missing: {token}")

print("PASS Domain Hub failed-publication ownership is duplicate-safe and redacted")
