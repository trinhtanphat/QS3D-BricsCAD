#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing project-owned native Table artifact service: " + str(SERVICE.relative_to(ROOT)))
else:
    text = SERVICE.read_text(encoding="utf-8")
    inspect_start = text.find("public static IReadOnlyList<ModelHealthIssue> Inspect(")
    validate_start = text.find("private static void ValidateSnapshot", inspect_start if inspect_start >= 0 else 0)
    if inspect_start < 0 or validate_start < 0 or validate_start <= inspect_start:
        errors.append("could not isolate native Table health inspection scope")
        health = ""
    else:
        health = text[inspect_start:validate_start]

    required_health = (
        'Issue("METADATA_INVALID"',
        'Issue("RENDER_INVALID"',
        'Issue("MISSING"',
        'Issue("CAD_CELL_UNREADABLE"',
        "OpenMode.ForRead",
        "catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
    )
    for token in required_health:
        if token not in health:
            errors.append("native Table health missing token: " + token)

    filtered_health_catches = health.count("catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))")
    if filtered_health_catches < 3:
        errors.append(
            "native Table health must filter metadata, render, and cell-read catches; found %d"
            % filtered_health_catches
        )

    if "catch (Exception ex)\n" in health:
        errors.append("native Table health contains an unfiltered broad Exception catch")

    resolve_start = text.find("private static bool TryResolve(")
    identity_start = text.find("private static string ProjectIdentityToken", resolve_start if resolve_start >= 0 else 0)
    if resolve_start < 0 or identity_start < 0 or identity_start <= resolve_start:
        errors.append("could not isolate shared native Table handle resolver")
        resolver = ""
    else:
        resolver = text[resolve_start:identity_start]

    required_resolver = (
        "catch (Exception ex) when (IsRecoverableDiagnosticFailure(ex))",
        "private static bool IsRecoverableDiagnosticFailure(Exception exception)",
        "!(exception is OutOfMemoryException)",
        "!(exception is StackOverflowException)",
        "!(exception is AccessViolationException)",
    )
    for token in required_resolver:
        if token not in resolver:
            errors.append("native Table resolver fatal-boundary contract missing token: " + token)

    if "catch { return false; }" in resolver:
        errors.append("native Table handle resolver must not use an unfiltered bare catch")

print("QS3D project-owned native Table runtime-health fatal propagation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: project-owned native Table health preserves recoverable metadata/render/cell/handle diagnostics while fatal runtime exceptions propagate through the native health boundary.")
