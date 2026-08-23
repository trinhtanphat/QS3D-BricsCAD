#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SAMPLES = ROOT / "samples" / "generated"
ERRORS: list[str] = []


def fail(message: str) -> None:
    ERRORS.append(message)


def require_file(name: str, maximum: int) -> Path:
    path = SAMPLES / name
    if not path.is_file():
        fail(f"missing synthetic sample: {path.relative_to(ROOT)}")
        return path
    size = path.stat().st_size
    if size <= 0 or size > maximum:
        fail(f"invalid sample size for {name}: {size}")
    return path


def validate_dxf(path: Path) -> None:
    if not path.is_file():
        return
    lines = path.read_text(encoding="ascii").splitlines()
    if len(lines) % 2:
        fail("DXF must contain complete group-code/value pairs")
        return
    pairs = [(lines[i].strip(), lines[i + 1].strip()) for i in range(0, len(lines), 2)]
    if pairs[-1] != ("0", "EOF"):
        fail("DXF does not terminate with EOF")
    if ("9", "$INSUNITS") not in pairs:
        fail("DXF is missing $INSUNITS")
    else:
        index = pairs.index(("9", "$INSUNITS"))
        if index + 1 >= len(pairs) or pairs[index + 1] != ("70", "6"):
            fail("DXF sample must use metres ($INSUNITS=6)")

    try:
        start = pairs.index(("2", "ENTITIES")) + 1
        end = pairs.index(("0", "ENDSEC"), start)
    except ValueError:
        fail("DXF is missing a bounded ENTITIES section")
        return
    handles = {value.upper() for code, value in pairs[start:end] if code == "5"}
    expected = {"22A", "22B", "22C", "22D", "22E", "22F", "230", "231", "232", "233", "234"}
    if handles != expected:
        fail(f"DXF entity Handles differ: expected={sorted(expected)}, actual={sorted(handles)}")
    layers = {value for code, value in pairs[start:end] if code == "8"}
    required_layers = {"QS3D_WALL", "QS3D_GLASS", "QS3D_COLUMN", "QS3D_SLAB", "QS3D_BEAM", "QS3D_DOOR", "QS3D_ROOM"}
    if not required_layers.issubset(layers):
        fail(f"DXF is missing semantic layers: {sorted(required_layers - layers)}")


def validate_qsdb(path: Path) -> None:
    if not path.is_file():
        return
    root = ET.parse(path).getroot()
    if root.tag != "qs3d" or root.get("schema") != "3":
        fail("QSDB must have qs3d/schema=3 root")
    change_version = root.get("changeVersion", "")
    if not re.fullmatch(r"0|[1-9][0-9]*", change_version):
        fail("QSDB schema-3 fixture must persist a canonical non-negative changeVersion")
    if root.get("drawingPath") or root.get("drawingFingerprint"):
        fail("public QSDB fixture must adopt drawing path/fingerprint on first open")

    metadata = root.find("metadata")
    if metadata is None:
        fail("QSDB is missing metadata")
    else:
        values: dict[str, str] = {}
        for item in metadata.findall("p"):
            name = item.get("name", "")
            if name in values:
                fail(f"QSDB metadata contains duplicate key: {name}")
            values[name] = item.get("value", "")
        expected_unit_binding = {
            "QS3D.DrawingUnitBound.v1": "Meter",
            "QS3D.DrawingUnit": "Meter",
            "QS3D.DrawingUnitBindingSource.v1": "NativeInsunits",
        }
        for name, expected_value in expected_unit_binding.items():
            if values.get(name) != expected_value:
                fail(f"QSDB metre unit binding mismatch for {name}: {values.get(name)!r}")
        if "QS3D.DrawingUnitOverride.v1" in values:
            fail("metre DXF fixture must use native INSUNITS binding, not a project unit override")

    elements = root.find("elements")
    if elements is None:
        fail("QSDB is missing elements")
        return
    expected = {
        "wall-10": "22A", "wall-11": "22B", "wall-12": "22C", "wall-13": "22D", "door-20": "22E",
        "column-30": "22F", "column-31": "230", "slab-40": "231", "beam-50": "232", "glass-60": "233", "room-70": "234",
    }
    actual: dict[str, str] = {}
    for element in elements.findall("element"):
        handles = [node.text.strip().upper() for node in element.findall("./handles/h") if node.text and node.text.strip()]
        if len(handles) == 1:
            actual[element.get("id", "")] = handles[0]
    if actual != expected:
        fail(f"QSDB Element/Handle mapping differs: {actual}")
    if any(element.get("drawingFingerprint") for element in elements.findall("element")):
        fail("public QSDB elements must start with blank drawing fingerprints")


def validate_template(path: Path) -> None:
    if not path.is_file():
        return
    root = ET.parse(path).getroot()
    if root.tag != "qs3dTemplate" or root.get("schema") != "1":
        fail("qstemplate must have qs3dTemplate/schema=1 root")
    families = root.findall("./families/family")
    rules = root.findall("./rules/rule")
    mappings = root.findall("./layerMappings/map")
    if len(families) != 7 or len(rules) != 3 or len(mappings) != 7:
        fail(f"unexpected template coverage: families={len(families)}, rules={len(rules)}, mappings={len(mappings)}")
    patterns = [item.get("pattern", "") for item in mappings]
    if len(patterns) != len(set(patterns)) or any(not pattern.startswith("QS3D_") for pattern in patterns):
        fail("template layer mappings must be unique QS3D_ patterns")


