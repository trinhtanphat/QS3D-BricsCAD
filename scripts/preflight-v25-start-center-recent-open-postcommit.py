#!/usr/bin/env python3
"""Guard Start Center recent-project open against false failure after native commit."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterWindow.cs"
text = SOURCE.read_text(encoding="utf-8")

signature = "private void OpenRecentProject(StartCenterRecentProject recent)"
start = text.find(signature)
if start < 0:
    print("ERROR: OpenRecentProject method not found", file=sys.stderr)
    raise SystemExit(1)

brace = text.find("{", start)
if brace < 0:
    print("ERROR: OpenRecentProject body not found", file=sys.stderr)
    raise SystemExit(1)

depth = 0
end = None
for i in range(brace, len(text)):
    if text[i] == "{":
        depth += 1
    elif text[i] == "}":
        depth -= 1
        if depth == 0:
            end = i + 1
            break
if end is None:
    print("ERROR: OpenRecentProject body is unbalanced", file=sys.stderr)
    raise SystemExit(1)

body = text[brace:end]
open_call = "Application.DocumentManager.Open(normalized, false);"
record_call = "StartCenterUserStateStore.RecordProject(normalized);"
safe_open_failure = 'ShowSafeFailure("Không thể mở dự án gần đây. Vui lòng thử lại.");'

failures = []
open_idx = body.find(open_call)
record_idx = body.find(record_call)
first_catch_idx = body.find("catch", open_idx + len(open_call)) if open_idx >= 0 else -1

if open_idx < 0:
    failures.append("missing native recent-project open call")
if record_idx < 0:
    failures.append("missing recent-project bookkeeping call")
if body.count(open_call) != 1:
    failures.append("native recent-project open must occur exactly once (no retry/reopen)")
if first_catch_idx < 0:
    failures.append("native open has no explicit failure boundary")
elif record_idx >= 0 and first_catch_idx > record_idx:
    failures.append("post-commit bookkeeping still shares the native-open failure boundary")

if first_catch_idx >= 0 and record_idx >= 0 and first_catch_idx < record_idx:
    catch_end = body.find("}", first_catch_idx)
    if catch_end < 0:
        failures.append("native-open catch block is malformed")
    else:
        catch_text = body[first_catch_idx:catch_end]
        if safe_open_failure not in catch_text:
            failures.append("native-open catch must use the stable redacted open-failure message")
        if "return;" not in catch_text:
            failures.append("native-open failure must return before post-commit bookkeeping")

success_idx = body.find('"Đã mở "')
queue_idx = body.find("QueueHomeRefresh(recordActiveDrawing: true);")
if record_idx >= 0 and success_idx >= 0 and success_idx < record_idx:
    failures.append("success status must not be published before bookkeeping is attempted")
if success_idx < 0 or queue_idx < 0 or (success_idx >= 0 and queue_idx < success_idx):
    failures.append("successful native open must still publish success and queue refresh")

if failures:
    for failure in failures:
        print(f"ERROR: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("Start Center recent-open post-commit preflight passed")
