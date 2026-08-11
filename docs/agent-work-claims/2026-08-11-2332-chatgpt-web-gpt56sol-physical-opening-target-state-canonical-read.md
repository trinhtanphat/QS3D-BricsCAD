# Work claim — physical opening target-state canonical read

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-physical-opening-target-state-canonical-read`
- Registered: `2026-08-11T23:32:00+07:00`
- Baseline main SHA: `449187efd9e2e45d04e764d374d2afaa9baa3041`
- Priority: evidence-driven persisted ownership integrity found during owner-requested continue-all audit

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.Write(...)` emits a canonical semicolon-separated Base64 representation of trimmed opening IDs, while `TryRead(...)` previously trimmed serialized tokens and trimmed decoded IDs. As a result, tampered/non-canonical persisted state such as padded Base64 tokens or a Base64 payload containing a padded semantic ID was silently repaired during read. This conflicted with the repository's current persistence rule that padded persisted identifiers/keys fail closed instead of being normalized on load.

## Reserved scope

Harden only the persisted `TryRead(...)` path so stored physical-opening target state must already be in the exact canonical form produced by `Write(...)`. Keep authoring-time `Normalize(...)`/`Write(...)` behavior unchanged: caller IDs may still be trimmed and sorted before canonical persistence.

## Expected surfaces

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateCanonicalReadSmoke.cs`
- this claim file

## Excluded scope

- No native BricsCAD physical-cut execution, transaction, Boolean, rollback, Undo or save/reopen behavior.
- No OpeningHostMatcher/host-search enumeration work.
- No change to `Normalize(...)` trimming/sorting semantics for fresh API input.
- No project persistence/QSDB implementation changes.
- No GitHub Actions dispatch and no V25 runtime qualification.

## Delivered behavior

- Persisted encoded tokens must no longer contain leading/trailing whitespace.
- Decoded bytes must re-encode to the exact Base64 text stored, rejecting whitespace-tolerant/non-canonical Base64 forms that `Convert.FromBase64String` could otherwise accept.
- Decoded semantic opening IDs must already be trimmed; padded IDs fail closed instead of being repaired.
- Existing strict UTF-8, duplicate, empty, overlong, bounded-count and deterministic ordering behavior remains in place.
- `Normalize(...)` and `Write(...)` continue canonicalizing fresh caller input as before.

## Commits

- Registration: `a4d38ba4c68d0f551d12a057599c1ef3a1c2b50f` — `chore(agent): claim physical opening canonical target-state read`.
- Implementation: `8c0e944c563b390b258057dec3a9a8abf67e7aec` — `fix(opening): require canonical persisted cut targets`.
- Regression: `e302ecb1e5bcbb14d598d895a51e5e077fb396db` — `test(opening): guard canonical persisted cut targets`.

## Validation actually performed

- Re-fetched `PhysicalOpeningCutTargetStateCodec.cs` from current remote `main` after integration and confirmed the canonical token/Base64/decoded-ID checks are present.
- Re-fetched the focused smoke from current remote `main`; it covers canonical writer roundtrip, padded encoded token, padded decoded ID and whitespace inside Base64.
- Focused smoke uses a module initializer and does not touch the shared smoke registration file.
- Writes used current blob SHA checks; no force-push was used.
- No GitHub Actions were dispatched.
- This hosted environment does not provide a local .NET SDK/compiler or licensed BricsCAD V25 runtime, so no unexecuted build/runtime PASS is claimed. This Core persisted-state hardening does not introduce a new native runtime scenario.

## Coordination

Concurrent opening-host bounded-enumeration work remained separate from this persisted target-state codec. Other concurrent UI/health/host-link/template/release lanes were not touched.

## Completion condition

Satisfied: current `main` accepts canonical writer output but rejects non-canonical/tampered persisted physical-opening target-state without normalization, focused deterministic Core regression coverage is present, and this claim is closed as `COMPLETED`.
