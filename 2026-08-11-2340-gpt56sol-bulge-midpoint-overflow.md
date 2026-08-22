# Work claim — bulge arc midpoint overflow integrity

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-bulge-midpoint-overflow-20260811-2340`
- Registered: `2026-08-11T23:40:00+07:00`
- Baseline main SHA: `4d4b6e96cc6dbdcd266d8c385b8a1b60cd643958`
- Released: `2026-08-11T23:43:00+07:00`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Released scope

The reserved `BulgeArcTessellator` stable-midpoint defect was independently implemented on `main` by another concurrent agent after this claim was registered. Current `main` already computes the midpoint from the finite validated deltas and includes a focused `BulgeMidpointOverflowSmoke` regression.

## Coordination outcome

- PR #534 was closed without merge to avoid duplicate source/test coverage.
- The feature-branch candidate `e0942a20c7ad5963cdd728a47dd8aeca1014b90f` was not merged.
- Remote `main` was re-read and confirmed to contain the same stable midpoint arithmetic plus explicit midpoint finite validation and regression coverage.
- No force-push and no GitHub Actions dispatch occurred.

## Original defect

The old tessellator computed the midpoint as `(start.X + end.X) * 0.5` / `(start.Y + end.Y) * 0.5`, allowing two same-sign finite coordinates with a finite chord to overflow the intermediate sum. The concurrent implementation on `main` resolves that defect, so this lane is intentionally released rather than duplicated.
