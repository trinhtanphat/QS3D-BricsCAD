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
        "private readonly string _projectIdAtOpen;",
        "private readonly DateTime _updatedUtcAtOpen;",
        "private readonly long _changeVersionAtOpen;",
        "private readonly string _drawingFingerprintAtOpen;",
        "private bool _staleSnapshot;",
        "private const string LocateFailureMessage =",
        "private const string FreshnessFailureReason =",
        "_projectIdAtOpen = projectAtOpen.ProjectId;",
        "_changeVersionAtOpen = projectAtOpen.ChangeVersion;",
        "Activated += (_, __) => RefreshSnapshotFreshness();",
        "EnsureActiveAndCurrent();",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var current)",
        "MatchesSnapshot(current)",
        "current.ChangeVersion == _changeVersionAtOpen",
        "MarkSnapshotStale(FreshnessFailureReason);",
        "IssueGrid.IsEnabled = false;",
        "SNAPSHOT ĐÃ CŨ",
        "QS3DHEALTH hoặc QS3DHEALTHALL",
    )
    for token in required:
        if token not in text:
            errors.append("ModelHealthWindow missing stale-snapshot stamp token: " + token)

    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Model Health freshness checks are read-only and must not create/cache project state")
    if "private readonly ProjectState _projectAtOpen;" in text:
        errors.append("Model Health must store immutable snapshot stamps instead of retaining a stale ProjectState instance")

    ensure_pos = text.find("private void EnsureActiveAndCurrent()")
    refresh_pos = text.find("RefreshSnapshotFreshness();", ensure_pos)
    stale_pos = text.find("if (_staleSnapshot)", refresh_pos)
    callback_pos = text.find("_locate(issue);")
    locate_guard_pos = text.find("EnsureActiveAndCurrent();")
    if min(ensure_pos, refresh_pos, stale_pos, callback_pos, locate_guard_pos) < 0:
        errors.append("ModelHealthWindow stale locate ordering could not be verified")
    elif not locate_guard_pos < callback_pos:
        errors.append("Model Health locate callback must remain behind active-DWG/current-project validation")

    locate_start = text.find("private void Locate()")
    ensure_start = text.find("private void EnsureActiveAndCurrent()")
    refresh_start = text.find("private void RefreshSnapshotFreshness()")
    matches_start = text.find("private bool MatchesSnapshot(")
    if min(locate_start, ensure_start, refresh_start, matches_start) < 0:
        errors.append("Model Health method boundaries could not be verified for error-surface safety")
    else:
        locate_block = text[locate_start:ensure_start]
        refresh_block = text[refresh_start:matches_start]
        if "ex.Message" in locate_block:
            errors.append("Model Health Locate must not surface raw exception messages")
        if "ex.Message" in refresh_block:
            errors.append("Model Health freshness stale reason must not surface raw exception messages")
        if "MessageBox.Show(this, LocateFailureMessage" not in locate_block:
            errors.append("Model Health Locate failure must use the stable redacted failure message")
        if "MarkSnapshotStale(FreshnessFailureReason);" not in refresh_block:
            errors.append("Model Health freshness exception must use the stable redacted stale reason")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Model Health stores immutable semantic snapshot stamps, rechecks current state read-only, blocks stale Locate callbacks, and redacts failure details")
