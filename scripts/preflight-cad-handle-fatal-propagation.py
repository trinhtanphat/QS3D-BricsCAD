#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing CAD handle service: " + str(SERVICE.relative_to(ROOT)))
else:
    text = SERVICE.read_text(encoding="utf-8")

    required = (
        "private static bool IsRecoverableDiagnosticFailure(Exception exception)",
        "!(exception is OutOfMemoryException)",
        "!(exception is StackOverflowException)",
        "!(exception is AccessViolationException)",
        "OpenMode.ForRead",
        "NormalizeHexHandle",
        "StringComparer.OrdinalIgnoreCase",
    )
    for token in required:
        if token not in text:
            errors.append("CAD handle fatal-boundary contract missing token: " + token)

    filtered = text.count("catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))")
    if filtered < 4:
        errors.append("CAD handle service must filter all four recovery catches; found %d" % filtered)

    forbidden = (
        "catch { }",
        "catch { }\n",
        "catch {\n",
    )
    for token in forbidden:
        if token in text:
            errors.append("CAD handle service still contains an unfiltered bare catch")
            break

    normalize_start = text.find("public static string? NormalizeHexHandle")
    select_start = text.find("public static int Select", normalize_start if normalize_start >= 0 else 0)
    if normalize_start < 0 or select_start < 0 or select_start <= normalize_start:
        errors.append("could not isolate NormalizeHexHandle")
    else:
        normalize = text[normalize_start:select_start]
        for token in (
            'normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase)',
            "NumberStyles.HexNumber",
            "value <= 0L",
            'value.ToString("X", CultureInfo.InvariantCulture)',
        ):
            if token not in normalize:
                errors.append("CAD handle canonicalization regressed; missing token: " + token)

print("QS3D CAD handle fatal propagation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: CAD handle resolution/live-read helpers retain recoverable skip behavior and canonicalization while fatal runtime exceptions propagate.")
