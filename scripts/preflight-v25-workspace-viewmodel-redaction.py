#!/usr/bin/env python3
"""Fail closed when Workspace ViewModel family activation publishes raw exception details."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"


def fail(message: str) -> None:
    print(f"ERROR: V25 Workspace ViewModel redaction preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def body(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        fail(f"missing {signature}")
    brace = text.find("{", start)
    if brace < 0:
        fail(f"missing body for {signature}")
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    fail(f"unterminated body for {signature}")
    return ""


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")
    block = body(text, "public void SetActiveFamily(ProjectFamily? family)")

    if ".Message" in block:
        fail("SetActiveFamily still publishes Exception.Message")
    if re.search(r"catch\s*\(\s*InvalidOperationException\s+\w+\s*\)", block):
        fail("SetActiveFamily still captures InvalidOperationException for user-visible reporting")
    required = (
        "catch (InvalidOperationException)",
        "Status = \"Không thể chọn Family vì project hoặc Family đã thay đổi. Hãy Refresh Workspace và thử lại.\"",
        "ReferenceEquals(ownedFamily, family)",
        "ProjectFamilyActivationService.SetActive(project, family.Id)",
    )
    for token in required:
        if token not in block:
            fail(f"SetActiveFamily missing {token!r}")

    print("OK: V25 Workspace ViewModel family activation is exception-redacted and preserves affinity checks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
