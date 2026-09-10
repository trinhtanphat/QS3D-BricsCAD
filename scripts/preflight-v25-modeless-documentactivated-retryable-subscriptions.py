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


def require_order(text, first, second, message):
    a = text.find(first)
    b = text.find(second, a + len(first)) if a >= 0 else -1
    if a < 0 or b < 0 or a >= b:
        errors.append(message)


def check_palette_coordinator(path, surface):
    if not path.is_file():
        return
    text = path.read_text(encoding="utf-8")
    require(text, "_documentActivatedMayBeSubscribed", f"{surface} must retain conservative may-be-subscribed ownership")
    require(text, "_documentActivatedDetachInProgress", f"{surface} must fence detach reentrancy")
    require(text, "RetryDocumentActivatedDetach", f"{surface} must expose retryable DocumentActivated detach")
    require_order(
        text,
        "_documentActivatedMayBeSubscribed = true",
        "DocumentActivated += OnDocumentActivated",
        f"{surface} must publish conservative ownership before fallible DocumentActivated +=",
    )
    require_order(
        text,
        "DocumentActivated -= OnDocumentActivated",
        "_documentActivatedMayBeSubscribed = false",
        f"{surface} must clear ownership only after exact native DocumentActivated -= succeeds",
    )
    require(text, "isVisible = palette.Visible;", f"{surface} must isolate native PaletteSet visibility reads")
    visibility_read = text.find("isVisible = palette.Visible;")
    retry_after_read = text.find("RetryDocumentActivatedDetach();", visibility_read)
    refresh = text.find("RefreshFromDocument", visibility_read)
    if visibility_read < 0 or retry_after_read < 0 or refresh < 0 or retry_after_read > refresh:
        errors.append(f"{surface} must route failed/stale native visibility reads to detach retry before UI refresh")


check_palette_coordinator(START, "Start Center")
check_palette_coordinator(PROJECT, "Project Information")

if WORKSPACE.is_file():
    text = WORKSPACE.read_text(encoding="utf-8")
    for token, message in (
        ("_workspaceDocumentActivatedMayBeSubscribed", "Workspace must retain DocumentActivated ownership independently"),
        ("_workspaceDocumentDestroyMayBeSubscribed", "Workspace must retain DocumentToBeDestroyed ownership independently"),
        ("_workspaceDocumentAffinityDetachInProgress", "Workspace must fence detach reentrancy"),
        ("RetryWorkspaceDocumentAffinityDetach", "Workspace must retry retained native event detaches"),
        ("TryInvalidateWorkspaceDocumentStateFromNativeCallback", "Workspace native callbacks must fail-soft through one containment helper"),
        ("if (!IsLoaded)", "Workspace callback containment must retry detach when the panel is no longer loaded"),
    ):
        require(text, token, message)
    require_order(
        text,
        "_workspaceDocumentActivatedMayBeSubscribed = true",
        "DocumentActivated += OnWorkspaceDocumentActivated",
        "Workspace must publish DocumentActivated ownership before fallible +=",
    )
    require_order(
        text,
        "_workspaceDocumentDestroyMayBeSubscribed = true",
        "DocumentToBeDestroyed += OnWorkspaceDocumentToBeDestroyed",
        "Workspace must publish DocumentToBeDestroyed ownership before fallible +=",
    )
    require_order(
        text,
        "DocumentActivated -= OnWorkspaceDocumentActivated",
        "_workspaceDocumentActivatedMayBeSubscribed = false",
        "Workspace must clear DocumentActivated ownership only after native removal succeeds",
    )
    require_order(
        text,
        "DocumentToBeDestroyed -= OnWorkspaceDocumentToBeDestroyed",
        "_workspaceDocumentDestroyMayBeSubscribed = false",
        "Workspace must clear DocumentToBeDestroyed ownership only after native removal succeeds",
    )
    if "_workspaceDocumentAffinityAttached = false;\n            try { Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated; }" in text:
        errors.append("Workspace detach must not forget aggregate ownership before native removals succeed")
    destroyed_callback = text.find("private void OnWorkspaceDocumentToBeDestroyed")
    mdi_read = text.find("Application.DocumentManager.MdiActiveDocument", destroyed_callback)
    callback_catch = text.find("catch (Exception)", mdi_read)
    helper_call = text.find("TryInvalidateWorkspaceDocumentStateFromNativeCallback();", callback_catch)
    if destroyed_callback < 0 or mdi_read < 0 or callback_catch < 0 or helper_call < 0:
        errors.append("Workspace DocumentToBeDestroyed must contain native active-document read failures and fail closed")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 Start Center, Project Information and Workspace retain conservative native document-event ownership across partial add/remove failures, clear ownership only after exact removal, fence detach reentrancy, and contain stale native callbacks without duplicate handlers.")
