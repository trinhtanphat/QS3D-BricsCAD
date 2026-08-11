#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SupportBundleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing src/QS3D.BricsCAD.V25/SupportBundleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    required = [
        '[CommandMethod("QS3DSUPPORTBUNDLE", CommandFlags.Modal)]',
        "if (dialog.ShowDialog() != true) return;",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        'lines.Add("project_available=true")',
        'lines.Add("project_available=false")',
        '"project_schema=" + project.SchemaVersion.ToString(CultureInfo.InvariantCulture)',
        '"zone_count=" + project.Zones.Count.ToString(CultureInfo.InvariantCulture)',
        '"floor_count=" + project.Floors.Count.ToString(CultureInfo.InvariantCulture)',
        '"family_count=" + project.Families.Count.ToString(CultureInfo.InvariantCulture)',
        '"element_count=" + project.Elements.Count.ToString(CultureInfo.InvariantCulture)',
        '"dirty_element_count=" + project.Elements.Count(x => x.Dirty != ElementDirtyFlags.None).ToString(CultureInfo.InvariantCulture)',
        '"has_drawing_fingerprint=" + Bool(!string.IsNullOrWhiteSpace(project.DrawingFingerprint))',
        'lines.Add("category." + SafeToken(group.Key.ToString()) + "=" + group.Count().ToString(CultureInfo.InvariantCulture))',
        "PublishSupportBundle(dialog.FileName, lines);",
        "FinalizeSupportBundleUi(document, dialog.FileName);",
        "Cảnh báo UI sau export Support Bundle",
    ]
    for token in required:
        if token not in text:
            errors.append("Support Bundle missing read-only/privacy/export token: " + token)

    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("Support Bundle must not create/cache a QS3D project; use TryGetReadOnly only")

    dialog = text.find("if (dialog.ShowDialog() != true) return;")
    readonly = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    publish = text.find("PublishSupportBundle(dialog.FileName, lines);")
    finalize = text.find("FinalizeSupportBundleUi(document, dialog.FileName);")
    if min(dialog, readonly, publish, finalize) >= 0 and not (dialog < readonly < publish < finalize):
        errors.append("Support Bundle must confirm destination, read existing project without caching, persist report, then finalize UI")

    if dialog >= 0:
        before_dialog = text[:dialog]
        for forbidden in (
            "ProjectContextCoordinator.TryGetReadOnly",
            "ProjectContextCoordinator.GetOrCreate",
            "LoadProject(",
            "project.Elements",
            "project.Metadata",
        ):
            if forbidden in before_dialog:
                errors.append("Support Bundle Cancel path must not inspect/create project state before save confirmation: " + forbidden)

    if publish >= 0 and finalize >= 0:
        between = text[publish:finalize]
        for forbidden in ("PaletteCoordinator.", "Editor.WriteMessage"):
            if forbidden in between:
                errors.append("Support Bundle must not perform fallible UI work after file publish and before FinalizeSupportBundleUi: " + forbidden)

    if "File.WriteAllLines(dialog.FileName" in text or "File.WriteAllText(dialog.FileName" in text:
        errors.append("Support Bundle must not write/truncate the selected destination directly; use PublishSupportBundle")

    # Bundle content is deliberately aggregate-only. These output labels would cross the privacy boundary.
    forbidden_output_labels = (
        '"project_id="',
        '"project_name="',
        '"drawing_path="',
        '"drawing_name="',
        '"drawing_fingerprint="',
        '"source_handle="',
        '"generated_handle="',
        '"semantic_id="',
        '"family_name="',
        '"project_metadata="',
        '"user_name="',
        '"machine_name="',
    )
    for label in forbidden_output_labels:
        if label in text:
            errors.append("Support Bundle must not emit privacy-sensitive field: " + label)

if errors:
    print("QS3D Support Bundle read-only preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Support Bundle confirms the destination before project access, uses read-only project lookup, emits aggregate/privacy-safe fields only, publishes through its atomic writer, and isolates post-write UI from persistent export success.")
