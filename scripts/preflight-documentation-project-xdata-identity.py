#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GENERIC = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
SCHEDULE = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticScheduleNativeTableBuilder.cs"
errors = []

for path in (GENERIC, SCHEDULE):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    label = path.name

    for token in (
        'private const string ProjectIdentityTokenPrefix = "p1:";',
        "ProjectIdentityToken(projectId)",
        "MatchesProjectIdentity(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), projectId)",
        "var normalized = (projectId ?? string.Empty).Trim();",
        "SHA256.Create()",
        "Encoding.UTF8.GetBytes(normalized)",
        "string.Equals(storedIdentity, normalized, StringComparison.Ordinal)",
    ):
        if token not in text:
            errors.append(label + " missing Unicode-safe project XData identity token: " + token)

    for forbidden in (
        "new TypedValue((int)DxfCode.ExtendedDataAsciiString, projectId.Trim())",
        "new TypedValue((int)DxfCode.ExtendedDataAsciiString, (projectId ?? string.Empty).Trim())",
        "string.Equals(Convert.ToString(values[2].Value, CultureInfo.InvariantCulture), projectId, StringComparison.Ordinal)",
    ):
        if forbidden in text:
            errors.append(label + " still persists or matches raw ProjectId in QS3DDOC XData: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: native documentation tables persist ASCII SHA-256 project identity tokens in QS3DDOC XData while readers retain legacy raw-ProjectId compatibility.")
