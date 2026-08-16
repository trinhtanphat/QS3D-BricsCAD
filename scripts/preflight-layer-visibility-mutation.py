#!/usr/bin/env python3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Cad/LayerVisibilityService.cs"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "var changed = 0;",
        "var targetIsOff = !visible;",
        "var requiresThaw = visible && layer.IsFrozen;",
        "if (layer.IsOff == targetIsOff && !requiresThaw) continue;",
        "if (layer.IsOff != targetIsOff) layer.IsOff = targetIsOff;",
        "if (requiresThaw) layer.IsFrozen = false;",
        "if (layer.IsLocked == locked) continue;",
        "if (changed > 0) document.Editor.Regen();",
    )
    for token in required:
        if token not in text:
            errors.append("missing layer mutation contract: " + token)

    if text.count("if (changed > 0) document.Editor.Regen();") != 2:
        errors.append("visibility and lock paths must each gate Regen on an actual mutation")
    if text.count("count++;") != 2:
        errors.append("matched-layer return count must remain independent from mutation count in both paths")
    if text.count("changed++;") != 2:
        errors.append("both setters must track actual mutations separately")
    if text.count("layer.UpgradeOpen();") != 2:
        errors.append("each setter must write-open only inside its mutation path")
    if "layer.UpgradeOpen();\n                    layer.IsOff = !visible;" in text:
        errors.append("visibility path regressed to unconditional write-open/state assignment")
    return errors


def expect_mutation_failure(pristine: str, old: str, new: str, label: str) -> None:
    if old not in pristine:
        raise SystemExit("FAIL: mutation fixture missing token for " + label)
    mutated = pristine.replace(old, new, 1)
    if not validate(mutated):
        raise SystemExit("FAIL: guard accepted mutation that removes " + label)


def main() -> int:
    if not SOURCE.is_file():
        raise SystemExit("FAIL: missing LayerVisibilityService.cs")
    text = SOURCE.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    expect_mutation_failure(
        text,
        "if (layer.IsOff == targetIsOff && !requiresThaw) continue;",
        "if (false) continue;",
        "visibility no-op detection",
    )
    expect_mutation_failure(
        text,
        "if (layer.IsLocked == locked) continue;",
        "if (false) continue;",
        "lock no-op detection",
    )
    expect_mutation_failure(
        text,
        "if (requiresThaw) layer.IsFrozen = false;",
        "// thaw removed",
        "show-thaws-frozen-layer behavior",
    )
    expect_mutation_failure(
        text,
        "if (changed > 0) document.Editor.Regen();",
        "document.Editor.Regen();",
        "mutation-gated regeneration",
    )

    print(
        "PASS: LayerVisibilityService preserves matched-count semantics while skipping redundant write-open/state mutations, "
        "thawing frozen layers when shown, and regenerating only after actual changes."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
