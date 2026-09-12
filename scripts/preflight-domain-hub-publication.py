#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DomainHubCommands.cs"
text = SOURCE.read_text(encoding="utf-8-sig")

required = [
    "private static PublishedWindow? _pending;",
    "private static PublishedWindow? _published;",
    "PublishedWindow? owner = null;",
    "GetNativeDatabaseIdentity(document)",
    "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)",
    "PreparePublishedWindow(document, nativeDatabaseIdentity)",
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "owner = null;",
    "ClosePendingOnFailure(owner);",
    "private static void ReleaseOwnedWindow(PublishedWindow owner)",
]
missing = [token for token in required if token not in text]
if missing:
    raise SystemExit("Domain Hub generation-bound publication guard missing: " + ", ".join(missing))

for forbidden in (
    "private static DomainHubWindow? _window;",
    "private static DomainHubWindow? _pending;",
    "private static DomainHubWindow? _published;",
    '"\\nQS3DDOMAIN error: " + ex.Message',
):
    if forbidden in text:
        raise SystemExit("Domain Hub legacy/raw publication token remains: " + forbidden)

ordered = [
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "owner = null;",
]
pos = []
cursor = 0
for token in ordered:
    found = text.find(token, cursor)
    pos.append(found)
    if found >= 0:
        cursor = found + len(token)
if min(pos) < 0:
    raise SystemExit("Domain Hub owner must be rooted before show and published only after Loaded/exact-owner admission")

show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
post_generation = text.find("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)", show)
publish = text.find("_published = owner;", show)
if not (show < post_generation < publish):
    raise SystemExit("Domain Hub must revalidate exact document generation after host pumping and before publication")

print("PASS Domain Hub publication is generation-bound, pending-first, Loaded/exact-owner admitted, and redacted")
