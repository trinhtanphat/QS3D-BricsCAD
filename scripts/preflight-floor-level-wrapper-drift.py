#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml.cs"
SAFETY = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.WrapperDriftSafety.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"{signature} is missing.")
    brace = source.find("{", start)
    require(brace >= 0, f"{signature} body is missing.")
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError(f"{signature} body is unterminated.")


def main() -> int:
    window = WINDOW.read_text(encoding="utf-8")
    safety = SAFETY.read_text(encoding="utf-8")

    require("private readonly Document _document;" in window,
            "Level Picker must retain an explicit constructor wrapper whose drift is guarded.")
    require("DocumentBoundWindowLifetime.Attach(this, _document);" in window,
            "Level Picker must remain attached to the shared native-identity lifecycle coordinator.")

    required = [
        "partial class FloorLevelWindow",
        "_wrapperDriftNativeDatabaseIdentity",
        "CaptureWrapperDriftNativeIdentity()",
        "database.UnmanagedObject == IntPtr.Zero",
        "foreach (Document candidate in Application.DocumentManager)",
        "candidate.IsDisposed",
        "database.UnmanagedObject == _wrapperDriftNativeDatabaseIdentity",
        "ReferenceEquals(liveDocument, _document)",
        "OnContentRendered(EventArgs e)",
        "OnActivated(EventArgs e)",
        "OnPreviewMouseDown(MouseButtonEventArgs e)",
        "OnPreviewKeyDown(KeyEventArgs e)",
        "e.Handled = true",
        "PaletteCoordinator.SetStatus(WrapperDriftMessage)",
        "Close()",
        "mở lại QS3DLEVELS",
    ]
    missing = [needle for needle in required if needle not in safety]
    if missing:
        print("ERROR: Level Picker managed-wrapper drift guard is incomplete:")
        for needle in missing:
            print(" - missing:", needle)
        return 1

    capture = method_block(safety, "private void CaptureWrapperDriftNativeIdentity()")
    require("var database = _document.Database;" in capture,
            "Native database identity must be captured from the known-live constructor wrapper.")
    require(capture.index("database.UnmanagedObject == IntPtr.Zero") < capture.index("_wrapperDriftNativeDatabaseIdentity = database.UnmanagedObject"),
            "Native identity must be proven live before it is stored.")

    ensure = method_block(safety, "private bool EnsureManagedWrapperAffinity()")
    for marker in (
        "foreach (Document candidate in Application.DocumentManager)",
        "candidate.IsDisposed",
        "database.UnmanagedObject == _wrapperDriftNativeDatabaseIdentity",
        "ReferenceEquals(liveDocument, _document)",
        "CloseForManagedWrapperDrift()",
    ):
        require(marker in ensure, f"Managed-wrapper affinity check is missing: {marker}")
    require(ensure.index("database.UnmanagedObject == _wrapperDriftNativeDatabaseIdentity") < ensure.index("ReferenceEquals(liveDocument, _document)"),
            "Managed wrapper equality may only be trusted after stable native database identity is proven.")

    mouse = method_block(safety, "protected override void OnPreviewMouseDown(MouseButtonEventArgs e)")
    key = method_block(safety, "protected override void OnPreviewKeyDown(KeyEventArgs e)")
    for name, block in (("mouse", mouse), ("key", key)):
        require("if (!EnsureManagedWrapperAffinity())" in block and "e.Handled = true" in block,
                f"{name} input must be consumed when wrapper affinity cannot be proven.")
        require(block.index("e.Handled = true") < block.index("base.OnPreview"),
                f"{name} input must be consumed before base dispatch after wrapper drift.")

    forbidden = [
        "ProjectFloorService.",
        "ExistingProjectMutationContext.",
        "SemanticSelectionResolver.",
        "StartTransaction(",
        "TransactionManager",
        "Editor.WriteMessage",
        ".Message",
        ".StackTrace",
    ]
    present = [needle for needle in forbidden if needle in safety]
    if present:
        print("ERROR: wrapper-drift guard must remain host-affinity-only and redact host exception detail:")
        for needle in present:
            print(" - forbidden:", needle)
        return 1

    print("PASS: Level Picker captures native DB identity while live and closes before stale managed-wrapper UI input can reach project/CAD handlers.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
