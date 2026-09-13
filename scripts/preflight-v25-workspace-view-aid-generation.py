#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ViewAids.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []


def require(pattern: str, message: str) -> None:
    if re.search(pattern, text, flags=re.MULTILINE | re.DOTALL) is None:
        errors.append(message)


# Workspace viewport aids are modeless and document-scoped. One shared native
# read -> write boundary must capture the exact active managed Document/native
# DB generation and fence both sides of the host-pumping boundary.
require(
    r"MutateDocumentScopedSystemVariable\s*\(\s*string\s+name\s*,\s*Func<object\??,\s*object\??>\s+nextValue\s*\)",
    "WorkspacePanel.ViewAids.cs missing shared document-scoped system-variable mutation helper",
)
require(
    r"MutateDocumentScopedSystemVariable.*?MdiActiveDocument.*?DocumentGenerationGuard\.CaptureCurrent\(document\)",
    "Workspace viewport-aid mutation must capture active managed Document/native database generation",
)
require(
    r"MutateDocumentScopedSystemVariable.*?DocumentGenerationGuard\.IsCurrent\(document,\s*nativeDatabaseIdentity\).*?GetSystemVariable\(name\).*?DocumentGenerationGuard\.IsCurrent\(document,\s*nativeDatabaseIdentity\).*?SetSystemVariable\(name,",
    "Workspace viewport-aid mutation must revalidate generation before native read and again before native write",
)

# Every document-scoped write path must route through the fenced helper.
require(
    r"ToggleViewportBackgroundPreset.*?MutateDocumentScopedSystemVariable\(\s*\"BKGCOLOR\"",
    "BKGCOLOR preset/restore mutation must use the generation-fenced helper",
)
require(
    r"OnOrthoModeButtonClick.*?MutateDocumentScopedSystemVariable\(\s*\"ORTHOMODE\"",
    "ORTHOMODE Workspace toggle must use the generation-fenced helper",
)
require(
    r"OnObjectSnapButtonClick.*?MutateDocumentScopedSystemVariable\(\s*\"OSMODE\"",
    "OSMODE Workspace enable/suppress toggle must use the generation-fenced helper",
)
require(
    r"OnObjectSnapModeClick.*?MutateDocumentScopedSystemVariable\(\s*\"OSMODE\"",
    "OSMODE Workspace per-mode menu mutation must use the generation-fenced helper",
)

# The remembered BKGCOLOR restore value is also document-scoped state. It must
# be bound to the managed/native generation that produced it so another drawing
# cannot consume a restore color captured from the previous active document.
require(
    r"_lightBackgroundRestoreDocument.*?_lightBackgroundRestoreNativeDatabaseIdentity",
    "light-background restore state must carry document/native-generation ownership",
)
require(
    r"_contrastBackgroundRestoreDocument.*?_contrastBackgroundRestoreNativeDatabaseIdentity",
    "contrast-background restore state must carry document/native-generation ownership",
)
require(
    r"ToggleViewportBackgroundPreset.*?ReferenceEquals\(.*?restoreDocument.*?currentDocument.*?\).*?restoreNativeDatabaseIdentity",
    "BKGCOLOR restore state must be admitted only for the same managed/native document generation",
)

# A committed native write must not be reported as failed because a later WPF
# display refresh throws or observes a different document generation.
require(
    r"RefreshViewportAidStateBestEffort",
    "post-commit viewport-aid refresh must be best-effort display work",
)

# Preserve native semantics while hardening affinity.
require(
    r"ObjectSnapSuppressedBit\s*=\s*16384",
    "OSMODE suppression-bit contract unexpectedly changed",
)
require(
    r"ObjectSnapModeMask\s*=\s*ObjectSnapSuppressedBit\s*-\s*1",
    "OSMODE configured-mode mask contract unexpectedly changed",
)
require(
    r"BackgroundColorsEqual",
    "BKGCOLOR preset comparison contract unexpectedly removed",
)

if errors:
    print("ERROR: Workspace viewport-aid document-generation preflight failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("Workspace viewport-aid document-generation preflight passed.")
