#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILD3D = ROOT / "src" / "QS3D.BricsCAD.V25" / "Build3DCommands.cs"
VIEWPORT = ROOT / "src" / "QS3D.BricsCAD.V25" / "ViewportCommands.cs"


def fail(message: str) -> None:
    print(f"build3d-preserve-viewport preflight: FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label} is missing required token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(f"{label} contains forbidden token: {token}")


for required_path, label in (
    (BUILD3D, "Build3D command source"),
    (VIEWPORT, "viewport command source"),
):
    if not required_path.is_file():
        fail(f"missing {label}: {required_path.relative_to(ROOT)}")

build3d = BUILD3D.read_text(encoding="utf-8")
viewport = VIEWPORT.read_text(encoding="utf-8")

# Successful native builds must keep the current user camera. UI synchronization is still expected:
# refresh palette, regen the current view, select the generated solid/source, and report status.
for token in (
    '[CommandMethod("QS3DBUILD3D", CommandFlags.UsePickSet)]',
    'PaletteCoordinator.RefreshProject();',
    'document.Editor.Regen();',
    'CadHandleService.Select(document, generatedHandles)',
    'CadHandleService.Select(document, sourceHandles)',
    'PaletteCoordinator.SetStatus(status);',
):
    require(build3d, token, "Build3D command source")

# Regression guard: QS3DBUILD3D must never force the 3D view/Zoom Extents command after each build.
forbid(build3d, 'SendStringToExecute("QS3DVIEW3D', "Build3D command source")

# Preserve the explicit/manual command. The user can still request isometric + Zoom Extents intentionally.
require(viewport, '[CommandMethod("QS3DVIEW3D", CommandFlags.Modal)]', "viewport command source")
require(viewport, '_.VPOINT 1,-1,1 _.ZOOM _E', "viewport command source")

print("build3d-preserve-viewport preflight: PASS")
