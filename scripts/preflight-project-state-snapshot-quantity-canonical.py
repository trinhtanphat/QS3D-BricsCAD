#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectStateSnapshot.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("private static void RequireCanonicalQuantities(ProjectElement element)")
end = text.index("private static bool HasControlCharacter", start)
block = text[start:end]

canonical = "var canonicalName = quantity.Key.Trim();"
reject = "if (!string.Equals(canonicalName, quantity.Key, StringComparison.Ordinal))"
negative_zero = "quantity.Value == 0d && BitConverter.DoubleToInt64Bits(quantity.Value) < 0"
collision = "if (!canonicalNames.Add(canonicalName))"
materialize = "foreach (var quantity in source.Quantities) target.SetQuantity(quantity.Key, quantity.Value);"

if canonical not in block:
    fail("quantity snapshot validation must derive the canonical trimmed identity")
if reject not in block:
    fail("quantity snapshot validation must reject stored keys whose spelling is not already canonical")
if block.index(reject) < block.index(canonical):
    fail("quantity canonical-spelling rejection must occur after deriving the canonical identity")
if negative_zero not in block:
    fail("quantity snapshot validation must reject negative zero before SetQuantity can silently canonicalize it")
if collision not in block or block.index(collision) < block.index(reject):
    fail("canonical collision detection must remain after canonical-spelling rejection")
if materialize not in text:
    fail("snapshot materialization must still flow quantity keys through ProjectElement.SetQuantity")

print("PASS: project snapshots reject non-canonical quantity keys and negative zero before materialization can silently normalize them")
