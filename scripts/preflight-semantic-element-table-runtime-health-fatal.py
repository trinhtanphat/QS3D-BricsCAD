#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticElementTableRuntimeHealthService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing semantic element table runtime health service: " + str(SERVICE.relative_to(ROOT)))
else:
    text = SERVICE.read_text(encoding="utf-8")

    required = (
        "catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
        "private static bool IsRecoverableDiagnosticFailure(Exception exception)",
        "!(exception is OutOfMemoryException)",
        "!(exception is StackOverflowException)",
        "!(exception is AccessViolationException)",
        '"SEMANTIC_ELEMENT_TABLE_RENDER_INVALID"',
        '"SEMANTIC_ELEMENT_TABLE_CAD_CELL_UNREADABLE"',
        "OpenMode.ForRead",
    )
    for token in required:
        if token not in text:
            errors.append("semantic element table runtime health missing token: " + token)

    filtered_catches = text.count("catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))")
    if filtered_catches < 3:
        errors.append(
            "semantic element table runtime health must filter snapshot, cell-read, and handle-resolution catches; found %d"
            % filtered_catches
        )

    forbidden = (
        "catch (Exception ex)\n",
        "catch { return false; }",
        "catch\n            {\n                return false;\n            }",
    )
    for token in forbidden:
        if token in text:
            errors.append("semantic element table runtime health contains unfiltered broad catch: " + repr(token))

    helper_start = text.find("private static bool IsRecoverableDiagnosticFailure")
    if helper_start < 0:
        errors.append("could not locate recoverable diagnostic helper")
    else:
        helper = text[helper_start:]
        for fatal in ("OutOfMemoryException", "StackOverflowException", "AccessViolationException"):
            if fatal not in helper:
                errors.append("recoverable diagnostic helper does not exclude fatal exception: " + fatal)

print("QS3D semantic element table runtime-health fatal propagation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic element table runtime health keeps recoverable diagnostics local while fatal runtime exceptions propagate through the native health boundary.")
