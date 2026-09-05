from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/DirectDrawCommands.cs")
text = SOURCE.read_text(encoding="utf-8")


def method_body(command_name: str, next_command_name: str) -> str:
    start_marker = f'[CommandMethod("{command_name}"'
    end_marker = f'[CommandMethod("{next_command_name}"'
    start = text.find(start_marker)
    end = text.find(end_marker, start + 1)
    if start < 0 or end < 0:
        raise SystemExit(f"Direct Draw command structure changed: {command_name}")
    return text[start:end]


contracts = [
    ("QS3DDRAWWALLADV", "QS3DDRAWBEAM", "AcquirePath", "bottomOffsetM"),
    ("QS3DDRAWBEAMADV", "QS3DDRAWSLAB", "AcquireFixedPath", "bottomOffsetM"),
    ("QS3DDRAWSLABADV", "QS3DDRAWCOLUMN", "AcquirePath", "bottomOffsetM"),
    ("QS3DDRAWCOLUMNADV", "internal static DirectDrawCommitResult ExecuteDirect", "GetPoint", "bottomOffsetM"),
]

for command, next_marker, geometry_marker, final_prompt_variable in contracts:
    if next_marker.startswith("internal static"):
        start = text.find(f'[CommandMethod("{command}"')
        end = text.find(next_marker, start + 1)
        if start < 0 or end < 0:
            raise SystemExit(f"Direct Draw command structure changed: {command}")
        body = text[start:end]
    else:
        body = method_body(command, next_marker)

    geometry = body.find(geometry_marker)
    execute = body.find("ExecuteDirect(")
    final_prompt = body.rfind(final_prompt_variable, geometry, execute)
    snapshot_unit = body.find("CadUnitService.GetLengthUnit(document)", 0, geometry)
    snapshot_ucs = body.find("CurrentUserCoordinateSystem", 0, geometry)
    final_fence = body.rfind("RequirePromptContextUnchanged(document, promptUnit, promptUcs", final_prompt, execute)

    if snapshot_unit < 0 or snapshot_ucs < 0:
        raise SystemExit(f"{command} must snapshot drawing-unit and UCS context before geometry acquisition")
    if final_prompt < 0 or execute < 0:
        raise SystemExit(f"{command} prompt/ExecuteDirect structure changed")
    if final_fence < 0:
        raise SystemExit(f"{command} must revalidate prompt context after numeric prompts and immediately before ExecuteDirect")

create_line_start = text.find("private static ObjectId CreateLine(")
create_polyline_start = text.find("private static ObjectId CreatePolyline(")
if create_line_start < 0 or create_polyline_start < 0:
    raise SystemExit("Direct Draw geometry creation helpers changed")
if "document.Editor.CurrentUserCoordinateSystem" not in text[create_line_start:create_polyline_start]:
    raise SystemExit("Guard assumption changed: LINE creation no longer consumes current UCS at commit")

print("PASS: advanced Direct Draw revalidates unit/UCS prompt context immediately before geometry commit")
