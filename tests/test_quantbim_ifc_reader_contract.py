from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/QS3D.QuantBIM.Standalone/StepIfcDocumentReader.cs"
FIXTURE = ROOT / "tests/fixtures/quantbim/minimal.ifc"


def test_reader_remains_bricscad_independent_and_fail_closed():
    text = SRC.read_text(encoding="utf-8")
    assert "BricsCAD" not in text
    assert "ExpectedSha256" in text and "SHA256.HashData" in text
    assert "FILE_SCHEMA" in text
    assert "Duplicate IFC GlobalId" in text
    assert "IFC_STEP_IDENTITY" in text


def test_fixture_has_deterministic_products():
    text = FIXTURE.read_text(encoding="utf-8")
    assert "FILE_SCHEMA(('IFC4'))" in text
    assert text.count("IFCWALL(") == 1
    assert text.count("IFCSLAB(") == 1
