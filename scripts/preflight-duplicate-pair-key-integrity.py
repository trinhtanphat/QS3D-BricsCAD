from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
source_path = root / "src" / "QS3D.Core" / "Coordination" / "DuplicateDetection.cs"
smoke_path = root / "tests" / "QS3D.Core.SmokeTests" / "DuplicatePairKeyIntegritySmoke.cs"

errors = []
source = source_path.read_text(encoding="utf-8")
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.exists() else ""

if 'public string PairKey => LeftElementId + "|" + RightElementId;' in source:
    errors.append("DuplicatePair.PairKey must not use raw delimiter concatenation.")
if 'EscapePairComponent(LeftElementId) + "|" + EscapePairComponent(RightElementId)' not in source:
    errors.append("DuplicatePair.PairKey must escape both exact element-id components before joining them.")
if 'return elementId.Replace("|", "||");' not in source:
    errors.append("Duplicate pair-key escaping must double embedded delimiters deterministically.")
if not smoke_path.exists():
    errors.append("Duplicate PairKey collision smoke is missing.")
else:
    required_smoke = (
        'Element("A", "PairOne", firstBox)',
        'Element("B|C", "PairOne", firstBox)',
        'Element("A|B", "PairTwo", secondBox)',
        'Element("C", "PairTwo", secondBox)',
        'keys.Distinct(StringComparer.Ordinal).Count() != result.Pairs.Count',
        'PairIdentityRemainsInputOrderIndependent()',
    )
    for token in required_smoke:
        if token not in smoke:
            errors.append("Duplicate PairKey regression smoke is missing required boundary evidence: " + token)

if errors:
    print("Duplicate PairKey integrity preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Duplicate PairKey integrity preflight passed")
