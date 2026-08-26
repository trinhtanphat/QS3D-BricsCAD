#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RaftFoundationVisibleAddRoute.cs"
WORKFLOW = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RaftFoundationWorkflow.cs"


def main():
    failures = []
    if not SOURCE.is_file():
        failures.append("missing raft visible Add route source")
    if not WORKFLOW.is_file():
        failures.append("missing raft workflow source")
    if failures:
        print("Raft visible Add initialization preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    text = SOURCE.read_text(encoding="utf-8")
    workflow_text = WORKFLOW.read_text(encoding="utf-8")
    required = {
        "explicit WorkspacePanel type initializer": "static WorkspacePanel()",
        "deterministic visible Add route registration": "RegisterRaftVisibleAddRoute();",
        "void registration routine": "private static void RegisterRaftVisibleAddRoute()",
        "WPF class-handler registration": "EventManager.RegisterClassHandler(",
        "rendered + Add label": 'RaftVisibleAddLabel = "+ Add"',
        "raft subtype guard": "panel.IsRaftSubtypeFilter()",
        "handled routed click": "e.Handled = true;",
        "direct raft family creation": "panel.CreateFamilyFromWorkspaceSubtype(false);",
    }
    failures.extend(label + ": missing " + repr(token) for label, token in required.items() if token not in text)

    if text.count("static WorkspacePanel()") != 1:
        failures.append("visible Add route must define exactly one explicit WorkspacePanel type initializer")
    if text.count("EventManager.RegisterClassHandler(") != 1:
        failures.append("visible Add route must register exactly one WPF class handler")

    dispatch = "panel.CreateFamilyFromWorkspaceSubtype(false);"
    combined_dispatch_count = text.count(dispatch) + workflow_text.count(dispatch)
    if text.count(dispatch) != 1 or combined_dispatch_count != 1:
        failures.append("Móng Bè + Add must have exactly one Family creation dispatch across visible-route and legacy workflow sources")
    if "IsWorkspaceAddFamilyButton(button)" in workflow_text:
        failures.append("legacy raft workflow handler must not own the Add route; visible + Add route is authoritative")

    # A side-effect-only static property initializer can leave the type marked beforefieldinit,
    # so CLR initialization timing is not a safe prerequisite for the first live + Add click.
    if "RaftVisibleAddRouteRegistered" in text:
        failures.append("visible Add route must not depend on side-effect-only static property initialization")

    if failures:
        print("Raft visible Add initialization preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: WorkspacePanel deterministically registers one authoritative rendered + Add route and exactly one raft Family creation dispatch across workflow sources.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
