# Work claim — Generated logical owner collision-free pair identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-generated-logical-owner-key-collision-20260812`
- Registered: `2026-08-12T11:28:00+07:00`
- Completed: `2026-08-12T11:32:00+07:00`
- Baseline main SHA: `df5f2fba7abfe20a5800d971cdd73ff125043875`
- Integration SHA: `54ebb9d7611fa89eead490a814e00b80bc8c22a8` (PR #822)
- Priority: P1 — generated ownership enumeration must not collapse distinct logical owner pairs
- Task Key: `CORE-GENERATED-LOGICAL-OWNER-KEY-COLLISION`

## Confirmed defect

`GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(...)` de-duplicated `(handle, logical slot)` pairs using `handle + "\n" + slot`. Generated handle text is split on `;` but may contain embedded newlines, while dynamic owner property keys are accepted when they start with `Generated` and end with `Handle` / `Handles`, so those keys may also contain embedded newlines.

Two distinct valid pairs could therefore produce the same token and one pair was silently dropped. Concrete collision: `(handle="A", slot="GeneratedX\nGeneratedYHandle")` and `(handle="A\nGeneratedX", slot="GeneratedYHandle")` both stringify to `A\nGeneratedX\nGeneratedYHandle`.

## Completed repair

- Logical owner de-duplication now uses structural `KeyValuePair(handle, canonicalSlot)` identity with an ordinal-ignore-case comparer.
- Case-insensitive handle/slot identity is preserved.
- Host-solid alias canonicalization remains intact.
- Existing owner-handle splitting/enumeration semantics remain unchanged.
- Focused Core smoke preserves both newline-collision pairs and verifies host-solid aliases still de-duplicate to one logical pair.

## Readback / validation boundary

PR #822 was inspected as exactly three changed files before guarded squash merge. Integration SHA: `54ebb9d7611fa89eead490a814e00b80bc8c22a8`. Source and focused smoke were read back from `main` after integration.

No GitHub Actions/full .NET build/executable smoke/BricsCAD V25/V26 runtime PASS was claimed or executed.
