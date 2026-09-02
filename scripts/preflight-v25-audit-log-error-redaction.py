#!/usr/bin/env python3
"""Guard Audit Log modeless UI from exposing host exception details."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "AuditLogWindow.xaml.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

forbidden = 'ClearProjection("Không đọc được audit: " + ex.Message);'
required = 'ClearProjection("Không đọc được Audit Log. Vui lòng thử lại.");'

if forbidden in text:
    failures.append("Audit Log still exposes raw exception details")
if required not in text:
    failures.append("Audit Log is missing the stable redacted reload failure message")

# Error redaction must not weaken the window's document/database affinity or lifetime ownership.
for contract in (
    "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "DocumentBoundWindowLifetime.Attach(this, document);",
    "if (!TryResolveBoundDocument(out var document))",
    "if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;",
):
    if contract not in text:
        failures.append("Audit Log document/lifetime contract changed unexpectedly: " + contract)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("Audit Log exception-redaction preflight passed")
