# Work claim — Physical opening target-state bounded split

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-opening-target-state-split-bound-20260812-0826`
- Registered: `2026-08-12T08:26:00+07:00`
- Baseline main SHA: `0fd7642ea1e24f7f83a7fbdd114eb8f693c4b8f4`
- Priority: evidence-driven persisted-input resource bound during owner-requested `continue all`

## Confirmed defect

`PhysicalOpeningCutTargetStateCodec.TryRead(...)` limits persisted state to 4096 opening ids, but currently calls unbounded `raw.Split(';', StringSplitOptions.None)` before checking `tokens.Length > MaxOpeningIds`. A delimiter-dense payload inside the existing 4 MiB serialized-length limit can therefore allocate a token array far beyond the supported 4096-id contract before failing.

## Reserved scope

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs` — bounded tokenization in `TryRead` only.
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetStateSplitBoundSmoke.cs` — focused CAD-independent boundary regression.
- this claim file.

## Contract

Bound split materialization to at most `MaxOpeningIds + 1` tokens, then preserve the existing `tokens.Length > MaxOpeningIds` fail-closed path. Preserve 4 MiB serialized-length limit, Base64/UTF-8 canonicality, 128-char decoded ids, 1024-char encoded ids, uniqueness, canonical ordering, Write/Normalize behavior, host ownership and opening resolution semantics.

## Coordination

The physical-opening host-reference canonicality claim is completed. This lane does not reopen host relation validation, physical boolean mutation, cut freshness or CAD/native behavior.

## Validation plan

Prove a maximum-size 4096-id state still round-trips, a 4097-token persisted payload fails closed as too many targets, and ordinary two-id canonical state remains unchanged. Re-fetch source before write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.
