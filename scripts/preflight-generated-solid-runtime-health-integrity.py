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
        errors.append("could not isolate InspectGeneratedSolidOwnership for source-contract checks")
    else:
        ownership = text[start:end]

        required_flow = (
            "if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))",
            "id = document.Database.GetObjectId(false, new Handle(value), 0);",
            "if (id.IsNull || !id.IsValid)",
            "dbObject = transaction.GetObject(id, OpenMode.ForRead, true);",
            "if (dbObject == null)",
            "if (dbObject.IsErased)",
            "if (!(dbObject is Solid3d entity))",
            "if (!GeneratedGeometryService.HasMatchingOwnership(entity, project, element))",
        )
        for token in required_flow:
            if token not in ownership:
                errors.append("generated-solid health missing fail-visible/read-only flow token: " + token)

        required_diagnostics = {
            '"GENERATED_SOLID_HANDLE_INVALID"': 1,
            '"GENERATED_SOLID_HANDLE_UNRESOLVED"': 2,
            '"GENERATED_SOLID_ENTITY_UNREADABLE"': 2,
            '"GENERATED_SOLID_ENTITY_ERASED"': 1,
            '"GENERATED_SOLID_ENTITY_TYPE_MISMATCH"': 1,
            '"GENERATED_SOLID_OWNERSHIP_MISMATCH"': 1,
        }
        for token, minimum in required_diagnostics.items():
            count = ownership.count(token)
            if count < minimum:
                errors.append(
                    "generated-solid health diagnostic coverage regressed for %s: expected >= %d, found %d"
                    % (token, minimum, count))

        if ownership.count("issues.Add(new ModelHealthIssue(") < 8:
            errors.append("generated-solid ownership inspection must keep all corrupt/stale states fail-visible")

        if "OpenMode.ForRead" not in ownership:
            errors.append("generated-solid health must open referenced CAD objects only ForRead")

        forbidden_mutation_tokens = (
            "OpenMode.ForWrite",
            ".UpgradeOpen(",
            "ProjectMutationContext",
            "AuditTrail.ForProject",
            "project.Touch(",
            ".Save(",
            "GetOrCreate",
            ".Erase(",
            "StampOwnership(",
            "SetXData(",
        )
        for token in forbidden_mutation_tokens:
            if token in ownership:
                errors.append("generated-solid health must remain read-only; forbidden token: " + token)

        if "catch (System.Exception ex) when (IsRecoverableDiagnosticFailure(ex))" not in ownership:
            errors.append("recoverable CAD resolution/read failures must remain diagnostics instead of aborting health inspection")

print("QS3D generated-solid runtime-health integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated-solid ownership health keeps malformed, unresolved, unreadable, erased, type-mismatched and ownership-mismatched references fail-visible while the inspection path remains read-only.")
