from pathlib import Path

source = Path("src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs").read_text(encoding="utf-8")

errors = []

if "document.Database.UpdateExt(false)" in source:
    errors.append("cad_view_zoom_extents must not persistently update database extents")

required = [
    "RequireCompatibleViewAspect",
    "RequireCompatibleViewDirection",
    "requested view aspect",
    "requested view direction",
]
for token in required:
    if token not in source:
        errors.append(f"missing direct-view safety token: {token}")

set_start = source.find("private static string SetView")
apply_start = source.find("private static string ApplyExtents", set_start)
set_view = source[set_start:apply_start]

aspect_check = set_view.find("RequireCompatibleViewAspect")
direction_check = set_view.find("RequireCompatibleViewDirection")
center_write = set_view.find("view.CenterPoint =")
width_write = set_view.find("view.Width =")
direction_write = set_view.find("view.ViewDirection =")
set_current = set_view.find("document.Editor.SetCurrentView(view)")

if min(aspect_check, direction_check, center_write, width_write, set_current) < 0:
    errors.append("could not prove preflight-before-view-mutation ordering")
else:
    first_write = min(center_write, width_write)
    if aspect_check > first_write or direction_check > first_write:
        errors.append("aspect/direction compatibility must be proven before mutating the view object")
    if set_current < first_write:
        errors.append("SetCurrentView ordering is invalid")

if direction_write >= 0 and direction_check >= direction_write:
    errors.append("direction compatibility must be checked before any ViewDirection assignment")

if "UpdateScreen(" in set_view or "Regen(" in set_view:
    errors.append("cad_view_set must not force UpdateScreen/REGEN")

if errors:
    print("MCP direct-view dirty/aspect safety preflight FAILED:")
    for error in errors:
        print(" - " + error)
    raise SystemExit(1)

print("PASS MCP direct-view dirty/aspect safety source contract")
