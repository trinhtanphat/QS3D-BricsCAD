# Agent work claim — ProjectElement property-key mutation persistability

Status: `COMPLETED`

Agent: `chatgpt-web-gpt56sol-property-key-persistability-20260814-1303`

Registered: `2026-08-14T13:03:00+07:00`

Completed: `2026-08-14T13:06:15+07:00`

Baseline `main`: `344f6466d985ba58336c41379289af03141a4cff`

Priority: `P1` Core persistence-integrity hardening.

## Confirmed defect

`ProjectElement.SetProperty(string, string)` is the canonical public property writer used by direct authoring and bulk edit flows. It rejected blank names and trimmed surrounding whitespace, then wrote the normalized key to the case-insensitive `Properties` map and propagated established dirty/staleness policy.

The writer did not reject embedded control characters in the normalized property key. A supported call such as `SetProperty("Fire\u0001Rating", "2h")` could therefore create semantic state that normal QSDB XML serialization cannot represent for XML-invalid controls. This is the property-key sibling of the completed `SetQuantity` writer hardening; it is not direct dictionary corruption.

## Implemented

- `SetProperty` now rejects `key.Any(char.IsControl)` immediately after surrounding-whitespace normalization and before value normalization, map mutation, geometry-output policy evaluation, dirty propagation or timestamp changes.
- Canonical/padded key behavior, value semantics, case-insensitive identity, same-value no-op behavior, geometry/quantity invalidation policy and `RemoveProperty` semantics remain unchanged.
- No direct `Properties` dictionary behavior was altered.

## Regression coverage

Added self-registering `ProjectElementPropertyKeyMutationPersistabilitySmoke` which pins:

1. padded input still writes one canonical `Comment` key;
2. canonical property writes still propagate established Properties + Quantity dirty flags;
3. an exact same-value write on a clean element remains dirty/timestamp neutral;
4. an embedded `U+0001` property-key control character throws `ArgumentException`;
5. the rejected mutation preserves existing properties/count, adds no malformed key, keeps the element clean and leaves `UpdatedUtc` unchanged.

## Commits on `main`

- Claim: `e5ec6085e429a649c5d8c99095af8597f305a2c6`
- Source: `90198b228f24c9f26fc1f0c57600f7750655ea57`
- Focused regression: `50e9bc3ae6bb9ad5c6ea35603d9402d6a080abd8`

## Verification

- Remote source commit diff for `90198b228f24c9f26fc1f0c57600f7750655ea57` was read back and confirms exactly one source hunk: the control-character guard after `var key = name.Trim()`; no other `ProjectElement.cs` content changed.
- Remote regression source was re-read at live head `59d4331d75e0ad91d779955c1c314b9d4d416630` and contains the canonical/padded/no-op/control-character atomicity assertions.
- GitHub compare confirms live head `59d4331d75e0ad91d779955c1c314b9d4d416630` is one unrelated claim-only commit ahead of regression SHA `50e9bc3ae6bb9ad5c6ea35603d9402d6a080abd8`, with the regression SHA as merge base; this lane remains on current lineage.
- GitHub Actions: `NOT_RUN` / not dispatched.
- .NET Core smoke execution: `NOT_RUN` because this environment has no `dotnet` executable.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claimed.
- Force push: not used.

## Explicit non-scope retained

- No property-value character/content restriction; free-text value semantics remain unchanged.
- No direct `Properties` dictionary API redesign or corruption-repair policy change.
- No `SetQuantity` changes.
- No Family property-map, selection inspector, bulk-edit orchestration, QSDB schema/migration, mapping, interchange, UI, or BricsCAD/native changes.
