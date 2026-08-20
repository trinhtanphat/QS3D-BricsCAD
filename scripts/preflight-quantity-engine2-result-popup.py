#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RIBBON_REL = "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs"
COMMAND_REL = "src/QS3D.BricsCAD.V25/QuantityEngine2Commands.cs"
WINDOW_REL = "src/QS3D.BricsCAD.V25/UI/QuantityCalculationResultWindow.cs"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden contract: {needle}")


def main():
    ribbon = read(RIBBON_REL)
    command = read(COMMAND_REL)
    window = read(WINDOW_REL)

    calculate_marker = '"QS3D_QTY_BLT_CALCULATE"'
    export_marker = '"QS3D_QTY_BLT_EXPORT"'
    require(ribbon, calculate_marker, RIBBON_REL)
    require(ribbon, export_marker, RIBBON_REL)
    calculate = ribbon.split(calculate_marker, 1)[1].split(export_marker, 1)[0]
    require(calculate, '"Tính khối lượng\\n(Engine2)"', RIBBON_REL + "::calculate")
    require(calculate, '"QS3DQUANTITYENGINE2"', RIBBON_REL + "::calculate")
    forbid(calculate, '"QS3DREGEN"', RIBBON_REL + "::calculate")

    for needle in (
        '[CommandMethod("QS3DQUANTITYENGINE2", CommandFlags.Modal)]',
        'DrawingUnitWorkflow.EnsureResolved(document, "QS3DQUANTITYENGINE2")',
        'ExistingProjectMutationContext.TryGet(document, out var project)',
        'QuantityCalculationResultWindow.ShowNoProject(noProjectMessage)',
        'PaletteCoordinator.ShowBimWorkspace()',
        'new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())',
        '.RegenerateDirty(project)',
        'ProjectQuantityReportBuilder.Group(project)',
        'QuantityEngine2Summary.Build(rows, regenerated)',
        'if (summary.ElementCount == 0)',
        'QuantityCalculationResultWindow.ShowNoElements(noElementsMessage)',
        'Chưa có cấu kiện hợp lệ để tính khối lượng.',
        'var openQuantityReview = QuantityCalculationResultWindow.ShowSuccess(summary)',
        'if (openQuantityReview)',
        'new Commands().ShowQuantitySummary()',
        'QuantityCalculationResultWindow.ShowError(ex.Message)',
        'QuantityReportMath.FiniteAccumulator',
        'ElementCategory.Beam',
        'ElementCategory.StructuralWall',
        'ElementCategory.ArchitecturalWall',
        'ElementCategory.Slab',
        'ElementCategory.Foundation',
    ):
        require(command, needle, COMMAND_REL)

    forbid(command, 'ExistingProjectMutationContext.Require(document, "Tính khối lượng (Engine2)")', COMMAND_REL)
    forbid(command, 'ProjectContextCoordinator.GetOrCreate', COMMAND_REL)

    try_get_pos = command.find('ExistingProjectMutationContext.TryGet(document, out var project)')
    no_project_pos = command.find('QuantityCalculationResultWindow.ShowNoProject(noProjectMessage)', try_get_pos)
    regenerate_pos = command.find('.RegenerateDirty(project)', no_project_pos)
    if not (0 <= try_get_pos < no_project_pos < regenerate_pos):
        raise SystemExit(
            "FAIL: Engine2 must resolve the existing project, handle the no-project UX without creation, then regenerate only the existing project."
        )

    for needle in (
        'public static bool ShowNoProject(string message)',
        'offerModeling: true',
        'public static bool ShowNoElements(string message)',
        'public static bool ShowSuccess(QuantityEngine2Summary summary)',
        'offerQuantity: true',
        'return window._openQuantityRequested;',
        '"Tính khối lượng thành công (dùng lại kết quả — model chưa đổi):"',
        '"• Bê tông: "',
        '"• Cốp pha: "',
        '"• Chiều dài (dầm/tường): "',
        '"• Chu vi biên (sàn/móng): ngoài "',
        '"Chọn “Xem khối lượng” để mở bảng tổng hợp chi tiết."',
        'ok.Content = "Xem khối lượng"',
        '_openQuantityRequested = true;',
        'Content = "OK"',
        'Text = success ? "✓" : "!"',
        'WindowStyle = System.Windows.WindowStyle.None',
        'AllowsTransparency = true',
    ):
        require(window, needle, WINDOW_REL)

    print(
        "PASS: Engine2 keeps the no-silent-project-creation boundary, presents actionable missing/empty-project UX, calculates existing-project quantities, and can hand successful results to the canonical detailed quantity review."
    )


if __name__ == "__main__":
    main()
