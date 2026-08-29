#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "IfcRoundTripProjection.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "IfcRoundTripProjectionFailFastIdentitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

failures = []
start = source.find("public sealed class IfcRoundTripProjectionSet")
end = source.find("public static class IfcRoundTripProjectionComparer", start)
if start < 0 or end < 0:
    failures.append("missing IfcRoundTripProjectionSet source window")
else:
    window = source[start:end]
    required_order = (
        "var ifcGlobalIds = new HashSet<string>(StringComparer.Ordinal);",
        "var qs3dElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
        "while (enumerator.MoveNext())",
        "IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(",
        "if (items.Count == MaxProjections)",
        "var projection = enumerator.Current;",
        'if (projection == null) throw new ArgumentException("Projection collection cannot contain null entries.", nameof(projections));',
        "if (!ifcGlobalIds.Add(projection.IfcGlobalId))",
        "if (!qs3dElementIds.Add(projection.Qs3dElementId))",
        "items.Add(projection);",
    )
    positions = [window.find(token) for token in required_order]
    if any(position < 0 for position in positions):
        failures.append("projection-set traversal is missing admission/fail-fast identity tokens: " + str(positions))
    elif positions != sorted(positions):
        failures.append("projection-set traversal must enforce MoveNext -> Count/capacity admission -> Current -> semantic identity -> append: " + str(positions))

    if "for (var index = 0; index < items.Count; index++)" in window:
        failures.append("projection-set semantic validation regressed to post-traversal scan")

required_smoke = (
    "RejectsDuplicateIfcIdentityBeforeTailEnumeration",
    "RejectsDuplicateQs3dIdentityBeforeTailEnumeration",
    "RejectsNullProjectionBeforeTailEnumeration",
    "Caller tail was enumerated after a decisive projection-set semantic error.",
    "Equal(2, source.MoveNextCalls",
    "Equal(2, source.CurrentReads",
    "[ModuleInitializer]",
)
for token in required_smoke:
    if token not in smoke:
        failures.append("smoke missing token: " + token)

if failures:
    for failure in failures:
        print("FAIL IFC round-trip projection fail-fast identity: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("PASS IFC round-trip projection fail-fast identity")
