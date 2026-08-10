#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
errors = []

for path in (SERVICE, COMMANDS):
    if not path.is_file():
        errors.append("missing runtime-health provider-isolation file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        "InspectGeneratedSolidOwnership(document, project)",
        '"GeneratedSolidOwnershipRuntimeHealth"',
        "GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)",
        '"GeneratedGridAnnotationRuntimeHealthService"',
        "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
        '"GeneratedSemanticTagRuntimeHealthService"',
        "GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project)",
        '"GeneratedSemanticElementTableRuntimeHealthService"',
        "private static void AddProviderSafely(",
        "Func<IReadOnlyList<ModelHealthIssue>> provider",
        "catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
        '"RUNTIME_HEALTH_PROVIDER_FAILED"',
        "private static bool IsRecoverableDiagnosticFailure(System.Exception exception)",
        "!(exception is OutOfMemoryException)",
        "!(exception is StackOverflowException)",
        "!(exception is AccessViolationException)",
        "return issues.AsReadOnly();",
    ):
        if token not in text:
            errors.append("GeneratedSolidRuntimeHealthService.cs missing provider-isolation token: " + token)

    solid = text.find('"GeneratedSolidOwnershipRuntimeHealth"')
    grid = text.find('"GeneratedGridAnnotationRuntimeHealthService"', solid)
    tag = text.find('"GeneratedSemanticTagRuntimeHealthService"', grid)
    table = text.find('"GeneratedSemanticElementTableRuntimeHealthService"', tag)
    result = text.find("return issues.AsReadOnly();", table)
    if min(solid, grid, tag, table, result) < 0 or not solid < grid < tag < table < result:
        errors.append("Runtime health providers must be invoked independently before the aggregate returns, including Semantic Element Table health.")

    if "catch (System.Exception ex)\n" in text:
        errors.append("Runtime health provider isolation must not use an unfiltered broad System.Exception catch.")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    health_start = text.find('CommandMethod("QS3DHEALTH"')
    locate_start = text.find('CommandMethod("QS3DLOCATE"')
    health = text[health_start:locate_start if locate_start >= 0 else len(text)]
    if "Cad.GeneratedSolidRuntimeHealthService.Inspect(doc, project)" not in health:
        errors.append("QS3DHEALTH no longer invokes the isolated native runtime-health aggregate.")

print("QS3D runtime-health provider-isolation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: generated-solid, Grid annotation, Semantic Tag and Semantic Element Table native health providers are isolated so one recoverable provider failure becomes a diagnostic instead of aborting the whole QS3DHEALTH runtime report; fatal runtime failures still bubble.")
