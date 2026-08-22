#!/usr/bin/env python3
from pathlib import Path
import re
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
    current_provider_tokens = (
        "InspectGeneratedSolidOwnership(document, project)",
        '"GeneratedSolidOwnershipRuntimeHealth"',
        "if (element == null)",
        'throw new InvalidOperationException("Generated solid runtime health cannot inspect a project containing a null semantic element.");',
        "GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)",
        '"GeneratedGridAnnotationRuntimeHealthService"',
        "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
        '"GeneratedSemanticTagRuntimeHealthService"',
        "GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project)",
        '"GeneratedSemanticElementTableRuntimeHealthService"',
        "DoorOpeningNativeTableBuilder.Inspect(document, project)",
        '"DoorOpeningNativeTableBuilder"',
        "RoomFinishNativeTableBuilder.Inspect(document, project)",
        '"RoomFinishNativeTableBuilder"',
        "MaterialUsageNativeTableBuilder.Inspect(document, project)",
        '"MaterialUsageNativeTableBuilder"',
        "BqNativeTableBuilder.Inspect(document, project)",
        '"BqNativeTableBuilder"',
        "BbsNativeTableBuilder.Inspect(document, project)",
        '"BbsNativeTableBuilder"',
        "private static void AddProviderSafely(",
        "Func<IReadOnlyList<ModelHealthIssue>> provider",
        "catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
        '"RUNTIME_HEALTH_PROVIDER_FAILED"',
        "private static bool IsRecoverableDiagnosticFailure(System.Exception exception)",
        "!(exception is OutOfMemoryException)",
        "!(exception is StackOverflowException)",
        "!(exception is AccessViolationException)",
        "return issues.AsReadOnly();",
    )
    for token in current_provider_tokens:
        if token not in text:
            errors.append("GeneratedSolidRuntimeHealthService.cs missing provider-isolation/null-safety token: " + token)

    provider_names = (
        '"GeneratedSolidOwnershipRuntimeHealth"',
        '"GeneratedGridAnnotationRuntimeHealthService"',
        '"GeneratedSemanticTagRuntimeHealthService"',
        '"GeneratedSemanticElementTableRuntimeHealthService"',
        '"DoorOpeningNativeTableBuilder"',
        '"RoomFinishNativeTableBuilder"',
        '"MaterialUsageNativeTableBuilder"',
        '"BqNativeTableBuilder"',
        '"BbsNativeTableBuilder"',
    )
    positions = []
    start = 0
    for provider_name in provider_names:
        position = text.find(provider_name, start)
        positions.append(position)
        if position >= 0:
            start = position + len(provider_name)
    result = text.find("return issues.AsReadOnly();", start)
    if any(position < 0 for position in positions) or result < 0 or positions != sorted(positions):
        errors.append("Current native runtime health providers must be invoked independently before the aggregate returns.")

    all_inspect_calls = re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*)\.Inspect\(document, project\)", text)
    safe_lambda_calls = re.findall(r"\(\)\s*=>\s*([A-Za-z_][A-Za-z0-9_]*)\.Inspect\(document, project\)", text)
    if sorted(all_inspect_calls) != sorted(safe_lambda_calls):
        errors.append(
            "Every native Foo.Inspect(document, project) provider must be invoked through an AddProviderSafely lambda. "
            "all=" + repr(sorted(all_inspect_calls)) + ", safe=" + repr(sorted(safe_lambda_calls)))

    provider_invocations = text.count("AddProviderSafely(") - 1
    if provider_invocations < 9:
        errors.append("Expected at least nine isolated native runtime health providers; found %d." % provider_invocations)

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
print("PASS: all current native runtime-health providers are isolated, corrupt null semantic entries fail visibly through provider isolation, future Foo.Inspect(document, project) providers cannot bypass AddProviderSafely unnoticed, recoverable provider failures become diagnostics, and fatal runtime failures still bubble.")
