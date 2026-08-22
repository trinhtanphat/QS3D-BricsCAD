# Work claim — Tie Rebar cover/mode health integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-tie-rebar-cover-mode-health`
- Registered: `2026-08-12T12:07:00+07:00`
- Completed: `2026-08-12T12:11:00+07:00`
- Baseline main SHA: `e6f56ebe33a331ff4abaa1588566551752432296`
- Priority: P1 — writer-owned generated Tie Rebar cover/mode metadata must not bypass health validation.
- Task Key: `CORE-TIE-REBAR-COVER-MODE-HEALTH`

## Confirmed defect

`ColumnTieSolidBuilder.CommitSemanticUpdate(...)` always persists `GeneratedTieRebarCoverM` using `double.ToString("R", CultureInfo.InvariantCulture)` and `GeneratedTieRebarMode` as the exact literal `ColumnRectangularTies`. `ColumnTieLayoutPlanner` requires cover to be finite and nonnegative. `GeneratedTieRebarHealthService` validated handles, count, diameter and actual spacing but did not read either cover or mode.

Consequently malformed generated metadata such as non-finite/negative cover or unsupported mode could pass Tie Rebar health without field-specific evidence. Writer-valid aliases such as `0.050` or padded/case-varied mode text were also indistinguishable from writer-owned serialization.

## Completed implementation

- Claim commit: `b1b942bdbbfe44a9746f2bb0fd37381fc5ca18a9`.
- Source commit: `ca147afd4031705428691ff7596b214d135742ed`.
- Smoke commit: `2ba3691af45b59f357f239cdea29918071ba723b`.
- PR #863 squash merge: `9e6a5cc371b93a8df6a683775d7b9b59359421f0`.
- Merged source blob read back from `main`: `f140e98e51a1e06859226f0414740a5431f348ab`.
- Merged smoke blob read back from `main`: `0bdceab3ec4af09c518f49f4475e33ba901cfa34`.
- `main` readback immediately after merge was `9e6a5cc371b93a8df6a683775d7b9b59359421f0`, so the merge is the current verified ancestor/root of the snapshot.

## Final contract

- Generated cover must be present, finite and >= 0 or emits `TIE_REBAR_GENERATED_COVER_INVALID` as Warning.
- After cover validity, raw text must equal round-trip invariant spelling or emits `TIE_REBAR_GENERATED_COVER_NON_CANONICAL` as Error.
- Generated mode must be present and normalize to `ColumnRectangularTies`; missing/unsupported text emits `TIE_REBAR_GENERATED_MODE_INVALID` as Warning.
- A recognized case/outer-whitespace alias emits `TIE_REBAR_GENERATED_MODE_NON_CANONICAL` as Error instead of invalid.
- Existing handle/count/diameter/spacing/category/stale behavior remains unchanged.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
