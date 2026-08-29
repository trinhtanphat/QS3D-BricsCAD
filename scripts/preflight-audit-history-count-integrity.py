#!/usr/bin/env python3
from pathlib import Path

source = Path("src/QS3D.Core/Audit/AuditTrail.cs").read_text(encoding="utf-8")

for forbidden in (
    "foreach (var item in _events)",
    "foreach (var existing in _events)",
):
    if forbidden in source:
        raise SystemExit(f"Audit history traversal must not use Current-coupled foreach: {forbidden}")

required = (
    "using (var enumerator = _events.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "RequireCanReadCurrent(storedCount, observed);",
    "var item = enumerator.Current;",
    "var existing = enumerator.Current;",
    "RequireStableHistoryCount(storedCount);",
)
for token in required:
    if token not in source:
        raise SystemExit(f"Audit history count-integrity guard missing required source token: {token}")

move = source.find("while (enumerator.MoveNext())")
gate = source.find("RequireCanReadCurrent(storedCount, observed);", move)
current = source.find("enumerator.Current", move)
if not (move >= 0 and gate > move and current > gate):
    raise SystemExit("Audit history traversal must gate admitted Count after MoveNext and before Current.")

print("PASS audit history known-Count Current-safe traversal guard")
