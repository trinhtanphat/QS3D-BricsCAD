#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EXACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "MepExactClashCommands.cs"
INCREMENTAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "CoordinationIncrementalCommands.cs"

errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing required source file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def require(blob: str, token: str, label: str) -> None:
    if token not in blob:
        errors.append(f"missing {label}: {token}")


exact = read(EXACT)
incremental = read(INCREMENTAL)

for token, label in (
    ("private const int MaxRecognizedSolids = 500;", "standalone exact-scan 500-solid safety limit"),
    ("private const int MaxSparseRecognizedSolids = 5000;", "sparse exact recognized-solid safety limit"),
    ("var candidateLimit = allowedHandlePairKeys == null", "mode-specific candidate limit"),
    ("if (allowedHandlePairKeys == null)", "standalone full-pair branch"),
    ("candidateByHandle = new Dictionary<string, SolidCandidate>", "sparse handle index"),
    ("foreach (var pairKey in allowedHandlePairKeys.OrderBy", "deterministic sparse pair iteration"),
    ("TryParseHandlePairKey(pairKey", "canonical sparse pair-key validation"),
    ("EvaluateCandidatePair(left, right", "direct allowed-pair evaluation"),
    ("allowedHandlePairKeys.Count > MaxBroadPhasePairs", "sparse input pair budget"),
):
    require(exact, token, label)

if "if (candidates.Count > MaxRecognizedSolids)" in exact:
    errors.append("exact detector regressed to an unconditional 500-solid limit that also blocks sparse incremental callers")

for token, label in (
    ("private const int MaxLiveSolidComponents = 5000;", "bounded larger incremental live-solid scope"),
    ("private const int MaxAllowedNativeHandlePairs = 100000;", "incremental native-pair safety budget"),
    ("var handlePairKey = MepExactClashCommands.BuildHandlePairKey", "canonical allowed native pair identity"),
    ("allowedHandlePairs.Add(handlePairKey)", "deduplicated allowed native pair set"),
    ("allowedHandlePairs.Count > MaxAllowedNativeHandlePairs", "incremental pair-budget fail-closed boundary"),
    ("MepExactClashCommands.DetectExact(", "canonical exact detector reuse"),
    ("allowedHandlePairs);", "sparse allowed-pair handoff"),
):
    require(incremental, token, label)

if "private const int MaxLiveSolidComponents = 500;" in incremental:
    errors.append("incremental adapter regressed to the legacy 500-live-solid pre-spatial limit")

if errors:
    print("ERROR: issue-3537 incremental sparse exact source guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS: issue-3537 changed-only coordination uses bounded sparse native exact pairs without changing the standalone 500-solid exact-scan contract.")
print("NOTE: this is source/static evidence only; licensed V25/V26 correctness and performance remain LOCAL_ONLY/PENDING_LOCAL.")
