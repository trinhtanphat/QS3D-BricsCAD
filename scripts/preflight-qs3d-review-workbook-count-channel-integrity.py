#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Exporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Qs3dReviewWorkbookCountNoOverreadSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using System.Collections;",
    "source as ICollection<T>",
    "source as ICollection",
    "Count channels disagree at admission",
    "genericCollection.Count != expectedCount",
    "nonGenericCollection.Count != expectedCount",
    "var value = enumerator.Current;\n                    RequireStableCount();\n                    result.Add(value);",
]
required_smoke = [
    "AdmissionConflictingGenericCountFailsBeforeTraversal();",
    "CurrentInducedGenericCountDriftFailsBeforeRetention();",
    "StableMultiInterfaceSnapshotRemainsAccepted();",
    "IReadOnlyList<T>, ICollection<T>, ICollection",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("QS3D Review Count-channel integrity preflight missing contract token(s): " + ", ".join(repr(x) for x in missing))

current = source.index("var value = enumerator.Current;")
rebound = source.index("RequireStableCount();", current)
retain = source.index("result.Add(value);", current)
if not current < rebound < retain:
    raise SystemExit("QS3D Review snapshot must rebind all admitted Count channels after Current and before retention.")

print("PASS QS3D Review workbook multi-interface Count-channel integrity")
