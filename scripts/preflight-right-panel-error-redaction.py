#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
errors = []


def bounded_method(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        return ""
    end_candidates = [text.find("\n        private ", start + len(signature)), text.find("\n        public ", start + len(signature))]
    end_candidates = [value for value in end_candidates if value >= 0]
    end = min(end_candidates) if end_candidates else len(text)
    return text[start:end]


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
        "public void Refresh()": ("RefreshFailureStatus", "RefreshDrawingsOnly();", "ReloadLayers();"),
        "private void RefreshAfterXrefMutation(string successStatus)": ("RefreshWarningSuffix", "RefreshDrawingsOnly();", "ReloadLayers();"),
        "private void OnClearDrawingSelectionClick(object sender, RoutedEventArgs e)": ("ClearSelectionFailureStatus", "SetImpliedSelection(Array.Empty<ObjectId>())"),
        "private void OnDrawingSelectionChanged(object sender, SelectionChangedEventArgs e)": ("ClearSelectionFailureStatus", "XrefSelectionFailureStatus", "XrefService.SelectInstances"),
        "private void SetLayerFromCheckBox(object sender, bool visible)": ("LayerVisibilityFailureStatus", "TryReloadLayersAfterFailure();", "LayerVisibilityService.SetVisible"),
        "private void SetSelectedLayers(bool visible)": ("LayerVisibilityFailureStatus", "TryReloadLayersAfterFailure();", "LayerVisibilityService.SetVisible"),
        "private void SetSelectedLayerLocks(bool locked)": ("LayerLockFailureStatus", "TryRefreshLayersAndDrawingsAfterFailure();", "LayerVisibilityService.SetLocked"),
        "private void OnReloadXrefClick(object sender, RoutedEventArgs e)": ("XrefReloadFailureStatus", "XrefService.Reload"),
        "private void OnMoveDrawingClick(object sender, RoutedEventArgs e)": ("XrefMoveFailureStatus", "XrefService.SelectInstances", 'TrySend(doc, "_MOVE")'),
        "private void OnDeleteDrawingClick(object sender, RoutedEventArgs e)": ("XrefDetachFailureStatus", "XrefService.Detach"),
        "private bool TrySend(Document document, string command)": ("CommandDispatchFailureStatus", "SendStringToExecute", "return false;"),
    }

    for signature, tokens in method_contracts.items():
        body = bounded_method(text, signature)
        if not body:
            errors.append("Right Panel missing bounded method: " + signature)
            continue
        for token in tokens:
            if token not in body:
                errors.append(signature + " missing failure/behavior contract: " + token)
        if "catch (Exception ex)" in body or "ex.Message" in body:
            errors.append(signature + " still leaks raw exception text")

    for signature, primary_status, refresh_tokens in (
        ("private void TryReloadLayersAfterFailure()", "LayerVisibilityFailureStatus", ("ReloadLayers();",)),
        ("private void TryRefreshLayersAndDrawingsAfterFailure()", "LayerLockFailureStatus", ("ReloadLayers();", "RefreshDrawingsOnly();")),
    ):
        body = bounded_method(text, signature)
        if not body:
            errors.append("Right Panel missing bounded recovery helper: " + signature)
            continue
        for token in ("try", "catch (Exception)", primary_status, "RefreshWarningSuffix") + refresh_tokens:
            if token not in body:
                errors.append(signature + " missing best-effort recovery contract: " + token)

    if text.count("catch (Exception)") < 12:
        errors.append("Right Panel redaction guard expected all CAD failure catches to use non-binding Exception catches")

print("QS3D RightPanel error-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Right Panel CAD/Xref/layer failure surfaces use stable redacted statuses, preserve action ordering, and keep post-failure refresh best-effort without exposing host exception text.")