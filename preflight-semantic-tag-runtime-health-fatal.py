#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing semantic tag runtime health service: " + str(SERVICE.relative_to(ROOT)))
else:
    text = SERVICE.read_text(encoding="utf-8")

    required = (
        "catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
        "private static bool IsRecoverableDiagnosticFailure(Exception exception)",
        "!(exception is OutOfMemoryException)",
        "!(exception is StackOverflowException)",
        "!(exception is AccessViolationException)",
        '"SEMANTIC_TAG_MTEXT_MISSING"',
        "OpenMode.ForRead",
    )
    for token in required:
        if token not in text:
            errors.append("semantic tag runtime health missing token: " + token)

    filtered_catches = text.count("catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))")
    if filtered_catches < 2:
        errors.append(
            "semantic tag runtime health must filter both CAD resolution/read catches; found %d"
            % filtered_catches
        )

    if "catch\n                        {" in text or "catch\n            {" in text:
        errors.append("semantic tag runtime health still contains an unfiltered bare catch")

print("QS3D semantic tag runtime-health fatal propagation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic tag runtime health preserves recoverable missing-tag diagnostics while fatal runtime exceptions propagate through the native health boundary.")
