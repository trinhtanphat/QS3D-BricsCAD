#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterPanel.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []

def require(pattern: str, message: str) -> None:
    if re.search(pattern, text, flags=re.MULTILINE | re.DOTALL) is None:
        errors.append(message)

# The two document-scoped interaction toggles must route through one helper that
# captures the exact active managed Document/native DB generation. The helper
# must fence both sides of the native read -> write boundary so a modeless host
# reentrancy/document replacement cannot apply DWG A's click to DWG B.
require(
    r"ToggleDocumentScopedSystemVariable\s*\(\s*string\s+name\s*,\s*Func<int,\s*int>\s+nextValue\s*\)",
    "BltStartCenterPanel.cs missing shared document-scoped system-variable toggle helper",
)
require(
    r"ToggleDocumentScopedSystemVariable.*?MdiActiveDocument.*?DocumentGenerationGuard\.CaptureCurrent\(document\)",
    "document-scoped toggle must capture active managed Document/native database generation",
)
require(
    r"ToggleDocumentScopedSystemVariable.*?DocumentGenerationGuard\.IsCurrent\(document,\s*nativeDatabaseIdentity\).*?ReadRequiredSystemVariableInt\(name\).*?DocumentGenerationGuard\.IsCurrent\(document,\s*nativeDatabaseIdentity\).*?Application\.SetSystemVariable\(name,",
    "document-scoped toggle must revalidate generation before native read and again before native write",
)
require(
    r"ToggleOrtho\s*\(\s*\).*?ToggleDocumentScopedSystemVariable\(\s*\"ORTHOMODE\"",
    "ORTHOMODE toggle must use the generation-fenced document-scoped helper",
)
require(
    r"ToggleEntitySnap\s*\(\s*\).*?ToggleDocumentScopedSystemVariable\(\s*\"OSMODE\"",
    "OSMODE toggle must use the generation-fenced document-scoped helper",
)

# Application-global controls remain intentionally separate. This regression is
# about document-scoped mutation affinity, not a broad rewrite of all status UI.
require(r"ToggleLightTheme\s*\(\s*\)", "light-theme status action unexpectedly removed")
require(r"ToggleLinearContrast\s*\(\s*\)", "linear-contrast status action unexpectedly removed")

if errors:
    print("ERROR: Start Center status-toggle generation affinity preflight failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("Start Center status-toggle generation affinity preflight passed.")
