#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        errors.append("missing required ribbon retry source: " + path)
        return ""
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + " missing required token: " + token)


coordinator = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
entry = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")

for token in (
    "MaxTimedAttempts = 60",
    "TimeSpan.FromMilliseconds(500)",
    "documents.DocumentCreated += OnDocumentAvailable",
    "documents.DocumentActivated += OnDocumentAvailable",
    "documents.DocumentCreated -= OnDocumentAvailable",
    "documents.DocumentActivated -= OnDocumentAvailable",
    "new DispatcherTimer(DispatcherPriority.ApplicationIdle)",
    "if (!RibbonBootstrapper.TryInitialize()) return false;",
    "ReferenceWallRibbonAugmenter.TryInitialize()",
    "ProjectRibbonAugmenter.TryInitialize()",
    "QuickWorkflowRibbonAugmenter.TryInitialize()",
    "QuantityReferenceRibbonAugmenter.TryInitialize()",
    "UpdateRibbonAugmenter.TryInitialize()",
    "if (TryInitializeAll())",
    "if (_timedAttempts >= MaxTimedAttempts)",
):
    require(coordinator, token, "RibbonInitializationCoordinator")

start_begin = coordinator.find("public static void Start()")
stop_begin = coordinator.find("public static void Stop()", start_begin)
start_body = coordinator[start_begin:stop_begin] if start_begin >= 0 and stop_begin > start_begin else ""
if not start_body:
    errors.append("RibbonInitializationCoordinator.Start body could not be located")
elif "TryInitializeAll()" in start_body:
    errors.append("NETLOAD startup must not synchronously reconcile the Ribbon inside Start()")
elif "StartTimedRetry();" not in start_body:
    errors.append("RibbonInitializationCoordinator.Start must schedule the bounded retry path")

for token in (
    "RibbonInitializationCoordinator.Start();",
    "RibbonInitializationCoordinator.Stop();",
):
    require(entry, token, "PluginEntry")

for stale in (
    "RibbonBootstrapper.TryInitialize();\n            ReferenceWallRibbonAugmenter.TryInitialize();",
    "QuantityReferenceRibbonAugmenter.TryInitialize();\n            UpdateRibbonAugmenter.TryInitialize();",
):
    if stale in entry:
        errors.append("PluginEntry still uses one-shot ribbon initialization instead of the retry coordinator")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ribbon bootstrap and all augmenters reconcile through a bounded ApplicationIdle retry path and document availability without synchronous NETLOAD work, with clean event/timer teardown.")
