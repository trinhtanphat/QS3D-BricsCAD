# Work claim — Quantity Rule dirty propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-dirty-propagation`
- Registered: `2026-08-12T13:44:00+07:00`
- Baseline main SHA: `75d6b6420132852e4f3da4b10d7182b2bb0de1d4`
- Priority: P1 — persisted rule-owned quantity/provenance changes must not leave a previously clean semantic element false-clean.

## Confirmed defect

`QuantityRuleEngine.SetProvenance(...)` mutates `ProjectElement.Properties["Rule:<output>"]` directly and then calls only `TouchPersistenceState()`. `CleanupStaleOutputs(...)` similarly removes rule-owned quantities/provenance directly and only touches the persistence timestamp.

Concrete counterexamples:

1. An element is clean with quantity `NetArea=10` and provenance `Rule:NetArea=R1@1`. Re-applying rule `R1@2` whose expression still evaluates to `10` makes `SetQuantity(...)` a no-op, while provenance changes to `R1@2`; the element can remain `Dirty == None` even though persisted semantic state changed.
2. Removing a rule causes `CleanupStaleOutputs(...)` to retract the rule-owned quantity and provenance, but a previously clean element can again remain false-clean.

This scope does not alter rule evaluation, formula dependency ordering, QuantityRule identity/canonicality, or unrelated quantity services.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRuleEngine.cs`, limited to dirty propagation for provenance/stale managed-output mutation
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Any actual rule provenance change marks the element dirty for persisted Properties state; quantity result no-op must not hide that change.
- Any actual stale managed quantity/provenance cleanup marks the element dirty for the affected persisted state.
- Canonical no-op reapplication with unchanged quantity and unchanged provenance remains a no-op and does not invent dirty state.
- Preserve rule evaluation order, active/stale output ownership, family-variable behavior, and return counts.

## Validation boundary

No GitHub Actions or BricsCAD runtime/build PASS will be claimed unless actually observed.
