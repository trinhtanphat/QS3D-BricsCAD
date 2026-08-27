#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI_DIR = ROOT / "src/QS3D.BricsCAD.V25/UI"
SOURCE = UI_DIR / "WorkspacePanel.RaftFoundationVisibleAddRoute.cs"
WORKFLOW = UI_DIR / "WorkspacePanel.RaftFoundationWorkflow.cs"


def main():
    failures = []
    if not SOURCE.is_file():
        failures.append("missing raft visible Add route source")
    if not WORKFLOW.is_file():
        failures.append("missing raft workflow source")

    workspace_sources = sorted(UI_DIR.glob("WorkspacePanel*.cs"))
    if not workspace_sources:
        failures.append("no WorkspacePanel partial sources were found")

    if failures:
        print("Raft visible Add initialization preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    text = SOURCE.read_text(encoding="utf-8")
    workflow_text = WORKFLOW.read_text(encoding="utf-8")
    workspace_texts = {path: path.read_text(encoding="utf-8") for path in workspace_sources}

    required = {
        "deterministic field registration": "private static readonly bool _raftVisibleAddRouteRegistered = RegisterRaftVisibleAddRoute();",
        "bool registration routine": "private static bool RegisterRaftVisibleAddRoute()",
        "registration completion": "return true;",
        "WPF class-handler registration": "EventManager.RegisterClassHandler(",
        "rendered + Add label": 'RaftVisibleAddLabel = "+ Add"',
        "raft subtype guard": "panel.IsRaftSubtypeFilter()",
        "handled routed click": "e.Handled = true;",
        "direct raft family creation": "panel.CreateFamilyFromWorkspaceSubtype(false);",
    }
    failures.extend(label + ": missing " + repr(token) for label, token in required.items() if token not in text)

    # WorkspacePanel is one partial CLR type. An explicit static constructor anywhere on that
    # partial type suppresses beforefieldinit and makes all static field initializers run during
    # type initialization. Count across every partial source so a second cctor cannot false-green
    # this guard and later fail the V25 compile with CS0111.
    ctor_token = "static WorkspacePanel()"
    ctor_locations = [path for path, source_text in workspace_texts.items() if ctor_token in source_text]
    ctor_count = sum(source_text.count(ctor_token) for source_text in workspace_texts.values())
    if ctor_count != 1:
        names = ", ".join(path.name for path in ctor_locations) or "none"
        failures.append("WorkspacePanel partial type must define exactly one explicit static constructor across all sources; found %d in %s" % (ctor_count, names))
    if ctor_token in text:
        failures.append("raft visible Add route must reuse the existing WorkspacePanel type initializer, not define a second static constructor")

    if text.count("_raftVisibleAddRouteRegistered = RegisterRaftVisibleAddRoute();") != 1:
        failures.append("visible Add route must register exactly once through the type-initialized static field")
    if text.count("EventManager.RegisterClassHandler(") != 1:
        failures.append("visible Add route must register exactly one WPF class handler")

    dispatch = "panel.CreateFamilyFromWorkspaceSubtype(false);"
    combined_dispatch_count = text.count(dispatch) + workflow_text.count(dispatch)
    if text.count(dispatch) != 1 or combined_dispatch_count != 1:
        failures.append("Móng Bè + Add must have exactly one Family creation dispatch across visible-route and legacy workflow sources")
    if "IsWorkspaceAddFamilyButton(button)" in workflow_text:
        failures.append("legacy raft workflow handler must not own the Add route; visible + Add route is authoritative")

    if failures:
        print("Raft visible Add initialization preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: WorkspacePanel has one global type initializer, deterministically registers one authoritative rendered + Add route, and dispatches exactly one raft Family creation call.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
