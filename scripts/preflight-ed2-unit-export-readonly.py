#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
UNITS = ROOT / "src/QS3D.BricsCAD.V25/Services/DrawingUnitWorkflow.cs"
errors = []

if not COMMANDS.is_file():
    errors.append("missing " + str(COMMANDS.relative_to(ROOT)))
if not UNITS.is_file():
    errors.append("missing " + str(UNITS.relative_to(ROOT)))

if not errors:
    commands = COMMANDS.read_text(encoding="utf-8")
    units = UNITS.read_text(encoding="utf-8")

    ed2_start = commands.find('[CommandMethod("QS3DED2"')
    bbs_start = commands.find('[CommandMethod("QS3DBBS"', ed2_start + 1)
    if ed2_start < 0 or bbs_start < 0:
        errors.append("Commands.cs missing QS3DED2/QS3DBBS method boundaries")
    else:
        ed2 = commands[ed2_start:bbs_start]
        project = ed2.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
        ensure = ed2.find('DrawingUnitWorkflow.EnsureResolved(doc, "QS3DED2")', project + 1)
        snapshot = ed2.find("ProjectStateSnapshot.CreateDetachedCopy(project)", ensure + 1)
        details = ed2.find("ProjectQuantityReportBuilder.Detail(previewProject", snapshot + 1)
        live = ed2.find("EnsureEd2HandlesAreLive(doc, details);", details + 1)
        dialog = ed2.find("var dialog = new SaveFileDialog", live + 1)
        confirm = ed2.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
        export = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);", confirm + 1)
        if min(project, ensure, snapshot, details, live, dialog, confirm, export) < 0:
            errors.append("ED2 missing existing-project/unit/detached-report/save/export contract token")
        elif not project < ensure < snapshot < details < live < dialog < confirm < export:
            errors.append("ED2 must resolve an existing project and read-only unit policy before detached export validation, then write only after Save confirmation")
        if "ProjectContextCoordinator.GetOrCreate(doc)" in ed2 or "ExistingProjectMutationContext" in ed2:
            errors.append("ED2 export path must not create or bind mutable live project state")
        if confirm >= 0 and "XlsxQuantityExporter.ExportEd2(" in ed2[:confirm]:
            errors.append("ED2 must not write XLSX before Save confirmation")

    read_only_export = 'var readOnlyExportPreparation = string.Equals(operation, "QS3DED2", StringComparison.OrdinalIgnoreCase);'
    read_only_bq = 'var readOnlyBqPreparation = string.Equals(operation, "QS3DBQ", StringComparison.OrdinalIgnoreCase);'
    combined = "var readOnlyQuantityPreparation = readOnlyExportPreparation || readOnlyBqPreparation;"
    resolved_guard = "if (!readOnlyExportPreparation)\n                    PersistLegacyBindingIfNeeded(document, resolution);"
    unresolved_guard = "if (readOnlyQuantityPreparation)"
    prompt = "return PromptAndPersist(document);"
    for token, message in (
        (read_only_export, "DrawingUnitWorkflow no longer identifies QS3DED2 read-only export preparation"),
        (read_only_bq, "DrawingUnitWorkflow no longer identifies QS3DBQ quantity preparation"),
        (combined, "DrawingUnitWorkflow no longer combines ED2/BQ for unresolved-unit fail-closed preparation"),
        (resolved_guard, "resolved unit path no longer suppresses legacy binding persistence specifically for ED2 export preparation"),
    ):
        if token not in units:
            errors.append(message)

    resolved = units.find("CadUnitService.TryGetPolicy(document, out _, out var resolution)")
    resolved_return = units.find("return true;", resolved + 1)
    if resolved < 0 or resolved_return < 0:
        errors.append("DrawingUnitWorkflow missing resolved-unit preparation block")
    else:
        resolved_block = units[resolved:resolved_return]
        if "if (!readOnlyQuantityPreparation)" in resolved_block:
            errors.append("resolved BQ preparation must not suppress compatible legacy binding migration with ED2")
        if "PromptAndPersist(document)" in resolved_block or "GetOrCreate" in resolved_block:
            errors.append("resolved ED2/BQ unit block must not prompt for units or create a project")

    unresolved = units.find(unresolved_guard)
    prompt_index = units.find(prompt)
    if unresolved < 0 or prompt_index < 0 or unresolved > prompt_index:
        errors.append("unresolved read-only quantity unit policy is not blocked before PromptAndPersist")
    else:
        guarded_block = units[unresolved:prompt_index]
        if "return false;" not in guarded_block:
            errors.append("unresolved ED2/BQ unit policy must fail closed without project/unit persistence")
        if "readOnlyExportPreparation" not in guarded_block:
            errors.append("ED2-specific unresolved unit guidance must remain inside the shared read-only quantity guard")
        for forbidden in ("PromptAndPersist(document)", "GetOrCreate", "ProjectContextCoordinator.Save", ".Touch()"):
            if forbidden in guarded_block:
                errors.append("read-only quantity unit guard contains forbidden mutation token: " + forbidden)

print("QS3D ED2 unit export read-only preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 resolves existing state and unit policy read-only before detached export validation; ED2 suppresses legacy binding persistence, compatible BQ preparation may canonicalize an existing legacy binding, unresolved units fail closed, and explicit QS3DUNITS owns unit selection persistence.")
