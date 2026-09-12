#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EXACT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.ExactFace.cs"
RAFT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.RaftHighlight.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: missing {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        fail(f"{label}: forbidden aggregate ownership marker {needle}")


def check(path: Path, prefix: str, handler: str) -> None:
    text = path.read_text(encoding="utf-8")
    label = path.name
    aggregate = f"_{prefix}DocumentEventsAttached"
    forbid(text, aggregate, label)

    deactivating = f"_{prefix}DocumentToBeDeactivatedAttached"
    became_current = f"_{prefix}DocumentBecameCurrentAttached"
    require(text, f"private bool {deactivating};", label)
    require(text, f"private bool {became_current};", label)

    require(text, f"if (!{deactivating})", label)
    require(text, f"documents.DocumentToBeDeactivated += {handler};", label)
    require(text, f"{deactivating} = true;", label)
    require(text, f"if (!{became_current})", label)
    require(text, f"documents.DocumentBecameCurrent += {handler};", label)
    require(text, f"{became_current} = true;", label)

    # Detach must be independently attempted. A failed remove must leave its
    # ownership bit set so a later unload/retry cannot double-subscribe.
    require(text, f"if ({deactivating})", label)
    require(text, f"documents.DocumentToBeDeactivated -= {handler};", label)
    require(text, f"{deactivating} = false;", label)
    require(text, f"if ({became_current})", label)
    require(text, f"documents.DocumentBecameCurrent -= {handler};", label)
    require(text, f"{became_current} = false;", label)


check(EXACT, "quantityExactFace", "OnQuantityExactFaceDocumentSwitch")
check(RAFT, "raftQuantityHighlight", "OnRaftQuantityDocumentSwitch")
print("PASS: Quantity Insight native document-event ownership is tracked per handler across partial attach/detach failure")
