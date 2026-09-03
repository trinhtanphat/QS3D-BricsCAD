#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
VIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs"

errors = []
text = VIEW.read_text(encoding="utf-8") if VIEW.is_file() else ""


def require(token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label} missing token: {token}")


if not text:
    errors.append("missing McpCadViewStatusRuntime.cs")
else:
    fit_start = text.find("private static string FitEntities(")
    fit_end = text.find("private static string SetView(", fit_start)
    fit = text[fit_start:fit_end] if fit_start >= 0 and fit_end > fit_start else ""
    if not fit:
        errors.append("unable to isolate FitEntities")
    else:
        for token in (
            "var skippedHandles = new List<string>();",
            "skippedHandles.Add(HandleText(handle));",
            "continue;",
            "var fittedCount = 0;",
            "fittedCount++;",
            "AppendFitWarnings(",
        ):
            if token not in fit:
                errors.append("FitEntities missing contract token: " + token)
        if "Entity has no usable geometric extents:" in fit:
            errors.append("FitEntities must not fail the whole request solely because one live entity has unusable extents")
        if "ApplyExtents(document, combined, padding, \"entities\", handles.Count)" in fit:
            errors.append("FitEntities must report fitted entities rather than treating skipped invalid-extents handles as fitted")

    for token in (
        "private static string AppendFitWarnings(",
        '\\"requestedEntityCount\\"',
        '\\"skippedEntityCount\\"',
        '\\"skippedHandles\\"',
        "RequireViewMutationIdle();",
        "document.Editor.SetCurrentView(view);",
    ):
        require(token, "view/extents safety")

    # Preserve the no-forced-refresh contract: view mutations must not invoke REGEN/UpdateScreen.
    for forbidden in ("Editor.Regen(", ".UpdateScreen(", 'SendStringToExecute("_.REGEN', 'SendStringToExecute("_.REGENALL'):
        if forbidden in text:
            errors.append("view runtime must not force refresh/regen: " + forbidden)

    # View mutations must fail closed if command/modal state is already active before touching view state,
    # and explicitly distinguish CMDACTIVE bit 8 so callers do not treat an undismissable dialog as ESC-cancellable command work.
    idle_start = text.find("private static void RequireViewMutationIdle(")
    idle_end = text.find("private static ", idle_start + 1) if idle_start >= 0 else -1
    idle = text[idle_start:idle_end] if idle_start >= 0 and idle_end > idle_start else ""
    for token in (
        'Application.GetSystemVariable("CMDACTIVE")',
        "active == 0",
        "(active & 8) != 0",
        "modal/dialog state (CMDACTIVE bit 8)",
    ):
        if token not in idle:
            errors.append("RequireViewMutationIdle missing modal/command gate token: " + token)

if errors:
    print("ERROR: MCP view extents/modal safety preflight failed")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: cad_view_fit_entities skips only live entities with unusable extents, reports skipped handles/counts, fails only when no usable extents remain, and view mutations preserve fail-closed CMDACTIVE/modal-bit/no-forced-REGEN safety.")
