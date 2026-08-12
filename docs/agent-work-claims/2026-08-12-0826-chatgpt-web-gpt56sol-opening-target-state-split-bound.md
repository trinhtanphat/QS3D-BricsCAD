# Work claim — Physical opening target-state bounded split

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-opening-target-state-split-bound-20260812-0826`
- Registered: `2026-08-12T08:26:00+07:00`
- Completed: `2026-08-12T08:28:00+07:00`
- Baseline main SHA: `0fd7642ea1e24f7f83a7fbdd114eb8f693c4b8f4`
- Claim commit: `5b13c15aafc6c769d9025ec52da20f113c5e5f30`
- Source commit: `27b0901a6b860cb0105b68fb72295b97e61ff5d0`
- Regression commit: `ee29139ab9aa9b3f357c905b1e8cc27dc96b64c7`
- Priority: evidence-driven persisted-input resource bound during owner-requested `continue all`

## Confirmed defect fixed

`PhysicalOpeningCutTargetStateCodec.TryRead(...)` limits persisted state to 4096 opening ids, but previously called unbounded `raw.Split(';', StringSplitOptions.None)` before checking `tokens.Length > MaxOpeningIds`. A delimiter-dense payload inside the existing 4 MiB serialized-length limit could therefore allocate a token array far beyond the supported 4096-id contract before failing.

## Completed change

Tokenization now uses the count-bounded overload `raw.Split(new[] { ';' }, MaxOpeningIds + 1, StringSplitOptions.None)`. At most 4097 tokens are materialized, after which the existing `tokens.Length > MaxOpeningIds` path rejects overflow. The 4 MiB serialized-length limit, Base64/UTF-8 canonicality, decoded/encoded id length limits, uniqueness, canonical ordering, Write/Normalize behavior, host ownership and opening resolution semantics are unchanged.

## Regression evidence

`PhysicalOpeningCutTargetStateSplitBoundSmoke` proves a 4096-id target state still round-trips, a 4097-token persisted state fails with the existing too-many-targets contract, and an ordinary two-id state remains unchanged.

## Read-back validation

Current `main` source was re-fetched after publication and contains the bounded Split overload before the existing cardinality check. The focused smoke was also re-fetched from `main` with the intended max/overflow/ordinary cases intact.

## Coordination respected

The completed physical-opening host-reference canonicality behavior was not changed. This lane did not edit host relation validation, physical boolean mutation, cut freshness or CAD/native behavior.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
