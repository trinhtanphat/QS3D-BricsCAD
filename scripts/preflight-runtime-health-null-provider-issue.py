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
    start = text.find("private static void AddProviderSafely")
    end = text.find("private static bool IsRecoverableDiagnosticFailure", start if start >= 0 else 0)
    if start < 0 or end < 0 or end <= start:
        errors.append("could not isolate AddProviderSafely")
    else:
        helper = text[start:end]
        old_skip = "if (issue != null) target.Add(issue);"
        if old_skip in helper:
            errors.append("runtime health must not silently skip null provider issues")

        required = (
            "foreach (var issue in provider())",
            "if (issue == null)",
            'throw new InvalidOperationException("Runtime health providers must not return null issues.");',
            "target.Add(issue);",
            "catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
            '"RUNTIME_HEALTH_PROVIDER_FAILED"',
            "providerName +",
        )
        for token in required:
            if token not in helper:
                errors.append("runtime-health null-provider contract missing source token: " + token)

print("QS3D runtime-health null-provider-issue preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: null native runtime-health provider issues fail visible through RUNTIME_HEALTH_PROVIDER_FAILED and retain provider identity.")
