#!/usr/bin/env python3
"""Guard H.3 shared native document-lifecycle ownership for V25 modeless windows."""

# Lane-Key: issue-3621 — P06 retained the native reactor/WPF teardown signature while
# per-window registrations still directly owned BricsCAD document/native callbacks.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
COORDINATOR_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundNativeLifecycleCoordinator.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"{signature} is missing.")
    brace = source.find("{", start)
    require(brace >= 0, f"{signature} body is missing.")
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError(f"{signature} body is unterminated.")


require(WINDOW_SOURCE.exists(), "DocumentBoundWindowLifetime source is missing.")
require(
    COORDINATOR_SOURCE.exists(),
    "H.3 requires a shared DocumentBoundNativeLifecycleCoordinator so per-window registrations no longer root BricsCAD document reactors.",
)

window_source = WINDOW_SOURCE.read_text(encoding="utf-8")
coordinator_source = COORDINATOR_SOURCE.read_text(encoding="utf-8")

# Registration must consume managed subscription callbacks only; native BricsCAD document events
# are owned centrally so final-host reactor teardown does not have one native callback set per WPF window.
for forbidden in (
    "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
):
    require(
        forbidden not in window_source,
        f"Per-window Registration must not directly own BricsCAD document/native lifecycle callback: {forbidden}",
    )

attach = method_block(window_source, "public void Attach(Document document)")
require(
    "DocumentBoundNativeLifecycleCoordinator.Register(" in attach,
    "Per-window registration must bind through the shared native lifecycle coordinator.",
)

for marker in (
    "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
):
    require(marker in coordinator_source, f"Shared native lifecycle coordinator is missing ownership marker: {marker}")

require(
    "ModelessHostQuiescenceCoordinator.IsQuiescing" in coordinator_source,
    "Shared native lifecycle coordinator must honor the global host-quiescence barrier.",
)
require(
    "Dictionary<IntPtr, Entry>" in coordinator_source,
    "Shared native lifecycle coordinator must key document ownership by stable native database identity.",
)

print("[OK] V25 document-bound modeless windows consume managed lifecycle callbacks while one shared coordinator owns BricsCAD document/native reactors by stable native database identity.")
