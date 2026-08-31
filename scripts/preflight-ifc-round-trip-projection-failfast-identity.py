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

    def find_after(token, after):
        return window.find(token, after + 1)

    hash_ifc = window.find("var ifcGlobalIds = new HashSet<string>(StringComparer.Ordinal);")
    hash_qs3d = find_after("var qs3dElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);", hash_ifc)
    loop = find_after("while (true)", hash_qs3d)
    rebound_token = "IfcRoundTripKnownCountContract.RequireStableDuringTraversal("
    rebound_before_move = find_after(rebound_token, loop)
    move = find_after("if (!enumerator.MoveNext())", rebound_before_move)
    rebound_after_move = find_after(rebound_token, move)
    known_count = find_after("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(", rebound_after_move)
    capacity = find_after("if (items.Count == MaxProjections)", known_count)
    current = find_after("var projection = enumerator.Current;", capacity)
    rebound_after_current = find_after(rebound_token, current)
    null_guard = find_after('if (projection == null) throw new ArgumentException("Projection collection cannot contain null entries.", nameof(projections));', rebound_after_current)
    ifc_identity = find_after("if (!ifcGlobalIds.Add(projection.IfcGlobalId))", null_guard)
    qs3d_identity = find_after("if (!qs3dElementIds.Add(projection.Qs3dElementId))", ifc_identity)
    append = find_after("items.Add(projection);", qs3d_identity)

    positions = (
        hash_ifc,
        hash_qs3d,
        loop,
        rebound_before_move,
        move,
        rebound_after_move,
        known_count,
        capacity,
        current,
        rebound_after_current,
        null_guard,
        ifc_identity,
        qs3d_identity,
        append,
    )
    if any(position < 0 for position in positions):
        failures.append("projection-set traversal is missing Count-stability/admission/fail-fast identity tokens: " + str(positions))
    elif list(positions) != sorted(positions):
        failures.append(
            "projection-set traversal must enforce loop -> Count rebound -> MoveNext -> Count rebound -> "
            "Count/capacity admission -> Current -> Count rebound -> semantic identity -> append: " + str(positions)
        )

    if window.count(rebound_token) != 3:
        failures.append("projection-set traversal must contain exactly three traversal-time Count rebound checks")
    if "while (enumerator.MoveNext())" in window:
        failures.append("projection-set traversal cannot hide the pre-MoveNext Count rebound inside the loop condition")
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