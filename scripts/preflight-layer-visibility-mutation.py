#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/LayerVisibilityService.cs"


def validate(text: str) -> list[str]:
    errors: list[str] = []

    required = (
        "private const int MaxRequestedLayerNames = 10000;",
        "var wanted = BuildWantedNames(names);",
        "var mutationCount = 0;",
        "var desiredOff = !visible;",
        "var changeOffState = layer.IsOff != desiredOff;",
        "var thawLayer = visible && layer.IsFrozen;",
        "if (!changeOffState && !thawLayer) continue;",
        "if (changeOffState) layer.IsOff = desiredOff;",
        "if (thawLayer) layer.IsFrozen = false;",
        "if (layer.IsLocked == locked) continue;",
        "if (mutationCount > 0) document.Editor.Regen();",
        "private static HashSet<string> BuildWantedNames(IEnumerable<string> names)",
        "if (enumerated > MaxRequestedLayerNames)",
        "wanted.Add(name);",
    )
    for token in required:
        if token not in text:
            errors.append("missing layer visibility robustness contract: " + token)

    if text.count("if (mutationCount > 0) document.Editor.Regen();") != 2:
        errors.append("both visibility and lock operations must gate Regen on real mutation")

    if text.count("var wanted = BuildWantedNames(names);") != 2:
        errors.append("both public operations must use bounded name materialization")

    visible_start = text.find("public static int SetVisible")
    locked_start = text.find("public static int SetLocked")
    helper_start = text.find("private static HashSet<string> BuildWantedNames")
    if visible_start < 0 or locked_start <= visible_start or helper_start <= locked_start:
        errors.append("LayerVisibilityService method structure is not recognizable")
        return errors

    visible = text[visible_start:locked_start]
    locked = text[locked_start:helper_start]
    helper = text[helper_start:]

    for method, label in ((visible, "SetVisible"), (locked, "SetLocked")):
        count_pos = method.find("count++;")
        mutation_pos = method.find("mutationCount++;")
        regen_pos = method.find("if (mutationCount > 0) document.Editor.Regen();")
        if min(count_pos, mutation_pos, regen_pos) < 0:
            errors.append(label + " is missing matched/mutation/regen accounting")
        elif not count_pos < mutation_pos < regen_pos:
            errors.append(label + " must preserve matched count before mutation count and gated Regen")

    if "layer.UpgradeOpen();\n                    layer.IsOff = !visible;" in visible:
        errors.append("SetVisible regressed to unconditional write-open/property assignment")
    if "document.Editor.Regen();\n            return count;" in visible:
        errors.append("SetVisible regressed to unconditional Regen")
    if "document.Editor.Regen();\n            return count;" in locked:
        errors.append("SetLocked regressed to unconditional Regen")

    helper_required = (
        "var enumerated = 0;",
        "foreach (var name in names)",
        "enumerated++;",
        "throw new ArgumentException(",
        "nameof(names)",
    )
    for token in helper_required:
        if token not in helper:
            errors.append("bounded layer-name ingestion missing: " + token)

    return errors


def run_mutation_self_checks(pristine: str) -> list[str]:
    failures: list[str] = []
    mutations = {
        "unconditional visible writes": (
            "if (!changeOffState && !thawLayer) continue;",
            "if (false) continue;",
        ),
        "lost thaw-on-show": (
            "var thawLayer = visible && layer.IsFrozen;",
            "var thawLayer = false;",
        ),
        "unconditional regen": (
            "if (mutationCount > 0) document.Editor.Regen();",
            "document.Editor.Regen();",
        ),
        "unbounded request ingestion": (
            "if (enumerated > MaxRequestedLayerNames)",
            "if (false)",
        ),
    }
    for label, (needle, replacement) in mutations.items():
        if needle not in pristine:
            failures.append("self-check fixture missing mutation anchor: " + label)
            continue
        mutated = pristine.replace(needle, replacement, 1)
        if not validate(mutated):
            failures.append("guard did not detect mutation: " + label)
    return failures


def main() -> int:
    if not SOURCE.is_file():
        print("ERROR: missing " + str(SOURCE.relative_to(ROOT)))
        return 1

    text = SOURCE.read_text(encoding="utf-8")
    errors = validate(text)
    errors.extend(run_mutation_self_checks(text))

    print("QS3D layer visibility mutation/regen robustness preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with", len(errors), "error(s).")
        return 1

    print(
        "PASS: layer visibility/lock operations preserve matched counts, avoid no-op writes, "
        "thaw on show, gate Regen on real mutations, and bound requested-name ingestion."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
