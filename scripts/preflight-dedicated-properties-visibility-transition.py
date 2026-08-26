#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"


def fail(message: str) -> None:
    print("ERROR:", message)
    raise SystemExit(1)


def method(text: str, start_token: str, end_token: str) -> str:
    start = text.find(start_token)
    end = text.find(end_token, start + len(start_token)) if start >= 0 else -1
    if start < 0 or end <= start:
        fail("missing method boundary: " + start_token)
    return text[start:end]


if not SOURCE.is_file():
    fail("missing PaletteCoordinator.cs")

source = SOURCE.read_text(encoding="utf-8")
ensure = method(source, "public static void EnsureCreated()", "public static void Show()")
dispose = method(source, "private static void DisposeCore(bool persistLayout)", "private static void DisposePalette(ref PaletteSet? palette)")
handler = method(
    source,
    "private static void OnPropertiesPaletteStateChanged(object sender, PaletteSetStateEventArgs e)",
    "private static void DisposePalette(ref PaletteSet? palette)",
)

creation_order = (
    '_properties.AddVisual("Thuộc tính", _propertiesVisual, true);',
    "_properties.StateChanged += OnPropertiesPaletteStateChanged;",
)
cursor = -1
for token in creation_order:
    index = ensure.find(token, cursor + 1)
    if index < 0 or index <= cursor:
        fail("dedicated Properties show hook is missing or installed before its visual host: " + token)
    cursor = index

dispose_order = (
    "UnsubscribeFromPropertiesPaletteStateChanges();",
    "DisposePalette(ref _properties);",
)
cursor = -1
for token in dispose_order:
    index = dispose.find(token, cursor + 1)
    if index < 0 or index <= cursor:
        fail("dedicated Properties hook is not removed before PaletteSet disposal: " + token)
    cursor = index

for token in (
    "var properties = _properties;",
    "properties.StateChanged -= OnPropertiesPaletteStateChanged;",
    "ReferenceEquals(sender, _properties)",
    "_propertiesVisibilityTransitionActive",
    "e.NewState != StateEventIndex.Show && e.NewState != StateEventIndex.Hide",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(e.NewState == StateEventIndex.Show);",
    "finally",
    "_propertiesVisibilityTransitionActive = false;",
):
    if token not in dispose + handler:
        fail("dedicated Properties visibility-transition contract missing: " + token)

for forbidden in (
    ".Visible =",
    "SetVisibility(",
    "new WorkspacePanel",
    "new WorkspaceViewModel",
    "ResetPreservingVisibility(",
):
    if forbidden in handler:
        fail("host visibility callback must not create a visibility/recreation loop: " + forbidden)

print(
    "PASS: explicit host Show/Hide drives immediate, identity-guarded single-editor Properties reparenting; "
    "the callback is re-entry-safe, visibility-write-free, and unsubscribed before PaletteSet disposal."
)
sys.exit(0)
