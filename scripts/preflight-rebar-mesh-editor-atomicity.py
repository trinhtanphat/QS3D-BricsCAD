#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs"


def fail(message: str) -> int:
    print("ERROR:", message)
    return 1


def method_body(text: str, marker: str) -> str:
    start = text.find(marker)
    if start < 0:
        raise ValueError("missing " + marker)
    brace = text.find("{", start)
    if brace < 0:
        raise ValueError("missing body for " + marker)
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    raise ValueError("unterminated " + marker)


def main() -> int:
    if not TARGET.exists() or not COMMANDS.exists():
        return fail("Rebar Mesh editor/command source is missing")
    text = TARGET.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")
    try:
        save = method_body(text, "private void OnSave")
        notify = method_body(text, "private void NotifySavedAfterCommit")
    except ValueError as exc:
        return fail(str(exc))

    required = [
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "RestoreOrThrow(project, rollback, operationError)",
        "new AggregateException(operationError, restoreError)",
        "NotifySavedAfterCommit();",
    ]
    for token in required:
        if token not in text:
            return fail("Rebar Mesh atomic save contract missing token: " + token)

    capture = save.find("ProjectStateSnapshot.Capture(project)")
    mutation = save.find("element.SetProperty(")
    notify_pos = save.find("NotifySavedAfterCommit();")
    if min(capture, mutation, notify_pos) < 0:
        return fail("Rebar Mesh save is missing capture/mutation/post-commit callback")
    if not capture < mutation < notify_pos:
        return fail("Rebar Mesh save ordering must be capture -> semantic mutation -> post-commit callback")
    if "_saved();" not in notify or "catch (Exception callbackError)" not in notify:
        return fail("Rebar Mesh callback must be isolated as post-commit UI synchronization")

    if "PaletteCoordinator.RefreshProject();" not in commands or "PaletteCoordinator.SetStatus(" not in commands:
        return fail("Rebar Mesh saved callback contract changed; re-audit whether callback is still UI-only")
    forbidden_callback_tokens = ["Regenerate", "ProjectContextCoordinator.GetOrCreate", "SetProperty(", "Touch()", "Transaction"]
    callback_start = commands.find("new RebarMeshSetupWindow")
    callback_end = commands.find("});", callback_start)
    callback = commands[callback_start:callback_end] if callback_start >= 0 and callback_end >= 0 else ""
    if not callback:
        return fail("Could not locate Rebar Mesh saved callback")
    for token in forbidden_callback_tokens:
        if token in callback:
            return fail("Rebar Mesh saved callback is no longer UI-only; transaction boundary needs re-audit: " + token)

    print("PASS: Rebar Mesh editor uses atomic semantic mutation and isolates its UI-only saved callback after commit.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
