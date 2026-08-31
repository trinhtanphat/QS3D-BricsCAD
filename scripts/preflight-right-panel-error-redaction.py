#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    for forbidden in ("ex.Message", "+ ex.Message", "catch (Exception ex)"):
        if forbidden in text:
            errors.append("Right Panel user-visible failure surfaces must not retain raw exception detail: " + forbidden)

    required_constants = (
        "RefreshFailureStatus",
        "RefreshWarningSuffix",
        "ClearSelectionFailureStatus",
        "XrefSelectionFailureStatus",
        "LayerVisibilityFailureStatus",
        "LayerLockFailureStatus",
        "XrefReloadFailureStatus",
        "XrefMoveFailureStatus",
        "XrefDetachFailureStatus",
        "CommandDispatchFailureStatus",
    )
    for token in required_constants:
        if "private const string " + token not in text:
            errors.append("Right Panel missing stable redacted status constant: " + token)

    method_contracts = {
        "Refresh": ("RefreshFailureStatus", "RefreshDrawingsOnly();", "ReloadLayers();"),
        "RefreshAfterXrefMutation": ("RefreshWarningSuffix", "RefreshDrawingsOnly();", "ReloadLayers();"),
        "OnClearDrawingSelectionClick": ("ClearSelectionFailureStatus", "SetImpliedSelection(Array.Empty<ObjectId>())"),
        "OnDrawingSelectionChanged": ("ClearSelectionFailureStatus", "XrefSelectionFailureStatus", "XrefService.SelectInstances"),
        "SetLayerFromCheckBox": ("LayerVisibilityFailureStatus", "TryReloadLayersAfterFailure();", "LayerVisibilityService.SetVisible"),
        "SetSelectedLayers": ("LayerVisibilityFailureStatus", "TryReloadLayersAfterFailure();", "LayerVisibilityService.SetVisible"),
        "SetSelectedLayerLocks": ("LayerLockFailureStatus", "TryRefreshLayersAndDrawingsAfterFailure();", "LayerVisibilityService.SetLocked"),
        "OnReloadXrefClick": ("XrefReloadFailureStatus", "XrefService.Reload"),
        "OnMoveDrawingClick": ("XrefMoveFailureStatus", "XrefService.SelectInstances", 'TrySend(doc, "_MOVE")'),
        "OnDeleteDrawingClick": ("XrefDetachFailureStatus", "XrefService.Detach"),
        "TrySend": ("CommandDispatchFailureStatus", "SendStringToExecute", "return false;"),
    }

    method_names = list(method_contracts)
    for index, method in enumerate(method_names):
        start = text.find(" " + method + "(")
        if start < 0:
            errors.append("Right Panel missing bounded method: " + method)
            continue
        end_candidates = [text.find("\n        private ", start + 1), text.find("\n        public ", start + 1)]
        end_candidates = [value for value in end_candidates if value >= 0]
        end = min(end_candidates) if end_candidates else len(text)
        body = text[start:end]
        for token in method_contracts[method]:
            if token not in body:
                errors.append(method + " missing failure/behavior contract: " + token)
        if "catch (Exception ex)" in body or "ex.Message" in body:
            errors.append(method + " still leaks raw exception text")

    for helper, primary_status in (
        ("TryReloadLayersAfterFailure", "LayerVisibilityFailureStatus"),
        ("TryRefreshLayersAndDrawingsAfterFailure", "LayerLockFailureStatus"),
    ):
        start = text.find(" " + helper + "(")
        if start < 0:
            errors.append("Right Panel missing bounded recovery helper: " + helper)
            continue
        end = text.find("\n        private ", start + 1)
        body = text[start:end if end >= 0 else len(text)]
        for token in ("try", "catch (Exception)", primary_status, "RefreshWarningSuffix"):
            if token not in body:
                errors.append(helper + " missing best-effort recovery contract: " + token)

    if text.count("catch (Exception)") < 12:
        errors.append("Right Panel redaction guard expected all CAD failure catches to use non-binding Exception catches")

print("QS3D RightPanel error-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Right Panel CAD/Xref/layer failure surfaces use stable redacted statuses, preserve action ordering, and keep post-failure refresh best-effort without exposing host exception text.")
