#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.ReferenceDetail.cs").read_text(encoding="utf-8")

errors = []

required = [
    "ReferenceDetailRegistrationReady",
    "EventManager.RegisterClassHandler",
    "typeof(RightPanel)",
    "FrameworkElement.LoadedEvent",
    "_referenceDetailApplied",
    "DrawingList",
    "LayerList",
    'SelectedItemBinding(DrawingList, "Name"',
    'SelectedItemBinding(DrawingList, "Path"',
    'SelectedItemBinding(DrawingList, "Kind"',
    'SelectedItemBinding(DrawingList, "LockState"',
    'SelectedItemBinding(DrawingList, "InstanceText"',
    'SelectedItemBinding(DrawingList, "ScaleText"',
    'SelectedItemBinding(LayerList, "Name"',
    'SelectedItemBinding(LayerList, "IsVisible"',
    'SelectedItemBinding(LayerList, "IsLocked"',
    'SelectedItemBinding(LayerList, "ColorIndex"',
    'SelectedItemBinding(LayerList, "ColorBrush"',
    "BẢN VẼ / XREF ĐANG CHỌN",
    "LỚP ĐANG CHỌN",
]

for token in required:
    if token not in source:
        errors.append(f"missing RightPanel reference-detail contract: {token}")

for forbidden in [
    "SendStringToExecute",
    "LockDocument",
    "StartTransaction",
    "CommandMethod(",
    "XrefService",
    "LayerVisibilityService",
    "ProjectContextCoordinator",
    "ProjectState",
    "PaletteCoordinator",
    "DocumentLifecycleCoordinator",
]:
    if forbidden in source:
        errors.append(f"presentation-only RightPanel detail must not own mutation/lifecycle surface: {forbidden}")

if "Children.Add(CreateDrawingReferenceDetailCard())" not in source or "Children.Add(CreateLayerReferenceDetailCard())" not in source:
    errors.append("both drawing and layer selected-target detail cards must be installed")

if "TargetNullValue = fallback" not in source or "FallbackValue = fallback" not in source:
    errors.append("selected-target bindings must remain null/fallback safe")

if errors:
    print("Owner-reference RightPanel detail preflight FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)

print("Owner-reference RightPanel detail preflight PASSED")
