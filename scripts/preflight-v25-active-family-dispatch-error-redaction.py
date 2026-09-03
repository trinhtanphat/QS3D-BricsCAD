#!/usr/bin/env python3
"""Guard Active Family authoring dispatcher against exposing raw exception details."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ActiveFamilyQuickDrawCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

method_start = text.find("private static void DrawActiveFamilyCore(")
method_end = text.find("private static ProjectFamily RequireCurrentDispatchSnapshot(", method_start)
if method_start < 0 or method_end < 0:
    failures.append("cannot isolate DrawActiveFamilyCore dispatch boundary")
    method = ""
else:
    method = text[method_start:method_end]

for forbidden in (
    'catch (Exception ex)',
    'ex.Message',
    'Report(document, operation + " lỗi: "',
):
    if forbidden in method:
        failures.append("Active Family dispatcher still exposes exception detail: " + forbidden)

for required in (
    'catch (Exception)',
    'Report(document, operation + ": không thể hoàn tất thao tác. Vui lòng thử lại.");',
):
    if required not in method:
        failures.append("Active Family dispatcher is missing stable redacted failure behavior: " + required)

# Preserve the safety-critical dispatch validation path. Redaction must not bypass stale-document/project/family checks.
for required in (
    "RequireCurrentDispatchSnapshot(",
    "if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))",
    "currentProject.ChangeVersion != expectedChangeVersion",
    "if (routingChanged)",
    "Dispatch(document, dispatchFamily, advanced, operation);",
):
    if required not in text:
        failures.append("Active Family dispatch safety invariant changed unexpectedly: " + required)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("V25 Active Family dispatch exception-redaction preflight passed")
