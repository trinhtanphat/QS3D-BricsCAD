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
        "BrowserClassHandlersRegistered = RegisterBrowserClassHandlers()",
        "private static bool RegisterBrowserClassHandlers()",
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
        "ExistingProjectMutationContext.Require(document, context)",
        "RequireCanonicalBrowserMutationProject",
        "RequireBrowserVersionInvariant(project, version)",
        "_browserInspectionProjectId",
        "_browserInspectionDrawingFingerprint",
        "CaptureBrowserInspectionIdentity",
        "CAD inspection belongs to a stale/other DWG",
        "Project Browser canonical project instance changed before CAD selection",
        "Active DWG changed before Browser → CAD selection commit",
        "Project changed before Browser → CAD selection commit",
        "Project Browser canonical project instance changed; Refresh required",
        "Active DWG/project đã đổi",
        "callback Project Browser cũ không được phép tác động sang bản vẽ mới",
        "Project Browser chưa bind canonical project",
        "Không resolve đủ live CAD objects",
        "PICKFIRST was not changed",
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

    # Modeless fields may retain semantic/presentation strings and WPF controls only. Native CAD
    # wrappers or native identity must remain method-local and be re-resolved at action time.
    field_lines = [
        line.strip()
        for line in host.splitlines()
        if re.match(r"^\s*private\s+(?:static\s+)?(?:readonly\s+)?[^\(]+\s+_[A-Za-z0-9_]+(?:\s*=|\s*;)", line)
    ]
    for line in field_lines:
        if re.search(r"\b(?:Document|Database|ObjectId|Handle)\b", line):
            fail("modeless host persists a native CAD wrapper/identity field: " + line)

    for marker in (
        ".Touch(",
        "AuditTrail.ForProject",
        "GetOrCreate(",
        "ObjectId _browser",
        "Handle _browser",
        "Document _browser",
        "Database _browser",
        "static WorkspacePanel()",
    ):
        if marker in host:
            fail("host adapter violates presentation/composable-lifecycle boundary: " + marker)

    # Browser -> CAD: resolve complete live provenance, then re-check active DWG, canonical project
    # instance and semantic version immediately before PICKFIRST is changed.
    resolve_pos = host.find("var objectIds = CadHandleService.Resolve(document, handles);")
    parity_pos = host.find("if (objectIds.Count != handles.Count)", resolve_pos)
    active_pos = host.find("if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))", parity_pos)
    canonical_pos = host.find("!ReferenceEquals(currentProject, project)", active_pos)
    version_pos = host.find("if (project.ChangeVersion != sourceVersion)", canonical_pos)
    select_pos = host.find("document.Editor.SetImpliedSelection(objectIds.ToArray());", version_pos)
    if not (0 <= resolve_pos < parity_pos < active_pos < canonical_pos < version_pos < select_pos):
        fail("Browser -> CAD must revalidate exact active DWG/project/version after full live resolution and before PICKFIRST")

    # CAD -> Browser must bind each inspection snapshot to project/fingerprint identity before the
    # semantic resolver may consume it.
    sync_start = host.find("private void SyncProjectBrowserFromCad()")
    sync_end = host.find("private void OnBrowserPreviousNodesClick", sync_start)
    if sync_start < 0 or sync_end < 0:
        fail("cannot isolate CAD -> Browser synchronization boundary")
    sync = host[sync_start:sync_end]
    identity_pos = sync.find("_browserInspectionProjectId")
    resolve_selection_pos = sync.find("TryResolveSemanticSelection(project, _inspection")
    if not (0 <= identity_pos < resolve_selection_pos):
        fail("CAD -> Browser must validate inspection project/fingerprint before semantic resolution")

    # Presentation persistence must rebind the same canonical project object through the dedicated
    # existing-project mutation context and prove semantic ChangeVersion invariance.
    persist_start = host.find("private void PersistBrowserState(")
    persist_end = host.find("private static void RequireBrowserIdentity(", persist_start)
    if persist_start < 0 or persist_end < 0:
        fail("cannot isolate presentation persistence boundary")
    persist = host[persist_start:persist_end]
    for marker in (
        "RequireCanonicalBrowserMutationProject(document, expectedProject",
        "ExistingProjectMutationContext.Require(document, context)",
        "!ReferenceEquals(project, expectedProject)",
        "_browserStateStore.Save(project, state);",
        "var version = project.ChangeVersion;",
        "RequireBrowserVersionInvariant(project, version);",
    ):
        require(persist, marker, "presentation persistence")

    print("PASS hosted Project Browser uses composable Workspace lifecycle, semantic-only modeless state, exact-DWG/project commit revalidation and ChangeVersion-safe presentation persistence")


if __name__ == "__main__":
    main()
