#!/usr/bin/env python3
"""Guard Coordination Manager modeless UI from exposing exception details."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerWindow.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

# Outer UI boundaries must never concatenate raw exception messages into modeless status text.
for forbidden in (
    'SetMessage("Không thể làm mới Coordination Manager: " + ex.Message);',
    'SetMessage("Định vị bị từ chối: " + ex.Message);',
    'SetMessage("Không thể lưu Coordination issue: " + ex.Message);',
):
    if forbidden in text:
        failures.append("raw exception detail is still exposed by Coordination Manager UI: " + forbidden)

# Stable redacted messages make failure semantics deterministic and avoid host/path/native detail leakage.
for required in (
    'SetMessage("Không thể làm mới Coordination Manager. Vui lòng thử lại.");',
    'SetMessage("Không thể định vị Coordination issue. Vui lòng thử lại.");',
    'SetMessage("Không thể lưu Coordination issue. Vui lòng thử lại.");',
):
    if required not in text:
        failures.append("missing stable redacted Coordination Manager failure message: " + required)

# Keep the persistence rollback boundary intact: error redaction must not weaken mutation safety.
rollback = '''catch\n                {\n                    project.Metadata.Clear();\n                    foreach (var pair in metadataBefore) project.Metadata[pair.Key] = pair.Value;\n                    throw;\n                }'''
if rollback not in text:
    failures.append("Coordination Manager persistence rollback/rethrow boundary changed unexpectedly")

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("Coordination Manager exception-redaction preflight passed")
