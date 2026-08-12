#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SupportBundleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SupportBundleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    publish_call = "PublishSupportBundle(dialog.FileName, lines);"
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
        publish_call,
        "private static void PublishSupportBundle(string path, IReadOnlyList<string> lines)",
        "using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))",
        "writer.Flush();",
        "stream.Flush(true);",
        "File.Replace(temp, fullPath, null, true);",
        "File.Move(temp, fullPath);",
    )
    for needle in required:
        if needle not in text:
            errors.append("support bundle privacy/atomic contract missing: " + needle)

    publish_pos = text.find(publish_call)
    before_publish = text[:publish_pos] if publish_pos >= 0 else text
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
        if needle in before_publish:
            errors.append("support bundle may include private/raw report input: " + needle)

    # Drawing fingerprint may be reported only as a boolean presence flag, never as the fingerprint value.
    fingerprint_uses = [line.strip() for line in before_publish.splitlines() if "DrawingFingerprint" in line]
    expected_fingerprint = '"has_drawing_fingerprint=" + Bool(!string.IsNullOrWhiteSpace(project.DrawingFingerprint))'
    if len(fingerprint_uses) != 1 or expected_fingerprint not in fingerprint_uses[0]:
        errors.append("DrawingFingerprint must remain presence-only in the support bundle")

    # Raw exceptions may be shown locally after publication is attempted, but must never become bundle content.
    after_publish = text[publish_pos + len(publish_call):] if publish_pos >= 0 else ""
    if "ex.Message" not in after_publish:
        errors.append("local support command should still surface export/UI failure without adding it to bundle content")
    if publish_pos >= 0 and 'lines.Add(' in text[publish_pos + len(publish_call):text.find("private static void PublishSupportBundle", publish_pos)]:
        errors.append("support bundle content must be finalized before atomic publication")

print("QS3D support bundle privacy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: support bundle content remains aggregate/version-only before atomic publication; sensitive project/runtime identity and raw exception text stay outside exported evidence while local failures remain reportable.")
