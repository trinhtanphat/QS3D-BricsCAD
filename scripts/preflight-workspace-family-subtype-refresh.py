#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SYNC = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.FamilySubtypeRefreshSync.cs"
DOCUMENT_CONTEXT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DocumentContext.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def forbid(text: str, needle: str, message: str) -> None:
    if needle in text:
        raise SystemExit("ERROR: " + message + " (forbidden: " + needle + ")")


def main() -> int:
    sync = SYNC.read_text(encoding="utf-8")
    document_context = DOCUMENT_CONTEXT.read_text(encoding="utf-8")

    require(sync, "Families.CollectionChanged += OnFamilySubtypeRefreshFamiliesChanged",
            "Workspace Family reloads must trigger subtype-view reconciliation")
    require(sync, "DataContextChanged += OnFamilySubtypeRefreshDataContextChanged",
            "subtype refresh sync must follow WorkspaceViewModel replacement")
    require(sync, "Dispatcher.BeginInvoke(",
            "subtype reconciliation must be deferred until the reload stack unwinds")
    require(sync, "DispatcherPriority.ContextIdle",
            "subtype reconciliation must not re-enter synchronous Workspace loading")
    require(sync, "if (_loadingContext)",
            "deferred reconciliation must not run while Workspace loading is active")
    require(sync, "string.IsNullOrWhiteSpace(_familySubtypeFilter)",
            "subtype reconciliation must be inactive when no subtype is selected")
    require(sync, "ApplyFamilySubtypeFilter();",
            "same-document reloads must restore the subtype-aware Family view")

    require(document_context, "ResetWorkspaceAuthoringFilters();",
            "document switches must continue clearing authoring filters")
    require(document_context, "_familySubtypeFilter = string.Empty;",
            "document switches must clear stale subtype state before reload")

    forbid(sync, "ApplyFamilyFilter();",
           "refresh reconciliation must not broaden the Family view back to category-only filtering")
    forbid(sync, "ProjectFamilyService", "refresh reconciliation must remain view-only")
    forbid(sync, "ExistingProjectMutationContext", "refresh reconciliation must not mutate project state")
    forbid(sync, "ProjectContextCoordinator", "refresh reconciliation must not create or bind project state")
    forbid(sync, "SendStringToExecute", "refresh reconciliation must not issue BricsCAD commands")
    forbid(sync, "ScrollIntoView", "refresh reconciliation must not reintroduce generator/layout reentrancy")
    forbid(sync, "UpdateLayout", "refresh reconciliation must not force synchronous WPF layout")

    print("PASS: Workspace same-document refresh preserves the active Foundation subtype view without project/CAD mutation or synchronous layout re-entry.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
