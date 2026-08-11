#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RevisionWindow.xaml.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "private readonly RevisionSnapshot _afterSnapshot;",
        "private bool _staleSnapshot;",
        "Activated += (_, __) => RefreshSnapshotFreshness();",
        "EnsureActiveAndCurrent();",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject)",
        "var liveSnapshot = revisionService.Capture(currentProject, \"__revision_window_live__\");",
        "revisionService.Compare(_afterSnapshot, liveSnapshot).Count == 0",
        "MarkSnapshotStale(",
        "if (_staleSnapshot)",
        "Grid.IsEnabled = false",
        "SemanticGrid.IsEnabled = false",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        print("ERROR: RevisionWindow stale-snapshot contract is incomplete:")
        for needle in missing:
            print(" - missing:", needle)
        return 1

    locate_pos = text.find("private void Locate(QuantityRevisionRow row)")
    guard_pos = text.find("EnsureActiveAndCurrent();", locate_pos)
    callback_pos = text.find("_locate?.Invoke(row);", locate_pos)
    if locate_pos < 0 or guard_pos < 0 or callback_pos < 0 or not (locate_pos < guard_pos < callback_pos):
        print("ERROR: Revision Locate must revalidate the live semantic snapshot before invoking the locate callback.")
        return 1

    if "ChangeVersion" in text or "UpdatedUtc" in text:
        print("ERROR: Revision freshness must compare semantic revision content, not project Touch/audit timestamps.")
        return 1

    print("PASS: RevisionWindow blocks stale snapshot Locate while allowing audit-only project Touch changes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
