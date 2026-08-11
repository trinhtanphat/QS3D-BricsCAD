# Work claim — Quantity Settings pre-deserialization file-size bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-file-size-20260811-2357`
- Registered: `2026-08-11T23:57:00+07:00`
- Baseline main SHA observed: `4e15f8f1b7664aa31779a48f70af61b93665fba6`
- Priority: P1 — close the remaining pre-deserialization memory-amplification gap on machine/imported Quantity Settings JSON.

## Confirmed defect

`QuantityCalculationSettings` now rejects oversized rule collections during Clone/validation, but `QuantitySettingsStore.ReadAndValidate(path)` invokes `DataContractJsonSerializer.ReadObject(stream)` before those Core cardinality checks. An oversized JSON file can therefore force deserialization/allocation of a very large raw object graph before the validated collection limits get a chance to fail closed.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs`
- `scripts/preflight-quantity-settings-recovery.py`
- `scripts/preflight-quantity-settings-file-size.py` (new)
- this claim file for close-out

## Contract

- Add one explicit generous maximum Quantity Settings JSON byte size, currently 32 MiB. This is intentionally above a worst-case serialized payload at the already-valid 65,536 directed-rule ceiling, so the store must not reject a settings object merely because its valid maximum matrix serializes larger than an 8 MiB convenience estimate.
- Check the exact opened file stream length before constructing/using `DataContractJsonSerializer.ReadObject(...)`.
- Oversized primary settings are ordinary invalid data: existing `Load()` backup fallback remains available; a valid `.bak` can recover.
- Oversized imported templates fail without mutating machine settings.
- `CanRotatePrimaryIntoBackup()` must classify an oversized primary as non-rotatable so a last-known-good backup is not overwritten by invalid content.
- Future-schema behavior remains separately fail-closed and must not be hidden by the new size guard.
- Do not read the full file into memory just to measure it; use the opened file stream length.

## Excluded scope

- No Quantity Settings WPF/Core rule/deduction/matrix changes, no project persistence, CAD geometry, Ribbon/Start Center, updater/release or GitHub Actions.
- No change to path-rich detailed store exceptions; sanitized health command surfaces already redact them.

## Validation plan

- Extend recovery preflight to pin `File.Open -> stream.Length guard -> serializer/ReadObject -> future-schema check -> NormalizeAndValidate` ordering.
- Add focused size preflight requiring the explicit 32 MiB ceiling, early stream-length refusal, backup fallback compatibility and no `ReadAllBytes`/`ReadToEnd`/MemoryStream pre-read.
- Re-fetch current `main` before implementation/merge and preserve concurrent winners without force push.
- Source/static review only; no GitHub Actions dispatch and no native V25 runtime PASS claim.

## Coordination

Earlier Quantity Settings recovery/LKG/future-schema claims are completed. Recent active project-save/persistence, grid, takeoff and ownership claims do not own `QuantitySettingsStore.cs`. This lane is narrow and does not touch local-only native CAD semantics.

## Completion condition

Oversized Quantity Settings files fail before JSON object-graph deserialization, existing backup/future-schema contracts remain intact, focused regression source is merged to `main`, and this claim is marked `COMPLETED` with exact merge evidence.
