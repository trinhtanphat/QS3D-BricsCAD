#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.RaftFoundationVisibleAddRoute.cs"


def main():
    if not SOURCE.is_file():
        print("ERROR: missing raft visible Add route source")
        return 1

    text = SOURCE.read_text(encoding="utf-8")
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
    failures = [label + ": missing " + repr(token) for label, token in required.items() if token not in text]

    if text.count("static WorkspacePanel()") != 1:
        failures.append("visible Add route must define exactly one explicit WorkspacePanel type initializer")
    if text.count("EventManager.RegisterClassHandler(") != 1:
        failures.append("visible Add route must register exactly one WPF class handler")

    # A side-effect-only static property initializer can leave the type marked beforefieldinit,
    # so CLR initialization timing is not a safe prerequisite for the first live + Add click.
    if "RaftVisibleAddRouteRegistered" in text:
        failures.append("visible Add route must not depend on side-effect-only static property initialization")

    if failures:
        print("Raft visible Add initialization preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: WorkspacePanel deterministically registers exactly one rendered + Add raft route before live use.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
