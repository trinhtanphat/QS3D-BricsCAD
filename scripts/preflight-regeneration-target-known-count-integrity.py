#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationEngine.cs"


def require_order(block, tokens):
    cursor = -1
    for token in tokens:
        cursor = block.find(token, cursor + 1)
        if cursor < 0:
            print("ERROR: regeneration target Count contract missing ordered token:", token)
            return False
    return True


def main():
    text = SOURCE.read_text(encoding="utf-8")
    start = text.index("private static HashSet<string> CanonicalTargetIds")
    end = text.index("private static void RequireStableKnownTargetIdCounts", start)
    block = text[start:end]
    rebound = "RequireStableKnownTargetIdCounts(elementIds, knownCount);"
    tokens = [
        "var knownCount = ValidateKnownTargetIdCounts(elementIds);",
        rebound,
        "enumerator.MoveNext()",
        rebound,
        "if (knownCount.HasValue && index >= knownCount.Value)",
        "var value = enumerator.Current;",
        rebound,
        "var raw = value ?? string.Empty;",
        "string.IsNullOrWhiteSpace(raw)",
        "result.Contains(raw)",
        "result.Count >= maxCount",
        "result.Add(raw);",
        "knownCount.Value != index",
        rebound,
        "return result;",
    ]
    if block.count(rebound) < 4 or not require_order(block, tokens):
        print("ERROR: targeted regeneration must rebind admitted Count before/after MoveNext, immediately after Current before target validation/retention, and before publication.")
        return 1

    helper = text[text.index("private static void RequireStableKnownTargetIdCounts"):text.index("private int RegenerateTransactional")]
    required_helper = [
        "ICollection<string>",
        "IReadOnlyCollection<string>",
        "System.Collections.ICollection",
        "invalid negative known count",
        "conflicting known counts",
        "target id count changed during enumeration",
    ]
    missing = [token for token in required_helper if token not in helper]
    if missing:
        print("ERROR: regeneration target known-Count helper contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    print("PASS: regeneration target ids pin all admitted Count channels through Current-before-acceptance and final publication.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())