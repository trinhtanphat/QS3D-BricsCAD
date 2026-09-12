#!/usr/bin/env python3
"""Deterministic guard for Auto Host post-commit exact document/database-generation affinity."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "AutoHostLinkCommands.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print(f"ERROR: {message}")
    sys.exit(1)


def method_body(name: str) -> str:
    signature = re.search(
        rf"(?:private|internal)\s+static\s+(?:bool|void|string|IntPtr)\s+{re.escape(name)}\s*\([^)]*\)\s*\{{",
        text,
        re.S,
    )
    if not signature:
        fail(f"cannot locate block-bodied static method: {name}")

    open_brace = text.find("{", signature.start())
    depth = 0
    for index in range(open_brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[open_brace + 1:index]
    fail(f"unterminated static method body: {name}")
    return ""


identity = method_body("GetNativeDatabaseIdentity")
if "document.Database?.UnmanagedObject ?? IntPtr.Zero" not in identity:
    fail("GetNativeDatabaseIdentity must capture the native database generation")

generation = method_body("IsActiveDocumentGeneration")
for token in (
    "nativeDatabaseIdentity == IntPtr.Zero",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
):
    if token not in generation:
        fail("IsActiveDocumentGeneration missing exact managed/native identity token: " + token)

refresh = method_body("TryRefreshProject")
if "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;" not in refresh or "PaletteCoordinator.RefreshProject();" not in refresh:
    fail("TryRefreshProject must fail closed for stale document/database generations before global Workspace refresh")

status = method_body("TrySetPaletteStatus")
if "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;" not in status or "PaletteCoordinator.SetStatus(message);" not in status:
    fail("TrySetPaletteStatus must fail closed for stale document/database generations before global status publication")

finalize = method_body("FinalizeAutoHostUi")
if "PaletteCoordinator.RefreshProject();" in finalize or "PaletteCoordinator.SetStatus(" in finalize:
    fail("FinalizeAutoHostUi must not publish process-global Workspace state directly")
for token in (
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "TryRefreshProject(document, nativeDatabaseIdentity);",
    "TrySetPaletteStatus(document, nativeDatabaseIdentity, summary);",
    "if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
):
    if token not in finalize:
        fail("FinalizeAutoHostUi missing exact document/database-generation fence: " + token)

report = method_body("ReportAutoHostError")
if "PaletteCoordinator.SetStatus(" in report:
    fail("ReportAutoHostError must not publish process-global status directly")
for token in (
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "TrySetPaletteStatus(document, nativeDatabaseIdentity, message);",
    "if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
):
    if token not in report:
        fail("ReportAutoHostError missing exact document/database-generation fence: " + token)

print("PASS: Auto Host Workspace/status/editor publication is fenced to the exact active source Document and native database generation")
