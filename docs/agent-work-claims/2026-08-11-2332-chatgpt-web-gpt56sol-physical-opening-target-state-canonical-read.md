# Work claim — physical opening target-state canonical read

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-physical-opening-target-state-canonical-read`
- Registered: `2026-08-11T23:32:00+07:00`
- Baseline main SHA: `449187efd9e2e45d04e764d374d2afaa9baa3041`
- Priority: evidence-driven persisted ownership integrity found during owner-requested continue-all audit

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Write(...)` emits a canonical semicolon-separated Base64 representation of trimmed opening IDs, while `TryRead(...)` currently trims serialized tokens and trims decoded IDs. As a result, tampered/non-canonical persisted state such as padded Base64 tokens or a Base64 payload containing a padded semantic ID is silently repaired during read. This conflicts with the repository's current persistence rule that padded persisted identifiers/keys fail closed instead of being normalized on load.

## Reserved scope

Harden only the persisted `TryRead(...)` path so stored physical-opening target state must already be in the exact canonical form produced by `Write(...)`. Keep authoring-time `Normalize(...)`/`Write(...)` behavior unchanged: caller IDs may still be trimmed and sorted before canonical persistence.

## Expected surfaces

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateCanonicalReadSmoke.cs`
- module-initializer registration in the same new smoke file or a dedicated new registration file
- this claim file

## Excluded scope

- No native BricsCAD physical-cut execution, transaction, Boolean, rollback, Undo or save/reopen behavior.
- No OpeningHostMatcher/host-search enumeration work.
- No change to `Normalize(...)` trimming/sorting semantics for fresh API input.
- No project persistence/QSDB implementation changes.
- No GitHub Actions dispatch and no V25 runtime qualification.

## Validation plan

- `Write` then `TryRead` roundtrip remains valid.
- Padded serialized Base64 token fails closed.
- Decoded semantic ID containing leading/trailing whitespace fails closed rather than being repaired.
- Non-canonical Base64 text that decodes but differs from canonical encoder output fails closed when representable.
- Existing duplicate/empty/overlong/strict-UTF8 checks remain intact.
- Focused smoke auto-registers without touching shared `SmokeTestRegistration.cs`.
- Re-fetch exact target blobs before writes and read back current `main` after integration; never force-push.

## Coordination

Recent concurrent opening-host work reserves bounded host/source enumeration, not this persisted target-state codec. Other active lanes observed around UI curtain project identity, health severity, host-link audit revision, template category definedness and release/update surfaces are separate.

## Completion condition

Current `main` accepts canonical writer output but rejects non-canonical/tampered persisted physical-opening target-state without normalization, focused deterministic Core regression coverage is present, and this claim is closed as `COMPLETED` with exact commits and actual validation scope.
