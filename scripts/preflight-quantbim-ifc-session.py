#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQuantBimIfcSession.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsQuantBimIfcSessionSmoke.cs"
DOC = ROOT / "docs/benchmark-parity/quantbim-ifc-session.md"

def req(text, needle, label):
    if needle not in text:
        raise SystemExit(f"QuantBIM IFC session preflight failed: missing {label}: {needle}")

def forbid(text, needle, label):
    if needle in text:
        raise SystemExit(f"QuantBIM IFC session preflight failed: forbidden {label}: {needle}")

def main():
    for path in (SOURCE, SMOKE, DOC):
        if not path.is_file():
            raise SystemExit(f"QuantBIM IFC session preflight failed: missing {path.relative_to(ROOT)}")
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")
    req(source, "IfcStepStandaloneSource().Parse(path, stepText)", "canonical STEP parser reuse")
    req(source, "IfcStepGeometryResolver(stepText)", "same-payload geometry binding")
    req(source, "QuantBimStandaloneWorkbench", "canonical workbench reuse")
    req(source, "QuantBimStandaloneSceneBuilder", "canonical scene builder reuse")
    req(source, "ValidateGeneration", "generation fence")
    req(source, "StringComparison.OrdinalIgnoreCase", "path identity check")
    req(source, "StringComparison.Ordinal", "revision identity check")
    forbid(source, "Bricscad.", "BricsCAD dependency")
    forbid(source, "Autodesk.AutoCAD", "CAD-host dependency")
    req(smoke, "[ModuleInitializer]", "automatic smoke registration")
    req(smoke, "stale revision", "stale-generation regression")
    req(smoke, "different path", "path-generation regression")
    req(smoke, "scene geometry generation", "scene generation regression")
    req(doc, "QTO remains authoritative", "quantity evidence boundary")
    req(doc, "same STEP payload", "generation contract")
    req(doc, "BricsCAD", "optional host boundary")
    return 0

if __name__ == "__main__":
    sys.exit(main())
