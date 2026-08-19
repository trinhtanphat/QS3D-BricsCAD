#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "GeneratedFoundationMeshHealthService.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("ERROR: " + message + " (missing: " + needle + ")")


def forbid(text: str, needle: str, message: str) -> None:
    if needle in text:
        raise SystemExit("ERROR: " + message + " (forbidden: " + needle + ")")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find('var handleText = item ?? string.Empty;')
    end = source.find('if (!string.Equals(handleText, handle, StringComparison.Ordinal))', start)
    if start < 0 or end < 0:
        raise SystemExit("ERROR: cannot locate Foundation mesh generated-handle validation block")

    validation = " ".join(source[start:end].split())
    require(
        validation,
        "ulong.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)",
        "Foundation mesh generated handles must use unsigned hexadecimal parsing so high-bit CAD handles remain valid",
    )
    require(
        validation,
        "parsed == 0",
        "zero must remain invalid even after switching generated handles to unsigned parsing",
    )
    forbid(
        validation,
        "!long.TryParse(handle, NumberStyles.HexNumber",
        "signed hexadecimal parsing rejects valid high-bit CAD handles such as 8000000000000000",
    )

    print("PASS: Foundation mesh health accepts positive unsigned high-bit generated CAD handles and still rejects zero.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
