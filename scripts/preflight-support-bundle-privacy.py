#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SupportBundleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SupportBundleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"QS3D_SUPPORT_BUNDLE_V1"',
        '"privacy=No drawing path, source/generated handles, semantic IDs, Family names, project metadata, user name or machine name are included."',
        '"project_schema="',
        '"zone_count="',
        '"floor_count="',
        '"family_count="',
        '"element_count="',
        '"dirty_element_count="',
        '"has_drawing_fingerprint=" + Bool(!string.IsNullOrWhiteSpace(project.DrawingFingerprint))',
        'lines.Add("category." + SafeToken(group.Key.ToString()) + "=" + group.Count().ToString(CultureInfo.InvariantCulture));',
        'File.WriteAllLines(dialog.FileName, lines, new System.Text.UTF8Encoding(false));',
    )
    for needle in required:
        if needle not in text:
            errors.append("support bundle privacy contract missing: " + needle)

    before_write = text.split("File.WriteAllLines(dialog.FileName, lines", 1)[0]
    forbidden_report_inputs = (
        "Environment.UserName",
        "Environment.MachineName",
        "Environment.GetEnvironmentVariable",
        "document.Name",
        "document.Database.Filename",
        "Database.Filename",
        "project.Metadata",
        ".SourceHandles",
        "GeneratedSolidHandle",
        "GeneratedRebarHandles",
        "PhysicalOpeningCutSolidHandle",
        "File.ReadAllBytes",
        "File.ReadAllText",
        "ZipArchive",
        "Directory.GetFiles",
        "ex.Message",
        "Exception.ToString",
    )
    for needle in forbidden_report_inputs:
        if needle in before_write:
            errors.append("support bundle may include private/raw report input: " + needle)

    # Drawing fingerprint may be reported only as a boolean presence flag, never as the fingerprint value.
    fingerprint_uses = [line.strip() for line in before_write.splitlines() if "DrawingFingerprint" in line]
    expected_fingerprint = '"has_drawing_fingerprint=" + Bool(!string.IsNullOrWhiteSpace(project.DrawingFingerprint))'
    if len(fingerprint_uses) != 1 or expected_fingerprint not in fingerprint_uses[0]:
        errors.append("DrawingFingerprint must remain presence-only in the support bundle")

    # Raw exceptions may be shown locally after the export attempt, but must not become bundle content.
    after_write = text.split("File.WriteAllLines(dialog.FileName, lines", 1)[1] if "File.WriteAllLines(dialog.FileName, lines" in text else ""
    if "ex.Message" not in after_write:
        errors.append("local support command should still surface export failure without adding it to bundle content")

print("QS3D support bundle privacy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: support bundle remains aggregate/version-only and excludes drawing paths/content, semantic/generated handles, IDs, names, environment identity, local file reads and raw exception text from exported evidence.")
