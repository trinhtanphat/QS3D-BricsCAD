#!/usr/bin/env python3
"""Guard V25 Untrack command UI from exposing host/native exception details."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ViewportCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

for forbidden in (
    '"\\n[QS3D] Cảnh báo UI sau untrack commit: " + warning.Message',
    '"Không thể bỏ theo dõi " + label + ": " + ex.Message',
):
    if forbidden in text:
        failures.append("raw exception detail is still exposed by Untrack command UI: " + forbidden)

for required in (
    '"\\n[QS3D] Cảnh báo UI sau untrack commit; semantic change đã được lưu nhưng UI chưa đồng bộ hoàn toàn."',
    '"Không thể bỏ theo dõi " + label + ". Vui lòng thử lại."',
):
    if required not in text:
        failures.append("missing stable redacted Untrack command message: " + required)

# Preserve the post-commit boundary: UI refresh/status/editor failures remain warnings and do not
# enter the mutation catch path or imply the semantic change was rolled back.
for required in (
    'FinalizeUntrackUi(doc, result.Count, label);',
    'try { PaletteCoordinator.RefreshProject(); }',
    'if (warning == null) return;',
):
    if required not in text:
        failures.append("Untrack post-commit warning boundary changed unexpectedly: " + required)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("V25 Untrack command exception-redaction preflight passed")
