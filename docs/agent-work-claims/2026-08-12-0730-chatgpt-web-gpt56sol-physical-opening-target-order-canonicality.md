# Work Claim: Physical Opening Target Order Canonicality

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `3134625a1ea1b8bb3bde47d6a90ac2db8f526091`
- Scope: require persisted physical-opening cut target-state to use the same deterministic opening-id order emitted by `Write(...)`.

## Reserved files

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateOrderCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateOrderCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0730-chatgpt-web-gpt56sol-physical-opening-target-order-canonicality.md`

## Defect evidence

`Write(...)` canonicalizes fresh opening ids through `Normalize(...)`, which sorts them with `StringComparer.OrdinalIgnoreCase`, before serializing. `TryRead(...)` now correctly rejects padded/non-canonical Base64 and padded decoded ids, but after parsing it always sorts the ids before returning them without requiring the persisted token sequence to have already been in writer order. A tampered persisted state containing the same canonical ids in reversed order is therefore accepted and silently normalized in memory even though it is not the exact representation produced by `Write(...)`.

The earlier canonical target-state read claim is `COMPLETED`; its stated persisted contract was exact writer form, while its focused regression covered token/Base64/id canonicality and left sequence-order acceptance unchanged. No current active claim was found for this codec/order contract.

## Boundaries

- Core persisted-state codec only; no BricsCAD/native Boolean/host matching/UI changes.
- Preserve authoring-time `Normalize(...)` trimming/sorting and `Write(...)` behavior.
- Preserve existing count/length/strict-UTF8/Base64/id/duplicate checks and `Resolve(...)` semantics.
- No GitHub Actions dispatch.

## Validation plan

- Keep writer roundtrip valid for two or more opening ids supplied in non-canonical caller order.
- Reject persisted state whose otherwise-valid Base64 tokens are swapped out of canonical opening-id order.
- Keep a canonical persisted sequence accepted and returned unchanged.
- Add isolated smoke coverage plus module-initializer registration without editing shared smoke registries.
- Review exact PR diff and re-read current `main` source before merge.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke PASS unless actually available.
