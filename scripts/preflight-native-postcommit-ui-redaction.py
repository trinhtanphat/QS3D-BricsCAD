#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadPostCommitUi.cs"


def main() -> int:
    text = HELPER.read_text(encoding="utf-8")
    required = (
        "public static void TryRegen(Document document, string operation)",
        "document.Editor.Regen();",
        "catch (Exception)",
        '"\\nQS3D " + operation + " đã commit; viewport could not refresh."',
        "Post-commit diagnostics are optional and must never escape.",
    )
    for token in required:
        if token not in text:
            raise AssertionError("missing post-commit UI token: " + token)
    for forbidden in ("ex.Message", ".Message", "throw;", "throw new"):
        if forbidden in text:
            raise AssertionError("post-commit UI must not contain: " + forbidden)
    if text.count("catch") < 2:
        raise AssertionError("Regen and warning-message failures must remain independently contained")
    print("PASS: shared native post-commit Regen diagnostics are stable-redacted and non-escaping.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
