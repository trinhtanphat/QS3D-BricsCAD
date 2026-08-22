# Work claim — Quantity Rule dirty propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-dirty-propagation`
- Registered: `2026-08-12T13:44:00+07:00`
- Baseline main SHA: `75d6b6420132852e4f3da4b10d7182b2bb0de1d4`
- Priority: P1 — persisted rule-owned quantity/provenance changes must not leave a previously clean semantic element false-clean.

## Confirmed defect

`QuantityRuleEngine.SetProvenance(...)` mutated `ProjectElement.Properties["Rule:<output>"]` directly and then called only `TouchPersistenceState()`. `CleanupStaleOutputs(...)` similarly removed rule-owned quantities/provenance directly and only touched the persistence timestamp.

Concrete counterexamples:

1. An element is clean with quantity `NetArea=10` and provenance `Rule:NetArea=R1@1`. Re-applying rule `R1@2` whose expression still evaluates to `10` makes `SetQuantity(...)` a no-op, while provenance changes to `R1@2`; the element previously could remain `Dirty == None` even though persisted semantic state changed.
2. Removing a rule caused `CleanupStaleOutputs(...)` to retract the rule-owned quantity and provenance while a previously clean element could again remain false-clean.

## Implemented contract

- Rule provenance mutation now uses `ProjectElement.SetProperty(...)`, so actual provenance changes participate in normal Properties/Quantity dirty propagation while unchanged provenance remains a no-op.
- Stale provenance cleanup uses `ProjectElement.RemoveProperty(...)`; quantity-only removal explicitly marks `ElementDirtyFlags.Quantity` when needed.
- Canonical no-op reapplication with unchanged quantity and unchanged provenance remains clean.
- Rule evaluation order, active/stale output ownership, family-variable behavior, generated-geometry policy, and return counts are unchanged.

## Commits

- Claim: `4601711a34deca54f60dc4c61810d1e0b37b22f8`
- Source fix: `cf048d51a29c22d17fe8a095895940f1c2643334`
- Regression smoke: `a4abd6deb170c4332db72f659814b9852a6f764c`

## Validation

Read-back from `main` confirmed both dirty-aware source helpers and the focused smoke regression after concurrent repository writes. The regression covers unchanged-rule no-op, version-only provenance change with unchanged numeric output, and stale managed quantity/provenance cleanup. No GitHub Actions were dispatched and no BricsCAD runtime/build PASS is claimed.
