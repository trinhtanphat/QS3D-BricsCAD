#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.XrefLock.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel.XrefLock.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    for forbidden in (
        "catch (Exception ex)",
        "ex.Message",
        "exception.Message",
        "GetBaseException()",
        "StackTrace",
    ):
        if forbidden in text:
            errors.append("Xref lock/unlock user surface leaks host exception detail: " + forbidden)

    required = (
        "private const string XrefInstanceLockFailureStatus",
        "private const string XrefInstanceUnlockFailureStatus",
        "private void SetSelectedXrefInstanceLayerLocks(bool locked)",
        "XrefService.SetInstanceLayersLocked(document, item.Name, locked)",
        "RefreshAfterXrefMutation(status);",
        "catch (Exception)",
        "var failureStatus = locked ? XrefInstanceLockFailureStatus : XrefInstanceUnlockFailureStatus;",
        "_viewModel.Status = failureStatus;",
        "RefreshDrawingsOnly();",
        "ReloadLayers();",
        "_viewModel.Status = failureStatus + RefreshWarningSuffix;",
    )
    for token in required:
        if token not in text:
            errors.append("missing Xref lock/unlock failure-isolation token: " + token)

    start = text.find("private void SetSelectedXrefInstanceLayerLocks(bool locked)")
    body = text[start:] if start >= 0 else ""
    ordered = (
        "XrefService.SetInstanceLayersLocked(document, item.Name, locked)",
        "RefreshAfterXrefMutation(status);",
        "catch (Exception)",
        "_viewModel.Status = failureStatus;",
        "RefreshDrawingsOnly();",
        "ReloadLayers();",
        "_viewModel.Status = failureStatus + RefreshWarningSuffix;",
    )
    positions = [body.find(token) for token in ordered]
    if min(positions) < 0 or positions != sorted(positions) or len(set(positions)) != len(positions):
        errors.append("Xref lock/unlock must preserve native mutation before success refresh, then stable failure status before best-effort recovery")

    if text.count("catch (Exception)") < 2:
        errors.append("Xref lock/unlock must independently catch primary native failure and secondary recovery failure")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Right Panel Xref lock/unlock failures are redacted, primary failure status is stable, and secondary panel recovery is independently fail-isolated.")
