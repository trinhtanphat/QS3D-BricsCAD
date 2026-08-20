#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENGINE = ROOT / "src" / "QS3D.BricsCAD.V25" / "QuantityEngine2Commands.cs"
RESULT_WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityCalculationResultWindow.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    engine = ENGINE.read_text(encoding="utf-8")
    result_window = RESULT_WINDOW.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")

    required_engine = [
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "QuantityCalculationResultWindow.ShowNoProject(noProjectMessage)",
        "PaletteCoordinator.ShowBimWorkspace()",
        "QuantityCalculationResultWindow.ShowNoElements(noElementsMessage)",
    ]
    for fragment in required_engine:
        if fragment not in engine:
            return fail("Quantity Engine2 missing-project contract is missing: " + fragment)

    if "ExistingProjectMutationContext.Require(document, \"Tính khối lượng (Engine2)\")" in engine:
        return fail("Quantity Engine2 must not throw the generic existing-project mutation error for a missing project.")
    if "ProjectContextCoordinator.GetOrCreate" in engine:
        return fail("Quantity Engine2 must not silently create a QS3D project.")

    try_get = engine.index("ExistingProjectMutationContext.TryGet(document, out var project)")
    regenerate = engine.index(".RegenerateDirty(project)")
    if try_get > regenerate:
        return fail("Quantity Engine2 must resolve the existing project before regeneration.")

    required_window = [
        "public static bool ShowNoProject(string message)",
        "Bản vẽ chưa có dự án QS3D.",
        "offerModeling: true",
        "public static bool ShowNoElements(string message)",
    ]
    for fragment in required_window:
        if fragment not in result_window:
            return fail("Quantity result window missing-project UX is missing: " + fragment)

    if '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"' not in v26_project:
        return fail("V26 no longer shares the V25 adapter source; mirror the Engine2 fix explicitly before changing this guard.")

    print("PASS: Quantity Engine2 handles a missing project without silent creation or a dead-end generic mutation error.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
