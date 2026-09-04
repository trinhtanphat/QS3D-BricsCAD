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
    finalize = method_block(source, "private static void FinalizeUi(Document document, string message)", "private static void Report(Document document, string message)")

    require("ex.Message" not in build, "Beam Stirrup mutation must not expose raw exception messages")
    require("ex.Message" not in health, "Beam Stirrup health must not expose raw exception messages")
    require("ex.Message" not in finalize, "post-commit UI sync must not expose raw exception messages")

    require("OperationFailure" in source, "missing stable Beam Stirrup operation failure message")
    require("HealthFailure" in source, "missing stable Beam Stirrup health failure message")
    require("UiSyncWarning" in source, "missing stable post-commit UI sync warning")

    require("Report(document, OperationFailure);" in build, "mutation catch must report the stable operation failure")
    require("Report(document, HealthFailure);" in health, "health catch must report the stable health failure")
    require(
        'TryWriteMessage(document, "\\nQS3D " + message + " " + UiSyncWarning);' in finalize,
        "post-commit UI failure must preserve the success message and append the stable warning",
    )

    forbidden = ("DocumentLock", "LockDocument(", "StartTransaction(", "Commit(", "Abort(")
    for token in forbidden:
        require(token not in finalize, f"UI-sync recovery must not perform native transaction/rollback work: {token}")

    print("PASS: Beam Stirrup command failures are redacted and post-commit UI sync remains fail-soft")


if __name__ == "__main__":
    main()
