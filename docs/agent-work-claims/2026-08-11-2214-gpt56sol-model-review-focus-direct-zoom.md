# Work claim — Model Review focus exact zoom dispatch

- Status: `COMPLETED`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T22:14:00+07:00`
- Completed: `2026-08-11T22:24:00+07:00`
- Baseline main SHA: `b1b22130e2715dd3639e2e18073144f17dfe8dc9`
- Merged PR: `#500`
- Main implementation SHA: `25c516d863cfb447c24f830c8f3b845b558e77d6`
- Replaced PR: `#497` (closed unmerged after a source-safe object-level rebase)
- Priority: continue source-safe audit after exact Opening Auto Host; remove an unnecessary asynchronous QS3D command re-entry from Model Review Focus.

## Reserved scope

Harden `QS3DFOCUS` so the already-resolved source `Document` and implied/prompted selection are zoomed in the same command execution instead of queueing `QS3DZOOMSELECTED` through `Document.SendStringToExecute`.

## Completed implementation

- `QS3DFOCUS` now calls `ViewportCommands.TryZoomSelection(document)` directly on the already-resolved source `Document`.
- The existing canonical zoom routine changed only from `private` to `internal`; its WCS-to-DCS framing, extents, camera direction, and viewport update behavior were not duplicated or refactored.
- `QS3DZOOMSELECTED` continues to use the same `TryZoomSelection(doc)` routine, so Focus and the standalone command share one exact implementation.
- Focus now reports a scoped failure if a highlighted selection cannot produce valid zoom extents rather than queueing a later command turn.
- `QS3DISOLATE` / `QS3DUNISOLATE` were intentionally left unchanged because they dispatch native BricsCAD behavior and were outside this claim.

## Regression gate

Added auto-discovered `scripts/preflight-model-review-focus-exact-zoom.py`.

The gate requires:
- direct `ViewportCommands.TryZoomSelection(document)` use from `QS3DFOCUS`;
- no `SendStringToExecute("QS3DZOOMSELECTED ...")` re-entry from Model Review;
- the canonical helper to remain document-bound and free of `MdiActiveDocument`, `Active()`, or `SendStringToExecute` ambient resolution;
- `QS3DZOOMSELECTED` to keep using the same helper.

`scripts/preflight-all.py` discovers this gate automatically, so no central registration file was modified.

## Coordination / merge safety

- The initial branch was based on a rapidly moving `main`; concurrent changes were compared before merge and did not overlap this lane.
- Because the connector safety guard blocked force-updating the original branch ref, PR `#497` was closed unmerged.
- A replacement branch was rebuilt object-level on the then-current `main` tree, overlaying only the three intended blobs; PR `#500` was then squash-merged with exact expected head SHA.
- Production diff was limited to one changed line in `ModelReviewCommands.cs` and one accessibility line in `ViewportCommands.cs`, plus the new preflight gate.
- No GitHub Actions were dispatched; the merge head reported no combined status checks.

## Runtime qualification

BricsCAD V25 native interaction/runtime evidence remains `LOCAL_ONLY`. This source-side completion does **not** claim `LOCAL_PASS` or a native BricsCAD runtime qualification.
