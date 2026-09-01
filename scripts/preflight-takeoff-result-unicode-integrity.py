from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Takeoff" / "TakeoffResult.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "TakeoffResultIntegritySmoke.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

require(source, "EnsureValidUnicodeScalarText(canonicalHandle, nameof(handle), \"Takeoff handle\");", "handle scalar admission")
require(source, "EnsureValidUnicodeScalarText(canonicalUnit, nameof(unit), \"Takeoff unit\");", "unit scalar admission")
require(source, "char.IsSurrogate(current)", "surrogate detection")
require(source, "char.IsHighSurrogate(current)", "high-surrogate pairing")
require(source, "char.IsLowSurrogate(value[index + 1])", "low-surrogate pairing")
require(source, '" must contain valid Unicode scalar text."', "stable malformed-Unicode diagnostic")
require(smoke, "TokenUnicodeScalarContractIsExplicit", "executable Unicode regression")
require(smoke, '"H-\\uD800-X"', "isolated high-surrogate handle regression")
require(smoke, '"H-\\uDC00-X"', "isolated low-surrogate handle regression")
require(smoke, '"m\\uD800"', "isolated high-surrogate unit regression")
require(smoke, '"m\\uDC00"', "isolated low-surrogate unit regression")
require(smoke, '"H-\\U0001F680"', "valid supplementary handle control")
require(smoke, '"m\\U0001D41A"', "valid supplementary unit control")

print("PASS: TakeoffResult rejects malformed UTF-16 on public handle/unit admission while preserving valid supplementary Unicode and existing token canonicality.")
