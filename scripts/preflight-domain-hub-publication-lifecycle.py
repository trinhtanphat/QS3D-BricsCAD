#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/DomainHubCommands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml.cs"
commands = COMMANDS.read_text(encoding="utf-8-sig")
window = WINDOW.read_text(encoding="utf-8-sig")
errors = []

def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")

def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"forbidden {label}: {token}")

for token in (
    "private static PublishedWindow? _pending;",
    "private static PublishedWindow? _published;",
    "GetNativeDatabaseIdentity(document)",
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
    "try { owner.Window.Close(); } catch { }",
    "if (!owner.Window.IsLoaded) ReleaseOwnedWindow(owner);",
    "if (published.NativeDatabaseIdentity == requestedNativeDatabaseIdentity &&",
    "ReferenceEquals(published.Document, requestedDocument)",
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
):
    require(commands, token, "generation-bound Domain Hub lifecycle token")

for token in (
    "private static DomainHubWindow? _window;",
    "private static DomainHubWindow? _pending;",
    "private static DomainHubWindow? _published;",
    '"\\nQS3DDOMAIN error: " + ex.Message',
):
    forbid(commands, token, "legacy/raw Domain Hub lifecycle token")

ordered = (
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
)
positions = [commands.find(token) for token in ordered]
if min(positions) < 0 or positions != sorted(positions) or len(set(positions)) != len(positions):
    errors.append("Domain Hub candidate must be generation-owned, Closed-bound and pending-rooted before host show")

show = commands.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
post = commands.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show)
loaded = commands.find("if (!window.IsLoaded)", post)
exact = commands.find("if (!ReferenceEquals(_pending, owner))", loaded)
clear_pending = commands.find("_pending = null;", exact)
publish = commands.find("_published = owner;", clear_pending)
release_local = commands.find("owner = null;", publish)
if min(show, post, loaded, exact, clear_pending, publish, release_local) < 0 or not (
    show < post < loaded < exact < clear_pending < publish < release_local
):
    errors.append("Domain Hub must revalidate generation then Loaded/exact-owner before pending-to-published transfer")

for token in (
    "public DomainHubWindow()",
    "Application.DocumentManager.MdiActiveDocument",
    'document.SendStringToExecute(command + " ", true, false, false);',
):
    require(window, token, "click-time active-document dispatch")
for token in ("DomainHubWindow(Document", "private readonly Document", "private Document _document"):
    forbid(window, token, "retained-document state")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Domain Hub lifecycle is exact-generation bound, pending-first, veto-safe, duplicate-safe, and click dispatch stays host-global.")
