#!/usr/bin/env python3
"""Fail closed when Workspace ViewModel publishes raw exception details."""

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
    if ".Message" in text:
        fail("WorkspaceViewModel still references Exception.Message in a user-visible mutation path")

    helper = body(text, "private void ReportMutationFailure(string operation)")
    for token in ("Status =", "không hoàn tất", "Chi tiết nội bộ đã được ẩn", "Refresh Workspace"):
        if token not in helper:
            fail(f"stable mutation-failure helper missing {token!r}")

    active_family = body(text, "public void SetActiveFamily(ProjectFamily? family)")
    for token in (
        "catch (InvalidOperationException)",
        "ReportMutationFailure(\"Chọn Family\")",
        "ReferenceEquals(ownedFamily, family)",
        "ProjectFamilyActivationService.SetActive(project, family.Id)",
    ):
        if token not in active_family:
            fail(f"SetActiveFamily missing {token!r}")

    selected = body(text, "public void SetSelectedElement(ProjectElement? element)")
    for token in (
        "ReferenceEquals(ownedElement, element)",
        "catch (InvalidOperationException)",
        "ReportMutationFailure(\"Chọn cấu kiện\")",
    ):
        if token not in selected:
            fail(f"SetSelectedElement missing {token!r}")

    representative = (
        ("private string ApplyFamilyName(", "ReportMutationFailure(\"Đổi tên Family\")"),
        ("private string ApplyFamilyProperty(", "ReportMutationFailure(\"Cập nhật \" + DisplayNameFor(key))"),
        ("private string ApplyInstanceProperty(", "ProjectSemanticMutationExecutor.Execute("),
        ("private void ResetInstanceProperty(", "ReportMutationFailure(\"Đặt lại Instance\")"),
        ("private bool TryGetCurrentProjectForMutation(", "ReferenceEquals(current, _project)"),
    )
    for signature, token in representative:
        block = body(text, signature)
        if token not in block:
            fail(f"{signature} missing preserved mutation/redaction contract {token!r}")

    mutation = body(text, "private string ApplyInstanceProperty(")
    for token in (
        "ReportMutationFailure(\"Cập nhật Instance\")",
        "ReportMutationFailure(\"Cập nhật \" + DisplayNameFor(key))",
        "ReferenceEquals(ownedElement, element)",
        "ReferenceEquals(ownedFamily, family)",
    ):
        if token not in mutation:
            fail(f"ApplyInstanceProperty missing {token!r}")

    current = body(text, "private bool TryGetCurrentProjectForMutation(")
    if "ReportMutationFailure(operation)" not in current:
        fail("current-project mutation gate does not use stable redacted failure reporting")

    if re.search(r"catch\s*\(\s*InvalidOperationException\s+\w+\s*\)", text):
        fail("WorkspaceViewModel still captures InvalidOperationException solely for user-visible detail publication")

    print("OK: V25 Workspace ViewModel mutation failures are exception-redacted while affinity and mutation contracts remain intact.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
