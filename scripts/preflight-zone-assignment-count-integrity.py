from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectZoneService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ZoneAssignmentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "zone-assignment-count-integrity.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(source, "using (var enumerator = elements.GetEnumerator())", "explicit assignment-target enumerator")
    require(source, "while (enumerator.MoveNext())", "MoveNext-controlled traversal")
    require(source, "knownTargetCount.HasValue && observedEntries > knownTargetCount.Value", "known-Count overrun gate before Current")
    require(source, "var element = enumerator.Current;", "Current read after count gates")
    require(source, "var currentKnownTargetCount = SnapshotAssignmentTargetKnownCount(elements);", "post-traversal Count rebinding")
    require(source, "knownTargetCount != currentKnownTargetCount", "Count drift comparison")
    require(source, "known count changed during enumeration", "Count drift diagnostic")
    require(smoke, "KnownCountOverrunRejectsBeforeCurrentRead", "N+1 Current no-overread regression")
    require(smoke, "ExactTraversalCountDriftFailsClosed", "post-traversal Count drift regression")
    require(smoke, "StableCountedInputAssigns", "stable counted control")
    require(smoke, "StreamingInputRemainsAccepted", "streaming control")
    require(smoke, "StreamingHardCapRejectsBeforeCurrentRead", "streaming hard-cap Current no-overread regression")
    require(runbook, "MoveNext", "MoveNext/Current boundary documentation")
    require(runbook, "post-traversal", "Count rebind documentation")

    print("PASS Zone assignment Count integrity preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())