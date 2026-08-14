#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUGMENTER = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs"
REGISTRATION = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceTreeRegistration.cs"
errors = []


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"cannot read {path.relative_to(ROOT)}: {exc}")
        return ""


augmenter = read(AUGMENTER)
registration = read(REGISTRATION)

for token in (
    "private static readonly object RegistrationGate = new object();",
    "public static bool EnsureRegistered()",
    "lock (RegistrationGate)",
    "if (_registered) return true;",
    "EventManager.RegisterClassHandler(",
    "typeof(WorkspacePanel)",
    "FrameworkElement.LoadedEvent",
    "new RoutedEventHandler(OnWorkspaceLoaded)",
    "_registered = true;",
    "return true;",
    "catch",
    "return false;",
):
    if token not in augmenter:
        errors.append("reference Workspace tree registration contract missing: " + token)

register_call = augmenter.find("EventManager.RegisterClassHandler(")
latch = augmenter.find("_registered = true;", register_call)
failed_return = augmenter.find("return false;", latch)
if min(register_call, latch, failed_return) < 0 or not register_call < latch < failed_return:
    errors.append("registered latch must be set only after class-handler registration succeeds, with failure leaving retry available")

if augmenter.find("_registered = true;") < register_call:
    errors.append("reference Workspace tree registration must not latch success before RegisterClassHandler")

for token in (
    "internal static readonly bool ReferenceWorkspaceTreeRegistrationReady = RegisterReferenceWorkspaceTree();",
    "private static bool RegisterReferenceWorkspaceTree() =>",
    "ReferenceWorkspaceTreeAugmenter.EnsureRegistered();",
):
    if token not in registration:
        errors.append("WorkspacePanel type-initializer registration contract missing: " + token)

for forbidden in (
    "ReferenceWorkspaceTreeAugmenter.EnsureRegistered();\n            return true;",
    "throw;",
):
    if forbidden in registration:
        errors.append("WorkspacePanel registration must propagate the actual fail-safe result, not force success or rethrow")

if errors:
    print("Reference Workspace tree registration resilience preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print(
    "Reference Workspace tree registration resilience preflight PASS: presentation-only class-handler registration "
    "cannot poison WorkspacePanel type initialization, latches only after success, and remains retryable after failure."
)
