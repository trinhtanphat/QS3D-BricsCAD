#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LAYER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadLayerStateRuntime.cs"
VIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadViewStatusRuntime.cs"


def block(text: str, start_token: str, end_token: str) -> str:
    start = text.find(start_token)
    end = text.find(end_token, start + 1)
    if start < 0 or end <= start:
        raise SystemExit(f"FAIL mcp native document affinity: cannot isolate {start_token}")
    return text[start:end]


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL mcp native document affinity: " + message)


def require_order(section: str, *tokens: str) -> None:
    positions = [section.find(token) for token in tokens]
    require(all(position >= 0 for position in positions), "missing ordered token: " + " -> ".join(tokens))
    require(positions == sorted(positions), "invalid ordering: " + " -> ".join(tokens))


def guard_block(text: str) -> str:
    start_token = "private static void EnsureSameActiveDocument(Document document, string operation)"
    start = text.find(start_token)
    if start < 0:
        return ""
    end = text.find("private static ", start + len(start_token))
    return text[start:] if end < 0 else text[start:end]


def main() -> None:
    layer = LAYER.read_text(encoding="utf-8")
    view = VIEW.read_text(encoding="utf-8")

    require("private static void EnsureSameActiveDocument(Document document, string operation)" in layer,
            "layer runtime must define exact active-document guard")
    require("private static void EnsureSameActiveDocument(Document document, string operation)" in view,
            "view runtime must define exact active-document guard")

    set_layer = block(layer, "private static string SetLayerState(", "private static string CaptureSnapshot(")
    require_order(set_layer,
                  "using (document.LockDocument())",
                  "EnsureSameActiveDocument(document, \"cad_layer_set_state\")",
                  "StartTransaction()",
                  "transaction.Commit();",
                  "EnsureSameActiveDocument(document, \"cad_layer_set_state_result\")",
                  "McpCadAgentRuntime.AuditDomainMutation(")

    restore = block(layer, "private static string RestoreSnapshot(", "private static string EncodeSnapshot(")
    require_order(restore,
                  "using (document.LockDocument())",
                  "EnsureSameActiveDocument(document, \"cad_layer_restore\")",
                  "StartTransaction()",
                  "transaction.Commit();",
                  "EnsureSameActiveDocument(document, \"cad_layer_restore_result\")",
                  "McpCadAgentRuntime.AuditDomainMutation(")

    zoom = block(view, "private static string ZoomExtents(", "private static string FitEntities(")
    require_order(zoom,
                  "using (document.LockDocument())",
                  "EnsureSameActiveDocument(document, \"cad_view_zoom_extents\")",
                  "ApplyExtents(document, extents, padding, \"drawing_extents\", 0)")

    fit = block(view, "private static string FitEntities(", "private static string AppendFitWarnings(")
    require_order(fit,
                  "using (document.LockDocument())",
                  "EnsureSameActiveDocument(document, \"cad_view_fit_entities\")",
                  "StartOpenCloseTransaction()",
                  "ApplyExtents(document, combined, padding, \"entities\", fittedCount)")

    set_view = block(view, "private static string SetView(", "private static void RequireCompatibleViewAspect(")
    require_order(set_view,
                  "using (document.LockDocument())",
                  "EnsureSameActiveDocument(document, \"cad_view_set\")",
                  "document.Editor.SetCurrentView(view);",
                  "EnsureSameActiveDocument(document, \"cad_view_set_result\")",
                  "CurrentViewJson(document, \"set\")")

    apply_extents = block(view, "private static string ApplyExtents(", "private static Extents3d TransformExtents(")
    require_order(apply_extents,
                  "document.Editor.SetCurrentView(view);",
                  "EnsureSameActiveDocument(document, \"cad_view_result\")",
                  "CurrentViewJson(document, source)")

    for source, name in ((layer, "layer"), (view, "view")):
        guard = guard_block(source)
        require("ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" in guard,
                f"{name} exact-document guard must use reference identity")
        require("active BricsCAD document changed" in guard,
                f"{name} exact-document guard must fail closed with explicit document-change error")

    print("PASS mcp native layer/view active-document affinity")


if __name__ == "__main__":
    main()
