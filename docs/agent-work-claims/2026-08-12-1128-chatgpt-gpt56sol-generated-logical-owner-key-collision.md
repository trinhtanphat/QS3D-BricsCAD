# Work claim — Generated logical owner collision-free pair identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-generated-logical-owner-key-collision-20260812`
- Registered: `2026-08-12T11:28:00+07:00`
- Baseline main SHA: `df5f2fba7abfe20a5800d971cdd73ff125043875`
- Priority: P1 — generated ownership enumeration must not collapse distinct logical owner pairs
- Task Key: `CORE-GENERATED-LOGICAL-OWNER-KEY-COLLISION`

## Confirmed defect

`GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(...)` de-duplicates `(handle, logical slot)` pairs using `handle + "\n" + slot`. Generated handle text is split on `;` but may contain embedded newlines, while dynamic owner property keys are accepted when they start with `Generated` and end with `Handle` / `Handles`, so those keys may also contain embedded newlines.

Two distinct valid pairs can therefore produce the same token and one pair is silently dropped. Concrete collision: `(handle="A", slot="GeneratedX\nGeneratedYHandle")` and `(handle="A\nGeneratedX", slot="GeneratedYHandle")` both stringify to `A\nGeneratedX\nGeneratedYHandle`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs`
- one focused Core smoke + smoke registration if required
- this claim file

## Intended repair

- De-duplicate logical owner pairs structurally, not through delimiter-concatenated text identity.
- Preserve case-insensitive handle/slot identity and host-solid alias canonicalization.
- Preserve deterministic enumeration order and existing owner-handle splitting semantics.
- Add focused regression for the concrete newline collision and same-pair de-duplication non-regression.

## Validation boundary

Deterministic source/smoke diff and GitHub readback only. No GitHub Actions/full .NET build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
