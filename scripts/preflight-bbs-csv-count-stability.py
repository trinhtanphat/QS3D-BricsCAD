from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "RebarCsvExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BbsCsvCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "ReadKnownCount(rows)",
    "ICollection<RebarScheduleRow>",
    "IReadOnlyCollection<RebarScheduleRow>",
    "ICollection nonGenericCollection",
    "ValidateKnownCount(rows, admittedCount);",
    "using (var enumerator = rows.GetEnumerator())",
    "var moved = enumerator.MoveNext();",
    "rowCount >= admittedCount.Value",
    "rowCount != admittedCount.Value",
    "conflicting row Count evidence",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing BBS CSV Count-stability source guard token: {token}")

if "foreach (var row in rows)" in source:
    raise SystemExit("BBS CSV must not regress to foreach because Current could be read before Count-integrity admission")

required_smoke = [
    "GrowthRejectsBeforeUnexpectedCurrentRead",
    "ShrinkRejectsBeforeSecondCurrentRead",
    "UnderYieldRejectsAgainstAdmittedCount",
    "ConflictingInterfacesRejectBeforeEnumeration",
    "OversizedKnownCountRejectsBeforeEnumeration",
    "StableKnownCountPreservesOutput",
    "PureStreamingSourceRemainsSupported",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing BBS CSV Count-stability smoke token: {token}")

print("PASS BBS CSV known-Count stability source guard")
