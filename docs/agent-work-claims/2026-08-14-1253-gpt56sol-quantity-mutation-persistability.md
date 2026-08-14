# Work claim — ProjectElement quantity mutation persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-quantity-mutation-persistability-20260814-1253`
- Registered: `2026-08-14T12:53:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `b0fa998d78b8399bfbbf9561f405e90e3fc40052`
- Claim commit: `8ae774f229197539c283bf0f8446cfdc7ed15635`
- Pre-write source blob: `49c228384f4b87b91451e1db4ddaba9b4c870790`

## Confirmed defect

`ProjectElement.SetQuantity(string, double)` is a public semantic writer. It rejects blank names, trims surrounding whitespace, finite-checks the value, canonicalizes signed zero and then mutates the case-insensitive `Quantities` map. It does not reject embedded control characters in the quantity name.

That allows the supported writer API to create quantity state which downstream canonical boundaries reject: quantity coverage explicitly rejects control characters in quantity identities, and QSDB serialization rejects XML-invalid control characters. A successful writer call can therefore leave an element in a state that cannot participate in normal analysis and, for XML-invalid control characters, cannot be persisted.

This is distinct from direct `Quantities` dictionary corruption: the defect is specifically that the supported `SetQuantity` mutation boundary admits the invalid state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs`
- new `tests/QS3D.Core.SmokeTests/ProjectElementQuantityMutationPersistabilitySmoke.cs`
- `docs/agent-work-claims/2026-08-14-1253-gpt56sol-quantity-mutation-persistability.md`

## Intended change

Reject control characters in the normalized quantity name before any dictionary mutation or dirty-state update. Preserve all established `SetQuantity` semantics: blank rejection, surrounding-whitespace normalization, case-insensitive identity, finite-value validation, signed-zero canonicalization, same-value no-op behavior and quantity dirty propagation.

Do not change direct dictionary mutation semantics; existing persistence/analysis fail-closed guards remain responsible for externally corrupted maps.

## Regression plan

Add a focused self-registering Core smoke which proves:

1. canonical quantity names still write normally;
2. padded public input still normalizes to the canonical key;
3. an embedded control-character quantity name throws before mutation;
4. the failed mutation does not add a key or set `ElementDirtyFlags.Quantity` after the element was marked clean.

## Excluded scope

- no `SetProperty` or generic property-map changes;
- no quantity arithmetic/rule/report changes;
- no direct `Quantities` dictionary API redesign;
- no QSDB schema/migration changes;
- no mapping/IFC/export/native/V25 changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

This environment has GitHub connector read/write but no local GitHub checkout/.NET/native runner. Source/regression commits may be published only after live-main reconciliation and remote readback. Executable PASS will not be claimed unless independently evidenced on the resulting SHA.

## Completion condition

Claim-only reservation is visible on remote `main`; source + focused regression are reconciled against current `main`; remote readback confirms the fix and regression source; then this claim is closed `COMPLETED` with explicit validation limitations.
