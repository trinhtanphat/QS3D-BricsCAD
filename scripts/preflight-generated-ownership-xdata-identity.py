#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CAD = ROOT / "src/QS3D.BricsCAD.V25/Cad"
HELPER = CAD / "GeneratedOwnershipIdentityToken.cs"
TARGETS = [
    CAD / "GeneratedRebarNativeOwnershipService.cs",
    CAD / "GeneratedCurtainFrameNativeOwnershipService.cs",
    CAD / "GeneratedCurtainPanelNativeOwnershipService.cs",
    CAD / "GeneratedGeometryService.cs",
]
errors = []

for path in [HELPER] + TARGETS:
    if not path.is_file():
        errors.append("missing generated ownership identity source: " + str(path.relative_to(ROOT)))

if HELPER.is_file():
    helper = HELPER.read_text(encoding="utf-8")
    for token in (
        'private const string ProjectPrefix = "p1:";',
        'private const string ElementPrefix = "e1:";',
        "SHA256.Create()",
        "Encoding.UTF8.GetBytes(normalized)",
        "value.ToString(\"x2\", CultureInfo.InvariantCulture)",
        "string.Equals(storedIdentity, BuildNormalized(prefix, normalized), StringComparison.Ordinal)",
        "string.Equals(storedIdentity, normalized, StringComparison.OrdinalIgnoreCase)",
    ):
        if token not in helper:
            errors.append("identity helper missing token/legacy contract: " + token)

for path in TARGETS:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    label = path.name
    for token in (
        "GeneratedOwnershipIdentityToken.Project(",
        "GeneratedOwnershipIdentityToken.Element(",
        "GeneratedOwnershipIdentityToken.MatchesProject(",
        "GeneratedOwnershipIdentityToken.MatchesElement(",
    ):
        if token not in text:
            errors.append(label + " missing tokenized ownership contract: " + token)

    raw_writer_patterns = (
        "DxfCode.ExtendedDataAsciiString, project.ProjectId.Trim()",
        "DxfCode.ExtendedDataAsciiString, element.Id.Trim()",
        "DxfCode.ExtendedDataAsciiString, projectId.Trim()",
        "DxfCode.ExtendedDataAsciiString, elementId.Trim()",
    )
    for pattern in raw_writer_patterns:
        if pattern in text:
            errors.append(label + " still writes raw identity into XData: " + pattern)

    for token in (
        "DxfCode.ExtendedDataRegAppName",
        "OwnershipVersion",
        "GetXDataForApplication",
    ):
        if token not in text:
            errors.append(label + " lost existing ownership marker boundary: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] generated ownership XData uses bounded versioned project/element tokens and retains legacy raw-marker matching")
