#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SingleFootingCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

# Interactive command failures must not expose arbitrary exception detail.
if re.search(r'catch\s*\(Exception\s+\w+\)[\s\S]{0,300}?Report\([^;]*\.Message', text):
    errors.append("interactive Single Footing boundary still reports raw Exception.Message")

# The command needs a stable operation-level failure status.
if 'QS3DDRAWSINGLEFOOTING failed. See diagnostics for details.' not in text:
    errors.append("stable redacted interactive failure status is missing")

# Report may write back to the captured editor, but process-wide Palette publication
# must be exact-source-document fenced immediately at the reporting boundary.
report = re.search(r'private\s+static\s+void\s+Report\s*\(Document\s+document,\s*string\s+message\)\s*\{([\s\S]*?)\n\s*\}', text)
if not report:
    errors.append("Report(Document,string) helper not found")
else:
    body = report.group(1)
    if 'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)' not in body:
        errors.append("Report does not exact-fence process-wide status to source MDI document")
    if 'PaletteCoordinator.SetStatus(message)' not in body:
        errors.append("Report no longer publishes the expected Palette status")
    fence_pos = body.find('ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)')
    palette_pos = body.find('PaletteCoordinator.SetStatus(message)')
    if fence_pos >= 0 and palette_pos >= 0 and fence_pos > palette_pos:
        errors.append("Palette status publication occurs before source-document affinity fence")

# The deterministic one-shot bridge must remain exception-transparent.
bridge = re.search(r'internal\s+static\s+string\s+PlaceActiveSingleFootingAt\s*\([^)]*\)\s*\{([\s\S]*?)\n\s*\}', text)
if bridge and re.search(r'catch\s*\(', bridge.group(1)):
    errors.append("one-shot Single Footing bridge must remain exception-transparent")

# This hardening must not introduce deferred/event lifetime machinery into the command.
for forbidden in ('DispatcherTimer', 'BeginInvoke(', 'ImpliedSelectionChanged +=', 'DocumentActivated +='):
    if forbidden in text:
        errors.append(f"unexpected deferred/subscription machinery introduced: {forbidden}")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    raise SystemExit(1)

print("Single Footing status-boundary preflight PASS")
