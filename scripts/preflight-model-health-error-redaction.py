#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml.cs"
errors = []

if not WINDOW.is_file():
    errors.append("missing ModelHealthWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")

    required = (
        'private const string LocateFailureMessage = "Không thể định vị Model Health. Hãy xác nhận đúng bản vẽ và thử lại.";',
        'private const string FreshnessFailureReason = "Không thể xác nhận project hiện hành. Đóng cửa sổ và chạy lại Health.";',
        "catch (Exception)\n            {\n                MessageBox.Show(this, LocateFailureMessage",
        "catch (Exception)\n            {\n                MarkSnapshotStale(FreshnessFailureReason);",
        "EnsureActiveAndCurrent();",
        "_locate(issue);",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var current)",
        "if (_staleSnapshot) return;",
    )
    for token in required:
        if token not in text:
            errors.append("Model Health redaction contract missing token: " + token)

    locate_start = text.find("private void Locate()")
    ensure_start = text.find("private void EnsureActiveAndCurrent()")
    refresh_start = text.find("private void RefreshSnapshotFreshness()")
    matches_start = text.find("private bool MatchesSnapshot(")
    if min(locate_start, ensure_start, refresh_start, matches_start) < 0:
        errors.append("Model Health redaction method boundaries could not be located")
    else:
        locate_block = text[locate_start:ensure_start]
        refresh_block = text[refresh_start:matches_start]
        if "ex.Message" in locate_block or "+ ex.Message" in locate_block:
            errors.append("Locate failure leaks raw exception text")
        if "ex.Message" in refresh_block or "+ ex.Message" in refresh_block:
            errors.append("Freshness failure leaks raw exception text")

        guard_pos = locate_block.find("EnsureActiveAndCurrent();")
        callback_pos = locate_block.find("_locate(issue);")
        failure_pos = locate_block.find("MessageBox.Show(this, LocateFailureMessage")
        if min(guard_pos, callback_pos, failure_pos) < 0 or not guard_pos < callback_pos < failure_pos:
            errors.append("Locate must validate active/current state before callback and use the redacted catch surface")

    if 'SummaryText.Text = "SNAPSHOT ĐÃ CŨ • " + reason' not in text:
        errors.append("stale banner must continue to route through the centralized stale reason")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("redaction must not turn freshness verification into a project-creating path")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Model Health locate/freshness failure surfaces use stable redacted messages while preserving fail-closed snapshot guards")
