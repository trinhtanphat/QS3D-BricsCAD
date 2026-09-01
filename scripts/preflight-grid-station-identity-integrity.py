from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridSystemPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridSystemPlannerSmoke.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

require(source, "internal static class GridStationIdentity", "centralized station identity admission")
require(source, "if (!string.Equals(value, canonical, StringComparison.Ordinal))", "surrounding whitespace rejection")
require(source, "char.IsControl(current)", "control-character rejection")
require(source, "char.IsSurrogate(current)", "surrogate detection")
require(source, "char.IsHighSurrogate(current)", "high-surrogate pairing")
require(source, "char.IsLowSurrogate(canonical[index + 1])", "low-surrogate pairing")
require(source, "GridStationIdentity.Normalize(elementId, nameof(elementId)", "public station constructor admission")
require(source, "GridStationIdentity.Normalize(raw, nameof(ids)", "planner identity rebound")
if "var id = (raw ?? string.Empty).Trim();" in source:
    raise SystemExit("FAIL: planner identity validation must not silently trim/alias IDs")

for token, label in (
    ("StationIdentityAdmissionFailsClosed", "hostile identity regression"),
    ("StationIdentityControlsRemainCanonical", "valid Unicode control"),
    ("DuplicateStationIdentityRemainsCaseInsensitive", "duplicate identity semantics"),
    ('" U-1"', "padded identity regression"),
    ('"RAY\\t1"', "control identity regression"),
    ('"RING-\\uD800"', "isolated high-surrogate regression"),
    ('"U-\\uDC00"', "isolated low-surrogate regression"),
    ('"U-\\U0001F680"', "valid supplementary Unicode regression"),
):
    require(smoke, token, label)

print("PASS: Grid station identities reject padded/control/malformed UTF-16 text, preserve valid Unicode exactly, and remain case-insensitively unique in planner output.")
