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
        "var observedCodes = CollectObservedCodes(settings)",
        "observedCodes.Contains(source.Value)",
        "observedCodes.Contains(target.Value)",
        "settings.FindIntersectionRule(source.Value, target.Value) != null",
        "document.Editor.GetKeywords(",
        'string.Equals(confirm.StringResult, "Yes", StringComparison.OrdinalIgnoreCase)',
        "var latestSettings = store.Load().Clone()",
        "latestSettings.NormalizeAndValidate()",
        "var latestObservedCodes = CollectObservedCodes(latestSettings)",
        "latestObservedCodes.Contains(source.Value)",
        "latestObservedCodes.Contains(target.Value)",
        "latestSettings.FindIntersectionRule(source.Value, target.Value) != null",
        "latestSettings.IntersectionRules.Add(new QuantityIntersectionRuleSetting",
        "Source = source.Value",
        "Target = target.Value",
        "store.Save(latestSettings)",
        "Luật chiều ngược không được tự tạo",
        "settings.CategoryRules.Select(x => x.Category)",
        ".Concat(settings.IntersectionRules.Select(x => x.Source))",
        ".Concat(settings.IntersectionRules.Select(x => x.Target))",
    )
    for token in required:
        if token not in text:
            errors.append("quantity rule create missing contract: " + token)

    load = text.find("var settings = store.Load().Clone()")
    duplicate = text.find("settings.FindIntersectionRule(source.Value, target.Value) != null")
    confirm = text.find("var confirm = document.Editor.GetKeywords(")
    latest_load = text.find("var latestSettings = store.Load().Clone()")
    latest_codes = text.find("var latestObservedCodes = CollectObservedCodes(latestSettings)")
    latest_duplicate = text.find("latestSettings.FindIntersectionRule(source.Value, target.Value) != null")
    append = text.find("latestSettings.IntersectionRules.Add(new QuantityIntersectionRuleSetting")
    validate_after_append = text.find("latestSettings.NormalizeAndValidate();", append)
    save = text.find("store.Save(latestSettings)")
    if min(load, duplicate, confirm, latest_load, latest_codes, latest_duplicate, append, validate_after_append, save) < 0 or not (
        load < duplicate < confirm < latest_load < latest_codes < latest_duplicate < append < validate_after_append < save
    ):
        errors.append("rule creation must preflight initial settings, confirm, reload latest settings, revalidate categories/duplicate, append one directed row, validate again, then save latest state")

    if text.count("latestSettings.IntersectionRules.Add(new QuantityIntersectionRuleSetting") != 1:
        errors.append("rule creation must append exactly one directed rule to the latest settings and must not auto-create the reverse pair")
    if "settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting" in text:
        errors.append("rule creation must not persist the pre-prompt settings clone after confirmation")
    if "store.Save(settings)" in text:
        errors.append("rule creation must save the refreshed latest settings, not the pre-prompt clone")

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

print("PASS: QS3DRULECREATE has one canonical owner, preserves confirmation/cancel behavior, refreshes latest settings after confirmation, revalidates categories/duplicates, appends one A->B row, and persists only through QuantitySettingsStore.")
