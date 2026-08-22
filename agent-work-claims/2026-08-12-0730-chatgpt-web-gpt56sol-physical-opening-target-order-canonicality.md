# Work Claim: Physical Opening Target Order Canonicality

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `3134625a1ea1b8bb3bde47d6a90ac2db8f526091`
- Scope: require persisted physical-opening cut target-state to use the same deterministic opening-id order emitted by `Write(...)`.

## Reserved files

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateOrderCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateOrderCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0730-chatgpt-web-gpt56sol-physical-opening-target-order-canonicality.md`

## Completed work

- `TryRead(...)` now computes the same deterministic `StringComparer.OrdinalIgnoreCase` ordering used by authoring-time `Normalize(...)`/`Write(...)` and requires the persisted parsed sequence to already match that canonical order.
- Otherwise-valid target tokens stored in reversed/non-canonical order now fail closed instead of being silently sorted during read.
- Canonical writer output remains accepted unchanged and returned in its persisted order.
- Existing strict token/Base64/UTF-8/id validation, duplicate rejection, count/length bounds, `Resolve(...)`, and fresh authoring-time normalization remain unchanged.
- Added isolated Core smoke coverage plus module-initializer registration without editing shared smoke registries.

## Published commits / integration

- Claim-first commit: `5b048eed0ad469f5cd91953e1de9b94aee9b2c97`.
- Initial branch implementation commits: `db40f19857800d3af377acc76497b205a5e12aee`, `4130c17c7831044c6f410232fb741ffe7c158cda`, `a719e8c6d9ae3e794986564ca0b1d27976aefe28`, with smoke cleanup `f00915149a5f7173798761c894bf9bf35c171b75`.
- PR #619 was closed unmerged after synchronization against a rapidly moving `main` made its ancestry include unrelated concurrent files; it was deliberately not used for publication.
- Clean source integration directly on current `main`: `f1764a3329a8f70eaf973b8566c02a81eb12b9d3`.
- Focused smoke on `main`: `d4aa2c250c8afb46e8b5c971c53479364487611f`.
- Smoke registration on `main`: `c601481b8c484f3fe696a85dd163c4d18d72d2e9`.

## Validation notes

- Re-read current `main` source after direct integration and confirmed the canonical-order guard is present.
- Re-read the focused smoke from current `main`; it proves writer canonicalization for two ids and rejects the same valid tokens after their persisted order is swapped.
- The source write used the freshly re-fetched current blob SHA after an earlier concurrency conflict; no force-push was used.
- GitHub Actions were not dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test PASS.

## Coordination note

A temporary synchronization note was accidentally committed and immediately removed while handling the fast-moving branch ancestry; no temporary file remains in the repository tree. It did not modify product source or tests.

## Blocked dependencies

None.