def cell_column(reference: str) -> int:
    match = re.match(r"([A-Z]+)", reference.upper())
    if not match:
        return -1
    value = 0
    for char in match.group(1):
        value = value * 26 + ord(char) - 64
    return value - 1


def validate_xlsx(path: Path) -> None:
    if not path.is_file():
        return
    with zipfile.ZipFile(path) as archive:
        required = {"[Content_Types].xml", "xl/workbook.xml", "xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml"}
        missing = required - set(archive.namelist())
        if missing:
            fail(f"XLSX is missing package parts: {sorted(missing)}")
            return
        if len(archive.namelist()) > 200 or sum(item.file_size for item in archive.infolist()) > 4_000_000:
            fail("XLSX fixture package exceeds bounded validation limits")
            return
        ns = {"x": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
        workbook = ET.fromstring(archive.read("xl/workbook.xml"))
        sheet_names = [item.get("name") for item in workbook.findall("./x:sheets/x:sheet", ns)]
        if sheet_names != ["KHOI_LUONG", "TONG_HOP", "HUONG_DAN"]:
            fail(f"unexpected XLSX sheet order: {sheet_names}")

        shared: list[str] = []
        if "xl/sharedStrings.xml" in archive.namelist():
            shared_root = ET.fromstring(archive.read("xl/sharedStrings.xml"))
            for item in shared_root.findall("x:si", ns):
                shared.append("".join(node.text or "" for node in item.findall(".//x:t", ns)))

        sheet = ET.fromstring(archive.read("xl/worksheets/sheet1.xml"))
        values: dict[tuple[int, int], str] = {}
        formulas: dict[tuple[int, int], str] = {}
        for row in sheet.findall(".//x:sheetData/x:row", ns):
            row_number = int(row.get("r", "0"))
            for cell in row.findall("x:c", ns):
                column = cell_column(cell.get("r", ""))
                cell_type = cell.get("t", "")
                inline = cell.find("x:is", ns)
                raw = cell.findtext("x:v", default="", namespaces=ns)
                if inline is not None:
                    value = "".join(node.text or "" for node in inline.findall(".//x:t", ns))
                elif cell_type == "s" and raw.isdigit() and int(raw) < len(shared):
                    value = shared[int(raw)]
                else:
                    value = raw
                values[(row_number, column)] = value
                formula = cell.findtext("x:f", default="", namespaces=ns)
                if formula:
                    formulas[(row_number, column)] = formula

        expected_headers = {16: "QS3D Element ID", 17: "CAD Handle (hex)", 18: "QS3D Drawing Fingerprint"}
        for column, expected in expected_headers.items():
            if values.get((1, column)) != expected:
                fail(f"XLSX header mismatch at column {column + 1}: {values.get((1, column))!r}")
        expected_handles = {2: "22A", 3: "22B", 4: "22C", 5: "22D", 6: "22E", 7: "22F;230", 8: "231", 9: "232", 10: "233", 11: "234"}
        for row, expected in expected_handles.items():
            if values.get((row, 17)) != expected:
                fail(f"XLSX Handle mismatch at row {row}: {values.get((row, 17))!r}")
            if values.get((row, 18), ""):
                fail(f"XLSX public fingerprint must be blank at row {row}")
            if formulas.get((row, 6)) != f"E{row}-F{row}":
                fail(f"XLSX net quantity formula mismatch at G{row}: {formulas.get((row, 6))!r}")


def main() -> int:
    print("QS3D synthetic sample preflight")
    readme = require_file("README.md", 128_000)
    dxf = require_file("QS3D-Sample.dxf", 2_000_000)
    dwg = require_file("QS3D-Sample.dwg", 2_000_000)
    qsdb = require_file("QS3D-Sample.qsdb", 2_000_000)
    workbook = require_file("QS3D-Quantity-Template.xlsx", 4_000_000)
    template = require_file("QS3D-Architecture.qstemplate", 1_000_000)
    validate_dxf(dxf)
    if dwg.is_file() and not dwg.read_bytes()[:6].startswith(b"AC10"):
        fail("DWG sample is missing an Autodesk DWG version signature")
    validate_qsdb(qsdb)
    validate_template(template)
    validate_xlsx(workbook)
    for path in SAMPLES.glob("*.dll"):
        fail(f"sample folder must not contain DLLs: {path.name}")
    if readme.is_file() and "no BLT source" not in readme.read_text(encoding="utf-8"):
        fail("sample README must state its provenance boundary")
    if ERRORS:
        for error in ERRORS:
            print("ERROR:", error)
        print(f"FAILED with {len(ERRORS)} error(s).")
        return 1
    print("PASS: synthetic DXF/QSDB/XLSX/template files are bounded, cross-mapped and contain no vendored DLLs.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
