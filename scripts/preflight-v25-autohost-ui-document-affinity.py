#!/usr/bin/env python3
"""Deterministic guard for Auto Host post-commit Workspace/document affinity."""

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
        rf"(?:private|internal)\s+static\s+(?:bool|void|string)\s+{re.escape(name)}\s*\([^)]*\)\s*\{{",
        text,
        re.S,
    )
    if not signature:
        fail(f"cannot locate static method body: {name}")

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


if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in text:
    fail("Auto Host must fence process-global Workspace publication to the exact active source Document")

is_active = method_body("IsActiveDocument")
if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in is_active:
    fail("IsActiveDocument must use exact native Document identity")

refresh = method_body("TryRefreshProject")
if "if (!IsActiveDocument(document)) return;" not in refresh or "PaletteCoordinator.RefreshProject();" not in refresh:
    fail("TryRefreshProject must fail closed for stale source documents before global Workspace refresh")

status = method_body("TrySetPaletteStatus")
if "if (!IsActiveDocument(document)) return;" not in status or "PaletteCoordinator.SetStatus(message);" not in status:
    fail("TrySetPaletteStatus must fail closed for stale source documents before global status publication")

finalize = method_body("FinalizeAutoHostUi")
if "PaletteCoordinator.RefreshProject();" in finalize or "PaletteCoordinator.SetStatus(" in finalize:
    fail("FinalizeAutoHostUi must not publish process-global Workspace state directly")
if "TryRefreshProject(document);" not in finalize:
    fail("FinalizeAutoHostUi must route refresh through the exact-document affinity fence")
if "TrySetPaletteStatus(document, summary);" not in finalize:
    fail("FinalizeAutoHostUi must route status through the exact-document affinity fence")

report = method_body("ReportAutoHostError")
if "PaletteCoordinator.SetStatus(" in report:
    fail("ReportAutoHostError must not publish process-global status directly")
if "TrySetPaletteStatus(document, message);" not in report:
    fail("ReportAutoHostError must route status through the exact-document affinity fence")

print("PASS: Auto Host Workspace/status publication is fenced to the exact active source Document")
