#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def require_order(text, needles, label):
    cursor = -1
    for needle in needles:
        index = text.find(needle, cursor + 1)
        if index < 0:
            fail(f"{label}: missing ordered token: {needle}")
        if index <= cursor:
            fail(f"{label}: token out of order: {needle}")
        cursor = index


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    function = read("src/QS3D.BricsCAD.V25/Ribbon/BltModelingRibbonFunctionRefiner.cs")
    move_z = read("src/QS3D.BricsCAD.V25/ModelingRibbonCommands.cs")
    family = read("src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs")
    init = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
    v26 = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")

    # Every visible reference action gets one exact route. These native command names use the
    # underscore convention so localized BricsCAD installs resolve the English command token.
    routes = {
        "MATERIAL": "_.MATERIALS",
        "STEEL_PROFILE": "_.BIMPROFILES",
        "CREATE_DETAIL": "_.BIMCREATEDETAIL",
        "PLANE_XY": "_.UCS _World",
        "LINE": "_.LINE",
        "POLYLINE": "_.PLINE",
        "RECTANGLE": "_.RECTANG",
        "CIRCLE": "_.CIRCLE",
        "ARC": "_.ARC",
        "JOIN_POLYLINE": "_.JOIN",
        "OFFSET": "_.OFFSET",
        "MOVE": "_.MOVE",
        "COPY": "_.COPY",
        "MOVE_Z": "QS3DMOVEZ",
        "EXTRUDE": "_.EXTRUDE",
        "SWEEP": "_.SWEEP",
        "LOFT": "_.LOFT",
        "ATTACH_FAMILY": "QS3DFAMILIES",
        "UNION": "_.UNION",
        "SUBTRACT": "_.SUBTRACT",
        "INTERSECT": "_.INTERSECT",
    }
    if len(routes) != 21:
        fail("MODELING route table must cover exactly 21 visible actions")
    for suffix, route in routes.items():
        require(function, f'[ButtonPrefix + "{suffix}"] = "{route}"', f"MODELING route {suffix}")

    # A final functional pass must reject missing/duplicate buttons, missing handlers and stale
    # CommandParameter values rather than declaring the topbar ready with inert actions.
    for token in (
        'if (buttons.Count != ExpectedRoutes.Count)',
        'if (!buttons.TryGetValue(expected.Key, out var button))',
        'if (GetProperty(button, "CommandHandler") == null)',
        'SetProperty(button, "CommandParameter", expected.Value);',
        'GetProperty(button, "CommandParameter") as string',
        'if (result.ContainsKey(id))',
    ):
        require(function, token, "MODELING functional finalization")

    # Regression: the old MOVE_Z route was unrestricted _.MOVE and asked the user to manually
    # enter @0,0,dz. The dedicated command now preserves PICKFIRST, supports post-selection,
    # prompts only for dz, validates freshness/finite input, then delegates mutation to native MOVE
    # with X=Y=0 through its explicit Displacement option.
    for token in (
        '[CommandMethod("QS3DMOVEZ", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        'var selection = editor.SelectImplied();',
        'selection = editor.GetSelection(options);',
        'var distance = editor.GetDouble(distanceOptions);',
        'AllowNegative = true',
        'AllowZero = false',
        'double.IsNaN(distance.Value)',
        'double.IsInfinity(distance.Value)',
        'ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)',
        '"_.MOVE",',
        'selection.Value,',
        '"_Displacement",',
        'new Point3d(0d, 0d, distance.Value)',
        'PaletteCoordinator.SetStatus(',
        'editor.WriteMessage("\\n" + message)',
    ):
        require(move_z, token, "QS3DMOVEZ functional contract")

    # The Family button points at a real production command, not a screenshot-only placeholder.
    for token in (
        '[CommandMethod("QS3DFAMILIES", CommandFlags.Modal)]',
        'ExistingProjectMutationContext.TryGet(document, out _);',
        'new FamilyManagerWindow(document)',
        'Application.ShowModelessWindow',
        'inheritance-safe semantic assignment',
    ):
        require(family, token, "Gắn vào Family workflow")

    # Function pinning happens after the owner-reference buttons are built and before icons/fallback
    # capture the final state; teardown resets both refiners for NETLOAD/reload safety.
    require_order(
        init,
        (
            "BltModelingRibbonAugmenter.TryInitialize()",
            "BltModelingRibbonFunctionRefiner.TryInitialize()",
            "BltModelingRibbonVisualRefiner.TryInitialize()",
            "RibbonBootstrapIconAugmenter.TryInitialize()",
            "RibbonCommandParameterFallback.TryInitialize()",
        ),
        "MODELING function lifecycle",
    )
    require(init, "BltModelingRibbonFunctionRefiner.Reset();", "MODELING function teardown")

    # V26 links the V25 C# source tree, so the same button routes and QS3DMOVEZ helper compile into
    # both supported host-major adapters instead of diverging by product version.
    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared function source")

    print("PASS: all 21 MODELING buttons have pinned routes; Theo phương Z is genuinely Z-constrained.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
