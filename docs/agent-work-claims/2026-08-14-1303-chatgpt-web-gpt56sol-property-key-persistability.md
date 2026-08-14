# Agent work claim — ProjectElement property-key mutation persistability

Status: `ACTIVE`

Agent: `chatgpt-web-gpt56sol-property-key-persistability-20260814-1303`

Registered: `2026-08-14T13:03:00+07:00`

Baseline `main`: `344f6466d985ba58336c41379289af03141a4cff`

Priority: `P1` Core persistence-integrity hardening.

## Confirmed defect

`ProjectElement.SetProperty(string, string)` is the canonical public property writer used by direct authoring and bulk edit flows. It rejects blank names and trims surrounding whitespace, then writes the normalized key to the case-insensitive `Properties` map and propagates established dirty/staleness policy.

The writer does not reject embedded control characters in the normalized property key. A supported call such as `SetProperty("Fire\u0001Rating", "2h")` therefore creates semantic state that normal QSDB XML serialization cannot represent for XML-invalid controls. This is the property-key sibling of the completed `SetQuantity` writer hardening; it is not direct dictionary corruption.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` only `SetProperty` property-key validation.
- new focused `tests/QS3D.Core.SmokeTests/ProjectElementPropertyKeyMutationPersistabilitySmoke.cs`.
- this claim file.

## Acceptance

1. Canonical property keys continue to write normally and preserve existing dirty/staleness behavior.
2. Surrounding-whitespace input continues to normalize to the canonical key.
3. An embedded control character in the normalized property key throws `ArgumentException` before dictionary mutation or dirty/timestamp propagation.
4. A rejected property-key mutation preserves existing properties and leaves a clean element clean.
5. Existing empty-string/null value semantics, case-insensitive identity, same-value no-op behavior, geometry-output invalidation policy, and `RemoveProperty` behavior remain unchanged.

## Explicit non-scope

- No property-value character/content restriction; free-text value semantics remain unchanged.
- No direct `Properties` dictionary API redesign or corruption-repair policy change.
- No `SetQuantity` changes; quantity writer lane is already `COMPLETED`.
- No Family property-map, selection inspector, bulk-edit orchestration, QSDB schema/migration, mapping, interchange, UI, or BricsCAD/native changes.

## Evidence / history

- Live `ProjectElement.cs` at the baseline still has `SetProperty` trim/write semantics with no control-character guard, while adjacent `SetQuantity` now rejects `key.Any(char.IsControl)` after completed quantity persistability hardening.
- `QsdbProjectStore` persists element property keys into XML attribute names/values through the property map and validates XML character representability before save publication; XML-invalid control characters therefore make the supported writer-created state unpersistable.
- Existing bulk/direct-authoring commits intentionally route canonical writes through `ProjectElement.SetProperty`, making this writer the correct enforcement boundary.
- No current commit-history claim was found for `SetProperty` property-key control-character/persistability hardening.

## Validation plan

- Add focused self-registering Core smoke source for canonical/padded writes, control-character rejection, and atomicity on a clean element.
- Re-read source/test from remote current `main` after writes and verify lineage.
- GitHub Actions: `NOT_RUN` / do not dispatch.
- .NET Core smoke execution: `NOT_RUN` because this environment has no `dotnet` executable.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claim.

## Completion condition

Claim-only reservation is visible on remote `main`; source + focused regression are reconciled against current `main`; remote readback verifies the final writer guard and regression source; then this claim is closed `COMPLETED` with exact on-main commit SHAs and validation limitations.
