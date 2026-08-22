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
        ensure = ed2.find('DrawingUnitWorkflow.EnsureResolved(doc, "QS3DED2")')
        confirm = ed2.find("if (dialog.ShowDialog() != true) return;")
        project = ed2.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
        if min(ensure, confirm, project) < 0:
            errors.append("ED2 missing unit/save/read-only project contract token")
        elif not ensure < confirm < project:
            errors.append("ED2 contract changed: unit policy check must remain non-mutating before Save and live project lookup must remain after Save confirmation")

    marker = 'var readOnlyExportPreparation = string.Equals(operation, "QS3DED2", StringComparison.OrdinalIgnoreCase);'
    resolved_guard = "if (!readOnlyExportPreparation)\n                    PersistLegacyBindingIfNeeded(document, resolution);"
    unresolved_guard = "if (readOnlyExportPreparation)"
    prompt = "return PromptAndPersist(document);"
    if marker not in units:
        errors.append("DrawingUnitWorkflow no longer identifies QS3DED2 read-only export preparation")
    if resolved_guard not in units:
        errors.append("resolved ED2 unit policy can persist legacy project binding before Save confirmation")
    unresolved = units.find(unresolved_guard)
    prompt_index = units.find(prompt)
    if unresolved < 0 or prompt_index < 0 or unresolved > prompt_index:
        errors.append("unresolved ED2 unit policy is not blocked before PromptAndPersist")
    else:
        guarded_block = units[unresolved:prompt_index]
        if "return false;" not in guarded_block:
            errors.append("unresolved ED2 unit policy must fail closed without project/unit persistence")
        for forbidden in ("PromptAndPersist(document)", "GetOrCreate", "ProjectContextCoordinator.Save", ".Touch()"):
            if forbidden in guarded_block:
                errors.append("ED2 read-only unit guard contains forbidden mutation token: " + forbidden)

print("QS3D ED2 unit export read-only preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 unit resolution before Save confirmation is read-only; unresolved units fail closed and explicit QS3DUNITS owns persistence.")
