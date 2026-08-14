# Work claim — ProjectElement quantity mutation persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-quantity-mutation-persistability-20260814-1253`
- Registered: `2026-08-14T12:53:00+07:00`
- Completed: `2026-08-14T13:00:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `b0fa998d78b8399bfbbf9561f405e90e3fc40052`
- Claim commit: `8ae774f229197539c283bf0f8446cfdc7ed15635`
- Claim reconciliation: `9133fabbec2866001c9499bd613911f572417099`
- Pre-write source blob: `49c228384f4b87b91451e1db4ddaba9b4c870790`
- Source: `61bc152b9c4d6bbd8e0a822edd119d53b44f6f07`
- Regression: `4da4caa528f9fb8614bd180a9e824612920699e2`

## Confirmed defect

`ProjectElement.SetQuantity(string, double)` is a public semantic writer. It rejected blank names, trimmed surrounding whitespace, finite-checked the value, canonicalized signed zero and then mutated the case-insensitive `Quantities` map, but it admitted embedded control characters in the normalized quantity name.

That allowed the supported writer API to create quantity state which downstream canonical boundaries reject: quantity coverage rejects control characters in quantity identities, and XML-invalid control characters cannot be represented by normal QSDB serialization. The supported writer could therefore leave an element in a state that could not participate in normal analysis and, for XML-invalid control characters, could not be persisted.

This is distinct from direct `Quantities` dictionary corruption: the completed fix is specifically at the supported `SetQuantity` mutation boundary.

## Completed change

- `ProjectElement.SetQuantity` now rejects any control character in the normalized quantity name before dictionary mutation or dirty-state propagation.
- Existing surrounding-whitespace normalization remains intact.
- Existing blank-name rejection, case-insensitive quantity identity, finite-value validation, signed-zero canonicalization, same-value no-op behavior and quantity dirty propagation remain unchanged.
- Direct `Quantities` dictionary mutation semantics were not broadened; existing persistence/analysis fail-closed guards continue to handle externally corrupted maps.

## Regression coverage

Added self-registering `ProjectElementQuantityMutationPersistabilitySmoke` which pins:

1. padded public input still normalizes to the canonical quantity key and writes normally;
2. canonical writes still set quantity dirty state;
3. an embedded `U+0001` quantity-name control character throws `ArgumentException`;
4. the rejected mutation preserves existing quantity count/value, adds no malformed key and leaves a clean element clean.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/ProjectElementQuantityMutationPersistabilitySmoke.cs`
- `docs/agent-work-claims/2026-08-14-1253-gpt56sol-quantity-mutation-persistability.md`

## Excluded scope

- no `SetProperty` or generic property-map changes;
- no quantity arithmetic/rule/report changes;
- no direct `Quantities` dictionary API redesign;
- no QSDB schema/migration changes;
- no mapping/IFC/export/native/V25 changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation

Remote GitHub diff for source commit `61bc152b9c4d6bbd8e0a822edd119d53b44f6f07` confirms exactly one source hunk: the control-character guard inside `SetQuantity`. Remote readback at regression SHA `4da4caa528f9fb8614bd180a9e824612920699e2` confirms both the source guard and focused smoke source. GitHub compare confirms `4da4caa528f9fb8614bd180a9e824612920699e2` is ahead of source commit `61bc152b9c4d6bbd8e0a822edd119d53b44f6f07` with that source SHA as merge base. A later live-main read at `1d0ab71be6242e50e4a2d0607bad7545af44fe65` is ahead of the regression SHA with the regression SHA as merge base, so both changes remain on current lineage despite concurrent agents.

Executable .NET/native validation was **not run** in this environment because there is no local GitHub checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Completion condition

Satisfied: claim-first reservation, corrected live reservation metadata, writer-boundary fix, focused regression source, live-main ancestry/readback verification and explicit validation boundary are all present on `main`.
