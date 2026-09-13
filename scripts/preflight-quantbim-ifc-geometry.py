#!/usr/bin/env python3
from pathlib import Path
import sys
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQuantBimIfcGeometry.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsQuantBimIfcGeometrySmoke.cs"
DOC = ROOT / "docs/benchmark-parity/quantbim-ifc-geometry.md"

def req(text, needle, label):
    if needle not in text:
        raise SystemExit(f"QuantBIM IFC geometry preflight failed: missing {label}: {needle}")

def forbid(text, needle, label):
    if needle in text:
        raise SystemExit(f"QuantBIM IFC geometry preflight failed: forbidden {label}: {needle}")

def main():
    for p in (SOURCE, SMOKE, DOC):
        if not p.is_file(): raise SystemExit(f"QuantBIM IFC geometry preflight failed: missing {p.relative_to(ROOT)}")
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")
    req(source, "IfcStepGeometryResolver", "STEP geometry resolver")
    req(source, "IFCPRODUCTDEFINITIONSHAPE", "product representation traversal")
    req(source, "IFCSHAPEREPRESENTATION", "Body shape traversal")
    req(source, "IFCEXTRUDEDAREASOLID", "swept-solid traversal")
    req(source, "IFCRECTANGLEPROFILEDEF", "bounded profile support")
    req(source, "non-vertical extrusion is outside the supported geometry subset", "fail-closed unsupported direction")
    forbid(source, "Bricscad.", "BricsCAD dependency")
    forbid(source, "Autodesk.AutoCAD", "CAD-host dependency")
    req(smoke, "[ModuleInitializer]", "automatic smoke registration")
    req(smoke, "scene node count", "scene integration regression")
    req(smoke, "zero depth", "invalid-dimension regression")
    req(doc, "QTO remains authoritative", "quantity-evidence boundary")
    req(doc, "BricsCAD", "optional-host architecture boundary")
    return 0

if __name__ == "__main__":
    sys.exit(main())
