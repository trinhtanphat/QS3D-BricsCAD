#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs"
RESULT = ROOT / "src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripEvidenceResultCurrentCountSmoke.cs"
errors = []

for path, label in ((EVIDENCE, "quantity evidence"), (RESULT, "exchange result"), (SMOKE, "regression smoke")):
    if not path.is_file():
        errors.append("missing IFC evidence/result Current Count file: " + label)

def verify(source, source_name, collection_name, current_token, acceptance_token):
    stable = "IfcRoundTripKnownCountContract.RequireStableDuringTraversal("
    loop = source.find("while (true)")
    before_move = source.find(stable, loop)
    move = source.find("enumerator.MoveNext()", before_move)
    after_move = source.find(stable, before_move + len(stable))
    guard = source.find("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(", after_move)
    current = source.find(current_token, guard)
    after_current = source.find(stable, current)
    acceptance = source.find(acceptance_token, after_current)
    post = source.find("IfcRoundTripKnownCountContract.RequireStableAfterTraversal(", acceptance)
    if min(loop, before_move, move, after_move, guard, current, after_current, acceptance, post) < 0 or not (
        loop < before_move < move < after_move < guard < current < after_current < acceptance < post
    ):
        errors.append(source_name + " must rebind Count before/after MoveNext and immediately after Current before item acceptance, then rebind after traversal")

if EVIDENCE.is_file():
    evidence = EVIDENCE.read_text(encoding="utf-8")
    verify(
        evidence,
        "IFC quantity evidence",
        "evidence",
        "var candidate = enumerator.Current;",
        "if (candidate == null)")
    for token in ("MaxCandidates", "candidates.Add(candidate);", "candidates.Sort(IfcRoundTripQuantityEvidenceComparer.Instance)"):
        if token not in evidence:
            errors.append("IFC quantity evidence lost historical invariant: " + token)

if RESULT.is_file():
    result = RESULT.read_text(encoding="utf-8")
    verify(
        result,
        "IFC exchange result",
        "results",
        "var item = enumerator.Current;",
        "observedResultCount++;")
    for token in ("MaxResultsPerCollection", "DuplicateExternalIdentityDetail", "byExternalIdentity.Add(item.ExternalObjectId, item)"):
        if token not in result:
            errors.append("IFC exchange result lost historical invariant: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "EvidenceMoveNextTransientDriftFailsBeforeCurrent();",
        "EvidenceCurrentDriftFailsBeforeNullValidation();",
        "ResultMoveNextTransientDriftFailsBeforeCurrent();",
        "ResultCurrentDriftFailsBeforeNullValidation();",
        "StableCountedInputsRemainAccepted();",
        "PureStreamingInputsRemainAccepted();",
        "Equal(0, input.CurrentReads",
        "Equal(1, input.CurrentReads",
        "DriftPoint.MoveNext",
        "DriftPoint.Current",
        '"IFC round-trip quantity evidence source Count changed during traversal."',
        '"IFC exchange result source Count changed during traversal."',
    ):
        if token not in smoke:
            errors.append("IFC evidence/result Current Count smoke missing assertion/control: " + token)

print("QS3D IFC evidence/result Current Count-stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: IFC quantity-evidence and exchange-result builders rebind admitted Count around advancement and immediately after Current before item acceptance/publication.")
