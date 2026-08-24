#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "QsWorkbookTemplateEngine.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsWorkbookTemplateEngineSmoke.cs"
REGISTRY = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def fail(message: str) -> None:
    print("ERROR: XLSX template engine preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


for path in (SOURCE, SMOKE, REGISTRY):
    if not path.is_file():
        fail("missing required file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registry = REGISTRY.read_text(encoding="utf-8")

required_source = {
    "canonical quantity projection": "IReadOnlyList<QuantityReportRow> rows",
    "template definition": "QsWorkbookTemplateDefinition",
    "explicit field mapping": "QsWorkbookTemplateMapping",
    "atomic destination commit": "AtomicFileCommit.ReplaceWithoutBackup",
    "package validation": "XlsxPackageValidator.Validate",
    "drawing provenance": "QsWorkbookTemplateField.DrawingFingerprint",
    "semantic provenance": "QsWorkbookTemplateField.ElementIds",
    "CAD provenance": "QsWorkbookTemplateField.SourceHandles",
    "deterministic trace": 'return "QTPL1:" + hex',
    "generic trace reader": "public static class QsWorkbookTemplateTraceReader",
    "formula fail-closed": "Mapped template cell ",
    "merge fail-closed": "Merged cells cannot intersect the configured template data block",
    "footer expansion fail-closed": "Template has worksheet rows below the reserved data block",
}
for label, token in required_source.items():
    if token not in source:
        fail(label + " contract is missing")

for forbidden in (
    "Microsoft.Office.Interop",
    "Excel.Application",
    "Autodesk.AutoCAD",
    "Bricscad.",
    "Teigha.",
    "BrxMgd",
):
    if forbidden in source:
        fail("Core template engine must remain host/Office-neutral: " + forbidden)

required_smoke = (
    "RendersCanonicalRowsAndPreservesTemplateParts",
    "PreservesDestinationOnInvalidMapping",
    "RejectsMappedFormulaCells",
    "RejectsUnsafeExpansionPastFooter",
    "QsWorkbookTemplateTraceReader.Read",
    'mergeCell ref=\\"A1:D1\\"',
    "SUM(1,1)",
)
for token in required_smoke:
    if token not in smoke:
        fail("deterministic smoke coverage is missing token: " + token)

if "QsWorkbookTemplateEngineSmoke.Run();" not in registry:
    fail("template engine smoke is not registered in the deterministic suite")

print("PASS: reusable XLSX template mapping stays canonical, atomic, host-neutral and traceable")
