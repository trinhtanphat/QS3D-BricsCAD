#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

old_publish = "_lastTabId = currentId;\n\n                // BricsCAD may reconstruct"
if old_publish in text:
    raise SystemExit("FAIL workspace route retry guard: tab identity is published before routing succeeds")

required = {
    "HOME commits observed tab only after route": "RouteHomeSurface();\n                    _lastTabId = currentId;",
    "PROJECT commits observed tab only after route": "RouteProjectSurface();\n                    _lastTabId = currentId;",
    "non-QS3D route commits after both hides": "ProjectSetupPaletteCoordinator.Hide();\n                    _lastTabId = currentId;",
    "BIM commits after workspace reassert": "ReassertBimWorkspace();\n                _lastTabId = currentId;",
    "outer fail-soft containment": "catch\n            {\n                // Ribbon polling is presentation-only.",
}
missing = [name for name, token in required.items() if token not in text]
if missing:
    raise SystemExit("FAIL workspace route retry guard: missing " + ", ".join(missing))

home_route = text.index("RouteHomeSurface();")
home_publish = text.index("_lastTabId = currentId;", home_route)
project_route = text.index("RouteProjectSurface();")
project_publish = text.index("_lastTabId = currentId;", project_route)
if home_publish <= home_route or project_publish <= project_route:
    raise SystemExit("FAIL workspace route retry guard: HOME/PROJECT success publication ordering regressed")

print("PASS V25 workspace routing retries transient tab transitions until route completion")
