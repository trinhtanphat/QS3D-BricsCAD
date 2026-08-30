#!/usr/bin/env python3
from pathlib import Path

source = Path("src/QS3D.Core/Audit/AuditTrail.cs").read_text(encoding="utf-8")

for forbidden in (
    "foreach (var item in _events)",
    "foreach (var existing in _events)",
    "while (enumerator.MoveNext())",
):
    if forbidden in source:
        raise SystemExit(f"Audit history traversal must keep explicit Count-safe enumeration: {forbidden}")

required = (
    "using (var enumerator = _events.GetEnumerator())",
    "while (true)",
    "RequireStableHistoryCount(storedCount);",
    "if (!enumerator.MoveNext())",
    "RequireCanReadCurrent(storedCount, observed);",
    "var item = enumerator.Current;",
    "var existing = enumerator.Current;",
    "RequireObservedHistoryCount(storedCount, observed);",
)
for token in required:
    if token not in source:
        raise SystemExit(f"Audit history count-integrity guard missing required source token: {token}")


def require_order(block, label, current_token):
    loop = block.index("while (true)")
    pre = block.index("RequireStableHistoryCount(storedCount);", loop)
    move = block.index("if (!enumerator.MoveNext())", pre)
    terminal = block.index("RequireStableHistoryCount(storedCount);", move + 1)
    brk = block.index("break;", terminal)
    post = block.index("RequireStableHistoryCount(storedCount);", brk + 1)
    gate = block.index("RequireCanReadCurrent(storedCount, observed);", post)
    current = block.index(current_token, gate)
    if not (loop < pre < move < terminal < brk < post < gate < current):
        raise SystemExit(label + " must preserve Count -> MoveNext -> Count -> capacity -> Current ordering.")


events_start = source.index("public IReadOnlyList<AuditEvent> Events")
events_end = source.index("public static AuditTrail ForProject", events_start)
require_order(source[events_start:events_end], "AuditTrail.Events", "var item = enumerator.Current;")

validate_start = source.index("private int ValidateExistingHistory")
validate_end = source.index("private int RequireSupportedHistoryCount", validate_start)
require_order(source[validate_start:validate_end], "AuditTrail.ValidateExistingHistory", "var existing = enumerator.Current;")

print("PASS audit history transient known-Count traversal guard")
