#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/QS3D.BricsCAD.V25"
SOURCE = SRC / "QuantityRuleCreateCommands.cs"
errors = []

expected_owner = "src/QS3D.BricsCAD.V25/QuantityRuleCreateCommands.cs"
for command in ("QS3DRULECREATE", "QS3DINTERSECTIONRULECREATE"):
    owners = []
    pattern = re.compile(r'\[CommandMethod\("' + re.escape(command) + r'"', re.IGNORECASE)
    for path in SRC.rglob("*.cs"):
        if pattern.search(path.read_text(encoding="utf-8")):
            owners.append(path.relative_to(ROOT).as_posix())
    if owners != [expected_owner]:
        errors.append(command + " must have exactly one canonical owner; found: " + ", ".join(owners))

if not SOURCE.is_file():
    errors.append("missing QuantityRuleCreateCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DRULECREATE", CommandFlags.Modal)]',
        '[CommandMethod("QS3DINTERSECTIONRULECREATE", CommandFlags.Modal)]',
        "var settings = store.Load().Clone()",
        "settings.NormalizeAndValidate()",
        "settings.CategoryRules.Select(x => x.Category)",
        ".Concat(settings.IntersectionRules.Select(x => x.Source))",
        ".Concat(settings.IntersectionRules.Select(x => x.Target))",
        "observedCodes.Contains(source.Value)",
        "observedCodes.Contains(target.Value)",
        "settings.FindIntersectionRule(source.Value, target.Value) != null",
        "document.Editor.GetKeywords(",
        'string.Equals(confirm.StringResult, "Yes", StringComparison.OrdinalIgnoreCase)',
        "settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting",
        "Source = source.Value",
        "Target = target.Value",
        "store.Save(settings)",
        "Luật chiều ngược không được tự tạo",
    )
    for token in required:
        if token not in text:
            errors.append("quantity rule create missing contract: " + token)

    load = text.find("var settings = store.Load().Clone()")
    duplicate = text.find("settings.FindIntersectionRule(source.Value, target.Value) != null")
    confirm = text.find("var confirm = document.Editor.GetKeywords(")
    append = text.find("settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting")
    validate_after_append = text.find("settings.NormalizeAndValidate();", append)
    save = text.find("store.Save(settings)")
    if min(load, duplicate, confirm, append, validate_after_append, save) < 0 or not (
        load < duplicate < confirm < append < validate_after_append < save
    ):
        errors.append("rule creation must load/validate, reject duplicates, confirm, append one directed row, validate again, then save")

    if text.count("settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting") != 1:
        errors.append("rule creation must append exactly one directed rule and must not auto-create the reverse pair")

    forbidden = (
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "CadHandleService",
        "TransactionManager",
        "DataContractJsonSerializer",
        "File.Write",
        "File.Move",
        "File.Replace",
    )
    for token in forbidden:
        if token in text:
            errors.append("quantity rule create must not bypass settings store or mutate CAD/project state: " + token)

print("QS3D quantity directed rule creation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DRULECREATE has one canonical owner, creates exactly one confirmed missing A->B settings row, preserves reverse-rule independence, rejects unknown/duplicate pairs, validates before persistence, and writes only through QuantitySettingsStore.")
