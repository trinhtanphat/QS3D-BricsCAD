#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing generated-solid runtime health service: " + str(SERVICE.relative_to(ROOT)))
else:
    text = SERVICE.read_text(encoding="utf-8")
    start = text.find("private static IReadOnlyList<ModelHealthIssue> InspectGeneratedSolidOwnership")
    end = text.find("private static void AddProviderSafely", start if start >= 0 else 0)
    if start < 0 or end < 0 or end <= start:
        errors.append("could not isolate InspectGeneratedSolidOwnership")
    else:
        ownership = text[start:end]
        if "if (element == null) continue;" in ownership:
            errors.append("generated-solid ownership health must not silently skip null semantic elements")
        required_ownership = (
            "if (element == null)",
            'throw new InvalidOperationException("Generated solid runtime health cannot inspect a project containing a null semantic element.");',
            "if (!element.Properties.TryGetValue(HandleKey, out var rawHandle)) continue;",
            '"GENERATED_SOLID_HANDLE_INVALID"',
            "OpenMode.ForRead",
        )
        for token in required_ownership:
            if token not in ownership:
                errors.append("generated-solid null-element contract missing source token: " + token)

    required_isolation = (
        '"GeneratedSolidOwnershipRuntimeHealth"',
        "() => InspectGeneratedSolidOwnership(document, project)",
        "catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
        '"RUNTIME_HEALTH_PROVIDER_FAILED"',
    )
    for token in required_isolation:
        if token not in text:
            errors.append("generated-solid provider isolation regressed; missing token: " + token)

print("QS3D generated-solid null-element health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated-solid ownership health fails visible on null semantic elements while aggregate provider isolation remains intact.")
