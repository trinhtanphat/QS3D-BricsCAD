#!/usr/bin/env python3
from pathlib import Path
import sys

SOURCE = Path("src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs")


def fail(message: str) -> None:
    print(f"preflight-v25-coordination-review-attachment-rollback: FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")

    required = [
        "private readonly WrapPanel _reviewPanel;",
        "_reviewPanel = new WrapPanel",
        "RemoveReviewPanelBestEffort();",
        "private void RemoveReviewPanelBestEffort()",
        "_root.Children.Remove(_reviewPanel)",
    ]
    for token in required:
        if token not in text:
            fail(f"missing atomic visual rollback contract: {token}")

    catch_start = text.find("catch\n                {", text.find("public void Attach()"))
    if catch_start < 0:
        fail("Attach failure path not found")
    catch_end = text.find("throw;", catch_start)
    if catch_end < 0:
        fail("Attach failure rethrow not found")
    catch_body = text[catch_start:catch_end]
    if "RemoveReviewPanelBestEffort();" not in catch_body:
        fail("Attach failure does not roll back the inserted Review CAD panel before rethrow")

    dispose_start = text.find("public void Dispose()")
    dispose_end = text.find("private void DetachHandlersBestEffort()", dispose_start)
    if dispose_start < 0 or dispose_end < 0:
        fail("Dispose lifecycle not found")
    if "RemoveReviewPanelBestEffort();" not in text[dispose_start:dispose_end]:
        fail("Dispose does not own visual-tree cleanup")

    print("preflight-v25-coordination-review-attachment-rollback: PASS")


if __name__ == "__main__":
    main()
