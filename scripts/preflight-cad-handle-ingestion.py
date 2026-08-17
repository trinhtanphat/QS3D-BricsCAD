#!/usr/bin/env python3
"""Fail closed if CadHandleService loses bounded/canonical handle ingestion."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "CadHandleService.cs"
MAX_COUNT = "private const int MaxHandleInputCount = 10000;"


def require(source: str, token: str, message: str) -> None:
    if token not in source:
        raise SystemExit(f"CadHandleService ingestion guard failed: {message}")


def require_before(source: str, first: str, second: str, message: str) -> None:
    first_index = source.find(first)
    second_index = source.find(second)
    if first_index < 0 or second_index < 0 or first_index >= second_index:
        raise SystemExit(f"CadHandleService ingestion guard failed: {message}")


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")

    require(source, MAX_COUNT, "10,000 raw-handle bound is missing")
    require(source, "ValidateKnownCount(handles);", "known-count fast rejection is missing")
    require(source, "handles is ICollection<string> collection", "ICollection known-count guard is missing")
    require(source, "handles is IReadOnlyCollection<string> readOnlyCollection", "IReadOnlyCollection known-count guard is missing")
    require(source, "rawCount++;", "streaming raw-item counter is missing")
    require(source, "if (rawCount > MaxHandleInputCount)", "streaming limit+1 rejection is missing")
    require_before(
        source,
        "rawCount++;",
        "var normalized = NormalizeHexHandle(text);",
        "streaming bound must be checked before normalization/host lookup",
    )
    require_before(
        source,
        "ValidateKnownCount(handles);",
        "var candidates = new List<ObjectId>();",
        "known-count oversize input must reject before candidate materialization/host lookup",
    )

    require(source, "if (string.IsNullOrWhiteSpace(text)) return null;", "blank handle skip contract is missing")
    require(
        source,
        "if (!string.Equals(text, text!.Trim(), StringComparison.Ordinal)) return null;",
        "padded nonblank handle rejection is missing",
    )
    if "var normalized = (text ?? string.Empty).Trim();" in source:
        raise SystemExit("CadHandleService ingestion guard failed: padded handle tokens are being canonicalized by Trim")

    require(source, 'StartsWith("0x", StringComparison.OrdinalIgnoreCase)', "canonical 0x prefix support is missing")
    require(source, "StringComparer.OrdinalIgnoreCase", "case-insensitive canonical dedupe is missing")
    require(source, "document.Database.GetObjectId(false, new Handle(value), 0)", "live database handle resolution changed unexpectedly")
    require(source, "IsRecoverableDiagnosticFailure", "recoverable host-failure boundary is missing")

    print("CadHandleService bounded/canonical ingestion guard passed.")


if __name__ == "__main__":
    main()
