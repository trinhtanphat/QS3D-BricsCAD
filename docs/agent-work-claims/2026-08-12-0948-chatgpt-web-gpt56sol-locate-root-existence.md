# Work claim — Locate requested-root existence integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:48:00+07:00`
- Completed: `2026-08-12T09:51:00+07:00`
- Baseline main SHA: `4fd253b56a62576f9c9f7f99fe4ccf50fd847a1e`
- Claim commit: `ad21eb922bfd11fb77586c06ab3c2514ff22d7f0`
- Source fix commit: `6055d9e02e91557584eefacef2c165c53187d2a5`
- Regression smoke commit: `e1d54d5b2911fd9815594aa66ee831834347af9a`
- Priority: P1 Core Locate integrity during owner-requested `continue all`
- Task Key: `CORE-LOCATE-ROOT-EXISTENCE-INTEGRITY`

## Confirmed defect

`SourceHandleResolver.Resolve(...)` materialized requested semantic root IDs, validated root-enumeration freshness, and built a fail-closed project element index. However, during traversal it silently `continue`d when a requested root ID was absent from that index. A stale or invalid semantic selection could therefore be converted into an empty/partial Locate result instead of surfacing that the explicitly requested semantic element no longer existed.

This differed from the same resolver's missing-dependency behavior and from other semantic planning boundaries that reject explicitly requested missing element IDs. The defect was limited to caller-requested roots; traversal-derived Room provenance semantics are not part of this lane.

## Implemented contract

- after root materialization/freshness and full project identity indexing, every nonblank requested root is preflighted against the current project before handle traversal starts;
- a missing requested root fails closed with a Locate-specific stale-selection repair message;
- mixed valid+missing root sets fail before any partial handle result is constructed;
- blank-root filtering and current trim/case-insensitive root lookup remain unchanged;
- dependency validation, Auto Room traversal, boundary/generated-owner fallback and valid direct-handle resolution remain unchanged;
- resolver remains read-only.

## Regression coverage

`SourceHandleRequestedRootExistenceSmoke` is auto-registered with a module initializer and covers:

- a single missing requested root fails closed without project persistence mutation;
- a mixed valid+missing root request fails closed without touching the valid element/project state;
- a valid padded/case-varied requested root still resolves the canonical direct source handle, preserving existing input normalization semantics.

## Validation performed

- Current-main readback confirmed `SourceHandleResolver.cs` contains the requested-root preflight with blob SHA `f1efad0b8dcff47e563187478e8ed0765c5d7b58`.
- Current-main readback confirmed `tests/QS3D.Core.SmokeTests/SourceHandleRequestedRootExistenceSmoke.cs` is present with blob SHA `63be466be47d5b2cf0d9dfe14a0bd43a9a4a5e9f` and targets current public APIs.
- No GitHub Actions were dispatched. No executable .NET smoke/full build PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this connector-only session.

## Coordination

Recent Locate claims covered root enumeration freshness/input bounds, dependency integrity/canonicality, source-handle canonicality and boundary-handle bounds. None reserved requested-root existence, and the latest boundary-handle claim was already completed before this lane. No conflicting `SourceHandleResolver.cs` edit was observed between claim and source write.

## Completion

`COMPLETED`: Locate no longer silently drops explicitly requested semantic roots that disappeared from the current project; stale/missing root selection now fails closed before traversal while valid root lookup behavior remains compatible.
