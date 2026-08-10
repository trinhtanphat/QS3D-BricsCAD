#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs"


def fail(message: str) -> int:
    print("ERROR:", message)
    return 1


def method_body(text: str, name: str) -> str:
    marker = f"private void {name}"
    start = text.find(marker)
    if start < 0:
        raise ValueError(f"missing method {name}")
    brace = text.find("{", start)
    if brace < 0:
        raise ValueError(f"missing body for {name}")
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    raise ValueError(f"unterminated method {name}")


def main() -> int:
    if not TARGET.exists():
        return fail(f"missing {TARGET.relative_to(ROOT)}")
    text = TARGET.read_text(encoding="utf-8")

    try:
        save = method_body(text, "OnSaveClick")
        recalc = method_body(text, "OnRecalculateClick")
    except ValueError as exc:
        return fail(str(exc))

    required_global = [
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "RestoreOrThrow(project, rollback, operationError",
        "TrySyncCommittedUi(",
        "new AggregateException(operationError, restoreError)",
    ]
    for token in required_global:
        if token not in text:
            return fail(f"Curtain family atomicity contract missing token: {token}")

    for name, body, mutation in [
        ("OnSaveClick", save, "ApplyFamilyValue(project, family"),
        ("OnRecalculateClick", recalc, "element.MarkDirty(ElementDirtyFlags.Quantity)"),
    ]:
        capture = body.find("ProjectStateSnapshot.Capture(project)")
        mutate = body.find(mutation)
        restore = body.find("RestoreOrThrow(project, rollback, operationError")
        if capture < 0 or mutate < 0 or restore < 0:
            return fail(f"{name} is missing capture/mutation/rollback boundary")
        if capture > mutate:
            return fail(f"{name} mutates project before capturing rollback state")
        if restore < mutate:
            return fail(f"{name} rollback handler is not after the guarded mutation")
        if "TrySyncCommittedUi(" not in body:
            return fail(f"{name} must isolate post-commit UI synchronization")

    if len(re.findall(r"ProjectStateSnapshot\.Capture\(project\)", text)) < 2:
        return fail("Curtain save and recalculate must each capture a complete project snapshot")

    print("PASS: Curtain Family save/recalculate are project-atomic and post-commit UI sync is isolated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
