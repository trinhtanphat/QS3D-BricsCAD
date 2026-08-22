#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/QuantityRuleCreateCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing QuantityRuleCreateCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DRULECREATE", CommandFlags.Modal)]')
    end = text.find('[CommandMethod("QS3DINTERSECTIONRULECREATE", CommandFlags.Modal)]', start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QS3DRULECREATE")
    else:
        command = text[start:end]
        tokens = {
            "initial_load": "var settings = store.Load().Clone();",
            "initial_codes": "var observedCodes = CollectObservedCodes(settings);",
            "initial_duplicate": "settings.FindIntersectionRule(source.Value, target.Value) != null",
            "confirm": "var confirm = document.Editor.GetKeywords(",
            "confirmed": 'string.Equals(confirm.StringResult, "Yes", StringComparison.OrdinalIgnoreCase)',
            "latest_load": "var latestSettings = store.Load().Clone();",
            "latest_validate": "latestSettings.NormalizeAndValidate();",
            "latest_codes": "var latestObservedCodes = CollectObservedCodes(latestSettings);",
            "latest_source": "latestObservedCodes.Contains(source.Value)",
            "latest_target": "latestObservedCodes.Contains(target.Value)",
            "latest_duplicate": "latestSettings.FindIntersectionRule(source.Value, target.Value) != null",
            "append": "latestSettings.IntersectionRules.Add(new QuantityIntersectionRuleSetting",
            "save": "store.Save(latestSettings);",
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("quantity rule freshness missing token: " + token)

        ordered = (
            "initial_load", "initial_codes", "initial_duplicate", "confirm", "confirmed",
            "latest_load", "latest_validate", "latest_codes", "latest_source", "latest_target",
            "latest_duplicate", "append", "save",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("QS3DRULECREATE must reload latest settings only after confirmation, revalidate categories/duplicate, then append/save latest state")

        if command.count("store.Load().Clone()") != 2:
            errors.append("QS3DRULECREATE must have exactly one initial load and one post-confirmation latest-settings reload")
        if "store.Save(settings)" in command:
            errors.append("QS3DRULECREATE must not save the pre-prompt settings clone")
        if "settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting" in command:
            errors.append("QS3DRULECREATE must not mutate the pre-prompt settings clone after confirmation")
        if command.count("latestSettings.IntersectionRules.Add(new QuantityIntersectionRuleSetting") != 1:
            errors.append("QS3DRULECREATE must append exactly one directed rule to refreshed settings")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: QS3DRULECREATE refreshes persisted settings after confirmation, revalidates category/duplicate state, and saves only the latest clone instead of overwriting prompt-time concurrent changes.")
