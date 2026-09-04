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

    constructor_start = text.find("public Controller(")
    attach_start = text.find("public void Attach()", constructor_start)
    if constructor_start < 0 or attach_start < 0:
        fail("Controller construction/Attach lifecycle not found")
    constructor_body = text[constructor_start:attach_start]
    if "_root.Children.Insert" not in constructor_body or "_reviewPanel" not in constructor_body:
        fail("constructor does not publish the exact tracked review panel")

    catch_start = text.find("catch\n                {", attach_start)
    if catch_start < 0:
        fail("Attach failure path not found")
    catch_end = text.find("throw;", catch_start)
    if catch_end < 0:
        fail("Attach failure rethrow not found")
    catch_body = text[catch_start:catch_end]
    if "DetachHandlersBestEffort();" not in catch_body:
        fail("Attach failure no longer detaches partial subscriptions")
    if "DisposeSessionBestEffort();" not in catch_body:
        fail("Attach failure no longer disposes transient CAD review state")
    if "RemoveReviewPanelBestEffort();" not in catch_body:
        fail("Attach failure does not roll back the inserted Review CAD panel before rethrow")

    dispose_start = text.find("public void Dispose()")
    dispose_end = text.find("private void DetachHandlersBestEffort()", dispose_start)
    if dispose_start < 0 or dispose_end < 0:
        fail("Dispose lifecycle not found")
    dispose_body = text[dispose_start:dispose_end]
    if "RemoveReviewPanelBestEffort();" not in dispose_body:
        fail("Dispose does not own visual-tree cleanup")

    helper_start = text.find("private void RemoveReviewPanelBestEffort()")
    helper_end = text.find("private void ", helper_start + 1)
    if helper_start < 0:
        fail("visual rollback helper missing")
    helper_body = text[helper_start:helper_end if helper_end >= 0 else len(text)]
    if "_reviewPanel.Parent" not in helper_body:
        fail("visual rollback does not verify parent affinity")
    if "ReferenceEquals" not in helper_body:
        fail("visual rollback is not identity-affine to the captured root")
    if "_root.Children.Remove(_reviewPanel)" not in helper_body:
        fail("visual rollback does not remove the exact tracked panel")

    print("preflight-v25-coordination-review-attachment-rollback: PASS")


if __name__ == "__main__":
    main()
