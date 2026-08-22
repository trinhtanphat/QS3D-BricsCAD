#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
WORKFLOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "DrawingUnitWorkflow.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + " missing token: " + token)


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        pos = text.find(token, cursor + 1)
        if pos < 0:
            raise AssertionError(label + " missing ordered token: " + token)
        cursor = pos


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following method boundary: " + next_signature)
    return text[start:end]


def main():
    commands = COMMANDS.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")
    bq = method_slice(commands, "public void ShowQuantitySummary()", "public void ExportEd2Workflow()")
    ensure = method_slice(workflow, "public static bool EnsureResolved(", "public static void Configure(")
    persist_start = workflow.find("private static void PersistLegacyBindingIfNeeded(")
    if persist_start < 0:
        raise AssertionError("Missing PersistLegacyBindingIfNeeded helper")
    persist = workflow[persist_start:]

    require(bq, "DrawingUnitWorkflow.EnsureResolved(doc, \"QS3DBQ\")", "BQ unit preparation")
    require_order(
        ensure,
        "BQ projectless fast-fail",
        "var readOnlyBqPreparation = string.Equals(operation, \"QS3DBQ\"",
        "if (readOnlyBqPreparation && !ProjectContextCoordinator.TryGetReadOnly(document, out _))",
        "return false;",
        "CadUnitService.TryGetPolicy(document, out _, out var resolution)")
    require_order(
        ensure,
        "BQ compatible legacy-binding migration",
        "var readOnlyQuantityPreparation = readOnlyExportPreparation || readOnlyBqPreparation;",
        "CadUnitService.TryGetPolicy(document, out _, out var resolution)",
        "if (!readOnlyExportPreparation)",
        "PersistLegacyBindingIfNeeded(document, resolution)")
    if "if (!readOnlyQuantityPreparation)" in ensure:
        raise AssertionError("BQ resolved-unit preparation must allow compatible legacy binding migration instead of sharing ED2 persistence suppression.")

    require_order(
        ensure,
        "BQ unresolved-unit path",
        "if (readOnlyQuantityPreparation)",
        "QS3DBQ: drawing unit is undefined/unsupported. Run QS3DUNITS first",
        "return false;",
        "return PromptAndPersist(document);")

    if "GetOrCreate" in ensure:
        raise AssertionError("EnsureResolved must not directly create a project during BQ/ED2 preparation.")

    require_order(
        persist,
        "legacy binding migration scope",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var observedProject)",
        "if (observedProject.Elements.Count == 0) return;",
        "ExistingProjectMutationContext.Require(document, \"Legacy drawing-unit binding\")",
        "DrawingUnitResolutionPolicy.BindQuantityUnit(project.Metadata, true, resolution.Unit, resolution.Source)",
        "project.Touch();",
        "ProjectContextCoordinator.Save(document)")
    if "GetOrCreate" in persist or "PromptAndPersist" in persist:
        raise AssertionError("Legacy BQ binding migration must never create a project or prompt for a unit.")

    print("PASS: QS3DBQ projectless/unresolved preparation remains fail-closed; resolved existing projects may only canonicalize a compatible legacy quantity-unit binding, while ED2 persistence remains suppressed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
