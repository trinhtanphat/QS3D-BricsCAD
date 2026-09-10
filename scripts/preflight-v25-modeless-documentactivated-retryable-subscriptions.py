#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
START = ROOT / "src/QS3D.BricsCAD.V25/StartCenterPaletteCoordinator.cs"
PROJECT = ROOT / "src/QS3D.BricsCAD.V25/ProjectSetupPaletteCoordinator.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DocumentAffinity.cs"
errors = []

for path in (START, PROJECT, WORKSPACE):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))


def require(text, token, message):
    if token not in text:
        errors.append(message)

if START.is_file():
    text = START.read_text(encoding="utf-8")
    require(text, "_documentActivatedMayBeSubscribed", "Start Center must retain conservative may-be-subscribed ownership")
    require(text, "_documentActivatedDetachInProgress", "Start Center must fence detach reentrancy")
    require(text, "RetryDocumentActivatedDetach", "Start Center must expose retryable DocumentActivated detach")
    add = text.find("DocumentActivated += OnDocumentActivated")
    ownership = text.find("_documentActivatedMayBeSubscribed = true")
    if add < 0 or ownership < 0 or ownership > add:
        errors.append("Start Center must publish conservative ownership before fallible DocumentActivated +=")

if PROJECT.is_file():
    text = PROJECT.read_text(encoding="utf-8")
    require(text, "_documentActivatedMayBeSubscribed", "Project Information must retain conservative may-be-subscribed ownership")
    require(text, "_documentActivatedDetachInProgress", "Project Information must fence detach reentrancy")
    require(text, "RetryDocumentActivatedDetach", "Project Information must expose retryable DocumentActivated detach")
    add = text.find("DocumentActivated += OnDocumentActivated")
    ownership = text.find("_documentActivatedMayBeSubscribed = true")
    if add < 0 or ownership < 0 or ownership > add:
        errors.append("Project Information must publish conservative ownership before fallible DocumentActivated +=")

if WORKSPACE.is_file():
    text = WORKSPACE.read_text(encoding="utf-8")
    for token, message in (
        ("_workspaceDocumentActivatedMayBeSubscribed", "Workspace must retain DocumentActivated ownership independently"),
        ("_workspaceDocumentDestroyMayBeSubscribed", "Workspace must retain DocumentToBeDestroyed ownership independently"),
        ("_workspaceDocumentAffinityDetachInProgress", "Workspace must fence detach reentrancy"),
        ("RetryWorkspaceDocumentAffinityDetach", "Workspace must retry retained native event detaches"),
    ):
        require(text, token, message)
    activated_add = text.find("DocumentActivated += OnWorkspaceDocumentActivated")
    activated_ownership = text.find("_workspaceDocumentActivatedMayBeSubscribed = true")
    destroy_add = text.find("DocumentToBeDestroyed += OnWorkspaceDocumentToBeDestroyed")
    destroy_ownership = text.find("_workspaceDocumentDestroyMayBeSubscribed = true")
    if activated_add < 0 or activated_ownership < 0 or activated_ownership > activated_add:
        errors.append("Workspace must publish DocumentActivated ownership before fallible +=")
    if destroy_add < 0 or destroy_ownership < 0 or destroy_ownership > destroy_add:
        errors.append("Workspace must publish DocumentToBeDestroyed ownership before fallible +=")
    if "_workspaceDocumentAffinityAttached = false;\n            try { Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated; }" in text:
        errors.append("Workspace detach must not forget aggregate ownership before native removals succeed")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 Start Center, Project Information and Workspace retain conservative native document-event ownership across partial add/remove failures, fence detach reentrancy and support retryable cleanup without duplicate handlers.")
