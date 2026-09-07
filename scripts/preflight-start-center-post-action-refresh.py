#!/usr/bin/env python3
"""Regression guard for C03 issue #5990.

A successful Start Center CAD/file action is an authoritative success boundary.
Display-only refresh after that boundary must not share the action-failure catch,
otherwise a later UI refresh exception can falsely report that the CAD action failed.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterPanel.cs"


def method_body(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise AssertionError(f"missing method: {signature}")
    brace = text.find("{", start)
    if brace < 0:
        raise AssertionError(f"missing body for: {signature}")
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    raise AssertionError(f"unterminated body for: {signature}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    body = method_body(source, "private void RunUiAction(Action action)")

    action_index = body.find("action();")
    action_catch_index = body.find("catch", action_index)
    refresh_index = body.find("RefreshFromActiveDocument();")
    if min(action_index, action_catch_index, refresh_index) < 0:
        raise AssertionError("RunUiAction must invoke action, handle action failure, and refresh after success")

    # The post-success refresh must occur after the action's catch boundary.  If it
    # appears before that catch, the same catch can turn a committed CAD success
    # into a false UI failure.
    if refresh_index < action_catch_index:
        raise AssertionError(
            "Start Center post-action refresh is still inside the action failure boundary"
        )

    # A failed action must stop before any post-success refresh path is reached.
    catch_tail = body[action_catch_index:refresh_index]
    if "return;" not in catch_tail:
        raise AssertionError(
            "RunUiAction action-failure catch must return before post-success refresh"
        )

    # Display refresh is best-effort.  Requiring an explicit second try/catch keeps
    # source ordering deterministic and prevents future recoupling.
    post_success = body[refresh_index:]
    if "catch" not in post_success:
        raise AssertionError(
            "post-success Start Center refresh must have its own best-effort failure boundary"
        )

    failure_text = "Thao tác không thể hoàn tất an toàn. Hãy kiểm tra trạng thái bản vẽ và thử lại."
    failure_index = body.find(failure_text)
    if failure_index < action_catch_index or failure_index > refresh_index:
        raise AssertionError(
            "operation-failure status must belong only to the action-failure path"
        )

    print("PASS: Start Center preserves action truth across post-success UI refresh failures")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
