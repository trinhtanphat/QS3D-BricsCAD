#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs"
SAFETY = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.WrapperDriftSafety.cs"
BOOTSTRAP = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.FirstSaveBootstrap.cs"
HANDLER = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.FirstSaveHandler.cs"

def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)

def method(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"missing {signature}")
    brace = source.find("{", start)
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == "{": depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0: return source[start:index + 1]
    raise AssertionError(f"unterminated {signature}")

def main() -> int:
    ui = UI.read_text(encoding="utf-8")
    safety = SAFETY.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
    handler = HANDLER.read_text(encoding="utf-8")
    affinity = method(safety, "private bool EnsureBoundDocumentGeneration(string operation)")
    for token in ("MdiActiveDocument", "ReferenceEquals(activeDocument, _document)", "activeDocument.Database", "database.UnmanagedObject", "_wrapperDriftNativeDatabaseIdentity", "CloseForManagedWrapperDrift()"):
        require(token in affinity, f"generation guard missing {token}")
    bound = method(ui, "private void EnsureBoundDrawingIsActive(string operation)")
    require("EnsureBoundDocumentGeneration(operation)" in bound, "operation boundary must prove native generation")
    refresh = method(ui, "private void RefreshAfterCommit(Action refresh, string successMessage, string context)")
    require(refresh.count("EnsureBoundDocumentGeneration(context)") >= 2, "post-commit publication must revalidate generation before and after refresh")
    require("EnsureBoundDrawingIsActive(operation)" in ui, "vertical-level mutation must revalidate generation")
    require("assign Floor/Level selection" in ui, "selection assignment must revalidate generation")
    require("EnsureBoundDrawingIsActive(\"save Floor/Level\")" in handler, "first-save mutation must revalidate generation")
    require("EnsureBoundDrawingIsActive(\"save Floor/Level\")" in bootstrap, "first-save bootstrap must revalidate generation")
    require("ProjectContextCoordinator.Forget(_document)" in bootstrap, "bootstrap generation drift must drop stale project context")
    require("ex.Message" not in handler, "first-save failure must not publish raw exception-derived text")
    require("ReportFailure(\"save Floor/Level\")" in handler, "first-save failure must use stable redacted boundary")
    failure = method(ui, "private void ReportFailure(string operation)")
    warning = method(ui, "private void ReportPostCommitWarning(string successMessage, string context)")
    require("EnsureBoundDocumentGeneration(operation)" in failure, "failure publication must be generation-safe")
    require("EnsureBoundDocumentGeneration(context)" in warning, "warning publication must be generation-safe")
    print("PASS: Floor/Level modeless operations and first-save lifecycle are fenced to one native database generation with fail-closed public status.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())