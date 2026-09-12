#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def method_block(source: str, signature: str, next_signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"missing method marker: {signature}")
    end = source.find(next_signature, start + len(signature))
    require(end > start, f"missing end marker after: {signature}")
    return source[start:end]


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")
    build = method_block(source, "public void BuildBeamStirrups()", "[CommandMethod(\"QS3DBEAMSTIRRUPHEALTH\"")
    health = method_block(source, "public void BeamStirrupHealth()", "private static List<ProjectElement> ResolveBeamTargets")
    finalize = method_block(
        source,
        "private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)",
        "private static IntPtr GetNativeDatabaseIdentity(Document document)",
    )

    require("ex.Message" not in build, "Beam Stirrup mutation must not expose raw exception messages")
    require("ex.Message" not in health, "Beam Stirrup health must not expose raw exception messages")
    require("ex.Message" not in finalize, "post-commit UI sync must not expose raw exception messages")
    require("OperationFailure" in source, "missing stable Beam Stirrup operation failure message")
    require("HealthFailure" in source, "missing stable Beam Stirrup health failure message")
    require("UiSyncWarning" in source, "missing stable post-commit UI sync warning")
    require(
        "Report(document, nativeDatabaseIdentity, OperationFailure);" in build,
        "mutation catch must report the stable operation failure through exact generation affinity",
    )
    require("ReportHealth(document, HealthFailure);" in health, "health catch must report the stable health failure")
    require("ex.GetType().Name" in finalize, "post-commit UI warning may expose exception type only")
    require(
        "TryWriteMessage(document, nativeDatabaseIdentity" in finalize and "UiSyncWarning" in finalize,
        "post-commit UI failure must preserve success text through generation-aware output",
    )
    require(
        "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)" in finalize,
        "post-commit UI recovery must reject stale native database generations",
    )

    for token in ("DocumentLock", "LockDocument(", "StartTransaction(", "Commit(", "Abort("):
        require(token not in finalize, f"UI-sync recovery must not perform native transaction/rollback work: {token}")

    print("PASS: Beam Stirrup failures remain redacted while generation-aware post-commit UI stays fail-soft")


if __name__ == "__main__":
    main()
