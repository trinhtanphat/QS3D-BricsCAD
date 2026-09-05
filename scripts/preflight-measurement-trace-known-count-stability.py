#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Measurement" / "MeasurementTrace.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "MeasurementTraceKnownCountStabilitySmoke.cs"
ENTRY = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestEntryPoint.cs"


def validate(source: str, smoke: str, entry: str) -> list[str]:
    errors: list[str] = []
    scopes = (
        ("SnapshotFacts", "facts", "internal static IReadOnlyList<MeasurementTraceFact> SnapshotFacts", "internal static IReadOnlyList<MeasurementTraceAdjustment> SnapshotAdjustments"),
        ("SnapshotAdjustments", "adjustments", "internal static IReadOnlyList<MeasurementTraceAdjustment> SnapshotAdjustments", "internal static IReadOnlyList<string> SnapshotMessages"),
        ("SnapshotMessages", "messages", "internal static IReadOnlyList<string> SnapshotMessages", "private static int? RequireSupportedCount"),
    )
    for name, label, start_token, end_token in scopes:
        start = source.find(start_token)
        end = source.find(end_token, start + 1)
        if start < 0 or end <= start:
            errors.append(name + " scope is missing")
            continue
        body = source[start:end]
        known = body.find("var knownCount = RequireSupportedCount(")
        acquire = body.find("using (var enumerator = source.GetEnumerator())", known)
        acquisition_rebound = body.find("RequireKnownCountStable(source, knownCount", acquire)
        loop = body.find("while (true)", acquisition_rebound)
        pre_move_rebound = body.find("RequireKnownCountStable(source, knownCount", loop)
        move = body.find("var hasNext = enumerator.MoveNext();", pre_move_rebound)
        post_move_rebound = body.find("RequireKnownCountStable(source, knownCount", move)
        current = body.find("var item = enumerator.Current;", post_move_rebound)
        post_current_rebound = body.find("RequireKnownCountStable(source, knownCount", current)
        if min(known, acquire, acquisition_rebound, loop, pre_move_rebound, move, post_move_rebound, current, post_current_rebound) < 0:
            errors.append(name + " is missing a required Count traversal boundary")
            continue
        if not (known < acquire < acquisition_rebound < loop < pre_move_rebound < move < post_move_rebound < current < post_current_rebound):
            errors.append(name + " must rebound admitted Count around GetEnumerator, MoveNext, and Current in fail-closed order")
        if "foreach (var item in source)" in body:
            errors.append(name + " must not use implicit foreach for caller-controlled counted traversal")
        if label not in body:
            errors.append(name + " lost its collection-specific diagnostic label")

    required_smoke = (
        "EnumeratorAcquisitionDriftFailsBeforeTraversal",
        "MoveNextDriftFailsBeforeCurrent",
        "CurrentDriftFailsBeforeFurtherTraversal",
        "AdjustmentAndMessageSurfacesUseTheSameBoundary",
        "StableCountedAndStreamingControlsRemainAccepted",
        "Equal(0, source.MoveNextCalls",
        "Equal(0, source.CurrentCalls",
        "Equal(0, adjustments.MoveNextCalls",
        "Equal(0, warnings.MoveNextCalls",
        "DriftStage.GetEnumerator",
        "DriftStage.MoveNext",
        "DriftStage.Current",
    )
    for token in required_smoke:
        if token not in smoke:
            errors.append("MeasurementTrace Count stability smoke missing token: " + token)

    registration = "MeasurementTraceKnownCountStabilitySmoke.Run();"
    if registration not in entry:
        errors.append("MeasurementTrace Count stability smoke is not registered in the canonical smoke entrypoint")
    return errors


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
entry = ENTRY.read_text(encoding="utf-8")
errors = validate(source, smoke, entry)
if errors:
    raise SystemExit("MeasurementTrace known-Count stability failed: " + "; ".join(errors))

needle = "            using (var enumerator = source.GetEnumerator())\n            {\n                RequireKnownCountStable(source, knownCount, parameterName, \"facts\");"
if needle not in source:
    raise SystemExit("MeasurementTrace Count stability regression probe target is missing")
mutated = source.replace(
    needle,
    "            using (var enumerator = source.GetEnumerator())\n            {",
    1,
)
if not validate(mutated, smoke, entry):
    raise SystemExit("MeasurementTrace Count stability negative mutation did not fail closed")

print("PASS: MeasurementTrace facts, adjustments, and messages rebound caller-known Count around enumerator acquisition, MoveNext, and Current before item acceptance.")
