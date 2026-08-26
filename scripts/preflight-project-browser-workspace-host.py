#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ProjectBrowser.cs"
COORDINATOR = ROOT / "src" / "QS3D.Core" / "Navigation" / "ProjectBrowserWorkspaceCoordinator.cs"
STATE_STORE = ROOT / "src" / "QS3D.Core" / "Navigation" / "ProjectBrowserWorkspaceStateStore.cs"


def fail(message: str) -> None:
    print("ERROR: hosted Project Browser preflight failed: " + message)
    raise SystemExit(1)


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        fail(label + ": missing " + marker)


def main() -> None:
    for path in (HOST, COORDINATOR, STATE_STORE):
        if not path.exists():
            fail("missing source file " + str(path.relative_to(ROOT)))

    host = HOST.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")
    state_store = STATE_STORE.read_text(encoding="utf-8")

    host_markers = (
        "static WorkspacePanel()",
        "FrameworkElement.LoadedEvent",
        "FrameworkElement.UnloadedEvent",
        "ProjectBrowserWorkspaceCoordinator.Build",
        "ProjectBrowserWorkspaceCoordinator.ApplySelection",
        "ProjectBrowserWorkspaceCoordinator.SetExpanded",
        "ProjectBrowserVirtualizationPlanner.GetElementPage",
        "ProjectBrowserSelectionPlanner.PlanNodeSelection",
        "ProjectBrowserWorkspaceStateStore",
        "TryResolveSemanticSelection(project, _inspection",
        "SourceHandleResolver.Resolve(project, ids)",
        "CadHandleService.Resolve(document, handles)",
        "document.Editor.SetImpliedSelection(objectIds.ToArray())",
        "ExistingProjectMutationContext.Require(document, \"Project Browser presentation state\")",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
        "RequireBrowserIdentity(project, _browserProjectId, _browserDrawingFingerprint)",
        "RequireBrowserVersionInvariant(project, version)",
        "Active DWG/project đã đổi",
        "callback Project Browser cũ không được phép tác động sang bản vẽ mới",
        "Project Browser chưa bind canonical project",
        "Không resolve đủ live CAD objects",
        "PICKFIRST được giữ nguyên",
        "Family đã bị xóa/missing",
        "DataContextChanged += OnBrowserDataContextChanged",
        "ItemsControl.ItemsSourceProperty",
        "Project Browser selection đã clear fail-closed",
        "Tìm semantic",
        "Chỉ cấu kiện dirty",
        "Tầng > Category",
        "Zone > Category",
        "BrowserNodePageSize",
        "BrowserElementPageSize",
    )
    for marker in host_markers:
        require(host, marker, "host adapter")

    coordinator_markers = (
        "Single source-safe coordination seam for a modeless Project Browser UI",
        "without storing native CAD ObjectIds/handles or touching semantic versioning",
        "ProjectBrowserQueryPlanner.Build",
        "ProjectBrowserSelectionPlanner.PlanReveal",
        "ProjectBrowserVirtualizationPlanner.BuildViewport",
        "RequireSelectionFreshness",
    )
    for marker in coordinator_markers:
        require(coordinator, marker, "Core coordinator")

    store_markers = (
        'MetadataKey = "QS3D.ProjectBrowser.WorkspaceState"',
        "ValidateAgainstProject(project, state)",
        "SelectedElementIds",
        "PrimaryElementId",
        "ExpandedPaths",
    )
    for marker in store_markers:
        require(state_store, marker, "workspace state store")

    # Modeless fields may retain only semantic/presentation identity. Native wrappers must be
    # short-lived locals resolved at click time, never durable Workspace fields.
    field_lines = [
        line.strip()
        for line in host.splitlines()
        if re.match(r"^\s*private\s+(?:readonly\s+)?[^\(]+\s+_[A-Za-z0-9_]+(?:\s*=|\s*;)", line)
    ]
    for line in field_lines:
        if re.search(r"\b(?:Document|Database|ObjectId|Handle)\b", line):
            fail("modeless host persists a native CAD wrapper/identity field: " + line)

    forbidden = (
        ".Touch(",
        "AuditTrail.ForProject",
        "GetOrCreate(",
        "ObjectId _browser",
        "Handle _browser",
        "Document _browser",
        "Database _browser",
    )
    for marker in forbidden:
        if marker in host:
            fail("host adapter violates presentation/non-creating boundary: " + marker)

    # Browser -> CAD must resolve every current provenance Handle before PICKFIRST changes.
    resolve_pos = host.find("var objectIds = CadHandleService.Resolve(document, handles);")
    parity_pos = host.find("if (objectIds.Count != handles.Count)", resolve_pos)
    select_pos = host.find("document.Editor.SetImpliedSelection(objectIds.ToArray());", resolve_pos)
    if not (0 <= resolve_pos < parity_pos < select_pos):
        fail("Browser -> CAD must require complete live resolution before changing PICKFIRST")

    # Presentation persistence is allowed only through the dedicated state store and must prove
    # ChangeVersion invariance around that write.
    persist_start = host.find("private void PersistBrowserState(")
    persist_end = host.find("private static void RequireBrowserIdentity(", persist_start)
    if persist_start < 0 or persist_end < 0:
        fail("cannot isolate presentation persistence boundary")
    persist = host[persist_start:persist_end]
    for marker in (
        "ExistingProjectMutationContext.Require",
        "_browserStateStore.Save(project, state);",
        "var version = project.ChangeVersion;",
        "RequireBrowserVersionInvariant(project, version);",
    ):
        require(persist, marker, "presentation persistence")

    print("PASS hosted Project Browser uses semantic-only modeless state, current-DWG live CAD resolution and ChangeVersion-safe presentation persistence")


if __name__ == "__main__":
    main()
