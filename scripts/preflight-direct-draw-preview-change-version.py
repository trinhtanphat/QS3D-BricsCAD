#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Services/DirectDrawProjectPreviewContext.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawProjectPreviewContext.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "public long? ExpectedChangeVersion { get; }",
        "ExpectedChangeVersion = expectedChangeVersion;",
        "project.ProjectId.Trim(), project.ChangeVersion, expectedLengthUnit, expectedUcs)",
        "project.ChangeVersion != ExpectedChangeVersion.Value",
        "QS3D project đã được chỉnh sửa trong lúc xác nhận Direct Draw.",
    )
    for token in required:
        if token not in text:
            errors.append("missing stale-preview version guard token: " + token)

    if "DefaultsProject.ChangeVersion" in text:
        errors.append("preview version must be captured as a scalar snapshot, not read later from mutable DefaultsProject")

    identity_check = "string.Equals(project.ProjectId, ExpectedProjectId, StringComparison.OrdinalIgnoreCase)"
    if identity_check not in text:
        errors.append("existing Direct Draw project identity guard must remain in place")

    appeared_guard = "ProjectContextCoordinator.TryGetReadOnly(document, out _) || HasBackingStore(document)"
    if appeared_guard not in text:
        errors.append("project-appeared lifecycle guard must remain in place")

print("QS3D Direct Draw preview change-version preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Direct Draw snapshots project ChangeVersion before prompts and fails closed if the project changes before mutation.")
