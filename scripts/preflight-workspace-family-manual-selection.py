#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.FamilySubtype.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def forbid(text: str, needle: str, message: str) -> None:
    if needle in text:
        raise SystemExit("ERROR: " + message + " (forbidden: " + needle + ")")


def method_body(text: str, marker: str) -> str:
    start = text.find(marker)
    if start < 0:
        raise SystemExit("ERROR: missing handler: " + marker)
    end = text.find("\n        private ", start + len(marker))
    return text[start:] if end < 0 else text[start:end]


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    handler = method_body(source, "private void OnFamilySubtypeFamilySelectionChanged")
    compact = " ".join(handler.split())

    require(
        compact,
        "if (_loadingContext && _inspection.Count > 0 && inferred.Length > 0 &&",
        "Foundation subtype inference from Family selection must be limited to the loading/programmatic sync window",
    )
    require(
        handler,
        "_familySubtypeFilter = inferred;",
        "the established loading-time subtype synchronization must remain intact",
    )
    require(
        handler,
        "ApplyFamilySubtypeFilter();",
        "loading-time inferred subtype must still refresh the visible Family view",
    )
    forbid(
        handler,
        "_inspection = Array.Empty",
        "manual Family selection must not discard the current CAD inspection as a workaround",
    )
    forbid(
        handler,
        "_inspection.Clear",
        "manual Family selection must not mutate CAD inspection state as a workaround",
    )

    print("PASS: manual Workspace Family selection cannot inherit a stale CAD inspection subtype; loading-time synchronization remains intact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
