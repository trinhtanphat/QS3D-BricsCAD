#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RevisionWindow.xaml.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "private readonly RevisionSnapshot _afterSnapshot;",
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "private bool _staleSnapshot;",
        "Activated += (_, __) => RefreshSnapshotFreshness();",
        "var document = RequireBoundActiveDocument();",
        "RefreshSnapshotFreshness(document);",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
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

    for forbidden in (
        "private readonly Document _document",
        "ProjectContextCoordinator.TryGetReadOnly(_document",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
    ):
        if forbidden in text:
            print("ERROR: Revision freshness must not retain/dereference a stale managed Document wrapper:", forbidden)
            return 1

    locate_pos = text.find("private void Locate(QuantityRevisionRow row)")
    guard_pos = text.find("var document = EnsureActiveAndCurrent();", locate_pos)
    locate_current_pos = text.find("LocateCurrentElement(document, row);", locate_pos)
    if locate_pos < 0 or guard_pos < 0 or locate_current_pos < 0 or not (locate_pos < guard_pos < locate_current_pos):
        print("ERROR: Revision Locate must resolve/revalidate the live source Document before CAD locate.")
        return 1

    if "if (!TryGetBoundActiveDocument(out var document)) return;" not in text:
        print("ERROR: temporary activation of another DWG must not incorrectly mark the Revision snapshot stale.")
        return 1

    if "ChangeVersion" in text or "UpdatedUtc" in text:
        print("ERROR: Revision freshness must compare semantic revision content, not project Touch/audit timestamps.")
        return 1

    print("PASS: RevisionWindow resolves its live source Document by stable native database identity, blocks stale snapshot Locate, and allows audit-only project Touch changes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
