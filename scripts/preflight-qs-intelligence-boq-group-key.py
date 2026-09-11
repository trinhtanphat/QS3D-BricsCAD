from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
engine = (root / 'src/QS3D.Core/Intelligence/QsIntelligenceEngine.cs').read_text(encoding='utf-8-sig')
smoke = (root / 'tests/QS3D.Core.SmokeTests/QsIntelligenceSmoke.cs').read_text(encoding='utf-8-sig')

errors = []
if 'new Dictionary<GroupKey, Group>()' not in engine:
    errors.append('BOQ grouping must use typed GroupKey identity')
if 'classification + "|" + record.WbsCode' in engine:
    errors.append('delimiter-concatenated BOQ identity remains')
for token in ('StringComparer.OrdinalIgnoreCase.Equals(ClassificationCode', 'StringComparer.OrdinalIgnoreCase.GetHashCode(ClassificationCode)', 'BoqSuggestionCompositeKeyCollision'):
    if token not in engine and token not in smoke:
        errors.append('missing typed-key regression token: ' + token)
if '"CLS|WBS"' not in smoke or '"WBS|A"' not in smoke:
    errors.append('smoke does not preserve a delimiter-collision pair')

if errors:
    print('QS intelligence BOQ group-key preflight FAILED')
    for error in errors:
        print(' - ' + error)
    sys.exit(1)
print('QS intelligence BOQ group-key preflight PASS')
