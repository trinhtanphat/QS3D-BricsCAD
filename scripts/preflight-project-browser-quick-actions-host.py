#!/usr/bin/env python3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROJECT_BROWSER = (
    ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ProjectBrowser.cs"
)
QUICK_ACTIONS = (
    ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ReferenceQuickActions.cs"
)


def fail(message: str) -> None:
    print("ERROR: Project Browser quick-actions host preflight failed: " + message)
    raise SystemExit(1)


def require(text: str, marker: str, scope: str) -> None:
    if marker not in text:
        fail(scope + " is missing: " + marker)


def require_order(text: str, markers: tuple[str, ...], scope: str) -> None:
    cursor = -1
    for marker in markers:
        position = text.find(marker, cursor + 1)
        if position < 0:
            fail(scope + " is missing ordered marker: " + marker)
        if position <= cursor:
            fail(scope + " has an invalid order around: " + marker)
        cursor = position


def section(text: str, start: str, end: str, scope: str) -> str:
    start_index = text.find(start)
    end_index = text.find(end, start_index + len(start)) if start_index >= 0 else -1
    if start_index < 0 or end_index <= start_index:
        fail(scope + " could not be isolated")
    return text[start_index:end_index]


def simulate(initialization_order: tuple[str, str]) -> None:
    # Minimal behavioral model of the two production attachment seams. Both Loaded-handler
    # registration orders must converge to one band in the Mô hình tab host.
    root = ["tree"]
    model_tab: list[str] | None = None
    quick_actions_applied = False

    def attach_quick_actions() -> None:
        nonlocal quick_actions_applied
        if quick_actions_applied:
            return
        host = model_tab if model_tab is not None else root
        if "band" not in host:
            host.insert(host.index("tree"), "band")
        quick_actions_applied = True

    def attach_project_browser() -> None:
        nonlocal model_tab
        if model_tab is not None:
            return
        existing_band = "band" in root
        if existing_band:
            root.remove("band")
        root.remove("tree")
        model_tab = []
        if existing_band:
            model_tab.append("band")
        model_tab.append("tree")
        root.append("tabs")
        attach_quick_actions()

    actions = {
        "quick": attach_quick_actions,
        "browser": attach_project_browser,
    }
    for name in initialization_order:
        actions[name]()

    if root != ["tabs"] or model_tab != ["band", "tree"]:
        fail(
            "behavioral order "
            + " -> ".join(initialization_order)
            + f" produced root={root!r}, model_tab={model_tab!r}"
        )


def main() -> None:
    for path in (PROJECT_BROWSER, QUICK_ACTIONS):
        if not path.is_file():
            fail("missing source file " + str(path.relative_to(ROOT)))

    project_browser = PROJECT_BROWSER.read_text(encoding="utf-8")
    quick_actions = QUICK_ACTIONS.read_text(encoding="utf-8")
    ensure = section(
        project_browser,
        "private void EnsureProjectBrowserSurface()",
        "private FrameworkElement CreateProjectBrowserSurface()",
        "Project Browser surface attachment",
    )
    apply = section(
        quick_actions,
        "private void ApplyReferenceQuickActions()",
        "private static ComboBoxItem CreateReferenceDrawMode",
        "Reference quick-actions attachment",
    )

    require(
        quick_actions,
        'internal const string ReferenceQuickActionsTag = "QS3D_REFERENCE_QUICK_ACTIONS";',
        "shared quick-actions identity",
    )
    if quick_actions.count('"QS3D_REFERENCE_QUICK_ACTIONS"') != 1:
        fail("the quick-actions tag literal must have one canonical source declaration")

    require_order(
        ensure,
        (
            "var existingQuickActionsBand = modelDock.Children",
            "ReferenceQuickActionsTag",
            "modelDock.Children.Remove(existingQuickActionsBand);",
            "modelDock.Children.Remove(ModelTree);",
            "var modelTabHost = new DockPanel { LastChildFill = true };",
            "modelTabHost.Children.Add(existingQuickActionsBand);",
            "modelTabHost.Children.Add(ModelTree);",
            'new TabItem { Header = "Mô hình", Content = modelTabHost }',
            "_browserTabs = tabs;",
            "_browserTab = browserTab;",
            "ApplyReferenceQuickActions();",
        ),
        "order-independent Project Browser reparenting",
    )

    for marker in (
        "ModelTree.Parent is DockPanel modelDock",
        "ReferenceQuickActionsTag",
        "modelDock.Children.Insert(modelTreeIndex, band);",
    ):
        require(apply, marker, "authoritative quick-actions bridge")

    for marker in (
        "ExecuteWorkspaceDraw(advanced: true);",
        "ExecuteWorkspaceDraw(advanced: false);",
        'ExecuteWorkspaceBasicDraw("QS3DDRAWLINE", "Đường");',
        'ExecuteWorkspaceBasicDraw("QS3DDRAWRECT", "Chữ nhật");',
        'ExecuteWorkspaceBasicDraw("QS3DDRAWCIRCLE", "Hình tròn");',
        "private void OnReferenceAddClick(object sender, RoutedEventArgs e) => OnAddClick(sender, e);",
        "private void OnReferenceDeleteClick(object sender, RoutedEventArgs e) => OnDeleteClick(sender, e);",
        "private void OnReferenceCaptureClick(object sender, RoutedEventArgs e) => OnCaptureSelectedClick(sender, e);",
    ):
        require(quick_actions, marker, "authoritative Workspace action delegation")

    if project_browser.count("ApplyReferenceQuickActions();") != 1:
        fail("Project Browser must perform exactly one idempotent post-reparent attachment")
    if quick_actions.count("new Border") != 1:
        fail("quick-actions source must create exactly one tagged band implementation")

    simulate(("browser", "quick"))
    simulate(("quick", "browser"))

    print(
        "PASS: Project Browser and ReferenceQuickActions initialization orders converge to "
        "exactly one authoritative band inside the Mô hình tab host"
    )


if __name__ == "__main__":
    main()
