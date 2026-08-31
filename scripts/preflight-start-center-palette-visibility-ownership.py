#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/StartCenterPaletteCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

show = re.search(
    r"public static void Show\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public static void Hide",
    text,
    re.S,
)
if not show:
    errors.append("StartCenterPaletteCoordinator.Show was not found")
else:
    body = show.group("body")
    required = (
        "var wasVisible = palette.Visible;",
        "var wasSubscribed = _documentActivatedSubscribed;",
        "SubscribeToDocumentActivation();",
        "palette.Visible = true;",
        "panel.RefreshFromActiveDocument();",
        "if (!wasVisible)",
        "palette.Visible = false;",
        "if (!wasSubscribed)",
        "UnsubscribeFromDocumentActivation();",
        "throw;",
    )
    for token in required:
        if token not in body:
            errors.append(f"Show lifecycle transaction missing: {token}")

    subscribe = body.find("SubscribeToDocumentActivation();")
    visible = body.find("palette.Visible = true;")
    refresh = body.find("panel.RefreshFromActiveDocument();")
    if min(subscribe, visible, refresh) >= 0 and not (subscribe < visible < refresh):
        errors.append("Show must subscribe before native visibility and refresh only after visibility succeeds")

    if "catch" not in body:
        errors.append("Show must rollback visibility/event ownership when native activation fails")

hide = re.search(
    r"public static void Hide\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public static void Dispose",
    text,
    re.S,
)
if not hide:
    errors.append("StartCenterPaletteCoordinator.Hide was not found")
else:
    body = hide.group("body")
    hide_visible = body.find("palette.Visible = false;")
    unsubscribe = body.find("UnsubscribeFromDocumentActivation();")
    if hide_visible < 0:
        errors.append("Hide must hide the exact current PaletteSet")
    if unsubscribe < 0:
        errors.append("Hide must release DocumentActivated ownership while dormant")
    if hide_visible >= 0 and unsubscribe >= 0 and hide_visible > unsubscribe:
        errors.append("Hide must release host event ownership only after native hide succeeds")
    if "return;" in body[:unsubscribe if unsubscribe >= 0 else len(body)]:
        errors.append("Hide must not bypass callback release when the PaletteSet is absent or already hidden")

unsubscribe = re.search(
    r"private static void UnsubscribeFromDocumentActivation\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static void OnDocumentActivated",
    text,
    re.S,
)
if not unsubscribe:
    errors.append("UnsubscribeFromDocumentActivation was not found")
else:
    body = unsubscribe.group("body")
    if "Application.DocumentManager.DocumentActivated -= OnDocumentActivated;" not in body:
        errors.append("DocumentActivated detach is missing")
    detach = body.find("Application.DocumentManager.DocumentActivated -= OnDocumentActivated;")
    clear = body.find("_documentActivatedSubscribed = false;")
    if clear < 0 or (detach >= 0 and clear < detach):
        errors.append("subscription ownership flag must clear only after native detach succeeds")
    if "catch" not in body:
        errors.append("failed native detach must remain retryable without escaping teardown")

if errors:
    print("Start Center palette visibility ownership preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS Start Center palette visibility and DocumentActivated ownership are transactional")
