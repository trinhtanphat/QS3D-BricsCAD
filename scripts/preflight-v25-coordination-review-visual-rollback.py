#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")

def require(token: str, message: str) -> None:
    if token not in text:
        print(f"ERROR: {message}")
        sys.exit(1)

def require_order(first: str, second: str, message: str) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        print(f"ERROR: {message}")
        sys.exit(1)

require("private readonly WrapPanel _review;", "review panel must be retained as exact controller-owned state")
require("private bool _reviewPublished;", "review panel publication state must be tracked")
require("PublishReviewPanel();", "Attach must publish the review panel explicitly")
require("RemoveReviewPanelBestEffort();", "failed attachment/disposal must remove the controller-owned panel")
require("_root.Children.Remove(_review)", "visual rollback must remove the exact review panel instance")

ctor_start = text.find("public Controller(")
attach_start = text.find("public void Attach()")
if ctor_start < 0 or attach_start < 0 or ctor_start >= attach_start:
    print("ERROR: unable to isolate Controller constructor/Attach blocks")
    sys.exit(1)
constructor = text[ctor_start:attach_start]
if "_root.Children.Insert" in constructor:
    print("ERROR: constructor publishes Review CAD panel before handler attachment can succeed")
    sys.exit(1)

require_order("_attachments |= Attachment.DocumentToBeDestroyed;", "PublishReviewPanel();", "visual publication must occur only after the complete subscription sequence")
require_order("PublishReviewPanel();", "_attached = true;", "Attach must not report success before visual publication completes")

catch_start = text.find("catch\n                {", attach_start)
catch_end = text.find("throw;", catch_start)
if catch_start < 0 or catch_end < 0:
    print("ERROR: unable to isolate Attach rollback block")
    sys.exit(1)
rollback = text[catch_start:catch_end]
if "RemoveReviewPanelBestEffort();" not in rollback:
    print("ERROR: Attach rollback does not remove a partially published Review CAD panel")
    sys.exit(1)

print("PASS: Coordination Manager review UI publication is attachment-atomic and rollback-safe")
